namespace BizFirst.Ai.ProcessEngine.Service.Executors.Workflow;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BizFirst.Ai.ProcessEngine.Domain.Execution;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Context;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Memory;
using BizFirst.Ai.ProcessEngine.Domain.Execution.State;
using BizFirst.Ai.ProcessEngine.Domain.Node.Interfaces;
using BizFirst.Ai.ProcessEngine.Domain.Node.Validation;
using BizFirst.Ai.ProcessEngine.Service.Orchestration;
using BizFirst.Ai.Process.Domain.Entities;

/// <summary>
/// Executor for sub-workflow invocation.
/// Enables recursive workflow execution with input/output mapping and depth limiting.
/// </summary>
public class SubWorkflowExecutor : IActionNodeExecution
{
    private readonly IOrchestrationProcessor _orchestrationProcessor;
    private readonly ILogger<SubWorkflowExecutor> _logger;

    // Track nesting depth to prevent infinite recursion: ProcessExecutionID -> depth
    private static readonly Dictionary<int, int> _nestingDepthTracker = new();
    private const int MaxNestingDepth = 10;

    /// <summary>Initializes a new instance.</summary>
    public SubWorkflowExecutor(
        IOrchestrationProcessor orchestrationProcessor,
        ILogger<SubWorkflowExecutor> logger)
    {
        _orchestrationProcessor = orchestrationProcessor ?? throw new ArgumentNullException(nameof(orchestrationProcessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<NodeExecutionResult> ExecuteAsync(
        ProcessElementExecutionContext executionContext,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing sub-workflow from {ElementKey}", executionContext.ProcessElementKey);

        try
        {
            // Get sub-workflow ID from configuration or input data
            var subWorkflowId = GetSubWorkflowId(executionContext);
            if (subWorkflowId <= 0)
            {
                return await Task.FromResult(new NodeExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Sub-workflow ID not provided in configuration or input data",
                    OutputPortKey = "error"
                });
            }

            var subWorkflowVersionId = GetSubWorkflowVersionId(executionContext) ?? 1;

            _logger.LogInformation(
                "Sub-workflow execution requested: ProcessThread={SubworkflowID}, Version={VersionID}",
                subWorkflowId, subWorkflowVersionId);

            // Check nesting depth
            var parentProcessExecutionId = executionContext.ParentThreadContext.ParentProcessContext?.ProcessExecutionID
                ?? executionContext.ParentThreadContext.ProcessThreadExecutionID;

            if (!_nestingDepthTracker.ContainsKey(parentProcessExecutionId))
            {
                _nestingDepthTracker[parentProcessExecutionId] = 0;
            }

            _nestingDepthTracker[parentProcessExecutionId]++;

            if (_nestingDepthTracker[parentProcessExecutionId] > MaxNestingDepth)
            {
                _logger.LogError(
                    "Sub-workflow nesting depth exceeded (max {MaxDepth})",
                    MaxNestingDepth);

                _nestingDepthTracker[parentProcessExecutionId]--;
                return await Task.FromResult(new NodeExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Sub-workflow nesting depth exceeded (max {MaxNestingDepth})",
                    OutputPortKey = "error"
                });
            }

            try
            {
                // Create child execution context
                var childContext = CreateChildContext(
                    executionContext,
                    subWorkflowId,
                    subWorkflowVersionId);

                // Execute sub-workflow
                _logger.LogDebug("Executing sub-workflow in child context");

                var subWorkflowExecution = await _orchestrationProcessor.ExecuteProcessThreadAsync(
                    subWorkflowId,
                    subWorkflowVersionId,
                    childContext,
                    cancellationToken);

                // Map output back to parent
                var result = MapResult(executionContext, subWorkflowExecution, childContext);

                _logger.LogInformation(
                    "Sub-workflow completed: Status={Status}",
                    subWorkflowExecution.ExecutionStatusID);

                return await Task.FromResult(result);
            }
            finally
            {
                _nestingDepthTracker[parentProcessExecutionId]--;
                if (_nestingDepthTracker[parentProcessExecutionId] <= 0)
                {
                    _nestingDepthTracker.Remove(parentProcessExecutionId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing sub-workflow from {ElementKey}", executionContext.ProcessElementKey);
            return await Task.FromResult(new NodeExecutionResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                OutputPortKey = "error"
            });
        }
    }

    /// <inheritdoc/>
    public async Task<ValidationResult> ValidateAsync(
        ProcessElementValidationContext validationContext,
        CancellationToken cancellationToken = default)
    {
        // Sub-workflow executor requires sub-workflow ID to be specified
        // This would be validated based on configuration structure
        return await Task.FromResult(ValidationResult.Success());
    }

    /// <inheritdoc/>
    public async Task<NodeExecutionResult> HandleErrorAsync(
        ProcessElementExecutionContext executionContext,
        Exception error,
        CancellationToken cancellationToken = default)
    {
        _logger.LogError(error, "Error in sub-workflow executor {ElementKey}", executionContext.ProcessElementKey);
        return await Task.FromResult(new NodeExecutionResult
        {
            IsSuccess = false,
            ErrorMessage = error.Message,
            OutputPortKey = "error"
        });
    }

    /// <inheritdoc/>
    public async Task CleanupAsync(
        ProcessElementExecutionContext executionContext,
        CancellationToken cancellationToken = default)
    {
        // No cleanup needed
        await Task.CompletedTask;
    }

    /// <summary>Gets sub-workflow ID from configuration.</summary>
    private int GetSubWorkflowId(ProcessElementExecutionContext executionContext)
    {
        // Try to get from input data first
        if (executionContext.InputData != null &&
            executionContext.InputData.TryGetValue("sub_workflow_id", out var idObj))
        {
            if (int.TryParse(idObj?.ToString() ?? string.Empty, out var id))
                return id;
        }

        // Try parent memory variables
        if (executionContext.ParentThreadContext.Memory.GetVariable("sub_workflow_id") is int memoryId)
            return memoryId;

        return -1;
    }

    /// <summary>Gets sub-workflow version ID from configuration.</summary>
    private int? GetSubWorkflowVersionId(ProcessElementExecutionContext executionContext)
    {
        // Try input data
        if (executionContext.InputData != null &&
            executionContext.InputData.TryGetValue("sub_workflow_version_id", out var versionObj))
        {
            if (int.TryParse(versionObj?.ToString() ?? string.Empty, out var version))
                return version;
        }

        // Try memory variables
        if (executionContext.ParentThreadContext.Memory.GetVariable("sub_workflow_version_id") is int memoryVersion)
            return memoryVersion;

        return null;
    }

    /// <summary>Creates a child execution context for the sub-workflow.</summary>
    private ProcessThreadExecutionContext CreateChildContext(
        ProcessElementExecutionContext parentElementContext,
        int subWorkflowId,
        int subWorkflowVersionId)
    {
        var parentThreadContext = parentElementContext.ParentThreadContext;

        // Map input data for child workflow
        var childInputData = MapInputData(parentElementContext);

        // Create child context
        var childContext = new ProcessThreadExecutionContext
        {
            ProcessThreadID = subWorkflowId,
            ProcessThreadVersionID = subWorkflowVersionId,
            ProcessThreadExecutionID = 0, // Will be assigned by persistence layer
            Memory = new ExecutionMemory(childInputData),
            InputData = childInputData,
            OutputData = new Dictionary<string, object>(),
            ParentProcessContext = parentThreadContext.ParentProcessContext,
            State = eExecutionState.Running,
            CompletedNodeCount = 0,
            TotalNodeCount = 0
        };

        return childContext;
    }

    /// <summary>Maps input data from parent to child workflow.</summary>
    private Dictionary<string, object> MapInputData(ProcessElementExecutionContext parentElementContext)
    {
        var childInput = new Dictionary<string, object>(parentElementContext.InputData ?? new());

        // Add parent node outputs to child context under special prefix
        foreach (var nodeOutput in parentElementContext.ParentThreadContext.Memory.NodeOutputs)
        {
            childInput[$"parent.output.{nodeOutput.Key}"] = nodeOutput.Value;
        }

        // Add parent variables
        foreach (var variable in parentElementContext.ParentThreadContext.Memory.Variables)
        {
            childInput[$"parent.var.{variable.Key}"] = variable.Value;
        }

        return childInput;
    }

    /// <summary>Maps output from child workflow back to parent.</summary>
    private NodeExecutionResult MapResult(
        ProcessElementExecutionContext parentElementContext,
        ProcessThreadExecution childExecution,
        ProcessThreadExecutionContext childContext)
    {
        // Check if child workflow succeeded
        var childSucceeded = childExecution.ExecutionStatusID == 1; // Assuming 1 = Completed

        if (!childSucceeded)
        {
            _logger.LogWarning(
                "Sub-workflow failed with status {StatusID}",
                childExecution.ExecutionStatusID);

            return new NodeExecutionResult
            {
                IsSuccess = false,
                ErrorMessage = $"Sub-workflow failed with status {childExecution.ExecutionStatusID}",
                OutputPortKey = "error"
            };
        }

        // Store child output in parent memory
        var outputData = new Dictionary<string, object>();

        // Copy child outputs to parent's execution memory
        foreach (var output in childContext.Memory.NodeOutputs)
        {
            parentElementContext.ParentThreadContext.Memory.SetNodeOutput($"subworkflow.{output.Key}", output.Value);
            outputData[$"subworkflow.{output.Key}"] = output.Value;
        }

        // Copy child variables
        foreach (var variable in childContext.Memory.Variables)
        {
            parentElementContext.ParentThreadContext.Memory.SetVariable($"subworkflow.{variable.Key}", variable.Value);
            outputData[$"subworkflow.{variable.Key}"] = variable.Value;
        }

        return new NodeExecutionResult
        {
            IsSuccess = true,
            OutputData = outputData.Count > 0 ? outputData : null,
            OutputPortKey = "success"
        };
    }
}
