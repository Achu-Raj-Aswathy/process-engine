namespace BizFirst.Ai.ProcessEngine.Service.ErrorHandling;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BizFirst.Ai.ProcessEngine.Domain.Definition;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Context;

/// <summary>
/// Handles errors during workflow execution with retry logic
/// </summary>
public interface IExecutionErrorHandler
{
    /// <summary>Handle error with automatic retry if configured</summary>
    Task<ExecutionErrorResult> HandleErrorAsync(
        ExecutionErrorContext errorContext,
        RetryPolicy retryPolicy,
        Func<int, Task> retryAction,
        CancellationToken cancellationToken = default);

    /// <summary>Log error with context</summary>
    void LogError(ExecutionErrorContext errorContext);

    /// <summary>Check if error should stop execution entirely</summary>
    bool IsFatalError(ExecutionErrorContext errorContext);
}

/// <summary>
/// Result of error handling operation
/// </summary>
public class ExecutionErrorResult
{
    /// <summary>Whether execution should continue (either success or retry successful)</summary>
    public bool ShouldContinue { get; set; }

    /// <summary>Whether a retry was attempted</summary>
    public bool WasRetried { get; set; }

    /// <summary>Number of retry attempts made</summary>
    public int RetryAttempts { get; set; }

    /// <summary>Final exception if no recovery was possible</summary>
    public Exception? FinalException { get; set; }

    /// <summary>Error context that caused failure</summary>
    public ExecutionErrorContext ErrorContext { get; set; } = null!;

    /// <summary>Create success result</summary>
    public static ExecutionErrorResult Success(ExecutionErrorContext context) =>
        new() { ShouldContinue = true, ErrorContext = context };

    /// <summary>Create failure result that should stop execution</summary>
    public static ExecutionErrorResult Fatal(ExecutionErrorContext context, Exception? exception = null) =>
        new()
        {
            ShouldContinue = false,
            ErrorContext = context,
            FinalException = exception ?? context.Exception
        };

    /// <summary>Create result for successful retry</summary>
    public static ExecutionErrorResult SuccessAfterRetry(ExecutionErrorContext context, int attempts) =>
        new()
        {
            ShouldContinue = true,
            WasRetried = true,
            RetryAttempts = attempts,
            ErrorContext = context
        };
}

/// <summary>
/// Implementation of error handling with retry logic
/// </summary>
public class ExecutionErrorHandler : IExecutionErrorHandler
{
    private readonly ILogger<ExecutionErrorHandler> _logger;

    public ExecutionErrorHandler(ILogger<ExecutionErrorHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<ExecutionErrorResult> HandleErrorAsync(
        ExecutionErrorContext errorContext,
        RetryPolicy retryPolicy,
        Func<int, Task> retryAction,
        CancellationToken cancellationToken = default)
    {
        LogError(errorContext);

        // Check if error is fatal
        if (IsFatalError(errorContext))
        {
            _logger.LogError("Fatal error in {ElementKey}: execution cannot continue", errorContext.ProcessElementKey);
            return ExecutionErrorResult.Fatal(errorContext, errorContext.Exception);
        }

        // Check if retryable by policy
        if (!retryPolicy.IsRetryable(errorContext.Exception ?? new Exception(errorContext.ErrorMessage)))
        {
            _logger.LogWarning(
                "Error in {ElementKey} is not retryable by policy",
                errorContext.ProcessElementKey);
            return ExecutionErrorResult.Fatal(errorContext, errorContext.Exception);
        }

        // Attempt retries
        var retryCount = 0;
        for (int attempt = 0; attempt < retryPolicy.MaxRetries; attempt++)
        {
            var delay = retryPolicy.GetDelayForAttempt(attempt + 1);
            _logger.LogInformation(
                "Retrying {ElementKey} (attempt {Attempt}/{MaxRetries}) after {DelayMs}ms",
                errorContext.ProcessElementKey,
                attempt + 1,
                retryPolicy.MaxRetries,
                delay);

            // Wait before retry
            if (delay > 0)
            {
                await Task.Delay(delay, cancellationToken);
            }

            try
            {
                // Attempt retry
                await retryAction(attempt + 1);

                _logger.LogInformation(
                    "Successfully retried {ElementKey} after {Attempts} attempt(s)",
                    errorContext.ProcessElementKey,
                    attempt + 1);

                return ExecutionErrorResult.SuccessAfterRetry(errorContext, attempt + 1);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Retry cancelled for {ElementKey}", errorContext.ProcessElementKey);
                return ExecutionErrorResult.Fatal(errorContext, new OperationCanceledException());
            }
            catch (Exception retryException)
            {
                _logger.LogWarning(
                    "Retry {Attempt} failed for {ElementKey}: {Message}",
                    attempt + 1,
                    errorContext.ProcessElementKey,
                    retryException.Message);

                retryCount = attempt + 1;

                if (attempt >= retryPolicy.MaxRetries - 1)
                {
                    // Last attempt failed
                    _logger.LogError(
                        "All {MaxRetries} retry attempts failed for {ElementKey}",
                        retryPolicy.MaxRetries,
                        errorContext.ProcessElementKey);

                    return ExecutionErrorResult.Fatal(errorContext, retryException);
                }
            }
        }

        return ExecutionErrorResult.Fatal(errorContext, errorContext.Exception);
    }

    /// <inheritdoc/>
    public void LogError(ExecutionErrorContext errorContext)
    {
        _logger.LogError(
            errorContext.Exception,
            "Execution error - {Message}. Element: {ElementKey} (ID: {ElementID}), Type: {ElementType}, Attempt: {Attempt}/{MaxRetries}",
            errorContext.GetFormattedMessage(),
            errorContext.ProcessElementKey,
            errorContext.ProcessElementID,
            errorContext.ElementType,
            errorContext.CurrentAttempt + 1,
            errorContext.MaxRetries + 1);
    }

    /// <inheritdoc/>
    public bool IsFatalError(ExecutionErrorContext errorContext)
    {
        // Check for specific fatal exception types
        if (errorContext.Exception is null)
            return false;

        var exceptionType = errorContext.Exception.GetType();

        // Consider these exceptions fatal regardless of retry policy
        var fatalExceptionTypes = new[]
        {
            typeof(NotSupportedException),
            typeof(NotImplementedException),
            typeof(ArgumentException),
            typeof(InvalidOperationException) // This might be retryable, depends on context
        };

        return false; // Currently, we let the policy decide - no absolute fatal errors
    }
}
