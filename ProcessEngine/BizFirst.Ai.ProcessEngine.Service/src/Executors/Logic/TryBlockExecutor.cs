namespace BizFirst.Ai.ProcessEngine.Service.Executors.Logic;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BizFirst.Ai.ProcessEngine.Domain.Execution;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Context;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Memory;
using BizFirst.Ai.ProcessEngine.Domain.Node.Interfaces;
using BizFirst.Ai.ProcessEngine.Domain.Node.Validation;

/// <summary>
/// Executor for try block start marker.
/// Sets up exception context stack and prepares for catch/finally handling.
/// </summary>
public class TryBlockExecutor : IActionNodeExecution
{
    private readonly ILogger<TryBlockExecutor> _logger;

    /// <summary>Initializes a new instance.</summary>
    public TryBlockExecutor(ILogger<TryBlockExecutor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<NodeExecutionResult> ExecuteAsync(
        ProcessElementExecutionContext executionContext,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Entering try block: {ElementKey}", executionContext.ProcessElementKey);

        try
        {
            // Enter try block scope in execution memory
            executionContext.ParentThreadContext.Memory.EnterTryBlock(executionContext.ProcessElementKey);

            _logger.LogDebug("Try block scope entered for {ElementKey}", executionContext.ProcessElementKey);

            // Configuration parsing would happen based on workflow definition structure
            // For now, execution router will wire up the catch and finally blocks

            return await Task.FromResult(new NodeExecutionResult
            {
                IsSuccess = true,
                OutputPortKey = "success"  // Routes to first node in try block body
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in try block {ElementKey}", executionContext.ProcessElementKey);
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
        // Try block is valid - catch and finally blocks are wired through execution router
        return await Task.FromResult(ValidationResult.Success());
    }

    /// <inheritdoc/>
    public async Task<NodeExecutionResult> HandleErrorAsync(
        ProcessElementExecutionContext executionContext,
        Exception error,
        CancellationToken cancellationToken = default)
    {
        _logger.LogError(error, "Error in try block {ElementKey}", executionContext.ProcessElementKey);
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
        // No cleanup needed at this point; finally block will handle it
        await Task.CompletedTask;
    }
}
