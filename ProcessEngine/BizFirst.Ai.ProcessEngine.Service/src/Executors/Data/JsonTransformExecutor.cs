namespace BizFirst.Ai.ProcessEngine.Service.Executors.Data;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BizFirst.Ai.ProcessEngine.Domain.Execution;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Context;
using BizFirst.Ai.ProcessEngine.Domain.Node.Interfaces;
using BizFirst.Ai.ProcessEngine.Domain.Node.Validation;

/// <summary>
/// Executor for JSON transformation operations.
/// Provides JSON parsing, querying, and manipulation capabilities.
/// </summary>
public class JsonTransformExecutor : IActionNodeExecution
{
    private readonly ILogger<JsonTransformExecutor> _logger;

    /// <summary>Initializes a new instance.</summary>
    public JsonTransformExecutor(ILogger<JsonTransformExecutor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<NodeExecutionResult> ExecuteAsync(
        ProcessElementExecutionContext executionContext,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing JSON transformation for {ElementKey}", executionContext.ProcessElementKey);

        try
        {
            // Get source data
            var sourceData = GetSourceData(executionContext);
            if (sourceData == null)
            {
                return await Task.FromResult(new NodeExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Source data not found",
                    OutputPortKey = "error"
                });
            }

            // Get transformation operation
            var operation = GetOperation(executionContext);
            if (string.IsNullOrEmpty(operation))
            {
                return await Task.FromResult(new NodeExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Transformation operation not specified",
                    OutputPortKey = "error"
                });
            }

            // Perform transformation
            var result = PerformTransformation(sourceData, operation, executionContext);

            _logger.LogDebug("JSON transformation completed: {Operation}", operation);

            return await Task.FromResult(new NodeExecutionResult
            {
                IsSuccess = true,
                OutputData = new Dictionary<string, object> { { "transformed_data", result } },
                OutputPortKey = "success"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in JSON transformation {ElementKey}", executionContext.ProcessElementKey);
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
        _logger.LogError(error, "Error in JSON transform {ElementKey}", executionContext.ProcessElementKey);
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

    /// <summary>Gets source data from input or memory.</summary>
    private object? GetSourceData(ProcessElementExecutionContext context)
    {
        // Try input.source
        if (context.InputData != null && context.InputData.TryGetValue("source", out var source))
        {
            return source;
        }

        // Try reference to node output
        if (context.InputData != null && context.InputData.TryGetValue("source_output", out var nodeKeyObj))
        {
            var nodeKey = nodeKeyObj?.ToString();
            if (!string.IsNullOrEmpty(nodeKey))
            {
                return context.ParentThreadContext.Memory.GetNodeOutput(nodeKey);
            }
        }

        return null;
    }

    /// <summary>Gets transformation operation from configuration.</summary>
    private string GetOperation(ProcessElementExecutionContext context)
    {
        if (context.InputData != null && context.InputData.TryGetValue("operation", out var op))
        {
            return op?.ToString() ?? string.Empty;
        }

        return string.Empty;
    }

    /// <summary>Performs the specified JSON transformation.</summary>
    private object? PerformTransformation(object sourceData, string operation, ProcessElementExecutionContext context)
    {
        // Convert source to JSON string if needed
        var jsonString = sourceData is string str ? str : JsonSerializer.Serialize(sourceData);

        // Parse to JsonElement for manipulation
        using var doc = JsonDocument.Parse(jsonString);
        var root = doc.RootElement;

        return operation.ToLowerInvariant() switch
        {
            "parse" => root.ValueKind switch
            {
                JsonValueKind.Object => JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString),
                JsonValueKind.Array => JsonSerializer.Deserialize<List<object>>(jsonString),
                _ => root.ToString()
            },

            "stringify" => JsonSerializer.Serialize(sourceData, new JsonSerializerOptions { WriteIndented = true }),

            "minify" => JsonSerializer.Serialize(sourceData),

            "extract" => ExtractProperty(root, context),

            "merge" => MergeObjects(root, context),

            "filter" => FilterArray(root, context),

            _ => root.ToString()
        };
    }

    /// <summary>Extracts a property from JSON.</summary>
    private object? ExtractProperty(JsonElement root, ProcessElementExecutionContext context)
    {
        if (context.InputData == null || !context.InputData.TryGetValue("property", out var propObj))
            return null;

        var property = propObj?.ToString();
        if (string.IsNullOrEmpty(property))
            return null;

        return root.TryGetProperty(property, out var prop) ? prop.ToString() : null;
    }

    /// <summary>Merges JSON objects.</summary>
    private object? MergeObjects(JsonElement root, ProcessElementExecutionContext context)
    {
        if (context.InputData == null || !context.InputData.TryGetValue("merge_with", out var mergeObj))
            return null;

        var mergeStr = mergeObj?.ToString();
        if (string.IsNullOrEmpty(mergeStr))
            return null;

        try
        {
            var merged = new Dictionary<string, object>();

            // Add source object properties
            if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in root.EnumerateObject())
                {
                    merged[prop.Name] = prop.Value.ToString();
                }
            }

            // Add merge object properties
            using var mergeDoc = JsonDocument.Parse(mergeStr);
            foreach (var prop in mergeDoc.RootElement.EnumerateObject())
            {
                merged[prop.Name] = prop.Value.ToString();
            }

            return merged;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Filters array elements based on criteria.</summary>
    private object? FilterArray(JsonElement root, ProcessElementExecutionContext context)
    {
        if (root.ValueKind != JsonValueKind.Array)
            return root.ToString();

        var filtered = new List<object>();

        foreach (var element in root.EnumerateArray())
        {
            // Simple filtering: include non-null, non-empty elements
            if (element.ValueKind != JsonValueKind.Null && element.ValueKind != JsonValueKind.Undefined)
            {
                filtered.Add(element.ToString());
            }
        }

        return filtered;
    }
}
