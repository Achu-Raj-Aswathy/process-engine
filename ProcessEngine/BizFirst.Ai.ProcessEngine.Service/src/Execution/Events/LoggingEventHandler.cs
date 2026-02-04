namespace BizFirst.Ai.ProcessEngine.Service.Execution.Events;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BizFirst.Ai.ProcessEngine.Domain.Execution;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Context;
using BizFirst.Ai.Process.Domain.Entities;

/// <summary>
/// Default logging implementation of IExecutionEventHandler.
/// Logs all workflow and node execution events for debugging and monitoring.
/// </summary>
public class LoggingEventHandler : IExecutionEventHandler
{
    private readonly ILogger<LoggingEventHandler> _logger;

    /// <summary>Initializes a new instance.</summary>
    public LoggingEventHandler(ILogger<LoggingEventHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public Task OnWorkflowStartingAsync(ProcessThreadExecutionContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Workflow execution starting - ProcessThreadID: {ThreadID}, Version: {VersionID}, Total Nodes: {TotalNodeCount}",
            context.ProcessThreadID,
            context.ProcessThreadVersionID,
            context.TotalNodeCount);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task OnNodeExecutingAsync(ProcessElementExecutionContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Node execution starting - ElementID: {ElementID}, ElementKey: {ElementKey}, ElementType: {ElementType}, Timeout: {Timeout}s",
            context.ProcessElementID,
            context.ProcessElementKey,
            context.ElementDefinition?.ProcessElementTypeName ?? "Unknown",
            context.ElementDefinition?.TimeoutSeconds ?? 300);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task OnNodeExecutedAsync(NodeExecutionResult result, ProcessElementExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (result.IsSuccess)
        {
            _logger.LogDebug(
                "Node execution completed successfully - ElementID: {ElementID}, ElementKey: {ElementKey}, OutputPort: {OutputPort}",
                context.ProcessElementID,
                context.ProcessElementKey,
                result.OutputPortKey ?? "default");
        }
        else
        {
            _logger.LogWarning(
                "Node execution failed - ElementID: {ElementID}, ElementKey: {ElementKey}, Error: {Error}, OutputPort: {OutputPort}",
                context.ProcessElementID,
                context.ProcessElementKey,
                result.ErrorMessage ?? "No error message",
                result.OutputPortKey ?? "error");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task OnErrorAsync(ProcessElementExecutionContext elementContext, Exception error, CancellationToken cancellationToken = default)
    {
        _logger.LogError(
            error,
            "Execution error occurred - ElementID: {ElementID}, ElementKey: {ElementKey}, ExceptionType: {ExceptionType}",
            elementContext.ProcessElementID,
            elementContext.ProcessElementKey,
            error?.GetType().Name ?? "Unknown");

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task OnWorkflowCompletedAsync(ProcessThreadExecution execution, ProcessThreadExecutionContext context, CancellationToken cancellationToken = default)
    {
        var duration = execution.Duration ?? 0;
        var durationSeconds = duration / 1000.0; // Convert milliseconds to seconds

        // ExecutionStatusID: 1=Running, 2=Paused, 3=Completed, 4=Failed, 5=Cancelled
        var isSuccess = execution.ExecutionStatusID == 3; // Completed status

        if (isSuccess)
        {
            _logger.LogInformation(
                "Workflow execution completed successfully - ProcessThreadID: {ThreadID}, Duration: {Duration}s, CompletedNodes: {NodeCount}/{TotalNodes}",
                context.ProcessThreadID,
                durationSeconds,
                context.CompletedNodeCount,
                context.TotalNodeCount);
        }
        else
        {
            var statusName = execution.ExecutionStatusID switch
            {
                1 => "Running",
                2 => "Paused",
                4 => "Failed",
                5 => "Cancelled",
                _ => "Unknown"
            };

            _logger.LogError(
                "Workflow execution completed with status - ProcessThreadID: {ThreadID}, Status: {Status}, Duration: {Duration}s, CompletedNodes: {NodeCount}/{TotalNodes}, Error: {Error}",
                context.ProcessThreadID,
                statusName,
                durationSeconds,
                context.CompletedNodeCount,
                context.TotalNodeCount,
                execution.ErrorMessage ?? "No error message");
        }

        return Task.CompletedTask;
    }
}
