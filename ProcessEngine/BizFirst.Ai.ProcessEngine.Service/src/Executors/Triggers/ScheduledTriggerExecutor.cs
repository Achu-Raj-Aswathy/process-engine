namespace BizFirst.Ai.ProcessEngine.Service.Executors.Triggers;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Context;
using BizFirst.Ai.ProcessEngine.Domain.Execution;
using BizFirst.Ai.ProcessEngine.Domain.Node.Interfaces;
using BizFirst.Ai.ProcessEngine.Domain.Node.Validation;

/// <summary>
/// Executor for scheduled trigger nodes.
/// Scheduled triggers are activated at specified times using cron expressions or interval configurations.
/// </summary>
public class ScheduledTriggerExecutor : ITriggerNodeExecution
{
    private readonly ILogger<ScheduledTriggerExecutor> _logger;

    /// <summary>Initializes a new instance.</summary>
    public ScheduledTriggerExecutor(ILogger<ScheduledTriggerExecutor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<NodeExecutionResult> ExecuteAsync(
        ProcessElementExecutionContext executionContext,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing scheduled trigger {ElementKey}", executionContext.ProcessElementKey);

        try
        {
            var config = executionContext.ElementDefinition.Configuration;
            if (string.IsNullOrEmpty(config) || !ValidateScheduleConfiguration(config))
            {
                return new NodeExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Schedule configuration is invalid or missing",
                    OutputPortKey = "error"
                };
            }

            // Scheduled trigger data includes schedule metadata
            var outputData = new Dictionary<string, object>(executionContext.InputData ?? new Dictionary<string, object>())
            {
                { "trigger_time", DateTime.UtcNow },
                { "trigger_type", "scheduled" }
            };

            return await Task.FromResult(new NodeExecutionResult
            {
                IsSuccess = true,
                OutputData = outputData,
                OutputPortKey = "main"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in scheduled trigger {ElementKey}", executionContext.ProcessElementKey);
            return new NodeExecutionResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                OutputPortKey = "error"
            };
        }
    }

    /// <inheritdoc/>
    public async Task<ValidationResult> ValidateAsync(
        ProcessElementValidationContext validationContext,
        CancellationToken cancellationToken = default)
    {
        var config = validationContext.Definition.Configuration;

        if (string.IsNullOrWhiteSpace(config))
        {
            return await Task.FromResult(ValidationResult.Failure("Schedule configuration is required"));
        }

        if (!ValidateScheduleConfiguration(config))
        {
            return await Task.FromResult(ValidationResult.Failure("Schedule configuration is invalid (must be valid cron expression or interval)"));
        }

        return await Task.FromResult(ValidationResult.Success());
    }

    /// <inheritdoc/>
    public async Task<NodeExecutionResult> HandleErrorAsync(
        ProcessElementExecutionContext executionContext,
        Exception error,
        CancellationToken cancellationToken = default)
    {
        _logger.LogError(error, "Error in scheduled trigger {ElementKey}", executionContext.ProcessElementKey);

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
        // Cleanup scheduled triggers (e.g., unregister from scheduler)
        _logger.LogDebug("Cleaning up scheduled trigger {ElementKey}", executionContext.ProcessElementKey);
        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<TriggerActivationResult> ListenAsync(
        ProcessElementExecutionContext executionContext,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listening for scheduled trigger activation {ElementKey}", executionContext.ProcessElementKey);

        // Scheduled triggers are considered activated by the scheduler
        return await Task.FromResult(new TriggerActivationResult
        {
            IsActivated = true,
            TriggerData = new Dictionary<string, object>
            {
                { "trigger_time", DateTime.UtcNow },
                { "trigger_type", "scheduled" }
            }
        });
    }

    /// <summary>Validate that schedule configuration is properly formatted.</summary>
    private bool ValidateScheduleConfiguration(string config)
    {
        if (string.IsNullOrWhiteSpace(config))
            return false;

        // TODO: Implement proper cron expression validation
        // For now, accept any non-empty configuration
        return !string.IsNullOrWhiteSpace(config);
    }
}
