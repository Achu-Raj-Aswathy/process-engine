namespace BizFirst.Ai.ProcessEngine.Service.Persistence;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BizFirst.Ai.ProcessEngine.Domain.Definition;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Context;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Memory;

/// <summary>
/// Interface for persisting and restoring execution state
/// </summary>
public interface IExecutionStateService
{
    /// <summary>Save execution stack when pausing</summary>
    Task SaveStackStateAsync(
        int processThreadExecutionID,
        Stack<ProcessElementDefinition> executionStack,
        CancellationToken cancellationToken = default);

    /// <summary>Save execution memory when pausing</summary>
    Task SaveMemoryStateAsync(
        int processThreadExecutionID,
        ExecutionMemory executionMemory,
        CancellationToken cancellationToken = default);

    /// <summary>Load previously saved execution stack</summary>
    Task<Stack<ProcessElementDefinition>> LoadStackStateAsync(
        int processThreadExecutionID,
        ProcessThreadDefinition threadDefinition,
        CancellationToken cancellationToken = default);

    /// <summary>Load previously saved execution memory</summary>
    Task<ExecutionMemory> LoadMemoryStateAsync(
        int processThreadExecutionID,
        CancellationToken cancellationToken = default);

    /// <summary>Mark saved state as inactive</summary>
    Task MarkStateInactiveAsync(
        int processThreadExecutionID,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of execution state persistence service
/// Persists execution state to allow pause/resume functionality
/// </summary>
public class ExecutionStateService : IExecutionStateService
{
    private readonly ILogger<ExecutionStateService> _logger;
    // In-memory cache for execution states during session
    // TODO: In production, replace with database persistence
    private static readonly Dictionary<int, (string StackData, string MemoryData, DateTime SavedAt, bool IsActive)> _stateCache = new();

    public ExecutionStateService(ILogger<ExecutionStateService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Save execution stack as JSON</summary>
    public async Task SaveStackStateAsync(
        int processThreadExecutionID,
        Stack<ProcessElementDefinition> executionStack,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Saving execution stack for ProcessThreadExecutionID {ThreadExecutionID}",
                processThreadExecutionID);

            // Serialize stack to JSON
            var stackList = new List<object>();
            foreach (var element in executionStack)
            {
                stackList.Add(new
                {
                    element.ProcessElementID,
                    element.ProcessElementKey,
                    element.Configuration
                });
            }

            var stackJson = JsonSerializer.Serialize(new
            {
                execution_stack = stackList,
                saved_at = DateTime.UtcNow,
                stack_depth = stackList.Count
            });

            _logger.LogDebug(
                "Serialized execution stack ({StackDepth} items)",
                stackList.Count);

            // Save to cache (TODO: persist to database)
            if (_stateCache.ContainsKey(processThreadExecutionID))
            {
                var current = _stateCache[processThreadExecutionID];
                _stateCache[processThreadExecutionID] = (stackJson, current.MemoryData, DateTime.UtcNow, true);
            }
            else
            {
                _stateCache[processThreadExecutionID] = (stackJson, "", DateTime.UtcNow, true);
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving execution stack");
            throw;
        }
    }

    /// <summary>Save execution memory as JSON</summary>
    public async Task SaveMemoryStateAsync(
        int processThreadExecutionID,
        ExecutionMemory executionMemory,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Saving execution memory for ProcessThreadExecutionID {ThreadExecutionID}",
                processThreadExecutionID);

            var memoryJson = JsonSerializer.Serialize(new
            {
                variables = executionMemory.Variables,
                node_outputs = executionMemory.NodeOutputs,
                saved_at = DateTime.UtcNow,
                variable_count = executionMemory.Variables.Count
            });

            _logger.LogDebug(
                "Serialized execution memory ({VariableCount} variables)",
                executionMemory.Variables.Count);

            // Save to cache (TODO: persist to database)
            if (_stateCache.ContainsKey(processThreadExecutionID))
            {
                var current = _stateCache[processThreadExecutionID];
                _stateCache[processThreadExecutionID] = (current.StackData, memoryJson, DateTime.UtcNow, true);
            }
            else
            {
                _stateCache[processThreadExecutionID] = ("", memoryJson, DateTime.UtcNow, true);
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving execution memory");
            throw;
        }
    }

    /// <summary>Load saved execution stack</summary>
    public async Task<Stack<ProcessElementDefinition>> LoadStackStateAsync(
        int processThreadExecutionID,
        ProcessThreadDefinition threadDefinition,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Loading execution stack for ProcessThreadExecutionID {ThreadExecutionID}",
                processThreadExecutionID);

            var restoredStack = new Stack<ProcessElementDefinition>();

            if (_stateCache.TryGetValue(processThreadExecutionID, out var state) && !string.IsNullOrEmpty(state.StackData))
            {
                var stackData = JsonSerializer.Deserialize<JsonElement>(state.StackData);
                if (stackData.TryGetProperty("execution_stack", out var stackArray))
                {
                    var itemList = new List<ProcessElementDefinition>();
                    foreach (var item in stackArray.EnumerateArray())
                    {
                        if (item.TryGetProperty("ProcessElementID", out var idElement) &&
                            idElement.TryGetInt32(out int elementId))
                        {
                            var element = threadDefinition.Elements
                                .FirstOrDefault(e => e.ProcessElementID == elementId);
                            if (element != null)
                            {
                                itemList.Add(element);
                            }
                        }
                    }

                    // Restore stack in reverse order (stack is LIFO)
                    foreach (var element in itemList.Reverse<ProcessElementDefinition>())
                    {
                        restoredStack.Push(element);
                    }
                }
            }

            _logger.LogInformation(
                "Loaded execution stack with {StackDepth} items",
                restoredStack.Count);

            return await Task.FromResult(restoredStack);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading execution stack");
            throw;
        }
    }

    /// <summary>Load saved execution memory</summary>
    public async Task<ExecutionMemory> LoadMemoryStateAsync(
        int processThreadExecutionID,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Loading execution memory for ProcessThreadExecutionID {ThreadExecutionID}",
                processThreadExecutionID);

            var restoredMemory = new ExecutionMemory(new Dictionary<string, object>());

            if (_stateCache.TryGetValue(processThreadExecutionID, out var state) && !string.IsNullOrEmpty(state.MemoryData))
            {
                var memoryData = JsonSerializer.Deserialize<JsonElement>(state.MemoryData);

                if (memoryData.TryGetProperty("variables", out var variablesElement))
                {
                    foreach (var property in variablesElement.EnumerateObject())
                    {
                        restoredMemory.Variables[property.Name] = property.Value.GetRawText();
                    }
                }

                if (memoryData.TryGetProperty("node_outputs", out var outputsElement))
                {
                    foreach (var property in outputsElement.EnumerateObject())
                    {
                        restoredMemory.NodeOutputs[property.Name] = property.Value.GetRawText();
                    }
                }
            }

            _logger.LogInformation(
                "Loaded execution memory with {VariableCount} variables",
                restoredMemory.Variables.Count);

            return await Task.FromResult(restoredMemory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading execution memory");
            throw;
        }
    }

    /// <summary>Mark state as inactive</summary>
    public async Task MarkStateInactiveAsync(
        int processThreadExecutionID,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Marking execution state inactive for ProcessThreadExecutionID {ThreadExecutionID}",
                processThreadExecutionID);

            if (_stateCache.TryGetValue(processThreadExecutionID, out var state))
            {
                _stateCache[processThreadExecutionID] = (state.StackData, state.MemoryData, state.SavedAt, false);
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking state inactive");
            throw;
        }
    }
}
