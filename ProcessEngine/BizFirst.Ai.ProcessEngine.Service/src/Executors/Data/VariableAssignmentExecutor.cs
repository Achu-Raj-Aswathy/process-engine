namespace BizFirst.Ai.ProcessEngine.Service.Executors.Data;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BizFirst.Ai.ProcessEngine.Domain.Execution;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Context;
using BizFirst.Ai.ProcessEngine.Domain.Node.Interfaces;
using BizFirst.Ai.ProcessEngine.Domain.Node.Validation;

/// <summary>
/// Executor for variable assignment.
/// Sets or updates variables in execution memory based on expressions or constant values.
/// </summary>
public class VariableAssignmentExecutor : IActionNodeExecution
{
    private readonly ILogger<VariableAssignmentExecutor> _logger;

    /// <summary>Initializes a new instance.</summary>
    public VariableAssignmentExecutor(ILogger<VariableAssignmentExecutor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<NodeExecutionResult> ExecuteAsync(
        ProcessElementExecutionContext executionContext,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing variable assignment for {ElementKey}", executionContext.ProcessElementKey);

        try
        {
            // Get variable name
            var variableName = GetVariableName(executionContext);
            if (string.IsNullOrEmpty(variableName))
            {
                return await Task.FromResult(new NodeExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Variable name not specified",
                    OutputPortKey = "error"
                });
            }

            // Get value (from input or configuration)
            var value = GetVariableValue(executionContext);

            // Set variable in memory
            executionContext.ParentThreadContext.Memory.SetVariable(variableName, value);

            _logger.LogDebug("Variable '{VariableName}' assigned value: {Value}",
                variableName, value?.ToString() ?? "null");

            var outputData = new Dictionary<string, object>
            {
                { variableName, value },
                { "assigned_variable", variableName },
                { "assigned_value", value ?? (object)"null" }
            };

            return await Task.FromResult(new NodeExecutionResult
            {
                IsSuccess = true,
                OutputData = outputData,
                OutputPortKey = "success"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning variable in {ElementKey}", executionContext.ProcessElementKey);
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
        // Variable assignment requires variable name
        return await Task.FromResult(ValidationResult.Success());
    }

    /// <inheritdoc/>
    public async Task<NodeExecutionResult> HandleErrorAsync(
        ProcessElementExecutionContext executionContext,
        Exception error,
        CancellationToken cancellationToken = default)
    {
        _logger.LogError(error, "Error in variable assignment {ElementKey}", executionContext.ProcessElementKey);
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

    /// <summary>Gets variable name from configuration or input.</summary>
    private string GetVariableName(ProcessElementExecutionContext context)
    {
        // Try input data first
        if (context.InputData != null && context.InputData.TryGetValue("variable_name", out var nameObj))
        {
            return nameObj?.ToString() ?? string.Empty;
        }

        // Try memory
        if (context.ParentThreadContext.Memory.GetVariable("variable_name") is string memoryName)
        {
            return memoryName;
        }

        return string.Empty;
    }

    /// <summary>Gets variable value from configuration, input, or memory.</summary>
    private object GetVariableValue(ProcessElementExecutionContext context)
    {
        // Try direct value in input
        if (context.InputData != null && context.InputData.TryGetValue("value", out var value))
        {
            return value;
        }

        // Try value_expression (reference to another variable or output)
        if (context.InputData != null && context.InputData.TryGetValue("value_expression", out var expr))
        {
            var exprStr = expr?.ToString();
            if (!string.IsNullOrEmpty(exprStr))
            {
                // Simple expression handling: resolve variable references like "var.x" or "output.nodekey"
                if (exprStr.StartsWith("var."))
                {
                    var varName = exprStr.Substring(4);
                    return context.ParentThreadContext.Memory.GetVariable(varName) ?? string.Empty;
                }

                if (exprStr.StartsWith("output."))
                {
                    var nodeKey = exprStr.Substring(7);
                    return context.ParentThreadContext.Memory.GetNodeOutput(nodeKey) ?? string.Empty;
                }

                if (exprStr.StartsWith("input."))
                {
                    var inputKey = exprStr.Substring(6);
                    return context.ParentThreadContext.Memory.InputData.ContainsKey(inputKey)
                        ? context.ParentThreadContext.Memory.InputData[inputKey]
                        : string.Empty;
                }
            }
        }

        return null;
    }
}
