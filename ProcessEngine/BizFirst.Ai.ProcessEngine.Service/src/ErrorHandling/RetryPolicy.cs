namespace BizFirst.Ai.ProcessEngine.Service.ErrorHandling;

using System;
using System.Linq;
using System.Net.Http;

/// <summary>
/// Defines retry behavior for failed node executions
/// </summary>
public class RetryPolicy
{
    /// <summary>Maximum number of retry attempts</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>Initial delay in milliseconds before first retry</summary>
    public int InitialDelayMs { get; set; } = 1000;

    /// <summary>Maximum delay in milliseconds between retries</summary>
    public int MaxDelayMs { get; set; } = 30000;

    /// <summary>Exponential backoff multiplier (e.g., 2.0 = double delay each retry)</summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>Whether to retry on any exception or only specific ones</summary>
    public bool RetryOnAnyException { get; set; } = true;

    /// <summary>Exception types to retry on (if RetryOnAnyException is false)</summary>
    public Type[] RetryableExceptionTypes { get; set; } = new[] { typeof(TimeoutException), typeof(InvalidOperationException) };

    /// <summary>Calculate delay for specific retry attempt</summary>
    public int GetDelayForAttempt(int attemptNumber)
    {
        if (attemptNumber <= 0) return 0;

        var delay = (int)(InitialDelayMs * Math.Pow(BackoffMultiplier, attemptNumber - 1));
        return Math.Min(delay, MaxDelayMs);
    }

    /// <summary>Determine if exception is retryable</summary>
    public bool IsRetryable(Exception exception)
    {
        if (RetryOnAnyException)
            return true;

        return RetryableExceptionTypes.Any(t => t.IsInstanceOfType(exception));
    }

    /// <summary>Create default retry policy for action nodes</summary>
    public static RetryPolicy DefaultActionPolicy() => new()
    {
        MaxRetries = 3,
        InitialDelayMs = 1000,
        MaxDelayMs = 30000,
        BackoffMultiplier = 2.0,
        RetryOnAnyException = false,
        RetryableExceptionTypes = new[] { typeof(TimeoutException), typeof(HttpRequestException) }
    };

    /// <summary>Create default retry policy for critical nodes</summary>
    public static RetryPolicy StrictPolicy() => new()
    {
        MaxRetries = 5,
        InitialDelayMs = 500,
        MaxDelayMs = 60000,
        BackoffMultiplier = 1.5,
        RetryOnAnyException = false,
        RetryableExceptionTypes = new[] { typeof(TimeoutException) }
    };

    /// <summary>Create policy with no retries</summary>
    public static RetryPolicy NoRetryPolicy() => new()
    {
        MaxRetries = 0,
        RetryOnAnyException = false
    };
}
