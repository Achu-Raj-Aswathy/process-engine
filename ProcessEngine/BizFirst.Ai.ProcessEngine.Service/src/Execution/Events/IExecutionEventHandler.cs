namespace BizFirst.Ai.ProcessEngine.Service.Execution.Events;

using System;
using System.Threading;
using System.Threading.Tasks;
using BizFirst.Ai.ProcessEngine.Domain.Execution;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Context;
using BizFirst.Ai.Process.Domain.Entities;

/// <summary>
/// Defines lifecycle hooks for process execution events.
/// Implementations can subscribe to execution events at key points in workflow execution.
/// </summary>
public interface IExecutionEventHandler
{
    /// <summary>Called when a workflow is starting execution.</summary>
    /// <param name="context">The process thread execution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task OnWorkflowStartingAsync(ProcessThreadExecutionContext context, CancellationToken cancellationToken = default);

    /// <summary>Called just before a node begins execution.</summary>
    /// <param name="context">The process element execution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task OnNodeExecutingAsync(ProcessElementExecutionContext context, CancellationToken cancellationToken = default);

    /// <summary>Called after a node has completed execution.</summary>
    /// <param name="result">The execution result.</param>
    /// <param name="context">The process element execution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task OnNodeExecutedAsync(NodeExecutionResult result, ProcessElementExecutionContext context, CancellationToken cancellationToken = default);

    /// <summary>Called when an error occurs during execution.</summary>
    /// <param name="elementContext">The process element context where error occurred.</param>
    /// <param name="error">The exception that occurred.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task OnErrorAsync(ProcessElementExecutionContext elementContext, Exception error, CancellationToken cancellationToken = default);

    /// <summary>Called when a workflow completes execution.</summary>
    /// <param name="execution">The process thread execution record.</param>
    /// <param name="context">The process thread execution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task OnWorkflowCompletedAsync(ProcessThreadExecution execution, ProcessThreadExecutionContext context, CancellationToken cancellationToken = default);
}
