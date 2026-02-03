namespace BizFirst.Ai.ProcessEngine.Domain.Node.Interfaces;

using System.Threading;
using System.Threading.Tasks;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Context;
using BizFirst.Ai.ProcessEngine.Domain.Execution;

/// <summary>
/// Interface for sub-workflow nodes (nested workflow execution).
/// Sub-flow nodes execute other workflows recursively.
/// </summary>
public interface ISubFlowNodeExecution : IProcessElementExecution
{
    /// <summary>
    /// Execute a sub-workflow (nested workflow).
    /// </summary>
    /// <param name="executionContext">The execution context.</param>
    /// <param name="subProcessThreadID">ID of the sub-workflow to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result from the sub-workflow execution.</returns>
    Task<NodeExecutionResult> ExecuteSubWorkflowAsync(
        ProcessElementExecutionContext executionContext,
        int subProcessThreadID,
        CancellationToken cancellationToken = default);
}
