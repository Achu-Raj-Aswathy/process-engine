namespace BizFirst.Ai.ProcessEngine.Domain.Execution.Memory;

using System;
using System.Collections.Generic;

/// <summary>
/// Execution memory that persists across the entire process/thread execution.
/// Provides mutable working memory for variables, node outputs, and caching.
/// Used to share data between nodes and maintain state across execution.
/// </summary>
public class ExecutionMemory
{
    /// <summary>Immutable input data for the execution.</summary>
    public IReadOnlyDictionary<string, object> InputData { get; }

    /// <summary>Mutable variables stored during execution.</summary>
    public Dictionary<string, object> Variables { get; set; } = new();

    /// <summary>Node outputs accessible by subsequent nodes. Key: ProcessElementKey, Value: output data.</summary>
    public Dictionary<string, object> NodeOutputs { get; set; } = new();

    /// <summary>Temporary cache for execution-specific data.</summary>
    public Dictionary<string, object> Cache { get; set; } = new();

    /// <summary>Loop control signal - true when break statement encountered.</summary>
    public bool LoopBreakSignal { get; set; } = false;

    /// <summary>Loop control signal - true when continue statement encountered.</summary>
    public bool LoopContinueSignal { get; set; } = false;

    /// <summary>Current loop depth for nested loop tracking.</summary>
    public Stack<(string LoopNodeKey, int IterationCount)> LoopStack { get; set; } = new();

    /// <summary>Exception context stack for try-catch-finally scope tracking.</summary>
    public Stack<ExceptionContext> ExceptionContextStack { get; set; } = new();

    /// <summary>Current exception being handled in a catch block (if any).</summary>
    public Exception? CurrentException { get; set; }

    /// <summary>Indicates if parallel execution is currently active.</summary>
    public bool IsParallelExecutionActive { get; set; } = false;

    /// <summary>Tracks status of parallel lanes (completion status per lane).</summary>
    public Dictionary<string, object> ParallelLaneStatus { get; set; } = new();

    /// <summary>Stores outputs from parallel lanes. Key: lane identifier, Value: lane output data.</summary>
    public Dictionary<string, Dictionary<string, object>> ParallelLaneOutputs { get; set; } = new();

    /// <summary>Variable scope stack for managing variable lifetime and scoping.</summary>
    public Stack<VariableScope> ScopeStack { get; set; } = new();

    /// <summary>Global (root) scope that's always at the bottom of the scope stack.</summary>
    private VariableScope _globalScope { get; set; }

    /// <summary>Gets the current active scope.</summary>
    public VariableScope CurrentScope => ScopeStack.Count > 0 ? ScopeStack.Peek() : _globalScope;

    /// <summary>Initializes a new instance of ExecutionMemory with input data.</summary>
    /// <param name="inputData">Immutable input data for this execution.</param>
    public ExecutionMemory(Dictionary<string, object> inputData)
    {
        InputData = inputData ?? throw new ArgumentNullException(nameof(inputData));

        // Initialize global scope
        _globalScope = new VariableScope(
            "global",
            VariableScopeType.Global,
            parentScope: null)
        {
            Description = "Global scope for entire workflow execution"
        };

        // Populate global scope with input data
        foreach (var kvp in inputData)
        {
            _globalScope.SetLocalVariable(kvp.Key, kvp.Value);
        }
    }

    /// <summary>Gets a variable from the current scope (with scope chain lookup).</summary>
    /// <param name="key">The variable key.</param>
    /// <returns>The variable value, or null if not found.</returns>
    public object? GetVariable(string key)
    {
        return CurrentScope.GetVariable(key);
    }

    /// <summary>Sets a variable in the current scope.</summary>
    /// <param name="key">The variable key.</param>
    /// <param name="value">The variable value.</param>
    public void SetVariable(string key, object value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value), $"Cannot set null value for variable '{key}'");

        CurrentScope.SetVariable(key, value);
    }

    /// <summary>Sets a variable exclusively in the current scope (doesn't propagate to parent).</summary>
    /// <param name="key">The variable key.</param>
    /// <param name="value">The variable value.</param>
    public void SetLocalVariable(string key, object value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value), $"Cannot set null value for variable '{key}'");

        CurrentScope.SetLocalVariable(key, value);
    }

    /// <summary>Gets output from a node.</summary>
    /// <param name="nodeKey">The node's ProcessElementKey.</param>
    /// <returns>The node's output, or null if not found.</returns>
    public object? GetNodeOutput(string nodeKey)
    {
        return NodeOutputs.ContainsKey(nodeKey) ? NodeOutputs[nodeKey] : null;
    }

    /// <summary>Sets output for a node.</summary>
    /// <param name="nodeKey">The node's ProcessElementKey.</param>
    /// <param name="output">The output data.</param>
    public void SetNodeOutput(string nodeKey, object output)
    {
        NodeOutputs[nodeKey] = output ?? throw new ArgumentNullException(nameof(output), $"Cannot set null output for node '{nodeKey}'");
    }

    /// <summary>Gets a cached value.</summary>
    /// <param name="key">The cache key.</param>
    /// <returns>The cached value, or null if not found.</returns>
    public object? GetCachedValue(string key)
    {
        return Cache.ContainsKey(key) ? Cache[key] : null;
    }

    /// <summary>Sets a cached value.</summary>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    public void SetCachedValue(string key, object value)
    {
        Cache[key] = value ?? throw new ArgumentNullException(nameof(value), $"Cannot cache null value for key '{key}'");
    }

    /// <summary>Clears all execution memory (variables, outputs, cache).</summary>
    public void Clear()
    {
        Variables.Clear();
        NodeOutputs.Clear();
        Cache.Clear();
        LoopBreakSignal = false;
        LoopContinueSignal = false;
        LoopStack.Clear();
        ExceptionContextStack.Clear();
        CurrentException = null;
        IsParallelExecutionActive = false;
        ParallelLaneStatus.Clear();
        ParallelLaneOutputs.Clear();
        ScopeStack.Clear();
        _globalScope.ClearLocalVariables();
    }

    /// <summary>Gets the current parallel lane status.</summary>
    /// <returns>Dictionary containing lane completion status.</returns>
    public Dictionary<string, object> GetParallelLaneStatus()
    {
        return ParallelLaneStatus;
    }

    /// <summary>Gets outputs from all parallel lanes.</summary>
    /// <returns>Dictionary mapping lane IDs to their output data.</returns>
    public Dictionary<string, Dictionary<string, object>> GetParallelLaneOutputs()
    {
        return ParallelLaneOutputs;
    }

    /// <summary>Sets the parallel lane status.</summary>
    /// <param name="laneKey">The lane identifier.</param>
    /// <param name="status">The status value.</param>
    public void SetParallelLaneStatus(string laneKey, object status)
    {
        ParallelLaneStatus[laneKey] = status;
    }

    /// <summary>Sets output for a parallel lane.</summary>
    /// <param name="laneKey">The lane identifier.</param>
    /// <param name="output">The lane's output data.</param>
    public void SetParallelLaneOutput(string laneKey, Dictionary<string, object> output)
    {
        ParallelLaneOutputs[laneKey] = output ?? new();
    }

    /// <summary>Enters a try block scope.</summary>
    /// <param name="tryBlockKey">The ProcessElementKey of the try block node.</param>
    public void EnterTryBlock(string tryBlockKey)
    {
        var context = new ExceptionContext
        {
            TryBlockKey = tryBlockKey,
            EnterTime = DateTime.UtcNow,
            CatchHandlers = new List<CatchHandler>(),
            FinallyBlockKey = null
        };
        ExceptionContextStack.Push(context);
    }

    /// <summary>Adds a catch handler to the current try block.</summary>
    /// <param name="catchBlockKey">The ProcessElementKey of the catch block.</param>
    /// <param name="exceptionTypeName">The exception type to handle (null for all exceptions).</param>
    public void AddCatchHandler(string catchBlockKey, string? exceptionTypeName = null)
    {
        if (ExceptionContextStack.Count > 0)
        {
            var context = ExceptionContextStack.Peek();
            context.CatchHandlers.Add(new CatchHandler
            {
                CatchBlockKey = catchBlockKey,
                ExceptionTypeName = exceptionTypeName
            });
        }
    }

    /// <summary>Sets the finally block for the current try block.</summary>
    /// <param name="finallyBlockKey">The ProcessElementKey of the finally block.</param>
    public void SetFinallyBlock(string finallyBlockKey)
    {
        if (ExceptionContextStack.Count > 0)
        {
            ExceptionContextStack.Peek().FinallyBlockKey = finallyBlockKey;
        }
    }

    /// <summary>Gets the current exception context (current try block).</summary>
    public ExceptionContext? GetCurrentExceptionContext()
    {
        return ExceptionContextStack.Count > 0 ? ExceptionContextStack.Peek() : null;
    }

    /// <summary>Exits the current try block scope and removes it from the stack.</summary>
    public ExceptionContext? ExitTryBlock()
    {
        return ExceptionContextStack.Count > 0 ? ExceptionContextStack.Pop() : null;
    }

    /// <summary>Finds appropriate catch handler for an exception type.</summary>
    /// <param name="exceptionType">The actual exception type.</param>
    /// <returns>The catch block key that handles this exception, or null if no handler.</returns>
    public string? FindCatchHandler(Type exceptionType)
    {
        if (ExceptionContextStack.Count == 0)
            return null;

        var context = ExceptionContextStack.Peek();

        // Look for a handler that matches the exception type
        foreach (var handler in context.CatchHandlers)
        {
            // If no exception type specified, it catches all
            if (handler.ExceptionTypeName == null)
                return handler.CatchBlockKey;

            // Check if exception type matches
            if (exceptionType.Name == handler.ExceptionTypeName || exceptionType.FullName == handler.ExceptionTypeName)
                return handler.CatchBlockKey;

            // Check base types
            var baseType = exceptionType.BaseType;
            while (baseType != null && baseType != typeof(object))
            {
                if (baseType.Name == handler.ExceptionTypeName || baseType.FullName == handler.ExceptionTypeName)
                    return handler.CatchBlockKey;
                baseType = baseType.BaseType;
            }
        }

        return null;
    }

    /// <summary>Signals a break statement in the current loop.</summary>
    public void SignalBreak()
    {
        LoopBreakSignal = true;
    }

    /// <summary>Signals a continue statement in the current loop.</summary>
    public void SignalContinue()
    {
        LoopContinueSignal = true;
    }

    /// <summary>Clears loop control signals after handling them.</summary>
    public void ClearLoopSignals()
    {
        LoopBreakSignal = false;
        LoopContinueSignal = false;
    }

    /// <summary>Enters a loop context.</summary>
    public void EnterLoop(string loopNodeKey)
    {
        LoopStack.Push((loopNodeKey, iterationCount: 0));
    }

    /// <summary>Exits the current loop context.</summary>
    public void ExitLoop()
    {
        if (LoopStack.Count > 0)
        {
            LoopStack.Pop();
        }
    }

    /// <summary>Increments iteration count for current loop.</summary>
    public void IncrementLoopIteration()
    {
        if (LoopStack.Count > 0)
        {
            var (nodeKey, count) = LoopStack.Pop();
            LoopStack.Push((nodeKey, count + 1));
        }
    }

    /// <summary>Gets current loop iteration count.</summary>
    public int GetCurrentLoopIteration()
    {
        if (LoopStack.Count > 0)
        {
            return LoopStack.Peek().IterationCount;
        }
        return 0;
    }

    /// <summary>Enters a new variable scope.</summary>
    /// <param name="scopeId">Unique identifier for the scope.</param>
    /// <param name="scopeType">The type of scope.</param>
    /// <param name="description">Optional description for debugging.</param>
    /// <returns>The new scope.</returns>
    public VariableScope EnterVariableScope(string scopeId, VariableScopeType scopeType, string? description = null)
    {
        var newScope = new VariableScope(scopeId, scopeType, CurrentScope)
        {
            Description = description
        };
        ScopeStack.Push(newScope);
        return newScope;
    }

    /// <summary>Exits the current variable scope and returns to parent.</summary>
    /// <returns>The scope that was exited (null if already at global scope).</returns>
    public VariableScope? ExitVariableScope()
    {
        if (ScopeStack.Count > 0)
        {
            return ScopeStack.Pop();
        }
        return null;
    }

    /// <summary>Gets the current scope depth (0 for global, 1+ for nested scopes).</summary>
    public int GetScopeDepth() => ScopeStack.Count;

    /// <summary>Gets all variables visible in current scope (including inherited).</summary>
    /// <returns>Dictionary with all visible variables.</returns>
    public Dictionary<string, object> GetAllVisibleVariables()
    {
        return CurrentScope.GetAllVisibleVariables();
    }
}
