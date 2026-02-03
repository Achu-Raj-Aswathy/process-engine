namespace BizFirst.Ai.ProcessEngine.Service.ExpressionEngine;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Jint;

/// <summary>
/// Implementation of IExpressionEvaluator using JavaScript engine (Jint).
/// Evaluates dynamic expressions in workflow definitions.
/// </summary>
public class ExpressionEvaluator : IExpressionEvaluator
{
    private readonly ILogger<ExpressionEvaluator> _logger;

    /// <summary>Initializes a new instance.</summary>
    public ExpressionEvaluator(ILogger<ExpressionEvaluator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<object?> EvaluateAsync(string expression, Dictionary<string, object> executionVariables)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return null;
        }

        _logger.LogDebug("Evaluating expression: {Expression}", expression);

        try
        {
            // Create JavaScript engine instance
            var engine = new Engine();

            // Set up variables in the engine
            // Support both direct object reference and JSON conversion
            foreach (var kvp in executionVariables)
            {
                try
                {
                    engine.SetValue(kvp.Key, ConvertToJintValue(kvp.Value));
                }
                catch (Exception varEx)
                {
                    _logger.LogWarning(varEx, "Could not set variable {VariableName} in expression engine", kvp.Key);
                }
            }

            // Evaluate the expression
            var result = engine.Evaluate(expression);

            _logger.LogDebug("Expression evaluation result: {Result}", result?.ToString() ?? "null");

            // Return the result (Jint will handle type conversion)
            return await Task.FromResult(result?.ToObject());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating expression: {Expression}", expression);
            throw;
        }
    }

    /// <summary>Convert .NET objects to values suitable for Jint engine.</summary>
    private object? ConvertToJintValue(object? value)
    {
        if (value == null)
            return null;

        // Primitive types can be passed directly
        if (value is string || value is bool || value is int || value is long ||
            value is float || value is double || value is decimal)
        {
            return value;
        }

        // For complex types, convert to dictionary or use JSON
        if (value is Dictionary<string, object> dict)
        {
            return dict;  // Jint can handle dictionaries
        }

        // For other objects, serialize to JSON and parse back
        // This allows access to object properties in JavaScript
        try
        {
            var json = JsonSerializer.Serialize(value);
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        }
        catch
        {
            _logger.LogWarning("Could not convert value of type {Type} to Jint value", value.GetType().Name);
            return value.ToString();
        }
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, object>> EvaluateParametersAsync(
        Dictionary<string, object> parameters,
        Dictionary<string, object> executionVariables)
    {
        var evaluatedParameters = new Dictionary<string, object>();

        foreach (var kvp in parameters)
        {
            try
            {
                var value = kvp.Value;

                // If value is a string that looks like an expression, evaluate it
                if (value is string stringValue && (stringValue.Contains("{{") || stringValue.Contains("$")))
                {
                    var evaluatedValue = await EvaluateAsync(stringValue, executionVariables);
                    evaluatedParameters[kvp.Key] = evaluatedValue ?? stringValue;
                }
                else
                {
                    evaluatedParameters[kvp.Key] = value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error evaluating parameter {Key}", kvp.Key);
                evaluatedParameters[kvp.Key] = kvp.Value;
            }
        }

        return await Task.FromResult(evaluatedParameters);
    }
}
