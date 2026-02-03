namespace BizFirst.Ai.ProcessEngine.Service.Executors.Logic;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Context;
using BizFirst.Ai.ProcessEngine.Domain.Execution;
using BizFirst.Ai.ProcessEngine.Domain.Node.Interfaces;
using BizFirst.Ai.ProcessEngine.Domain.Node.Validation;

/// <summary>
/// Executor for loop nodes.
/// Iterates over a collection and routes to the body for each iteration.
/// </summary>
public class LoopExecutor : IDecisionNodeExecution
{
    private readonly ILogger<LoopExecutor> _logger;

    /// <summary>Initializes a new instance.</summary>
    public LoopExecutor(ILogger<LoopExecutor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<NodeExecutionResult> ExecuteAsync(
        ProcessElementExecutionContext executionContext,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing loop {ElementKey}", executionContext.ProcessElementKey);

        try
        {
            var config = ParseConfiguration(executionContext.ElementDefinition.Configuration);
            var collectionVariable = config.TryGetValue("items", out var items) ? items.ToString() : null;

            if (string.IsNullOrEmpty(collectionVariable))
            {
                return new NodeExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Loop items variable is not configured",
                    OutputPortKey = "error"
                };
            }

            // Get the collection from execution context memory
            var collection = executionContext.ParentThreadContext.Memory.Variables.TryGetValue(collectionVariable, out var col)
                ? col
                : null;

            if (collection == null)
            {
                return new NodeExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Collection variable '{collectionVariable}' not found in execution context",
                    OutputPortKey = "error"
                };
            }

            // Count items in collection
            int itemCount = CountItems(collection);
            _logger.LogDebug("Loop will iterate over {ItemCount} items", itemCount);

            // Initialize loop counter in execution context
            var loopData = new Dictionary<string, object>
            {
                { "collection_size", itemCount },
                { "current_index", 0 },
                { "items", collection }
            };

            return new NodeExecutionResult
            {
                IsSuccess = true,
                OutputData = loopData,
                OutputPortKey = "body"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in loop {ElementKey}", executionContext.ProcessElementKey);
            return new NodeExecutionResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                OutputPortKey = "error"
            };
        }
    }

    /// <inheritdoc/>
    public async Task<string> EvaluateConditionAsync(
        ProcessElementExecutionContext executionContext,
        CancellationToken cancellationToken = default)
    {
        // Evaluate whether loop should continue
        var config = ParseConfiguration(executionContext.ElementDefinition.Configuration);
        var collectionVariable = config.TryGetValue("items", out var items) ? items.ToString() : null;

        if (string.IsNullOrEmpty(collectionVariable))
        {
            return "error";
        }

        // Check if there are more items
        var collection = executionContext.ParentThreadContext.Memory.Variables.TryGetValue(collectionVariable, out var col)
            ? col
            : null;

        if (collection == null || CountItems(collection) == 0)
        {
            return "done";
        }

        return "body";
    }

    /// <inheritdoc/>
    public async Task<ValidationResult> ValidateAsync(
        ProcessElementValidationContext validationContext,
        CancellationToken cancellationToken = default)
    {
        var config = ParseConfiguration(validationContext.Definition.Configuration);

        if (!config.ContainsKey("items") || string.IsNullOrWhiteSpace(config["items"]?.ToString()))
        {
            return await Task.FromResult(ValidationResult.Failure("Loop items variable is required"));
        }

        return await Task.FromResult(ValidationResult.Success());
    }

    /// <inheritdoc/>
    public async Task<NodeExecutionResult> HandleErrorAsync(
        ProcessElementExecutionContext executionContext,
        Exception error,
        CancellationToken cancellationToken = default)
    {
        _logger.LogError(error, "Error in loop {ElementKey}", executionContext.ProcessElementKey);

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
        // Cleanup loop state
        _logger.LogDebug("Cleaning up loop {ElementKey}", executionContext.ProcessElementKey);
        await Task.CompletedTask;
    }

    /// <summary>Count items in a collection.</summary>
    private int CountItems(object? collection)
    {
        if (collection == null)
            return 0;

        if (collection is ICollection col)
            return col.Count;

        if (collection is IEnumerable enumerable)
            return enumerable.Cast<object>().Count();

        return 1;
    }

    /// <summary>Parse configuration JSON string to dictionary.</summary>
    private Dictionary<string, object> ParseConfiguration(string configJson)
    {
        if (string.IsNullOrWhiteSpace(configJson))
        {
            return new Dictionary<string, object>();
        }

        try
        {
            // TODO: Use proper JSON parser (System.Text.Json)
            // For now, return empty dict - configuration parsing should be implemented properly
            return new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }
}
