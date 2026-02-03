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
/// Executor for webhook trigger nodes.
/// Webhook triggers are activated by incoming HTTP POST requests.
/// </summary>
public class WebhookTriggerExecutor : ITriggerNodeExecution
{
    private readonly ILogger<WebhookTriggerExecutor> _logger;

    /// <summary>Initializes a new instance.</summary>
    public WebhookTriggerExecutor(ILogger<WebhookTriggerExecutor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<NodeExecutionResult> ExecuteAsync(
        ProcessElementExecutionContext executionContext,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing webhook trigger {ElementKey}", executionContext.ProcessElementKey);

        try
        {
            // Validate webhook payload
            if (!ValidateWebhookPayload(executionContext.InputData))
            {
                return new NodeExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid webhook payload",
                    OutputPortKey = "error"
                };
            }

            // Webhook data is passed through as-is
            return await Task.FromResult(new NodeExecutionResult
            {
                IsSuccess = true,
                OutputData = executionContext.InputData,
                OutputPortKey = "main"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in webhook trigger {ElementKey}", executionContext.ProcessElementKey);
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
        // Webhook triggers require minimal configuration
        // The webhook URL is typically auto-generated
        return await Task.FromResult(ValidationResult.Success());
    }

    /// <inheritdoc/>
    public async Task<NodeExecutionResult> HandleErrorAsync(
        ProcessElementExecutionContext executionContext,
        Exception error,
        CancellationToken cancellationToken = default)
    {
        _logger.LogError(error, "Error in webhook trigger {ElementKey}", executionContext.ProcessElementKey);

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
        // No cleanup needed for webhook trigger
        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<TriggerActivationResult> ListenAsync(
        ProcessElementExecutionContext executionContext,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listening for webhook activation {ElementKey}", executionContext.ProcessElementKey);

        // Webhook triggers are considered activated when HTTP request is received
        return await Task.FromResult(new TriggerActivationResult
        {
            IsActivated = true,
            TriggerData = executionContext.InputData ?? new Dictionary<string, object>()
        });
    }

    /// <summary>Validate webhook payload structure.</summary>
    private bool ValidateWebhookPayload(Dictionary<string, object>? payload)
    {
        // Webhook payload can be empty or contain any data
        return true;
    }
}
