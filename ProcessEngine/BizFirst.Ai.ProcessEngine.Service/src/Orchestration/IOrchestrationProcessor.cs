namespace BizFirst.Ai.ProcessEngine.Service.Orchestration;

using System.Threading;
using System.Threading.Tasks;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Context;
using BizFirst.Ai.Process.Domain.Entities;

/// <summary>
/// Core orchestration processor for executing workflows.
/// Manages the execution of processes and threads using stack-based execution model.
/// This is the heart of the process execution engine.
/// </summary>
public interface IOrchestrationProcessor
{
    /// <summary>
    /// Execute a complete process with all its threads.
    /// </summary>
    /// <param name="processID">ID of the process to execute.</param>
    /// <param name="executionContext">Execution context with input data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Persisted process execution record.</returns>
    Task<ProcessExecution> ExecuteProcessAsync(
        int processID,
        ProcessExecutionContext executionContext,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a single process thread (workflow).
    /// </summary>
    /// <param name="processThreadID">ID of the thread to execute.</param>
    /// <param name="processThreadVersionID">Version of the thread to execute.</param>
    /// <param name="executionContext">Execution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Persisted thread execution record.</returns>
    Task<ProcessThreadExecution> ExecuteProcessThreadAsync(
        int processThreadID,
        int processThreadVersionID,
        ProcessThreadExecutionContext executionContext,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pause a running process execution.
    /// </summary>
    /// <param name="processExecutionID">ID of execution to pause.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if pause was successful.</returns>
    Task<bool> PauseExecutionAsync(
        int processExecutionID,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resume a paused process execution.
    /// </summary>
    /// <param name="processExecutionID">ID of execution to resume.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if resume was successful.</returns>
    Task<bool> ResumeExecutionAsync(
        int processExecutionID,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel a running or paused execution.
    /// </summary>
    /// <param name="processExecutionID">ID of execution to cancel.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if cancel was successful.</returns>
    Task<bool> CancelExecutionAsync(
        int processExecutionID,
        CancellationToken cancellationToken = default);
}
