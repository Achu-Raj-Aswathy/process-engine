namespace BizFirst.Ai.ProcessEngine.Service.Definition;

using System.Threading;
using System.Threading.Tasks;
using BizFirst.Ai.ProcessEngine.Domain.Definition;

/// <summary>
/// Service for loading process thread definitions from database.
/// Provides caching for performance optimization.
/// </summary>
public interface IProcessThreadLoader
{
    /// <summary>
    /// Load a process thread definition from database.
    /// </summary>
    /// <param name="processThreadVersionID">ID of the thread version to load.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The process thread definition with all nodes and connections.</returns>
    Task<ProcessThreadDefinition> LoadProcessThreadAsync(
        int processThreadVersionID,
        CancellationToken cancellationToken = default);
}
