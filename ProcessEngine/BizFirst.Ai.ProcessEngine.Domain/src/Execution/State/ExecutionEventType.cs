namespace BizFirst.Ai.ProcessEngine.Domain.Execution.State;

/// <summary>
/// Type of execution event for tracing and monitoring.
/// Used to track significant events during workflow execution.
/// </summary>
public enum eExecutionEventType
{
    /// <summary>Process execution started.</summary>
    ProcessStarted = 0,

    /// <summary>Process thread started.</summary>
    ThreadStarted = 1,

    /// <summary>Process element started.</summary>
    NodeStarted = 2,

    /// <summary>Process element completed successfully.</summary>
    NodeCompleted = 3,

    /// <summary>Process element failed.</summary>
    NodeFailed = 4,

    /// <summary>Process element was skipped.</summary>
    NodeSkipped = 5,

    /// <summary>Conditional expression was evaluated.</summary>
    ConditionEvaluated = 6,

    /// <summary>Sub-workflow started.</summary>
    SubFlowStarted = 7,

    /// <summary>Sub-workflow completed.</summary>
    SubFlowCompleted = 8,

    /// <summary>Process thread completed.</summary>
    ThreadCompleted = 9,

    /// <summary>Process execution completed.</summary>
    ProcessCompleted = 10,

    /// <summary>Process was paused.</summary>
    ProcessPaused = 11,

    /// <summary>Process was resumed.</summary>
    ProcessResumed = 12,

    /// <summary>Process was cancelled.</summary>
    ProcessCancelled = 13,

    /// <summary>Error occurred during execution.</summary>
    ErrorOccurred = 14,
}
