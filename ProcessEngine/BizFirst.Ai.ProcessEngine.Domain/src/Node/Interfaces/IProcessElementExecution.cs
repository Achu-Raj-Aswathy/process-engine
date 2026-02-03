namespace BizFirst.Ai.ProcessEngine.Domain.Node.Interfaces;

using System;
using System.Threading;
using System.Threading.Tasks;
using BizFirst.Ai.ProcessEngine.Domain.Execution;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Context;
using BizFirst.Ai.ProcessEngine.Domain.Node.Validation;

/// <summary>
/// Base interface for all process element executors.
/// Every node type must implement this to define how it executes.
/// </summary>
public interface IProcessElementExecution
{
    /// <summary>
    /// Execute this element with the given input data.
    /// </summary>
    /// <param name="executionContext">The execution context for this element.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of execution.</returns>
    Task<NodeExecutionResult> ExecuteAsync(
        ProcessElementExecutionContext executionContext,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate element configuration before execution.
    /// </summary>
    /// <param name="validationContext">Context for validation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result.</returns>
    Task<ValidationResult> ValidateAsync(
        ProcessElementValidationContext validationContext,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Handle an error that occurred during execution.
    /// </summary>
    /// <param name="executionContext">The execution context.</param>
    /// <param name="error">The exception that occurred.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Error handling result.</returns>
    Task<NodeExecutionResult> HandleErrorAsync(
        ProcessElementExecutionContext executionContext,
        Exception error,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleanup and release resources after execution.
    /// </summary>
    /// <param name="executionContext">The execution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CleanupAsync(
        ProcessElementExecutionContext executionContext,
        CancellationToken cancellationToken = default);
}
