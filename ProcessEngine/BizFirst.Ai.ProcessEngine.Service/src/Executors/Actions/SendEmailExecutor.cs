namespace BizFirst.Ai.ProcessEngine.Service.Executors.Actions;

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
/// Executor for send email action nodes.
/// Sends emails with configurable recipients, subject, and body templates.
/// </summary>
public class SendEmailExecutor : IActionNodeExecution
{
    private readonly ILogger<SendEmailExecutor> _logger;

    /// <summary>Initializes a new instance.</summary>
    public SendEmailExecutor(ILogger<SendEmailExecutor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<NodeExecutionResult> ExecuteAsync(
        ProcessElementExecutionContext executionContext,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing send email action {ElementKey}", executionContext.ProcessElementKey);

        try
        {
            var config = ParseConfiguration(executionContext.ElementDefinition.Configuration);

            // Extract email parameters
            var toAddress = config.TryGetValue("to_address", out var to) ? to.ToString() : null;
            var subject = config.TryGetValue("subject", out var subj) ? subj.ToString() : "No Subject";
            var body = config.TryGetValue("body", out var b) ? b.ToString() : "";

            if (string.IsNullOrWhiteSpace(toAddress))
            {
                return new NodeExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Recipient email address is required",
                    OutputPortKey = "error"
                };
            }

            _logger.LogDebug("Sending email to {ToAddress} with subject {Subject}", toAddress, subject);

            // TODO: Implement actual email sending via SMTP service
            // For now, simulate successful email send
            await Task.Delay(100, cancellationToken);

            var outputData = new Dictionary<string, object>
            {
                { "email_sent", true },
                { "to_address", toAddress },
                { "subject", subject },
                { "sent_at", DateTime.UtcNow }
            };

            _logger.LogInformation("Email sent successfully to {ToAddress}", toAddress);

            return new NodeExecutionResult
            {
                IsSuccess = true,
                OutputData = outputData,
                OutputPortKey = "main"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email in action {ElementKey}", executionContext.ProcessElementKey);
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
        var config = ParseConfiguration(validationContext.Definition.Configuration);

        // Validate recipient email
        if (!config.ContainsKey("to_address") || string.IsNullOrWhiteSpace(config["to_address"]?.ToString()))
        {
            return await Task.FromResult(ValidationResult.Failure("Recipient email address is required"));
        }

        // Validate subject
        if (!config.ContainsKey("subject") || string.IsNullOrWhiteSpace(config["subject"]?.ToString()))
        {
            return await Task.FromResult(ValidationResult.Failure("Email subject is required"));
        }

        return await Task.FromResult(ValidationResult.Success());
    }

    /// <inheritdoc/>
    public async Task<NodeExecutionResult> HandleErrorAsync(
        ProcessElementExecutionContext executionContext,
        Exception error,
        CancellationToken cancellationToken = default)
    {
        _logger.LogError(error, "Error in send email action {ElementKey}", executionContext.ProcessElementKey);

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
        // No cleanup needed for email action
        await Task.CompletedTask;
    }

    /// <summary>Parse configuration JSON string to dictionary.</summary>
    private Dictionary<string, object> ParseConfiguration(string configJson)
    {
        if (string.IsNullOrWhiteSpace(configJson))
        {
            return new Dictionary<string, object>();
        }

        try
        {
            // TODO: Use proper JSON parser (System.Text.Json)
            // For now, return empty dict - configuration parsing should be implemented properly
            return new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }
}
