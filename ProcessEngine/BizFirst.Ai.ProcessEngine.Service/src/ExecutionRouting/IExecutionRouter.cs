namespace BizFirst.Ai.ProcessEngine.Service.ExecutionRouting;

using System.Collections.Generic;
using System.Threading.Tasks;
using BizFirst.Ai.ProcessEngine.Domain.Definition;

/// <summary>
/// Service for determining which nodes to execute next based on routing decisions.
/// Handles conditional routing and multi-output nodes.
/// </summary>
public interface IExecutionRouter
{
    /// <summary>
    /// Get the next nodes to execute based on current node and output port.
    /// </summary>
    /// <param name="sourceElement">The current element that just executed.</param>
    /// <param name="outputPortKey">The output port key (e.g., "main", "error", "success").</param>
    /// <param name="threadDefinition">The thread definition containing all nodes and connections.</param>
    /// <returns>List of next elements to execute.</returns>
    List<ProcessElementDefinition> GetDownstreamNodesForOutputPort(
        ProcessElementDefinition sourceElement,
        string outputPortKey,
        ProcessThreadDefinition threadDefinition);

    /// <summary>
    /// Evaluate whether a conditional connection should be taken.
    /// </summary>
    /// <param name="conditionExpression">The condition expression to evaluate.</param>
    /// <param name="executionVariables">Variables available for expression evaluation.</param>
    /// <returns>True if condition evaluates to true.</returns>
    Task<bool> EvaluateConditionalRouteAsync(
        string conditionExpression,
        Dictionary<string, object> executionVariables);
}
