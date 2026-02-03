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
/// Executor for catch block.
/// Handles exceptions caught from the associated try block.
/// Provides access to exception details and decides whether to rethrow or continue.
/// </summary>
public class CatchBlockExecutor : IActionNodeExecution
{
    private readonly ILogger<CatchBlockExecutor> _logger;

    /// <summary>Initializes a new instance.</summary>
    public CatchBlockExecutor(ILogger<CatchBlockExecutor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<NodeExecutionResult> ExecuteAsync(
        ProcessElementExecutionContext executionContext,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing catch block: {ElementKey}", executionContext.ProcessElementKey);

        try
        {
            var memory = executionContext.ParentThreadContext.Memory;
            var currentException = memory.CurrentException;

            if (currentException == null)
            {
                _logger.LogWarning("Catch block executed without an active exception");
                return await Task.FromResult(new NodeExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "No exception to handle",
                    OutputPortKey = "error"
                });
            }

            _logger.LogInformation("Handling exception type {ExceptionType}: {Message}",
                currentException.GetType().Name, currentException.Message);

            // Store exception details in execution memory for access by subsequent nodes
            memory.SetVariable("__exception_type__", currentException.GetType().FullName ?? currentException.GetType().Name);
            memory.SetVariable("__exception_message__", currentException.Message);
            memory.SetVariable("__exception_stacktrace__", currentException.StackTrace ?? string.Empty);

            // Default: catch blocks continue execution (handle the exception)
            // The routing logic determines next steps based on output port
            _logger.LogInformation("Exception handled, continuing normal flow");

            return await Task.FromResult(new NodeExecutionResult
            {
                IsSuccess = true,
                OutputPortKey = "success"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in catch block {ElementKey}", executionContext.ProcessElementKey);
            return await Task.FromResult(new NodeExecutionResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                OutputPortKey = "error"
            });
        }
    }

    /// <inheritdoc/>
    public async Task<ValidationResult> ValidateAsync(
        ProcessElementValidationContext validationContext,
        CancellationToken cancellationToken = default)
    {
        // Catch block should have an action configuration
        // But it's optional (defaults to continue)
        return await Task.FromResult(ValidationResult.Success());
    }

    /// <inheritdoc/>
    public async Task<NodeExecutionResult> HandleErrorAsync(
        ProcessElementExecutionContext executionContext,
        Exception error,
        CancellationToken cancellationToken = default)
    {
        _logger.LogError(error, "Error in catch block {ElementKey}", executionContext.ProcessElementKey);
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
        // No cleanup needed; exception remains in memory for finally block access
        await Task.CompletedTask;
    }
}
