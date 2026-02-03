namespace BizFirst.Ai.ProcessEngine.Domain.Execution;

using System;
using System.Collections.Generic;

/// <summary>
/// Lightweight result from executing a single process element/node.
/// Used internally during execution orchestration.
/// This result is later persisted to ProcessElementExecution in the Process module.
/// </summary>
public class NodeExecutionResult
{
    /// <summary>Output data from the node execution.</summary>
    public Dictionary<string, object> OutputData { get; set; } = new();

    /// <summary>Output port key indicating which port the node exited from (e.g., "success", "error", "main").</summary>
    public string OutputPortKey { get; set; } = "main";

    /// <summary>Whether the node execution was successful.</summary>
    public bool IsSuccess { get; set; } = true;

    /// <summary>Error message if execution failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Exception if one occurred during execution.</summary>
    public Exception? Exception { get; set; }
}
