namespace BizFirst.Ai.ProcessEngine.Domain.Execution.Tracing;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents a trace of execution events for debugging and monitoring.
/// Stores detailed execution history with node traces, variable states, and error information.
/// </summary>
public class ExecutionTrace
{
    /// <summary>Unique identifier for this execution trace.</summary>
    public string TraceId { get; set; }

    /// <summary>ProcessExecutionID this trace belongs to.</summary>
    public int ProcessExecutionId { get; set; }

    /// <summary>ProcessThreadExecutionID this trace belongs to.</summary>
    public int ProcessThreadExecutionId { get; set; }

    /// <summary>When trace collection started.</summary>
    public DateTime StartedAt { get; set; }

    /// <summary>When trace collection ended (null if still executing).</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Overall execution status: Running, Completed, Failed, Cancelled.</summary>
    public string ExecutionStatus { get; set; } = "Running";

    /// <summary>Collection of node execution traces.</summary>
    public List<NodeExecutionTrace> NodeTraces { get; set; } = new();

    /// <summary>Collection of variable state snapshots.</summary>
    public List<VariableStateTrace> VariableTraces { get; set; } = new();

    /// <summary>Collection of error traces.</summary>
    public List<ErrorTrace> ErrorTraces { get; set; } = new();

    /// <summary>Total duration of execution (in milliseconds).</summary>
    public long? DurationMilliseconds { get; set; }

    /// <summary>Optional execution summary/notes.</summary>
    public string? Summary { get; set; }

    /// <summary>Initializes a new ExecutionTrace.</summary>
    public ExecutionTrace()
    {
        TraceId = Guid.NewGuid().ToString("N");
        StartedAt = DateTime.UtcNow;
    }

    /// <summary>Completes the trace collection.</summary>
    public void Complete(string status = "Completed")
    {
        CompletedAt = DateTime.UtcNow;
        ExecutionStatus = status;
        DurationMilliseconds = (long)(CompletedAt.Value - StartedAt).TotalMilliseconds;
    }

    /// <summary>Adds a node execution trace.</summary>
    public void AddNodeTrace(NodeExecutionTrace nodeTrace)
    {
        NodeTraces.Add(nodeTrace);
    }

    /// <summary>Adds a variable state trace.</summary>
    public void AddVariableTrace(VariableStateTrace variableTrace)
    {
        VariableTraces.Add(variableTrace);
    }

    /// <summary>Adds an error trace.</summary>
    public void AddErrorTrace(ErrorTrace errorTrace)
    {
        ErrorTraces.Add(errorTrace);
    }
}

/// <summary>
/// Trace of a single node's execution.
/// Records when a node executed, how long it took, and its result.
/// </summary>
public class NodeExecutionTrace
{
    /// <summary>Unique identifier for this node trace.</summary>
    public string TraceId { get; set; }

    /// <summary>The ProcessElementKey of the executed node.</summary>
    public string ElementKey { get; set; } = string.Empty;

    /// <summary>The node's element type (if-condition, loop, etc).</summary>
    public string? ElementType { get; set; }

    /// <summary>Execution sequence number (1st, 2nd, 3rd node executed).</summary>
    public int ExecutionSequence { get; set; }

    /// <summary>When node execution started.</summary>
    public DateTime StartedAt { get; set; }

    /// <summary>When node execution completed.</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Duration of node execution (milliseconds).</summary>
    public long DurationMilliseconds { get; set; }

    /// <summary>Execution result: Success, Failed, Skipped, Timeout, Cancelled.</summary>
    public string Result { get; set; } = "Success";

    /// <summary>Output port taken after execution (e.g., "success", "error", "timeout").</summary>
    public string? OutputPortKey { get; set; }

    /// <summary>Node output data (first 1KB for storage efficiency).</summary>
    public string? OutputDataSnapshot { get; set; }

    /// <summary>Error message if execution failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Initializes a new NodeExecutionTrace.</summary>
    public NodeExecutionTrace()
    {
        TraceId = Guid.NewGuid().ToString("N");
        StartedAt = DateTime.UtcNow;
    }

    /// <summary>Completes the node trace.</summary>
    public void Complete(string result = "Success")
    {
        CompletedAt = DateTime.UtcNow;
        Result = result;
        DurationMilliseconds = (long)(CompletedAt.Value - StartedAt).TotalMilliseconds;
    }
}

/// <summary>
/// Trace of variable state at a point in execution.
/// Records variable names, types, and values for debugging.
/// </summary>
public class VariableStateTrace
{
    /// <summary>Unique identifier for this trace.</summary>
    public string TraceId { get; set; }

    /// <summary>When this snapshot was taken.</summary>
    public DateTime CapturedAt { get; set; }

    /// <summary>The node that just executed (or null if before/after workflow).</summary>
    public string? AfterElementKey { get; set; }

    /// <summary>Variable name.</summary>
    public string VariableName { get; set; } = string.Empty;

    /// <summary>Variable value type (string, number, object, array, etc).</summary>
    public string ValueType { get; set; } = "unknown";

    /// <summary>Variable value (truncated to 500 chars for storage).</summary>
    public string? Value { get; set; }

    /// <summary>Variable scope type (Global, Thread, Node, Loop).</summary>
    public string ScopeType { get; set; } = "Global";

    /// <summary>Initializes a new VariableStateTrace.</summary>
    public VariableStateTrace()
    {
        TraceId = Guid.NewGuid().ToString("N");
        CapturedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Trace of an error that occurred during execution.
/// Records error type, message, stack trace, and recovery actions.
/// </summary>
public class ErrorTrace
{
    /// <summary>Unique identifier for this error trace.</summary>
    public string TraceId { get; set; }

    /// <summary>When the error occurred.</summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>The node where error occurred.</summary>
    public string? ElementKey { get; set; }

    /// <summary>Error type/category (Timeout, NullReference, Validation, etc).</summary>
    public string ErrorType { get; set; } = "Unknown";

    /// <summary>Error message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Full exception stack trace.</summary>
    public string? StackTrace { get; set; }

    /// <summary>Error severity: Info, Warning, Error, Critical.</summary>
    public string Severity { get; set; } = "Error";

    /// <summary>Recovery action taken (Retry, Skip, Fallback, Propagate).</summary>
    public string? RecoveryAction { get; set; }

    /// <summary>Was the error recovered from.</summary>
    public bool IsRecovered { get; set; }

    /// <summary>Initializes a new ErrorTrace.</summary>
    public ErrorTrace()
    {
        TraceId = Guid.NewGuid().ToString("N");
        OccurredAt = DateTime.UtcNow;
    }
}
