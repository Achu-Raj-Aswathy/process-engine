namespace BizFirst.Ai.ProcessEngine.Service.Execution.Events;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BizFirst.Ai.ProcessEngine.Domain.Execution;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Context;
using BizFirst.Ai.Process.Domain.Entities;

/// <summary>
/// Publishes execution events to registered event handlers.
/// Manages subscriptions and broadcasts events during workflow execution.
/// </summary>
public class ExecutionEventPublisher
{
    private readonly List<IExecutionEventHandler> _handlers = new();
    private readonly ILogger<ExecutionEventPublisher> _logger;

    /// <summary>Initializes a new instance.</summary>
    public ExecutionEventPublisher(ILogger<ExecutionEventPublisher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Registers an event handler.</summary>
    public void Subscribe(IExecutionEventHandler handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        _handlers.Add(handler);
        _logger.LogDebug("Event handler {HandlerType} registered", handler.GetType().Name);
    }

    /// <summary>Unregisters an event handler.</summary>
    public void Unsubscribe(IExecutionEventHandler handler)
    {
        if (handler != null)
        {
            _handlers.Remove(handler);
            _logger.LogDebug("Event handler {HandlerType} unregistered", handler.GetType().Name);
        }
    }

    /// <summary>Publishes workflow starting event.</summary>
    public async Task PublishWorkflowStartingAsync(
        ProcessThreadExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Publishing WorkflowStarting event");

        foreach (var handler in _handlers)
        {
            try
            {
                await handler.OnWorkflowStartingAsync(context, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnWorkflowStarting handler {HandlerType}",
                    handler.GetType().Name);
                // Don't rethrow - continue with other handlers
            }
        }
    }

    /// <summary>Publishes node executing event.</summary>
    public async Task PublishNodeExecutingAsync(
        ProcessElementExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Publishing NodeExecuting event for {ElementKey}", context.ProcessElementKey);

        foreach (var handler in _handlers)
        {
            try
            {
                await handler.OnNodeExecutingAsync(context, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnNodeExecuting handler {HandlerType}",
                    handler.GetType().Name);
                // Don't rethrow - continue with other handlers
            }
        }
    }

    /// <summary>Publishes node executed event.</summary>
    public async Task PublishNodeExecutedAsync(
        NodeExecutionResult result,
        ProcessElementExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Publishing NodeExecuted event for {ElementKey}", context.ProcessElementKey);

        foreach (var handler in _handlers)
        {
            try
            {
                await handler.OnNodeExecutedAsync(result, context, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnNodeExecuted handler {HandlerType}",
                    handler.GetType().Name);
                // Don't rethrow - continue with other handlers
            }
        }
    }

    /// <summary>Publishes error event.</summary>
    public async Task PublishErrorAsync(
        ProcessElementExecutionContext elementContext,
        Exception error,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Publishing Error event for {ElementKey}: {ErrorType}",
            elementContext.ProcessElementKey, error.GetType().Name);

        foreach (var handler in _handlers)
        {
            try
            {
                await handler.OnErrorAsync(elementContext, error, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnError handler {HandlerType}",
                    handler.GetType().Name);
                // Don't rethrow - continue with other handlers
            }
        }
    }

    /// <summary>Publishes workflow completed event.</summary>
    public async Task PublishWorkflowCompletedAsync(
        ProcessThreadExecution execution,
        ProcessThreadExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Publishing WorkflowCompleted event");

        foreach (var handler in _handlers)
        {
            try
            {
                await handler.OnWorkflowCompletedAsync(execution, context, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnWorkflowCompleted handler {HandlerType}",
                    handler.GetType().Name);
                // Don't rethrow - continue with other handlers
            }
        }
    }

    /// <summary>Clears all registered handlers.</summary>
    public void ClearHandlers()
    {
        _handlers.Clear();
        _logger.LogDebug("All event handlers cleared");
    }

    /// <summary>Gets count of registered handlers.</summary>
    public int HandlerCount => _handlers.Count;
}
