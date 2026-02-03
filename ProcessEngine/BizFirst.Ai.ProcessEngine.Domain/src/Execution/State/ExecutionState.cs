namespace BizFirst.Ai.ProcessEngine.Domain.Execution.State;

/// <summary>
/// Execution state for process and thread execution.
/// Tracks the current status of workflow execution.
/// </summary>
public enum eExecutionState
{
    /// <summary>Not started yet.</summary>
    Idle = 0,

    /// <summary>Queued waiting to start.</summary>
    Queued = 1,

    /// <summary>Currently executing.</summary>
    Running = 2,

    /// <summary>Manually paused by user.</summary>
    Paused = 3,

    /// <summary>Waiting for external input or trigger.</summary>
    Waiting = 4,

    /// <summary>Finished successfully.</summary>
    Completed = 5,

    /// <summary>Finished with warnings but completed.</summary>
    CompletedWithWarnings = 6,

    /// <summary>Failed with error.</summary>
    Failed = 7,

    /// <summary>Cancelled by user.</summary>
    Cancelled = 8,

    /// <summary>Timed out during execution.</summary>
    TimedOut = 9,
}
