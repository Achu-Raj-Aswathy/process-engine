namespace BizFirst.Ai.ProcessEngine.Service.Executors.Data;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BizFirst.Ai.ProcessEngine.Domain.Execution;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Context;
using BizFirst.Ai.ProcessEngine.Domain.Node.Interfaces;
using BizFirst.Ai.ProcessEngine.Domain.Node.Validation;

/// <summary>
/// Executor for collection operations.
/// Performs operations like filter, map, and reduce on arrays and lists.
/// </summary>
public class CollectionOperationExecutor : IActionNodeExecution
{
    private readonly ILogger<CollectionOperationExecutor> _logger;

    /// <summary>Initializes a new instance.</summary>
    public CollectionOperationExecutor(ILogger<CollectionOperationExecutor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<NodeExecutionResult> ExecuteAsync(
        ProcessElementExecutionContext executionContext,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing collection operation for {ElementKey}", executionContext.ProcessElementKey);

        try
        {
            // Get collection
            var collection = GetCollection(executionContext);
            if (collection == null || collection.Count == 0)
            {
                return await Task.FromResult(new NodeExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Collection not provided or empty",
                    OutputPortKey = "error"
                });
            }

            // Get operation
            var operation = GetOperation(executionContext);
            if (string.IsNullOrEmpty(operation))
            {
                return await Task.FromResult(new NodeExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Operation not specified (filter|map|reduce|sort|distinct|count)",
                    OutputPortKey = "error"
                });
            }

            // Perform operation
            var result = PerformOperation(collection, operation, executionContext);

            _logger.LogDebug("Collection operation completed: {Operation}, Items: {Count}",
                operation, collection.Count);

            return await Task.FromResult(new NodeExecutionResult
            {
                IsSuccess = true,
                OutputData = new Dictionary<string, object> { { "result", result } },
                OutputPortKey = "success"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in collection operation {ElementKey}", executionContext.ProcessElementKey);
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
        return await Task.FromResult(ValidationResult.Success());
    }

    /// <inheritdoc/>
    public async Task<NodeExecutionResult> HandleErrorAsync(
        ProcessElementExecutionContext executionContext,
        Exception error,
        CancellationToken cancellationToken = default)
    {
        _logger.LogError(error, "Error in collection operation {ElementKey}", executionContext.ProcessElementKey);
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
        await Task.CompletedTask;
    }

    /// <summary>Gets collection from input or node output.</summary>
    private List<object>? GetCollection(ProcessElementExecutionContext context)
    {
        // Try input.collection
        if (context.InputData != null && context.InputData.TryGetValue("collection", out var coll))
        {
            if (coll is List<object> list)
                return list;
        }

        // Try node output
        if (context.InputData != null && context.InputData.TryGetValue("source_output", out var nodeKeyObj))
        {
            var nodeKey = nodeKeyObj?.ToString();
            if (!string.IsNullOrEmpty(nodeKey))
            {
                var output = context.ParentThreadContext.Memory.GetNodeOutput(nodeKey);
                if (output is List<object> list)
                    return list;
            }
        }

        return null;
    }

    /// <summary>Gets operation from configuration.</summary>
    private string GetOperation(ProcessElementExecutionContext context)
    {
        if (context.InputData != null && context.InputData.TryGetValue("operation", out var op))
        {
            return op?.ToString()?.ToLowerInvariant() ?? string.Empty;
        }

        return string.Empty;
    }

    /// <summary>Performs the specified collection operation.</summary>
    private object PerformOperation(
        List<object> collection,
        string operation,
        ProcessElementExecutionContext context)
    {
        return operation switch
        {
            "filter" => FilterCollection(collection, context),
            "map" => MapCollection(collection, context),
            "reduce" => ReduceCollection(collection, context),
            "sort" => SortCollection(collection, context),
            "distinct" => collection.Distinct().ToList(),
            "count" => collection.Count,
            "first" => collection.FirstOrDefault(),
            "last" => collection.LastOrDefault(),
            "reverse" => collection.AsEnumerable().Reverse().ToList(),
            _ => collection
        };
    }

    /// <summary>Filters collection based on criteria.</summary>
    private List<object>? FilterCollection(List<object> collection, ProcessElementExecutionContext context)
    {
        // Get filter criteria
        if (context.InputData == null || !context.InputData.TryGetValue("filter_criteria", out var criteriaObj))
            return collection;

        var criteria = criteriaObj?.ToString();
        if (string.IsNullOrEmpty(criteria))
            return collection;

        // Simple filtering: only include non-null, non-empty items
        if (criteria == "non-empty")
        {
            return collection.Where(item => item != null && !string.IsNullOrEmpty(item.ToString())).ToList();
        }

        // Filter by type
        if (criteria.StartsWith("type:"))
        {
            var typeName = criteria.Substring(5);
            return collection.Where(item => item?.GetType().Name == typeName).ToList();
        }

        return collection;
    }

    /// <summary>Maps collection items using transformation.</summary>
    private List<object>? MapCollection(List<object> collection, ProcessElementExecutionContext context)
    {
        // Get transformation
        if (context.InputData == null || !context.InputData.TryGetValue("map_transform", out var transformObj))
            return collection;

        var transform = transformObj?.ToString();
        if (string.IsNullOrEmpty(transform))
            return collection;

        var result = new List<object>();

        foreach (var item in collection)
        {
            var mapped = ApplyMapTransform(item, transform);
            result.Add(mapped);
        }

        return result;
    }

    /// <summary>Reduces collection to a single value.</summary>
    private object ReduceCollection(List<object> collection, ProcessElementExecutionContext context)
    {
        // Get reducer operation
        if (context.InputData == null || !context.InputData.TryGetValue("reduce_operation", out var opObj))
            return collection;

        var op = opObj?.ToString()?.ToLowerInvariant();

        return op switch
        {
            "sum" => collection.OfType<int>().Sum(),
            "average" => collection.OfType<int>().Count() > 0 ? collection.OfType<int>().Average() : 0,
            "min" => collection.OfType<int>().Count() > 0 ? collection.OfType<int>().Min() : 0,
            "max" => collection.OfType<int>().Count() > 0 ? collection.OfType<int>().Max() : 0,
            "join" => string.Join(",", collection.Select(x => x?.ToString() ?? "")),
            "count" => collection.Count,
            _ => collection
        };
    }

    /// <summary>Sorts collection.</summary>
    private List<object>? SortCollection(List<object> collection, ProcessElementExecutionContext context)
    {
        // Get sort direction
        var descending = false;
        if (context.InputData != null && context.InputData.TryGetValue("sort_descending", out var descObj))
        {
            if (bool.TryParse(descObj?.ToString() ?? "false", out var desc))
                descending = desc;
        }

        var sorted = descending
            ? collection.OrderByDescending(x => x?.ToString() ?? "").ToList()
            : collection.OrderBy(x => x?.ToString() ?? "").ToList();

        return sorted;
    }

    /// <summary>Applies a map transformation to an item.</summary>
    private object ApplyMapTransform(object item, string transform)
    {
        if (item == null)
            return null!;

        return transform.ToLowerInvariant() switch
        {
            "uppercase" => item.ToString()?.ToUpperInvariant() ?? item,
            "lowercase" => item.ToString()?.ToLowerInvariant() ?? item,
            "tostring" => item.ToString() ?? item,
            "reverse" => item.ToString() != null
                ? new string(item.ToString()!.Reverse().ToArray())
                : item,
            "length" => item.ToString()?.Length ?? 0,
            _ => item
        };
    }
}
