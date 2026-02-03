namespace BizFirst.Ai.ProcessEngine.Domain.Execution.State;

/// <summary>
/// How the process execution was initiated.
/// Indicates the trigger or initiation method for execution.
/// </summary>
public enum eExecutionMode
{
    /// <summary>Manually triggered by user.</summary>
    Manual = 0,

    /// <summary>Triggered by webhook.</summary>
    Webhook = 1,

    /// <summary>Triggered by scheduled trigger.</summary>
    Scheduled = 2,

    /// <summary>Triggered by event/webhook event.</summary>
    Event = 3,

    /// <summary>Test execution mode.</summary>
    Test = 4,

    /// <summary>Triggered by another process (sub-process).</summary>
    SubProcess = 5,
}
