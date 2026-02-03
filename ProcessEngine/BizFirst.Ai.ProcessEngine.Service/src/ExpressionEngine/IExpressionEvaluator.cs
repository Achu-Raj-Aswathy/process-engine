namespace BizFirst.Ai.ProcessEngine.Service.ExpressionEngine;

using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Service for evaluating expressions in node parameters and conditions.
/// Supports JavaScript and special variables like $json, $node, etc.
/// </summary>
public interface IExpressionEvaluator
{
    /// <summary>
    /// Evaluate an expression with the given context.
    /// </summary>
    /// <param name="expression">The expression to evaluate.</param>
    /// <param name="executionVariables">Variables available during evaluation.</param>
    /// <returns>Result of evaluation.</returns>
    Task<object?> EvaluateAsync(string expression, Dictionary<string, object> executionVariables);

    /// <summary>
    /// Evaluate all expressions in a parameter dictionary.
    /// </summary>
    /// <param name="parameters">Dictionary of parameter values (may contain expressions).</param>
    /// <param name="executionVariables">Variables available during evaluation.</param>
    /// <returns>Dictionary with evaluated values.</returns>
    Task<Dictionary<string, object>> EvaluateParametersAsync(
        Dictionary<string, object> parameters,
        Dictionary<string, object> executionVariables);
}
