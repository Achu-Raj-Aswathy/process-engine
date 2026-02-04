namespace BizFirst.Ai.ProcessEngine.JS.Services.ExpressionEngine;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BizFirst.Ai.ProcessEngine.Domain.Definition;

/// <summary>
/// Service for evaluating JavaScript expressions in workflow execution.
/// Provides secure expression evaluation with isolation modes.
/// </summary>
public interface IExpressionEvaluator
{
    /// <summary>
    /// Evaluates a JavaScript expression with optional security constraints.
    /// </summary>
    /// <param name="expression">The JavaScript expression to evaluate.</param>
    /// <param name="executionVariables">Variables available to the expression.</param>
    /// <param name="elementDefinition">Optional element definition for certificate-based isolation.</param>
    /// <returns>The result of the expression evaluation.</returns>
    /// <exception cref="ArgumentNullException">If expression is null.</exception>
    /// <exception cref="InvalidOperationException">If expression evaluation fails.</exception>
    Task<object?> EvaluateAsync(
        string expression,
        Dictionary<string, object> executionVariables,
        ProcessElementDefinition? elementDefinition = null);

    /// <summary>
    /// Evaluates a boolean JavaScript expression (for conditionals).
    /// </summary>
    /// <param name="expression">The JavaScript expression to evaluate as boolean.</param>
    /// <param name="executionVariables">Variables available to the expression.</param>
    /// <param name="elementDefinition">Optional element definition for certificate-based isolation.</param>
    /// <returns>Boolean result of expression evaluation.</returns>
    Task<bool> EvaluateBooleanAsync(
        string expression,
        Dictionary<string, object> executionVariables,
        ProcessElementDefinition? elementDefinition = null);

    /// <summary>
    /// Validates that an expression is syntactically correct.
    /// </summary>
    /// <param name="expression">The expression to validate.</param>
    /// <returns>Validation result with errors if any.</returns>
    Task<ExpressionValidationResult> ValidateExpressionAsync(string expression);
}

/// <summary>
/// Result of expression validation.
/// </summary>
public class ExpressionValidationResult
{
    /// <summary>Whether expression is valid.</summary>
    public bool IsValid { get; set; }

    /// <summary>Error message if invalid.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Creates a valid result.</summary>
    public static ExpressionValidationResult Valid() => new() { IsValid = true };

    /// <summary>Creates an invalid result with error message.</summary>
    public static ExpressionValidationResult Invalid(string errorMessage) =>
        new() { IsValid = false, ErrorMessage = errorMessage };
}
