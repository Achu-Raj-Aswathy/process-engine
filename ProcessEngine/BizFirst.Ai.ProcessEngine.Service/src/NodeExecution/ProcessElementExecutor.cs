namespace BizFirst.Ai.ProcessEngine.Service.NodeExecution;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BizFirst.Ai.ProcessEngine.Domain.Execution;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Context;
using BizFirst.Ai.ProcessEngine.Domain.Definition;
using BizFirst.Ai.ProcessEngine.Domain.Node.Validation;

/// <summary>
/// Implementation of IProcessElementExecutor.
/// Executes individual nodes by dispatching to appropriate executor and handling timeouts.
/// </summary>
public class ProcessElementExecutor : IProcessElementExecutor
{
    private readonly INodeExecutorFactory _executorFactory;
    private readonly ILogger<ProcessElementExecutor> _logger;

    /// <summary>Initializes a new instance.</summary>
    public ProcessElementExecutor(
        INodeExecutorFactory executorFactory,
        ILogger<ProcessElementExecutor> logger)
    {
        _executorFactory = executorFactory ?? throw new ArgumentNullException(nameof(executorFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<NodeExecutionResult> ExecuteAsync(
        ProcessElementExecutionContext elementContext,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Executing element {ElementKey} of type {ElementType}",
            elementContext.ProcessElementKey,
            elementContext.ElementDefinition.ProcessElementTypeName);

        elementContext.StartedAtUtc = DateTime.UtcNow;

        try
        {
            // Get executor for this element type
            var executor = _executorFactory.GetExecutorForNodeType(
                elementContext.ElementDefinition.ProcessElementTypeName);

            elementContext.Executor = executor;

            // Validate element configuration
            var validationContext = new ProcessElementValidationContext
            {
                Definition = elementContext.ElementDefinition,
                ExecutionContext = elementContext
            };

            var validationResult = await executor.ValidateAsync(validationContext, cancellationToken);
            if (!validationResult.IsValid)
            {
                _logger.LogError(
                    "Element {ElementKey} validation failed: {Error}",
                    elementContext.ProcessElementKey,
                    validationResult.FirstError);

                return new NodeExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = validationResult.FirstError
                };
            }

            // Execute with timeout
            var timeoutSeconds = elementContext.ElementDefinition.TimeoutSeconds > 0
                ? elementContext.ElementDefinition.TimeoutSeconds
                : 300; // Default 5 minutes

            using (var timeoutCts = new CancellationTokenSource(
                TimeSpan.FromSeconds(timeoutSeconds)))
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutCts.Token))
            {
                try
                {
                    var result = await executor.ExecuteAsync(elementContext, linkedCts.Token);

                    elementContext.StoppedAtUtc = DateTime.UtcNow;
                    elementContext.OutputData = result.OutputData as Dictionary<string, object> ?? new();

                    _logger.LogInformation(
                        "Element {ElementKey} execution completed successfully in {DurationMs}ms",
                        elementContext.ProcessElementKey,
                        (DateTime.UtcNow - elementContext.StartedAtUtc).TotalMilliseconds);

                    return result;
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    // Timeout occurred
                    _logger.LogError(
                        "Element {ElementKey} execution timed out after {TimeoutSeconds} seconds",
                        elementContext.ProcessElementKey,
                        timeoutSeconds);

                    return new NodeExecutionResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Element execution timed out after {timeoutSeconds} seconds",
                        OutputPortKey = "error"
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Element {ElementKey} execution failed with exception",
                elementContext.ProcessElementKey);

            elementContext.StoppedAtUtc = DateTime.UtcNow;
            elementContext.LastException = ex;

            // Try to handle error
            try
            {
                if (elementContext.Executor != null)
                {
                    var errorResult = await elementContext.Executor.HandleErrorAsync(
                        elementContext, ex, cancellationToken);
                    return errorResult;
                }
            }
            catch (Exception handlerEx)
            {
                _logger.LogError(handlerEx, "Error handler for element {ElementKey} also failed", elementContext.ProcessElementKey);
            }

            return new NodeExecutionResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                Exception = ex,
                OutputPortKey = "error"
            };
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ShouldExecuteElementAsync(
        ProcessElementDefinition elementDefinition,
        ProcessThreadExecutionContext threadContext)
    {
        // Don't execute disabled elements
        if (elementDefinition.IsDisabled)
        {
            return false;
        }

        // Element should execute
        return await Task.FromResult(true);
    }
}
