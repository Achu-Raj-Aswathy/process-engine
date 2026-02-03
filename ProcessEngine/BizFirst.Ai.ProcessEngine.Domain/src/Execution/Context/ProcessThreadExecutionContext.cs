namespace BizFirst.Ai.ProcessEngine.Domain.Execution.Context;

using System;
using System.Collections.Generic;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Memory;
using BizFirst.Ai.ProcessEngine.Domain.Execution.State;

/// <summary>
/// Execution context for a process thread (workflow).
/// Manages context for individual workflow execution within a process.
/// </summary>
public class ProcessThreadExecutionContext
{
    /// <summary>ID of the process thread being executed.</summary>
    public int ProcessThreadID { get; set; }

    /// <summary>Version of the process thread being executed.</summary>
    public int ProcessThreadVersionID { get; set; }

    /// <summary>ID of the execution record in database.</summary>
    public int ProcessThreadExecutionID { get; set; }

    /// <summary>Parent process context.</summary>
    public ProcessExecutionContext ParentProcessContext { get; set; } = null!;

    /// <summary>When execution started.</summary>
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Input data for the thread.</summary>
    public Dictionary<string, object> InputData { get; set; } = new();

    /// <summary>Output data from the thread.</summary>
    public Dictionary<string, object> OutputData { get; set; } = new();

    /// <summary>Memory scoped to this thread.</summary>
    public ExecutionMemory Memory { get; set; } = null!;

    /// <summary>Current execution state.</summary>
    public eExecutionState State { get; set; } = eExecutionState.Running;

    /// <summary>Number of nodes completed.</summary>
    public int CompletedNodeCount { get; set; }

    /// <summary>Total number of nodes in workflow.</summary>
    public int TotalNodeCount { get; set; }

    /// <summary>Contexts of elements executing within this thread.</summary>
    public List<ProcessElementExecutionContext> ElementContexts { get; set; } = new();
}
