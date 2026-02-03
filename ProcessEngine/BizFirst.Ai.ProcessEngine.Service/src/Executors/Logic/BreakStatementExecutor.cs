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
/// Executor for break statement.
/// Signals the loop to exit immediately.
/// </summary>
public class BreakStatementExecutor : IActionNodeExecution
{
    private readonly ILogger<BreakStatementExecutor> _logger;

    /// <summary>Initializes a new instance.</summary>
    public BreakStatementExecutor(ILogger<BreakStatementExecutor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<NodeExecutionResult> ExecuteAsync(
        ProcessElementExecutionContext executionContext,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing break statement in {ElementKey}", executionContext.ProcessElementKey);

        try
        {
            // Signal break to the execution memory
            executionContext.ParentThreadContext.Memory.SignalBreak();

            _logger.LogDebug("Break signal sent - loop will exit");

            return await Task.FromResult(new NodeExecutionResult
            {
                IsSuccess = true,
                OutputPortKey = "break"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in break statement {ElementKey}", executionContext.ProcessElementKey);
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
        // Break statement doesn't require configuration
        return await Task.FromResult(ValidationResult.Success());
    }

    /// <inheritdoc/>
    public async Task<NodeExecutionResult> HandleErrorAsync(
        ProcessElementExecutionContext executionContext,
        Exception error,
        CancellationToken cancellationToken = default)
    {
        _logger.LogError(error, "Error in break statement {ElementKey}", executionContext.ProcessElementKey);
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
        // No cleanup needed for break statement
        await Task.CompletedTask;
    }
}
