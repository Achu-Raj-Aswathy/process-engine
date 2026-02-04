namespace BizFirst.Ai.ProcessEngine.Service.Execution.Tracing;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Tracing;

/// <summary>
/// Service for managing execution traces.
/// Provides methods to create, update, and retrieve execution traces for debugging and monitoring.
/// </summary>
public interface IExecutionTracingService
{
    /// <summary>Creates a new execution trace.</summary>
    /// <param name="processExecutionId">The ProcessExecutionID.</param>
    /// <param name="processThreadExecutionId">The ProcessThreadExecutionID.</param>
    /// <returns>The created execution trace.</returns>
    ExecutionTrace CreateTrace(int processExecutionId, int processThreadExecutionId);

    /// <summary>Records a node execution trace.</summary>
    /// <param name="traceId">The execution trace ID.</param>
    /// <param name="nodeTrace">The node execution trace to record.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordNodeTraceAsync(string traceId, NodeExecutionTrace nodeTrace, CancellationToken cancellationToken = default);

    /// <summary>Records a variable state trace.</summary>
    /// <param name="traceId">The execution trace ID.</param>
    /// <param name="variableTrace">The variable state trace to record.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordVariableTraceAsync(string traceId, VariableStateTrace variableTrace, CancellationToken cancellationToken = default);

    /// <summary>Records an error trace.</summary>
    /// <param name="traceId">The execution trace ID.</param>
    /// <param name="errorTrace">The error trace to record.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordErrorTraceAsync(string traceId, ErrorTrace errorTrace, CancellationToken cancellationToken = default);

    /// <summary>Completes an execution trace.</summary>
    /// <param name="traceId">The execution trace ID.</param>
    /// <param name="status">Final execution status (Completed, Failed, Cancelled).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CompleteTraceAsync(string traceId, string status = "Completed", CancellationToken cancellationToken = default);

    /// <summary>Gets an execution trace by ID.</summary>
    /// <param name="traceId">The execution trace ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The execution trace, or null if not found.</returns>
    Task<ExecutionTrace?> GetTraceAsync(string traceId, CancellationToken cancellationToken = default);

    /// <summary>Gets traces by execution ID (limits results for performance).</summary>
    /// <param name="processExecutionId">The ProcessExecutionID.</param>
    /// <param name="limit">Maximum number of traces to return (default 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of execution traces.</returns>
    Task<List<ExecutionTrace>> GetTracesByExecutionIdAsync(int processExecutionId, int limit = 100, CancellationToken cancellationToken = default);

    /// <summary>Deletes an execution trace (for cleanup after a period of time).</summary>
    /// <param name="traceId">The execution trace ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteTraceAsync(string traceId, CancellationToken cancellationToken = default);

    /// <summary>Deletes traces older than specified days.</summary>
    /// <param name="olderThanDays">Delete traces older than this many days.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of traces deleted.</returns>
    Task<int> DeleteOldTracesAsync(int olderThanDays, CancellationToken cancellationToken = default);
}
