namespace BizFirst.Ai.ProcessEngine.Domain.Execution.Context;

using System;
using System.Collections.Generic;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Memory;
using BizFirst.Ai.ProcessEngine.Domain.Execution.State;
using BizFirst.Ai.AiSession.Domain.Session.Hierarchy;

/// <summary>
/// Execution context for a process (top level).
/// Manages context shared across all threads in the process.
/// </summary>
public class ProcessExecutionContext
{
    /// <summary>ID of the process being executed.</summary>
    public int ProcessID { get; set; }

    /// <summary>ID of the execution record in database.</summary>
    public int ProcessExecutionID { get; set; }

    /// <summary>How the process was triggered.</summary>
    public eExecutionMode ExecutionMode { get; set; }

    /// <summary>When execution started.</summary>
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Input data for the process.</summary>
    public Dictionary<string, object> InputData { get; set; } = new();

    /// <summary>Data that triggered the process (if triggered by webhook, etc.).</summary>
    public Dictionary<string, object>? TriggerData { get; set; }

    /// <summary>Request session context for multi-tenant isolation.</summary>
    public RequestAiSession RequestSession { get; set; } = null!;

    /// <summary>Shared memory across all threads in this process.</summary>
    public ExecutionMemory Memory { get; set; } = null!;

    /// <summary>Contexts of threads executing within this process.</summary>
    public List<ProcessThreadExecutionContext> ThreadContexts { get; set; } = new();

    /// <summary>Current execution state.</summary>
    public eExecutionState CurrentExecutionState { get; set; } = eExecutionState.Running;
}
