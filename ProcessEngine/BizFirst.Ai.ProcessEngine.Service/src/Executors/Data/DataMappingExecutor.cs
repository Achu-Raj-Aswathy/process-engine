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
/// Executor for data field mapping.
/// Maps fields from source objects to target objects using field-level configuration.
/// </summary>
public class DataMappingExecutor : IActionNodeExecution
{
    private readonly ILogger<DataMappingExecutor> _logger;

    /// <summary>Initializes a new instance.</summary>
    public DataMappingExecutor(ILogger<DataMappingExecutor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<NodeExecutionResult> ExecuteAsync(
        ProcessElementExecutionContext executionContext,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing data mapping for {ElementKey}", executionContext.ProcessElementKey);

        try
        {
            // Get source data
            var sourceData = GetSourceData(executionContext);
            if (sourceData == null)
            {
                return await Task.FromResult(new NodeExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Source data not provided",
                    OutputPortKey = "error"
                });
            }

            // Get mappings configuration
            var mappings = GetMappings(executionContext);
            if (mappings == null || mappings.Count == 0)
            {
                return await Task.FromResult(new NodeExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "No field mappings specified",
                    OutputPortKey = "error"
                });
            }

            // Perform mapping
            var mappedData = PerformMapping(sourceData, mappings);

            _logger.LogDebug("Data mapping completed: {MappingCount} fields mapped", mappings.Count);

            return await Task.FromResult(new NodeExecutionResult
            {
                IsSuccess = true,
                OutputData = mappedData,
                OutputPortKey = "success"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in data mapping {ElementKey}", executionContext.ProcessElementKey);
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
        _logger.LogError(error, "Error in data mapping {ElementKey}", executionContext.ProcessElementKey);
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

    /// <summary>Gets source data from input or node output.</summary>
    private Dictionary<string, object>? GetSourceData(ProcessElementExecutionContext context)
    {
        // Try input.source
        if (context.InputData != null && context.InputData.TryGetValue("source", out var source))
        {
            if (source is Dictionary<string, object> dict)
                return dict;
        }

        // Try source node output
        if (context.InputData != null && context.InputData.TryGetValue("source_output", out var nodeKeyObj))
        {
            var nodeKey = nodeKeyObj?.ToString();
            if (!string.IsNullOrEmpty(nodeKey))
            {
                var output = context.ParentThreadContext.Memory.GetNodeOutput(nodeKey);
                if (output is Dictionary<string, object> dict)
                    return dict;
            }
        }

        return null;
    }

    /// <summary>Gets field mappings configuration.</summary>
    private List<FieldMapping>? GetMappings(ProcessElementExecutionContext context)
    {
        if (context.InputData == null || !context.InputData.TryGetValue("mappings", out var mappingsObj))
            return null;

        var mappings = new List<FieldMapping>();

        // Handle both list of dicts and direct mappings
        if (mappingsObj is List<Dictionary<string, object>> mapList)
        {
            foreach (var mapping in mapList)
            {
                var source = mapping.ContainsKey("source") ? mapping["source"]?.ToString() : null;
                var target = mapping.ContainsKey("target") ? mapping["target"]?.ToString() : null;
                var transform = mapping.ContainsKey("transform") ? mapping["transform"]?.ToString() : null;

                if (!string.IsNullOrEmpty(source) && !string.IsNullOrEmpty(target))
                {
                    mappings.Add(new FieldMapping { Source = source, Target = target, Transform = transform });
                }
            }
        }

        return mappings.Count > 0 ? mappings : null;
    }

    /// <summary>Performs field-level mapping from source to target.</summary>
    private Dictionary<string, object>? PerformMapping(
        Dictionary<string, object> sourceData,
        List<FieldMapping> mappings)
    {
        var result = new Dictionary<string, object>();

        foreach (var mapping in mappings)
        {
            if (sourceData.TryGetValue(mapping.Source, out var value))
            {
                // Apply transformation if specified
                var mappedValue = value;

                if (!string.IsNullOrEmpty(mapping.Transform))
                {
                    mappedValue = ApplyTransform(value, mapping.Transform);
                }

                result[mapping.Target] = mappedValue;
            }
        }

        return result;
    }

    /// <summary>Applies transformation to a value.</summary>
    private object ApplyTransform(object? value, string transform)
    {
        if (value == null)
            return null!;

        return transform.ToLowerInvariant() switch
        {
            "uppercase" => value.ToString()?.ToUpperInvariant() ?? value,
            "lowercase" => value.ToString()?.ToLowerInvariant() ?? value,
            "trim" => value.ToString()?.Trim() ?? value,
            "tostring" => value.ToString() ?? value,
            "toint" => int.TryParse(value.ToString(), out var intVal) ? intVal : 0,
            "todecimal" => decimal.TryParse(value.ToString(), out var decVal) ? decVal : 0,
            "toboolean" => bool.TryParse(value.ToString(), out var boolVal) && boolVal,
            _ => value
        };
    }

    /// <summary>Represents a single field mapping.</summary>
    private class FieldMapping
    {
        public string Source { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string? Transform { get; set; }
    }
}
