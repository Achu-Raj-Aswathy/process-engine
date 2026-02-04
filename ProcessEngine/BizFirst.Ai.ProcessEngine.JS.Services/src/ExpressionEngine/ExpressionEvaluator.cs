namespace BizFirst.Ai.ProcessEngine.JS.Services.ExpressionEngine;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Jint;
using BizFirst.Ai.ProcessEngine.Domain.Definition;
using BizFirst.Ai.ProcessEngine.JS.Services.Security;

/// <summary>
/// Evaluates JavaScript expressions with security isolation modes.
/// Supports both high isolation (uncertified) and low isolation (certified) modes.
/// </summary>
public class ExpressionEvaluator : IExpressionEvaluator
{
    private readonly ILogger<ExpressionEvaluator> _logger;
    private readonly INodeCertificationService _certificationService;

    /// <summary>Initializes a new instance.</summary>
    public ExpressionEvaluator(
        ILogger<ExpressionEvaluator> logger,
        INodeCertificationService certificationService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _certificationService = certificationService ?? throw new ArgumentNullException(nameof(certificationService));
    }

    public async Task<object?> EvaluateAsync(
        string expression,
        Dictionary<string, object> executionVariables,
        ProcessElementDefinition? elementDefinition = null)
    {
        if (string.IsNullOrWhiteSpace(expression))
            throw new ArgumentException("Expression cannot be null or whitespace", nameof(expression));

        if (executionVariables == null)
            throw new ArgumentNullException(nameof(executionVariables));

        try
        {
            // Determine isolation mode based on node certification
            var isolationMode = await _certificationService.GetIsolationModeAsync(elementDefinition);

            _logger.LogDebug("Evaluating expression with {IsolationMode} for element {ElementKey}",
                isolationMode, elementDefinition?.ProcessElementKey ?? "unknown");

            // Create engine with security constraints
            var engine = JintSecurityConfiguration.CreateEngine(isolationMode);

            // Set up variables in the engine
            foreach (var variable in executionVariables)
            {
                engine.SetValue(variable.Key, variable.Value);
            }

            // Evaluate the expression
            var result = engine.Evaluate(expression);
            var value = result?.ToObject();

            _logger.LogDebug("Expression evaluated successfully, result type: {ResultType}",
                value?.GetType().Name ?? "null");

            await Task.CompletedTask;
            return value;
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Expression evaluation timeout for element {ElementKey}: {Expression}",
                elementDefinition?.ProcessElementKey ?? "unknown", expression);
            throw new InvalidOperationException(
                $"Expression evaluation timeout: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating expression for element {ElementKey}: {Expression}",
                elementDefinition?.ProcessElementKey ?? "unknown", expression);
            throw new InvalidOperationException(
                $"Error evaluating expression: {ex.Message}", ex);
        }
    }

    public async Task<bool> EvaluateBooleanAsync(
        string expression,
        Dictionary<string, object> executionVariables,
        ProcessElementDefinition? elementDefinition = null)
    {
        var result = await EvaluateAsync(expression, executionVariables, elementDefinition);

        // Convert result to boolean
        return result switch
        {
            null => false,
            bool b => b,
            string s => !string.IsNullOrEmpty(s),
            int i => i != 0,
            long l => l != 0,
            double d => d != 0.0,
            decimal dec => dec != 0m,
            _ => Convert.ToBoolean(result)
        };
    }

    public async Task<ExpressionValidationResult> ValidateExpressionAsync(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return ExpressionValidationResult.Invalid("Expression cannot be null or whitespace");

        try
        {
            // Try to create an engine and parse the expression
            var engine = new Engine();
            engine.Execute(expression);
            await Task.CompletedTask;
            return ExpressionValidationResult.Valid();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Expression validation failed: {Error}", ex.Message);
            await Task.CompletedTask;
            return ExpressionValidationResult.Invalid(ex.Message);
        }
    }
}
