namespace BizFirst.Ai.ProcessEngine.Service.Executors.Logic;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Context;
using BizFirst.Ai.ProcessEngine.Domain.Execution;
using BizFirst.Ai.ProcessEngine.Domain.Node.Interfaces;
using BizFirst.Ai.ProcessEngine.Domain.Node.Validation;
using BizFirst.Ai.ProcessEngine.Service.ExpressionEngine;

/// <summary>
/// Executor for if-condition decision nodes.
/// Evaluates a condition and routes to true/false branches.
/// </summary>
public class IfConditionExecutor : IDecisionNodeExecution
{
    private readonly IExpressionEvaluator _expressionEvaluator;
    private readonly ILogger<IfConditionExecutor> _logger;

    /// <summary>Initializes a new instance.</summary>
    public IfConditionExecutor(
        IExpressionEvaluator expressionEvaluator,
        ILogger<IfConditionExecutor> logger)
    {
        _expressionEvaluator = expressionEvaluator ?? throw new ArgumentNullException(nameof(expressionEvaluator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<NodeExecutionResult> ExecuteAsync(
        ProcessElementExecutionContext executionContext,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing if-condition {ElementKey}", executionContext.ProcessElementKey);

        try
        {
            var outputPort = await EvaluateConditionAsync(executionContext, cancellationToken);

            return new NodeExecutionResult
            {
                IsSuccess = true,
                OutputData = new Dictionary<string, object>
                {
                    { "condition_result", outputPort == "true" }
                },
                OutputPortKey = outputPort
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating condition in {ElementKey}", executionContext.ProcessElementKey);

            return new NodeExecutionResult
            {
                IsSuccess = false,
                ErrorMessage = $"Condition evaluation failed: {ex.Message}",
                OutputPortKey = "error"
            };
        }
    }

    /// <inheritdoc/>
    public async Task<string> EvaluateConditionAsync(
        ProcessElementExecutionContext executionContext,
        CancellationToken cancellationToken = default)
    {
        // Parse configuration JSON to get condition expression
        var config = ParseConfiguration(executionContext.ElementDefinition.Configuration);
        if (!config.TryGetValue("condition", out var conditionExpression) || string.IsNullOrEmpty(conditionExpression.ToString()))
        {
            throw new InvalidOperationException("Condition expression not configured");
        }

        var result = await _expressionEvaluator.EvaluateAsync(
            conditionExpression.ToString(),
            executionContext.ParentThreadContext.Memory.Variables);

        var isTruthy = result is bool b ? b : bool.TryParse(result?.ToString(), out var parsed) && parsed;

        _logger.LogDebug(
            "Condition in {ElementKey} evaluated to {Result}",
            executionContext.ProcessElementKey,
            isTruthy);

        return isTruthy ? "true" : "false";
    }

    /// <inheritdoc/>
    public async Task<ValidationResult> ValidateAsync(
        ProcessElementValidationContext validationContext,
        CancellationToken cancellationToken = default)
    {
        // Validate that condition is configured
        var config = ParseConfiguration(validationContext.Definition.Configuration);
        if (!config.ContainsKey("condition") || string.IsNullOrEmpty(config["condition"]?.ToString()))
        {
            return ValidationResult.Failure("Condition expression is required");
        }

        return await Task.FromResult(ValidationResult.Success());
    }

    /// <inheritdoc/>
    public async Task<NodeExecutionResult> HandleErrorAsync(
        ProcessElementExecutionContext executionContext,
        Exception error,
        CancellationToken cancellationToken = default)
    {
        _logger.LogError(error, "Error in if-condition {ElementKey}", executionContext.ProcessElementKey);

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
        // No cleanup needed
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
            // Simple JSON parsing for configuration
            var config = new Dictionary<string, object>();
            // TODO: Use proper JSON parser (System.Text.Json or Newtonsoft.Json)
            // For now, return empty dict - configuration parsing should be implemented properly
            return config;
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }
}
