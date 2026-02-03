namespace BizFirst.Ai.ProcessEngine.Domain.Execution.Memory;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents a try-catch-finally block scope during execution.
/// Tracks which catch handlers are available and whether finally must execute.
/// </summary>
public class ExceptionContext
{
    /// <summary>The ProcessElementKey of the try block node.</summary>
    public string TryBlockKey { get; set; } = string.Empty;

    /// <summary>Time when this try block was entered.</summary>
    public DateTime EnterTime { get; set; }

    /// <summary>List of catch handlers in order (first match wins).</summary>
    public List<CatchHandler> CatchHandlers { get; set; } = new();

    /// <summary>The ProcessElementKey of the finally block (if any).</summary>
    public string? FinallyBlockKey { get; set; }

    /// <summary>Whether the finally block has already been executed for this scope.</summary>
    public bool FinallyExecuted { get; set; }

    /// <summary>Whether an exception occurred in this try block.</summary>
    public bool ExceptionOccurred { get; set; }

    /// <summary>The exception that occurred (if any).</summary>
    public Exception? Exception { get; set; }
}

/// <summary>
/// Represents a single catch handler within a try block.
/// Defines which exception types it handles.
/// </summary>
public class CatchHandler
{
    /// <summary>The ProcessElementKey of the catch block executor.</summary>
    public string CatchBlockKey { get; set; } = string.Empty;

    /// <summary>
    /// The exception type name this handler catches (e.g., "IOException", "Exception").
    /// Null means catch-all (catches any exception).
    /// </summary>
    public string? ExceptionTypeName { get; set; }
}
