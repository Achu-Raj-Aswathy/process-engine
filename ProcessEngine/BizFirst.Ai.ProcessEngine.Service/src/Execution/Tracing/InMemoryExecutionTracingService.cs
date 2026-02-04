namespace BizFirst.Ai.ProcessEngine.Service.Execution.Tracing;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Tracing;

/// <summary>
/// In-memory implementation of execution tracing service.
/// Stores traces in memory for quick access during execution.
/// For production, extend with database backend or event streaming.
/// </summary>
public class InMemoryExecutionTracingService : IExecutionTracingService
{
    private readonly ILogger<InMemoryExecutionTracingService> _logger;
    private readonly ConcurrentDictionary<string, ExecutionTrace> _traces;
    private readonly ConcurrentDictionary<int, List<string>> _executionTraceIndex;

    // Limit in-memory storage to prevent unbounded growth
    private const int MaxTracesInMemory = 1000;

    /// <summary>Initializes a new instance.</summary>
    public InMemoryExecutionTracingService(ILogger<InMemoryExecutionTracingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _traces = new ConcurrentDictionary<string, ExecutionTrace>();
        _executionTraceIndex = new ConcurrentDictionary<int, List<string>>();
    }

    public ExecutionTrace CreateTrace(int processExecutionId, int processThreadExecutionId)
    {
        var trace = new ExecutionTrace
        {
            ProcessExecutionId = processExecutionId,
            ProcessThreadExecutionId = processThreadExecutionId
        };

        _traces.TryAdd(trace.TraceId, trace);

        // Index by execution ID
        _executionTraceIndex.AddOrUpdate(
            processExecutionId,
            new List<string> { trace.TraceId },
            (key, list) =>
            {
                list.Add(trace.TraceId);
                return list;
            });

        _logger.LogDebug("Created execution trace: {TraceId}", trace.TraceId);

        return trace;
    }

    public async Task RecordNodeTraceAsync(string traceId, NodeExecutionTrace nodeTrace, CancellationToken cancellationToken = default)
    {
        if (_traces.TryGetValue(traceId, out var trace))
        {
            trace.AddNodeTrace(nodeTrace);
            _logger.LogDebug("Recorded node trace: {TraceId}, Element: {ElementKey}, Duration: {Duration}ms",
                traceId, nodeTrace.ElementKey, nodeTrace.DurationMilliseconds);
        }
        else
        {
            _logger.LogWarning("Execution trace not found: {TraceId}", traceId);
        }

        await Task.CompletedTask;
    }

    public async Task RecordVariableTraceAsync(string traceId, VariableStateTrace variableTrace, CancellationToken cancellationToken = default)
    {
        if (_traces.TryGetValue(traceId, out var trace))
        {
            trace.AddVariableTrace(variableTrace);
            _logger.LogDebug("Recorded variable trace: {TraceId}, Variable: {VariableName}, Type: {ValueType}",
                traceId, variableTrace.VariableName, variableTrace.ValueType);
        }
        else
        {
            _logger.LogWarning("Execution trace not found: {TraceId}", traceId);
        }

        await Task.CompletedTask;
    }

    public async Task RecordErrorTraceAsync(string traceId, ErrorTrace errorTrace, CancellationToken cancellationToken = default)
    {
        if (_traces.TryGetValue(traceId, out var trace))
        {
            trace.AddErrorTrace(errorTrace);
            _logger.LogWarning("Recorded error trace: {TraceId}, Element: {ElementKey}, ErrorType: {ErrorType}",
                traceId, errorTrace.ElementKey, errorTrace.ErrorType);
        }
        else
        {
            _logger.LogWarning("Execution trace not found: {TraceId}", traceId);
        }

        await Task.CompletedTask;
    }

    public async Task CompleteTraceAsync(string traceId, string status = "Completed", CancellationToken cancellationToken = default)
    {
        if (_traces.TryGetValue(traceId, out var trace))
        {
            trace.Complete(status);
            _logger.LogInformation("Completed execution trace: {TraceId}, Status: {Status}, Duration: {Duration}ms",
                traceId, status, trace.DurationMilliseconds);
        }
        else
        {
            _logger.LogWarning("Execution trace not found: {TraceId}", traceId);
        }

        await Task.CompletedTask;
    }

    public async Task<ExecutionTrace?> GetTraceAsync(string traceId, CancellationToken cancellationToken = default)
    {
        _traces.TryGetValue(traceId, out var trace);
        await Task.CompletedTask;
        return trace;
    }

    public async Task<List<ExecutionTrace>> GetTracesByExecutionIdAsync(int processExecutionId, int limit = 100, CancellationToken cancellationToken = default)
    {
        var result = new List<ExecutionTrace>();

        if (_executionTraceIndex.TryGetValue(processExecutionId, out var traceIds))
        {
            foreach (var traceId in traceIds.Take(limit))
            {
                if (_traces.TryGetValue(traceId, out var trace))
                {
                    result.Add(trace);
                }
            }
        }

        await Task.CompletedTask;
        return result;
    }

    public async Task<bool> DeleteTraceAsync(string traceId, CancellationToken cancellationToken = default)
    {
        var removed = _traces.TryRemove(traceId, out _);

        if (removed)
        {
            // Also remove from index
            foreach (var kvp in _executionTraceIndex)
            {
                kvp.Value.Remove(traceId);
            }

            _logger.LogDebug("Deleted execution trace: {TraceId}", traceId);
        }

        await Task.CompletedTask;
        return removed;
    }

    public async Task<int> DeleteOldTracesAsync(int olderThanDays, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);
        var tracesToDelete = _traces
            .Where(kvp => kvp.Value.CompletedAt.HasValue && kvp.Value.CompletedAt < cutoffDate)
            .Select(kvp => kvp.Key)
            .ToList();

        int deleted = 0;
        foreach (var traceId in tracesToDelete)
        {
            if (await DeleteTraceAsync(traceId, cancellationToken))
            {
                deleted++;
            }
        }

        _logger.LogInformation("Deleted {Count} old execution traces (older than {Days} days)", deleted, olderThanDays);

        await Task.CompletedTask;
        return deleted;
    }
}
