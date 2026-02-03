namespace BizFirst.Ai.ProcessEngine.Domain.Execution.State;

/// <summary>
/// Execution status for individual process elements (nodes).
/// Tracks the status of each node within a workflow.
/// </summary>
public enum eProcessElementExecutionStatus
{
    /// <summary>Not executed yet.</summary>
    Idle = 0,

    /// <summary>Currently executing.</summary>
    Running = 1,

    /// <summary>Executed successfully.</summary>
    Success = 2,

    /// <summary>Failed with error.</summary>
    Failed = 3,

    /// <summary>Skipped due to conditional routing.</summary>
    Skipped = 4,

    /// <summary>Waiting for external input.</summary>
    Waiting = 5,

    /// <summary>Currently retrying after failure.</summary>
    Retrying = 6,

    /// <summary>Execution timed out.</summary>
    Timeout = 7,
}
