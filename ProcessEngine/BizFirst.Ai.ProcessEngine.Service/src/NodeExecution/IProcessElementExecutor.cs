namespace BizFirst.Ai.ProcessEngine.Service.NodeExecution;

using System.Threading;
using System.Threading.Tasks;
using BizFirst.Ai.ProcessEngine.Domain.Execution;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Context;
using BizFirst.Ai.ProcessEngine.Domain.Definition;

/// <summary>
/// Service for executing individual process elements (nodes).
/// Dispatches to appropriate executor based on node type and handles timeouts/retries.
/// </summary>
public interface IProcessElementExecutor
{
    /// <summary>
    /// Execute a process element.
    /// </summary>
    /// <param name="elementContext">Context for the element execution.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of element execution.</returns>
    Task<NodeExecutionResult> ExecuteAsync(
        ProcessElementExecutionContext elementContext,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if an element should be executed based on conditions.
    /// </summary>
    /// <param name="elementDefinition">Element definition.</param>
    /// <param name="threadContext">Thread execution context.</param>
    /// <returns>True if element should be executed.</returns>
    Task<bool> ShouldExecuteElementAsync(
        ProcessElementDefinition elementDefinition,
        ProcessThreadExecutionContext threadContext);
}
