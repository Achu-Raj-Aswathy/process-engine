namespace BizFirst.Ai.ProcessEngine.Domain.Node.Interfaces;

using System.Threading;
using System.Threading.Tasks;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Context;

/// <summary>
/// Interface for decision/logic nodes (conditional branching).
/// Decision nodes evaluate conditions and route to appropriate branches.
/// Examples: If-Else, Switch, Filter, etc.
/// </summary>
public interface IDecisionNodeExecution : IProcessElementExecution
{
    /// <summary>
    /// Evaluate the condition and return which output port to use.
    /// </summary>
    /// <param name="executionContext">The execution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Key of the output port to route to (e.g., "true", "false").</returns>
    Task<string> EvaluateConditionAsync(
        ProcessElementExecutionContext executionContext,
        CancellationToken cancellationToken = default);
}
