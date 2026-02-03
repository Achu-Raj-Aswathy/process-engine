namespace BizFirst.Ai.ProcessEngine.Service.Executors.Triggers;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Context;
using BizFirst.Ai.ProcessEngine.Domain.Execution;
using BizFirst.Ai.ProcessEngine.Domain.Node.Interfaces;
using BizFirst.Ai.ProcessEngine.Domain.Node.Validation;

/// <summary>
/// Executor for manual trigger nodes.
/// Manual triggers are activated by user action through the API.
/// </summary>
public class ManualTriggerExecutor : ITriggerNodeExecution
{
    private readonly ILogger<ManualTriggerExecutor> _logger;

    /// <summary>Initializes a new instance.</summary>
    public ManualTriggerExecutor(ILogger<ManualTriggerExecutor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<NodeExecutionResult> ExecuteAsync(
        ProcessElementExecutionContext executionContext,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing manual trigger {ElementKey}", executionContext.ProcessElementKey);

        // Manual triggers simply pass through trigger data
        return await Task.FromResult(new NodeExecutionResult
        {
            IsSuccess = true,
            OutputData = executionContext.InputData,
            OutputPortKey = "main"
        });
    }

    /// <inheritdoc/>
    public async Task<ValidationResult> ValidateAsync(
        ProcessElementValidationContext validationContext,
        CancellationToken cancellationToken = default)
    {
        // Manual triggers have minimal validation
        return await Task.FromResult(ValidationResult.Success());
    }

    /// <inheritdoc/>
    public async Task<NodeExecutionResult> HandleErrorAsync(
        ProcessElementExecutionContext executionContext,
        Exception error,
        CancellationToken cancellationToken = default)
    {
        _logger.LogError(error, "Error in manual trigger {ElementKey}", executionContext.ProcessElementKey);

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
        // No cleanup needed for manual trigger
        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<TriggerActivationResult> ListenAsync(
        ProcessElementExecutionContext executionContext,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listening for manual trigger activation {ElementKey}", executionContext.ProcessElementKey);

        // Manual triggers are considered activated when execute is called
        return await Task.FromResult(new TriggerActivationResult
        {
            IsActivated = true,
            TriggerData = executionContext.InputData
        });
    }
}
