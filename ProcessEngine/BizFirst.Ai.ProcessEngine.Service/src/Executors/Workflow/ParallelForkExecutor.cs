namespace BizFirst.Ai.ProcessEngine.Service.Executors.Workflow;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BizFirst.Ai.ProcessEngine.Domain.Definition;
using BizFirst.Ai.ProcessEngine.Domain.Execution;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Context;
using BizFirst.Ai.ProcessEngine.Domain.Node.Interfaces;
using BizFirst.Ai.ProcessEngine.Domain.Node.Validation;

/// <summary>
/// Executor for parallel fork nodes that split execution into multiple lanes.
/// Initiates parallel execution of downstream lanes.
/// </summary>
public class ParallelForkExecutor : IActionNodeExecution
{
    private readonly ILogger<ParallelForkExecutor> _logger;

    /// <summary>Initializes a new instance.</summary>
    public ParallelForkExecutor(ILogger<ParallelForkExecutor> logger)
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
                "Parallel fork node executing - ElementKey: {ElementKey}",
                executionContext.ProcessElementKey);

            // Initialize parallel execution mode
            executionContext.ParentThreadContext.Memory.IsParallelExecutionActive = true;

            // Fork node doesn't perform any logic itself
            // It merely signals that subsequent nodes should execute in parallel
            // The actual lane splitting is handled by the orchestrator

            var output = new Dictionary<string, object>
            {
                { "fork_id", executionContext.ProcessElementKey },
                { "lanes_initialized", DateTime.UtcNow.ToString("O") }
            };

            _logger.LogDebug("Parallel fork initiated for element {ElementKey}", executionContext.ProcessElementKey);

            return await Task.FromResult(new NodeExecutionResult
            {
                IsSuccess = true,
                OutputData = output,
                OutputPortKey = "success"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in parallel fork node: {ElementKey}", executionContext.ProcessElementKey);
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
        // Parallel fork nodes are valid if properly configured
        // Connection validation happens during execution orchestration
        return await Task.FromResult(ValidationResult.Success());
    }

    /// <inheritdoc/>
    public async Task<NodeExecutionResult> HandleErrorAsync(
        ProcessElementExecutionContext executionContext,
        Exception error,
        CancellationToken cancellationToken = default)
    {
        _logger.LogError(error, "Error handling for parallel fork node: {ElementKey}", executionContext.ProcessElementKey);

        return await Task.FromResult(new NodeExecutionResult
        {
            IsSuccess = false,
            ErrorMessage = $"Fork failed: {error.Message}",
            Exception = error,
            OutputPortKey = "error"
        });
    }

    /// <inheritdoc/>
    public async Task CleanupAsync(ProcessElementExecutionContext executionContext, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Cleaning up parallel fork node: {ElementKey}", executionContext.ProcessElementKey);
        await Task.CompletedTask;
    }
}
