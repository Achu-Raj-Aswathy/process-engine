namespace BizFirst.Ai.ProcessEngine.Service.Executors.Logic;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BizFirst.Ai.ProcessEngine.Domain.Execution;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Context;
using BizFirst.Ai.ProcessEngine.Domain.Node.Interfaces;
using BizFirst.Ai.ProcessEngine.Domain.Node.Validation;

/// <summary>
/// Executor for finally block.
/// Executes cleanup code that must run regardless of whether an exception occurred.
/// Handles both normal completion and exception scenarios.
/// </summary>
public class FinallyBlockExecutor : IActionNodeExecution
{
    private readonly ILogger<FinallyBlockExecutor> _logger;

    /// <summary>Initializes a new instance.</summary>
    public FinallyBlockExecutor(ILogger<FinallyBlockExecutor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<NodeExecutionResult> ExecuteAsync(
        ProcessElementExecutionContext executionContext,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing finally block: {ElementKey}", executionContext.ProcessElementKey);

        var memory = executionContext.ParentThreadContext.Memory;
        var currentContext = memory.GetCurrentExceptionContext();

        try
        {
            // Mark that finally block is executing
            if (currentContext != null)
            {
                currentContext.FinallyExecuted = true;
            }

            // Execute cleanup logic - this could include resource cleanup, logging, etc.
            // The actual cleanup logic would be defined in child nodes
            // This executor just marks that finally is active

            _logger.LogDebug("Finally block setup complete");

            // Finally block completes cleanup
            // Check if there was an unhandled exception to rethrow
            var currentException = memory.CurrentException;

            // Normal flow: continue after finally
            _logger.LogInformation("Finally block complete, continuing execution");

            return await Task.FromResult(new NodeExecutionResult
            {
                IsSuccess = true,
                OutputPortKey = "success"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in finally block {ElementKey}", executionContext.ProcessElementKey);
            return await Task.FromResult(new NodeExecutionResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                OutputPortKey = "error"
            });
        }
        finally
        {
            // Clean up exception context when finally is done
            // Exit the try-catch-finally scope
            if (currentContext != null && currentContext.FinallyExecuted)
            {
                memory.ExitTryBlock();
                memory.CurrentException = null;
                _logger.LogDebug("Exception context cleaned up");
            }
        }
    }

    /// <inheritdoc/>
    public async Task<ValidationResult> ValidateAsync(
        ProcessElementValidationContext validationContext,
        CancellationToken cancellationToken = default)
    {
        // Finally block can have optional configuration
        return await Task.FromResult(ValidationResult.Success());
    }

    /// <inheritdoc/>
    public async Task<NodeExecutionResult> HandleErrorAsync(
        ProcessElementExecutionContext executionContext,
        Exception error,
        CancellationToken cancellationToken = default)
    {
        _logger.LogError(error, "Error in finally block {ElementKey}", executionContext.ProcessElementKey);
        // Finally block error takes precedence - mark as failed
        return await Task.FromResult(new NodeExecutionResult
        {
            IsSuccess = false,
            ErrorMessage = error.Message,
            OutputPortKey = "error"
        });
    }

    /// <inheritdoc/>
    public async Task CleanupAsync(
        ProcessElementExecutionContext executionContext,
        CancellationToken cancellationToken = default)
    {
        // Final cleanup of exception context
        var memory = executionContext.ParentThreadContext.Memory;
        var context = memory.GetCurrentExceptionContext();
        if (context != null)
        {
            memory.ExitTryBlock();
            memory.CurrentException = null;
        }
        await Task.CompletedTask;
    }
}
