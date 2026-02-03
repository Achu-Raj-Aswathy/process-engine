namespace BizFirst.Ai.ProcessEngine.Service.ContextManagement;

using System.Collections.Generic;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Context;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Memory;

/// <summary>
/// Service for creating and managing execution contexts.
/// Creates context objects for processes, threads, and elements.
/// </summary>
public interface IExecutionContextManager
{
    /// <summary>
    /// Create a process execution context.
    /// </summary>
    /// <param name="processID">ID of the process.</param>
    /// <param name="inputData">Input data for the process.</param>
    /// <returns>New process execution context.</returns>
    ProcessExecutionContext CreateProcessExecutionContext(int processID, Dictionary<string, object> inputData);

    /// <summary>
    /// Create a process thread execution context.
    /// </summary>
    /// <param name="processThreadID">ID of the thread.</param>
    /// <param name="parentProcessContext">Parent process context.</param>
    /// <param name="inputData">Input data for the thread.</param>
    /// <returns>New thread execution context.</returns>
    ProcessThreadExecutionContext CreateProcessThreadExecutionContext(
        int processThreadID,
        ProcessExecutionContext parentProcessContext,
        Dictionary<string, object> inputData);
}
