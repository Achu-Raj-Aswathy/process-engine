namespace BizFirst.Ai.ProcessEngine.Service.Executors.Logic;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Context;
using BizFirst.Ai.ProcessEngine.Domain.Execution;
using BizFirst.Ai.ProcessEngine.Domain.Node.Interfaces;
using BizFirst.Ai.ProcessEngine.Domain.Node.Validation;
using BizFirst.Ai.ProcessEngine.Service.ExpressionEngine;

/// <summary>
/// Executor for switch decision nodes.
/// Evaluates multiple conditions and routes to the matching case.
/// </summary>
public class SwitchExecutor : IDecisionNodeExecution
{
    private readonly IExpressionEvaluator _expressionEvaluator;
    private readonly ILogger<SwitchExecutor> _logger;

    /// <summary>Initializes a new instance.</summary>
    public SwitchExecutor(IExpressionEvaluator expressionEvaluator, ILogger<SwitchExecutor> logger)
    {
        _expressionEvaluator = expressionEvaluator ?? throw new ArgumentNullException(nameof(expressionEvaluator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<NodeExecutionResult> ExecuteAsync(
        ProcessElementExecutionContext executionContext,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing switch {ElementKey}", executionContext.ProcessElementKey);

        try
        {
            var outputPort = await EvaluateConditionAsync(executionContext, cancellationToken);

            return new NodeExecutionResult
            {
                IsSuccess = true,
                OutputData = new Dictionary<string, object>
                {
                    { "switch_case", outputPort }
                },
                OutputPortKey = outputPort
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating switch in {ElementKey}", executionContext.ProcessElementKey);

            return new NodeExecutionResult
            {
                IsSuccess = false,
                ErrorMessage = $"Switch evaluation failed: {ex.Message}",
                OutputPortKey = "error"
            };
        }
    }

    /// <inheritdoc/>
    public async Task<string> EvaluateConditionAsync(
        ProcessElementExecutionContext executionContext,
        CancellationToken cancellationToken = default)
    {
        var config = ParseConfiguration(executionContext.ElementDefinition.Configuration);

        // Get the expression to evaluate
        if (!config.TryGetValue("expression", out var exprObj) || string.IsNullOrEmpty(exprObj?.ToString()))
        {
            throw new InvalidOperationException("Switch expression not configured");
        }

        var expression = exprObj.ToString();

        // Evaluate the expression
        var result = await _expressionEvaluator.EvaluateAsync(
            expression,
            executionContext.ParentThreadContext.Memory.Variables);

        var switchValue = result?.ToString() ?? "";
        _logger.LogDebug("Switch expression evaluated to: {Result}", switchValue);

        // Try to find a matching case
        if (config.TryGetValue("cases", out var casesObj) && casesObj is Dictionary<string, object> cases)
        {
            if (cases.ContainsKey(switchValue))
            {
                return switchValue;
            }
        }

        // Return default case if no match found
        var defaultCase = config.TryGetValue("default", out var def) ? def.ToString() : "default";
        _logger.LogDebug("No matching case found, routing to default: {Default}", defaultCase);

        return defaultCase ?? "default";
    }

    /// <inheritdoc/>
    public async Task<ValidationResult> ValidateAsync(
        ProcessElementValidationContext validationContext,
        CancellationToken cancellationToken = default)
    {
        var config = ParseConfiguration(validationContext.Definition.Configuration);

        // Validate expression is provided
        if (!config.ContainsKey("expression") || string.IsNullOrWhiteSpace(config["expression"]?.ToString()))
        {
            return await Task.FromResult(ValidationResult.Failure("Switch expression is required"));
        }

        // Validate at least one case is defined
        if (!config.ContainsKey("cases"))
        {
            return await Task.FromResult(ValidationResult.Failure("Switch cases are required"));
        }

        return await Task.FromResult(ValidationResult.Success());
    }

    /// <inheritdoc/>
    public async Task<NodeExecutionResult> HandleErrorAsync(
        ProcessElementExecutionContext executionContext,
        Exception error,
        CancellationToken cancellationToken = default)
    {
        _logger.LogError(error, "Error in switch {ElementKey}", executionContext.ProcessElementKey);

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
