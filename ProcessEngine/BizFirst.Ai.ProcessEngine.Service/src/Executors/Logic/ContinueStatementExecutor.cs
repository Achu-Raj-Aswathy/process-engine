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
/// Executor for continue statement.
/// Signals the loop to skip to the next iteration.
/// </summary>
public class ContinueStatementExecutor : IActionNodeExecution
{
    private readonly ILogger<ContinueStatementExecutor> _logger;

    /// <summary>Initializes a new instance.</summary>
    public ContinueStatementExecutor(ILogger<ContinueStatementExecutor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<NodeExecutionResult> ExecuteAsync(
        ProcessElementExecutionContext executionContext,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing continue statement in {ElementKey}", executionContext.ProcessElementKey);

        try
        {
            // Signal continue to the execution memory
            executionContext.ParentThreadContext.Memory.SignalContinue();

            _logger.LogDebug("Continue signal sent - loop will skip to next iteration");

            return await Task.FromResult(new NodeExecutionResult
            {
                IsSuccess = true,
                OutputPortKey = "continue"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in continue statement {ElementKey}", executionContext.ProcessElementKey);
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
        // Continue statement doesn't require configuration
        return await Task.FromResult(ValidationResult.Success());
    }

    /// <inheritdoc/>
    public async Task<NodeExecutionResult> HandleErrorAsync(
        ProcessElementExecutionContext executionContext,
        Exception error,
        CancellationToken cancellationToken = default)
    {
        _logger.LogError(error, "Error in continue statement {ElementKey}", executionContext.ProcessElementKey);
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
        // No cleanup needed for continue statement
        await Task.CompletedTask;
    }
}
