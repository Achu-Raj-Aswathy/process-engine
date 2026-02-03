namespace BizFirst.Ai.ProcessEngine.Domain.Node.Interfaces;

using System.Threading;
using System.Threading.Tasks;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Context;
using BizFirst.Ai.ProcessEngine.Domain.Execution;

/// <summary>
/// Interface for integration nodes (API and external service calls).
/// Integration nodes call external APIs and services.
/// Examples: HTTP Request, REST API, GraphQL, SOAP, gRPC, etc.
/// </summary>
public interface IIntegrationNodeExecution : IProcessElementExecution
{
    /// <summary>
    /// Invoke the external service/API.
    /// </summary>
    /// <param name="executionContext">The execution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result from the service call.</returns>
    Task<NodeExecutionResult> InvokeExternalServiceAsync(
        ProcessElementExecutionContext executionContext,
        CancellationToken cancellationToken = default);
}
