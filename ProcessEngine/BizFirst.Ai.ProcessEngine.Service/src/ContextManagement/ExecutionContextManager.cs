namespace BizFirst.Ai.ProcessEngine.Service.ContextManagement;

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Context;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Memory;
using BizFirst.Ai.AiSession.Domain.Session.Accessors;

/// <summary>
/// Implementation of IExecutionContextManager.
/// Creates execution contexts for processes, threads, and elements.
/// </summary>
public class ExecutionContextManager : IExecutionContextManager
{
    private readonly IAiSessionContextAccessor _contextAccessor;
    private readonly ILogger<ExecutionContextManager> _logger;

    /// <summary>Initializes a new instance.</summary>
    public ExecutionContextManager(
        IAiSessionContextAccessor contextAccessor,
        ILogger<ExecutionContextManager> logger)
    {
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public ProcessExecutionContext CreateProcessExecutionContext(
        int processID,
        Dictionary<string, object> inputData)
    {
        _logger.LogDebug("Creating ProcessExecutionContext for Process {ProcessID}", processID);

        var context = new ProcessExecutionContext
        {
            ProcessID = processID,
            InputData = inputData ?? new Dictionary<string, object>(),
            RequestSession = _contextAccessor.CurrentRequestSession,
            Memory = new ExecutionMemory(inputData ?? new())
        };

        return context;
    }

    /// <inheritdoc/>
    public ProcessThreadExecutionContext CreateProcessThreadExecutionContext(
        int processThreadID,
        ProcessExecutionContext parentProcessContext,
        Dictionary<string, object> inputData)
    {
        _logger.LogDebug("Creating ProcessThreadExecutionContext for Thread {ThreadID}", processThreadID);

        var context = new ProcessThreadExecutionContext
        {
            ProcessThreadID = processThreadID,
            ParentProcessContext = parentProcessContext,
            InputData = inputData ?? new Dictionary<string, object>(),
            Memory = new ExecutionMemory(inputData ?? new())
        };

        return context;
    }
}
