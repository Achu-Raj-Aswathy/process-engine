namespace BizFirst.Ai.ProcessEngine.Domain.Node.Interfaces;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Context;

/// <summary>
/// Interface for trigger nodes (nodes that start workflows).
/// Trigger nodes listen for events and activate workflows.
/// </summary>
public interface ITriggerNodeExecution : IProcessElementExecution
{
    /// <summary>
    /// Listen for trigger activation events (webhooks, schedules, etc.).
    /// </summary>
    /// <param name="executionContext">The execution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of listening for trigger.</returns>
    Task<TriggerActivationResult> ListenAsync(
        ProcessElementExecutionContext executionContext,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of trigger activation.
/// </summary>
public class TriggerActivationResult
{
    /// <summary>Whether the trigger was activated.</summary>
    public bool IsActivated { get; set; }

    /// <summary>Data passed from the trigger.</summary>
    public Dictionary<string, object> TriggerData { get; set; } = new();

    /// <summary>Error message if activation failed.</summary>
    public string? ErrorMessage { get; set; }
}
