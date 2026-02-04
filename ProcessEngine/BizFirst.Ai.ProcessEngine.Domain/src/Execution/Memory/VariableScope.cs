namespace BizFirst.Ai.ProcessEngine.Domain.Execution.Memory;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents a variable scope in execution hierarchy.
/// Scopes form a stack-based inheritance model where child scopes can access parent scope variables.
/// Variable lookup follows the scope chain: current scope → parent scope → global scope.
/// </summary>
public class VariableScope
{
    /// <summary>
    /// Unique identifier for this scope.
    /// Examples: "global", "thread-123", "node-element-key", "loop-element-key-iteration-5"
    /// </summary>
    public string ScopeId { get; set; }

    /// <summary>Gets the scope type.</summary>
    public VariableScopeType ScopeType { get; set; }

    /// <summary>Gets the parent scope (null if global scope).</summary>
    public VariableScope? ParentScope { get; set; }

    /// <summary>Variables defined in this scope (excludes inherited variables).</summary>
    public Dictionary<string, object> LocalVariables { get; set; } = new();

    /// <summary>Creation timestamp for scope lifecycle tracking.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Optional description of the scope for debugging.</summary>
    public string? Description { get; set; }

    /// <summary>Initializes a new scope.</summary>
    /// <param name="scopeId">Unique identifier for this scope.</param>
    /// <param name="scopeType">The type of scope.</param>
    /// <param name="parentScope">The parent scope (null for global).</param>
    public VariableScope(string scopeId, VariableScopeType scopeType, VariableScope? parentScope = null)
    {
        ScopeId = scopeId ?? throw new ArgumentNullException(nameof(scopeId));
        ScopeType = scopeType;
        ParentScope = parentScope;
    }

    /// <summary>
    /// Gets a variable from this scope or parent scopes (scope chain lookup).
    /// </summary>
    /// <param name="key">The variable key.</param>
    /// <returns>The variable value, or null if not found in scope chain.</returns>
    public object? GetVariable(string key)
    {
        // Check local variables first
        if (LocalVariables.ContainsKey(key))
            return LocalVariables[key];

        // Check parent scope (if exists)
        return ParentScope?.GetVariable(key);
    }

    /// <summary>
    /// Sets a variable in this scope.
    /// If the variable exists in a parent scope, updates parent. Otherwise creates in current scope.
    /// </summary>
    /// <param name="key">The variable key.</param>
    /// <param name="value">The variable value (cannot be null).</param>
    public void SetVariable(string key, object value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value), $"Cannot set null value for variable '{key}'");

        // Check if variable exists in parent scope
        if (ParentScope != null && ParentScope.VariableExistsInChain(key) && !LocalVariables.ContainsKey(key))
        {
            // Update in parent scope
            ParentScope.SetVariable(key, value);
        }
        else
        {
            // Create/update in current scope
            LocalVariables[key] = value;
        }
    }

    /// <summary>
    /// Sets a variable exclusively in this scope (doesn't propagate to parent).
    /// </summary>
    /// <param name="key">The variable key.</param>
    /// <param name="value">The variable value.</param>
    public void SetLocalVariable(string key, object value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value), $"Cannot set null value for variable '{key}'");

        LocalVariables[key] = value;
    }

    /// <summary>
    /// Checks if a variable exists anywhere in the scope chain.
    /// </summary>
    private bool VariableExistsInChain(string key)
    {
        if (LocalVariables.ContainsKey(key))
            return true;

        return ParentScope?.VariableExistsInChain(key) ?? false;
    }

    /// <summary>
    /// Removes a variable from this scope.
    /// </summary>
    /// <param name="key">The variable key.</param>
    /// <returns>True if removed, false if not found.</returns>
    public bool RemoveVariable(string key)
    {
        return LocalVariables.Remove(key);
    }

    /// <summary>
    /// Clears all local variables in this scope.
    /// </summary>
    public void ClearLocalVariables()
    {
        LocalVariables.Clear();
    }

    /// <summary>
    /// Gets all variables visible in this scope (including inherited).
    /// </summary>
    /// <returns>Dictionary with all visible variables.</returns>
    public Dictionary<string, object> GetAllVisibleVariables()
    {
        var result = new Dictionary<string, object>();

        // Get parent variables first (so they can be overridden)
        if (ParentScope != null)
        {
            var parentVars = ParentScope.GetAllVisibleVariables();
            foreach (var kvp in parentVars)
            {
                result[kvp.Key] = kvp.Value;
            }
        }

        // Override with local variables
        foreach (var kvp in LocalVariables)
        {
            result[kvp.Key] = kvp.Value;
        }

        return result;
    }

    /// <summary>
    /// Gets the depth of this scope in the chain (0 for global, 1 for direct child, etc).
    /// </summary>
    public int GetDepth()
    {
        int depth = 0;
        var current = ParentScope;
        while (current != null)
        {
            depth++;
            current = current.ParentScope;
        }
        return depth;
    }
}

/// <summary>
/// Types of variable scopes.
/// </summary>
public enum VariableScopeType
{
    /// <summary>Global scope shared across entire workflow execution.</summary>
    Global = 0,

    /// <summary>Thread scope shared across a process thread execution.</summary>
    Thread = 1,

    /// <summary>Node scope local to a specific node execution.</summary>
    Node = 2,

    /// <summary>Loop scope local to a loop iteration.</summary>
    Loop = 3,

    /// <summary>Try-catch scope for exception handling.</summary>
    TryCatch = 4
}
