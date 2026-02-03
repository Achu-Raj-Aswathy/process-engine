namespace BizFirst.Ai.ProcessEngine.Domain.Execution.Context;

using System;
using System.Collections.Generic;
using BizFirst.Ai.ProcessEngine.Domain.Execution.State;
using BizFirst.Ai.ProcessEngine.Domain.Definition;
using BizFirst.Ai.ProcessEngine.Domain.Node.Interfaces;
using BizFirst.Ai.ProcessEngine.Domain.Connector;

/// <summary>
/// Execution context for a single process element (node).
/// Manages execution of an individual node within a workflow.
/// </summary>
public class ProcessElementExecutionContext
{
    /// <summary>ID of the process element.</summary>
    public int ProcessElementID { get; set; }

    /// <summary>Key of the process element for referencing.</summary>
    public string ProcessElementKey { get; set; } = string.Empty;

    /// <summary>ID of the thread execution this element belongs to.</summary>
    public int ProcessThreadExecutionID { get; set; }

    /// <summary>Parent thread context.</summary>
    public ProcessThreadExecutionContext ParentThreadContext { get; set; } = null!;

    /// <summary>Order of execution (1st, 2nd, 3rd, etc.).</summary>
    public int ExecutionOrder { get; set; }

    /// <summary>When execution started.</summary>
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>When execution stopped.</summary>
    public DateTime? StoppedAtUtc { get; set; }

    /// <summary>Duration of execution.</summary>
    public TimeSpan Duration => StoppedAtUtc.HasValue ? StoppedAtUtc.Value - StartedAtUtc : TimeSpan.Zero;

    /// <summary>Input data to this element.</summary>
    public Dictionary<string, object> InputData { get; set; } = new();

    /// <summary>Output data from this element.</summary>
    public Dictionary<string, object> OutputData { get; set; } = new();

    /// <summary>Error output if element failed.</summary>
    public object? ErrorOutput { get; set; }

    /// <summary>Definition of this element.</summary>
    public ProcessElementDefinition ElementDefinition { get; set; } = null!;

    /// <summary>Executor for this element type.</summary>
    public IProcessElementExecution? Executor { get; set; }

    /// <summary>Current execution status.</summary>
    public eProcessElementExecutionStatus Status { get; set; } = eProcessElementExecutionStatus.Idle;

    /// <summary>Error message if execution failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Last exception if one occurred.</summary>
    public Exception? LastException { get; set; }

    /// <summary>Number of retries that have occurred.</summary>
    public int RetryCount { get; set; }

    /// <summary>Connector configuration if using external service.</summary>
    public ConnectorData? ConnectorData { get; set; }

    /// <summary>ID of connector if using one.</summary>
    public int? ConnectorID { get; set; }

    /// <summary>Local memory scoped to this element.</summary>
    public Dictionary<string, object> LocalMemory { get; set; } = new();
}
