namespace BizFirst.Ai.ProcessEngine.Service.Executors.Workflow;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BizFirst.Ai.ProcessEngine.Domain.Definition;
using BizFirst.Ai.ProcessEngine.Domain.Execution;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Context;
using BizFirst.Ai.ProcessEngine.Domain.Node.Interfaces;
using BizFirst.Ai.ProcessEngine.Domain.Node.Validation;

/// <summary>
/// Executor for parallel join nodes that synchronize multiple lanes of execution.
/// Waits for all upstream parallel lanes to complete before continuing.
/// </summary>
public class ParallelJoinExecutor : IActionNodeExecution
{
    private readonly ILogger<ParallelJoinExecutor> _logger;

    /// <summary>Initializes a new instance.</summary>
    public ParallelJoinExecutor(ILogger<ParallelJoinExecutor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<NodeExecutionResult> ExecuteAsync(
        ProcessElementExecutionContext executionContext,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Parallel join node executing - ElementKey: {ElementKey}",
                executionContext.ProcessElementKey);

            // Get parallel lane data from memory
            var memory = executionContext.ParentThreadContext.Memory;

            // Check if all expected lanes have completed
            var laneStatus = memory.GetParallelLaneStatus();
            var allLanesComplete = laneStatus.TryGetValue("all_complete", out var allComplete) && (bool)allComplete;

            if (!allLanesComplete)
            {
                _logger.LogWarning(
                    "Join node {ElementKey} executing but not all parallel lanes have completed yet",
                    executionContext.ProcessElementKey);
            }

            // Mark end of parallel execution
            memory.IsParallelExecutionActive = false;

            // Collect outputs from all lanes
            var laneOutputs = new Dictionary<string, object>();
            foreach (var kvp in memory.GetParallelLaneOutputs())
            {
                laneOutputs.Add($"lane_{kvp.Key}", kvp.Value);
            }

            var output = new Dictionary<string, object>
            {
                { "join_id", executionContext.ProcessElementKey },
                { "lanes_completed", DateTime.UtcNow.ToString("O") },
                { "lane_count", laneOutputs.Count },
                { "lane_outputs", laneOutputs }
            };

            _logger.LogDebug(
                "Parallel join completed for element {ElementKey}, synchronized {LaneCount} lanes",
                executionContext.ProcessElementKey, laneOutputs.Count);

            return await Task.FromResult(new NodeExecutionResult
            {
                IsSuccess = true,
                OutputData = output,
                OutputPortKey = "success"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in parallel join node: {ElementKey}", executionContext.ProcessElementKey);
            return await Task.FromResult(new NodeExecutionResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                Exception = ex,
                OutputPortKey = "error"
            });
        }
    }

    /// <inheritdoc/>
    public async Task<ValidationResult> ValidateAsync(
        ProcessElementValidationContext validationContext,
        CancellationToken cancellationToken = default)
    {
        // Parallel join nodes are valid if properly configured
        // Connection validation happens during execution orchestration
        return await Task.FromResult(ValidationResult.Success());
    }

    /// <inheritdoc/>
    public async Task<NodeExecutionResult> HandleErrorAsync(
        ProcessElementExecutionContext executionContext,
        Exception error,
        CancellationToken cancellationToken = default)
    {
        _logger.LogError(error, "Error handling for parallel join node: {ElementKey}", executionContext.ProcessElementKey);

        return await Task.FromResult(new NodeExecutionResult
        {
            IsSuccess = false,
            ErrorMessage = $"Join failed: {error.Message}",
            Exception = error,
            OutputPortKey = "error"
        });
    }

    /// <inheritdoc/>
    public async Task CleanupAsync(ProcessElementExecutionContext executionContext, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Cleaning up parallel join node: {ElementKey}", executionContext.ProcessElementKey);
        await Task.CompletedTask;
    }
}
