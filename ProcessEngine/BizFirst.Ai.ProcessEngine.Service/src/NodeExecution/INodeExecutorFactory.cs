namespace BizFirst.Ai.ProcessEngine.Service.NodeExecution;

using System;
using BizFirst.Ai.ProcessEngine.Domain.Node.Interfaces;

/// <summary>
/// Factory for creating appropriate executor for different node types.
/// Uses Strategy pattern to dispatch to correct executor implementation.
/// </summary>
public interface INodeExecutorFactory
{
    /// <summary>
    /// Get the executor for a specific node type.
    /// </summary>
    /// <param name="nodeTypeName">The node type code (e.g., "http-request", "if-condition").</param>
    /// <returns>The executor for this node type.</returns>
    /// <throws>InvalidOperationException if no executor registered for type.</throws>
    IProcessElementExecution GetExecutorForNodeType(string nodeTypeName);

    /// <summary>
    /// Register an executor for a node type.
    /// </summary>
    /// <param name="nodeTypeName">The node type code.</param>
    /// <param name="executorType">The executor class type.</param>
    void RegisterExecutor(string nodeTypeName, Type executorType);
}
