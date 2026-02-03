namespace BizFirst.Ai.ProcessEngine.Service.ErrorHandling;

using System;
using System.Collections.Generic;

/// <summary>
/// Tracks error information for a failed node execution
/// </summary>
public class ExecutionErrorContext
{
    /// <summary>ID of the element that failed</summary>
    public int ProcessElementID { get; set; }

    /// <summary>Key of the element that failed</summary>
    public string ProcessElementKey { get; set; } = string.Empty;

    /// <summary>Type of element that failed</summary>
    public string ElementType { get; set; } = string.Empty;

    /// <summary>The exception that occurred</summary>
    public Exception? Exception { get; set; }

    /// <summary>Error message</summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>Stack trace</summary>
    public string StackTrace { get; set; } = string.Empty;

    /// <summary>Current retry attempt number (0-based)</summary>
    public int CurrentAttempt { get; set; }

    /// <summary>Total retry attempts available</summary>
    public int MaxRetries { get; set; }

    /// <summary>Whether this error is retryable</summary>
    public bool IsRetryable { get; set; }

    /// <summary>When the error occurred</summary>
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    /// <summary>Additional context data</summary>
    public Dictionary<string, object> ContextData { get; set; } = new();

    /// <summary>Whether this is the final failed attempt</summary>
    public bool IsFinal => CurrentAttempt >= MaxRetries;

    /// <summary>Create error context from exception</summary>
    public static ExecutionErrorContext CreateFromException(
        int processElementID,
        string elementKey,
        string elementType,
        Exception exception,
        int currentAttempt = 0,
        int maxRetries = 3)
    {
        return new ExecutionErrorContext
        {
            ProcessElementID = processElementID,
            ProcessElementKey = elementKey,
            ElementType = elementType,
            Exception = exception,
            ErrorMessage = exception.Message,
            StackTrace = exception.StackTrace ?? string.Empty,
            CurrentAttempt = currentAttempt,
            MaxRetries = maxRetries,
            IsRetryable = currentAttempt < maxRetries
        };
    }

    /// <summary>Get formatted error message for logging</summary>
    public string GetFormattedMessage()
    {
        return $"Error in {ElementType} '{ProcessElementKey}' (ID: {ProcessElementID}): {ErrorMessage}" +
               (IsRetryable ? $" [Attempt {CurrentAttempt + 1}/{MaxRetries + 1}]" : " [Final Attempt]");
    }
}
