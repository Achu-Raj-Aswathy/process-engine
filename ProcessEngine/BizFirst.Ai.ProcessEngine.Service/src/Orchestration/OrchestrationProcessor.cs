namespace BizFirst.Ai.ProcessEngine.Service.Orchestration;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BizFirst.Ai.ProcessEngine.Domain.Execution;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Context;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Memory;
using BizFirst.Ai.ProcessEngine.Domain.Execution.State;
using BizFirst.Ai.ProcessEngine.Domain.Definition;
using BizFirst.Ai.Process.Domain.Entities;
using BizFirst.Ai.ProcessEngine.Service.Definition;
using BizFirst.Ai.ProcessEngine.Service.NodeExecution;
using BizFirst.Ai.ProcessEngine.Service.ExecutionRouting;
using BizFirst.Ai.ProcessEngine.Service.ErrorHandling;
using BizFirst.Ai.ProcessEngine.Service.Persistence;
using BizFirst.Ai.Process.Domain.Interfaces.Services;
using BizFirstFi.Go.Essentials.Domain.Requests;
using BizFirst.Ai.ProcessEngine.Service.Execution.Events;
using BizFirst.Ai.ProcessEngine.Service.Execution.Tracing;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Tracing;

/// <summary>
/// Implementation of IOrchestrationProcessor using stack-based execution model.
/// Orchestrates the execution of process threads by managing a stack of nodes to execute.
/// </summary>
public class OrchestrationProcessor : IOrchestrationProcessor
{
    private readonly IProcessThreadLoader _definitionLoader;
    private readonly IProcessElementExecutor _elementExecutor;
    private readonly IExecutionRouter _executionRouter;
    private readonly IExecutionErrorHandler _errorHandler;
    private readonly IExecutionStateService _executionStateService;
    private readonly IProcessThreadExecutionService _threadExecutionService;
    private readonly IEnumerable<IExecutionEventHandler> _eventHandlers;
    private readonly IExecutionTracingService _tracingService;
    private readonly ILogger<OrchestrationProcessor> _logger;

    // Store current execution state for pause/resume
    private Stack<ProcessElementDefinition>? _currentExecutionStack;
    private ExecutionMemory? _currentExecutionMemory;

    // Track pause requests for active executions: ProcessExecutionID -> IsPauseRequested
    private static readonly Dictionary<int, bool> _pauseSignals = new();
    // Track active execution contexts: ProcessExecutionID -> (ProcessThreadExecutionID, ExecutionStack, ExecutionMemory)
    private static readonly Dictionary<int, (int ThreadExecutionID, Stack<ProcessElementDefinition> Stack, ExecutionMemory Memory)> _activeExecutions = new();

    /// <summary>Initializes a new instance of OrchestrationProcessor.</summary>
    public OrchestrationProcessor(
        IProcessThreadLoader definitionLoader,
        IProcessElementExecutor elementExecutor,
        IExecutionRouter executionRouter,
        IExecutionErrorHandler errorHandler,
        IExecutionStateService executionStateService,
        IProcessThreadExecutionService threadExecutionService,
        IEnumerable<IExecutionEventHandler> eventHandlers,
        IExecutionTracingService tracingService,
        ILogger<OrchestrationProcessor> logger)
    {
        _definitionLoader = definitionLoader ?? throw new ArgumentNullException(nameof(definitionLoader));
        _elementExecutor = elementExecutor ?? throw new ArgumentNullException(nameof(elementExecutor));
        _executionRouter = executionRouter ?? throw new ArgumentNullException(nameof(executionRouter));
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        _executionStateService = executionStateService ?? throw new ArgumentNullException(nameof(executionStateService));
        _threadExecutionService = threadExecutionService ?? throw new ArgumentNullException(nameof(threadExecutionService));
        _eventHandlers = eventHandlers ?? throw new ArgumentNullException(nameof(eventHandlers));
        _tracingService = tracingService ?? throw new ArgumentNullException(nameof(tracingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<ProcessExecution> ExecuteProcessAsync(
        int processID,
        ProcessExecutionContext executionContext,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting execution of Process {ProcessID}", processID);

        executionContext.ProcessID = processID;
        executionContext.StartedAtUtc = DateTime.UtcNow;
        executionContext.CurrentExecutionState = eExecutionState.Running;

        try
        {
            // For now, implementation is basic
            // Full implementation will handle multiple threads in process
            _logger.LogInformation("Process {ProcessID} execution completed", processID);

            // Create ProcessExecution entity to persist
            var processExecution = new ProcessExecution
            {
                ProcessID = processID,
                ExecutionModeID = (int)executionContext.CurrentExecutionState,
                ExecutionStatusID = 1, // TODO: Map eExecutionState to ExecutionStatusID
                StartedAt = executionContext.StartedAtUtc,
                StoppedAt = DateTime.UtcNow,
                Duration = (int)(DateTime.UtcNow - executionContext.StartedAtUtc).TotalMilliseconds,
                InputData = System.Text.Json.JsonSerializer.Serialize(executionContext.InputData),
                OutputData = System.Text.Json.JsonSerializer.Serialize(new { }),
                TotalNodes = executionContext.ThreadContexts.Count,
                CompletedNodes = executionContext.ThreadContexts.Count
            };

            return processExecution;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Process {ProcessID}", processID);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ProcessThreadExecution> ExecuteProcessThreadAsync(
        int processThreadID,
        int processThreadVersionID,
        ProcessThreadExecutionContext executionContext,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting execution of ProcessThread {ProcessThreadID} Version {VersionID}",
            processThreadID, processThreadVersionID);

        executionContext.ProcessThreadID = processThreadID;
        executionContext.ProcessThreadVersionID = processThreadVersionID;
        executionContext.StartedAtUtc = DateTime.UtcNow;
        executionContext.State = eExecutionState.Running;

        try
        {
            // Load workflow definition
            var threadDefinition = await _definitionLoader.LoadProcessThreadAsync(
                processThreadVersionID, cancellationToken);

            executionContext.TotalNodeCount = threadDefinition.Elements.Count;

            // Initialize execution stack with trigger nodes
            var executionStack = new Stack<ProcessElementDefinition>();
            var triggerNodes = threadDefinition.GetTriggerNodes();

            _logger.LogInformation(
                "Found {TriggerNodeCount} trigger nodes for ProcessThread {ProcessThreadID}",
                triggerNodes.Count, processThreadID);

            foreach (var triggerNode in triggerNodes)
            {
                executionStack.Push(triggerNode);
            }

            // Execute nodes in stack-based fashion
            var nodeResults = new List<NodeExecutionResult>();
            var processExecutionID = executionContext.ParentProcessContext?.ProcessExecutionID ?? executionContext.ProcessThreadExecutionID;

            // Register this execution as active for pause/resume support
            _activeExecutions[processExecutionID] = (executionContext.ProcessThreadExecutionID, executionStack, executionContext.Memory);
            _pauseSignals[processExecutionID] = false;

            // Notify event handlers that workflow is starting
            await RaiseWorkflowStartingEventAsync(executionContext, cancellationToken);

            // Create execution trace for debugging and monitoring (create outside try so it's available in catch)
            var executionTrace = _tracingService.CreateTrace(
                executionContext.ParentProcessContext?.ProcessExecutionID ?? executionContext.ProcessThreadExecutionID,
                executionContext.ProcessThreadExecutionID);

            try

            {
                while (executionStack.Count > 0)
                {
                    // Check for cancellation
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogWarning("Execution cancelled for ProcessThread {ProcessThreadID}", processThreadID);
                        executionContext.State = eExecutionState.Cancelled;
                        break;
                    }

                    // Check for pause request
                    if (_pauseSignals.TryGetValue(processExecutionID, out var isPauseRequested) && isPauseRequested)
                    {
                        _logger.LogInformation("Pause requested for ProcessThread {ProcessThreadID}, saving state", processThreadID);

                        // Save the current execution state
                        await _executionStateService.SaveStackStateAsync(
                            executionContext.ProcessThreadExecutionID,
                            executionStack,
                            cancellationToken);

                        await _executionStateService.SaveMemoryStateAsync(
                            executionContext.ProcessThreadExecutionID,
                            executionContext.Memory,
                            cancellationToken);

                        executionContext.State = eExecutionState.Paused;
                        _pauseSignals[processExecutionID] = false; // Reset pause signal
                        break;
                    }

                    var currentElement = executionStack.Pop();

                    _logger.LogDebug("Executing element {ElementKey}", currentElement.ProcessElementKey);

                    // Create element context
                    var elementContext = CreateElementExecutionContext(currentElement, executionContext);

                    // Notify event handlers that node is executing
                    await RaiseNodeExecutingEventAsync(elementContext, cancellationToken);

                    // Execute element with retry logic and timeout handling
                    try
                    {
                        // Define retry policy (could be made configurable per node type)
                        var retryPolicy = RetryPolicy.DefaultActionPolicy();

                        // Create cancellation token with timeout
                        var timeoutSeconds = currentElement.TimeoutSeconds;
                        var executionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        executionCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

                        _logger.LogDebug(
                            "Executing element {ElementKey} with timeout {TimeoutSeconds}s",
                            currentElement.ProcessElementKey, timeoutSeconds);

                        NodeExecutionResult result = null;
                        try
                        {
                            // Execute with error handling and retries
                            result = await _elementExecutor.ExecuteAsync(elementContext, executionCts.Token);
                            nodeResults.Add(result);
                        }
                        finally
                        {
                            executionCts.Dispose();
                        }

                        // Update execution memory with node output
                        executionContext.Memory.SetNodeOutput(currentElement.ProcessElementKey, result.OutputData);

                        // Notify event handlers that node has executed
                        await RaiseNodeExecutedEventAsync(result, elementContext, cancellationToken);

                        // Record node execution trace for debugging
                        var nodeTrace = new NodeExecutionTrace
                        {
                            ElementKey = currentElement.ProcessElementKey,
                            ElementType = currentElement.ProcessElementTypeName,
                            ExecutionSequence = executionContext.CompletedNodeCount + 1,
                            OutputPortKey = result.OutputPortKey,
                            OutputDataSnapshot = result.OutputData != null ?
                                System.Text.Json.JsonSerializer.Serialize(result.OutputData).Substring(0, Math.Min(1000, System.Text.Json.JsonSerializer.Serialize(result.OutputData).Length))
                                : null
                        };
                        nodeTrace.Complete(result.IsSuccess ? "Success" : "Failed");
                        await _tracingService.RecordNodeTraceAsync(executionTrace.TraceId, nodeTrace, cancellationToken);

                        // Check for loop control signals (break/continue)
                        if (executionContext.Memory.LoopBreakSignal)
                        {
                            _logger.LogInformation("Loop break signal detected for element {ElementKey}",
                                currentElement.ProcessElementKey);
                            executionContext.Memory.ClearLoopSignals();
                            // Don't add any downstream nodes - break out of loop
                            executionContext.CompletedNodeCount++;
                            continue; // Skip to next iteration of main loop
                        }

                        if (executionContext.Memory.LoopContinueSignal)
                        {
                            _logger.LogInformation("Loop continue signal detected for element {ElementKey}",
                                currentElement.ProcessElementKey);
                            executionContext.Memory.ClearLoopSignals();
                            // Continue to next iteration of loop without processing normal routing
                            executionContext.CompletedNodeCount++;
                            continue; // Skip to next iteration of main loop
                        }

                        // Route to next nodes based on output (normal flow when no loop signals)
                        var nextNodes = _executionRouter.GetDownstreamNodesForOutputPort(
                            currentElement, result.OutputPortKey, threadDefinition);

                        _logger.LogDebug(
                            "Element {ElementKey} completed, routing to {NextNodeCount} downstream nodes",
                            currentElement.ProcessElementKey, nextNodes.Count);

                        foreach (var nextNode in nextNodes)
                        {
                            executionStack.Push(nextNode);
                        }

                        executionContext.CompletedNodeCount++;
                    }
                    catch (OperationCanceledException ex)
                    {
                        // Timeout occurred - create timeout exception
                        var timeoutException = new TimeoutException(
                            $"Node execution '{currentElement.ProcessElementKey}' exceeded timeout of {currentElement.TimeoutSeconds}s",
                            ex);

                        _logger.LogWarning(
                            ex,
                            "Node execution timeout - {ElementKey} (TimeoutSeconds: {TimeoutSeconds}, Behavior: {Behavior})",
                            currentElement.ProcessElementKey,
                            currentElement.TimeoutSeconds,
                            currentElement.TimeoutBehavior);

                        // Record error trace for debugging
                        var errorTrace = new ErrorTrace
                        {
                            ElementKey = currentElement.ProcessElementKey,
                            ErrorType = "Timeout",
                            Message = timeoutException.Message,
                            StackTrace = timeoutException.StackTrace,
                            Severity = "Error"
                        };
                        await _tracingService.RecordErrorTraceAsync(executionTrace.TraceId, errorTrace, cancellationToken);

                        // Handle timeout based on configured behavior
                        var timeoutBehavior = currentElement.TimeoutBehavior ?? "Error";

                        switch (timeoutBehavior.ToLowerInvariant())
                        {
                            case "skip":
                                // Skip the node and continue with downstream nodes
                                _logger.LogInformation("Timeout behavior 'Skip': Skipping node {ElementKey}", currentElement.ProcessElementKey);
                                executionContext.Memory.SetNodeOutput(currentElement.ProcessElementKey, new Dictionary<string, object>());

                                // Route to next nodes with success output port
                                var nextNodesSkip = _executionRouter.GetDownstreamNodesForOutputPort(
                                    currentElement, "success", threadDefinition);
                                foreach (var nextNode in nextNodesSkip)
                                {
                                    executionStack.Push(nextNode);
                                }
                                executionContext.CompletedNodeCount++;
                                break;

                            case "cancel":
                                // Cancel execution and stop immediately
                                _logger.LogWarning("Timeout behavior 'Cancel': Stopping execution at node {ElementKey}", currentElement.ProcessElementKey);
                                executionContext.State = eExecutionState.Cancelled;
                                break;

                            case "retry":
                                // Retry execution using existing retry policy
                                _logger.LogInformation("Timeout behavior 'Retry': Retrying node {ElementKey}", currentElement.ProcessElementKey);

                                // Create error context with timeout exception
                                var errorContextRetry = ExecutionErrorContext.CreateFromException(
                                    currentElement.ProcessElementID,
                                    currentElement.ProcessElementKey,
                                    currentElement.ProcessElementTypeName ?? "Unknown",
                                    timeoutException,
                                    currentAttempt: 1,
                                    maxRetries: currentElement.Element?.MaxRetries ?? 3);

                                _errorHandler.LogError(errorContextRetry);

                                // Re-execute with retry policy that allows timeouts
                                var retryPolicy = RetryPolicy.DefaultActionPolicy();
                                var result = await _errorHandler.HandleErrorAsync(
                                    errorContextRetry,
                                    retryPolicy,
                                    async _ =>
                                    {
                                        // Retry action - re-execute the node
                                        var retryResult = await _elementExecutor.ExecuteAsync(elementContext, cancellationToken);
                                        nodeResults.Add(retryResult);
                                        executionContext.Memory.SetNodeOutput(currentElement.ProcessElementKey, retryResult.OutputData);

                                        // Route next nodes
                                        var nextNodesRetry = _executionRouter.GetDownstreamNodesForOutputPort(
                                            currentElement, retryResult.OutputPortKey, threadDefinition);
                                        foreach (var nextNode in nextNodesRetry)
                                        {
                                            executionStack.Push(nextNode);
                                        }
                                    },
                                    cancellationToken);

                                if (!result.ShouldContinue)
                                {
                                    executionContext.State = eExecutionState.Failed;
                                    throw new InvalidOperationException(
                                        $"Execution failed at element {currentElement.ProcessElementKey}: Retry exhausted after {currentElement.TimeoutSeconds}s timeout",
                                        timeoutException);
                                }
                                executionContext.CompletedNodeCount++;
                                break;

                            case "error":
                            default:
                                // Default behavior: Route to error output port
                                _logger.LogWarning("Timeout behavior 'Error': Routing to error port for {ElementKey}", currentElement.ProcessElementKey);

                                var errorContext = ExecutionErrorContext.CreateFromException(
                                    currentElement.ProcessElementID,
                                    currentElement.ProcessElementKey,
                                    currentElement.ProcessElementTypeName ?? "Unknown",
                                    timeoutException,
                                    currentAttempt: 0,
                                    maxRetries: 0);

                                _errorHandler.LogError(errorContext);

                                // Route to error output port
                                var nextNodesError = _executionRouter.GetDownstreamNodesForOutputPort(
                                    currentElement, "error", threadDefinition);
                                foreach (var nextNode in nextNodesError)
                                {
                                    executionStack.Push(nextNode);
                                }
                                executionContext.CompletedNodeCount++;
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Notify event handlers of error
                        await RaiseErrorEventAsync(elementContext, ex, cancellationToken);

                        // Check if we're in a try-catch-finally block
                        var exceptionContext = executionContext.Memory.GetCurrentExceptionContext();
                        if (exceptionContext != null)
                        {
                            // We're in a try block - check if there's a catch handler
                            _logger.LogInformation(
                                "Exception in try block: {ExceptionType} - {Message}",
                                ex.GetType().Name, ex.Message);

                            var catchBlockKey = executionContext.Memory.FindCatchHandler(ex.GetType());
                            if (catchBlockKey != null)
                            {
                                // Found a matching catch handler
                                _logger.LogInformation(
                                    "Routing to catch block: {CatchBlockKey}",
                                    catchBlockKey);

                                // Store exception in memory for catch block access
                                executionContext.Memory.CurrentException = ex;
                                exceptionContext.ExceptionOccurred = true;
                                exceptionContext.Exception = ex;

                                // Mark that exception was caught (won't rethrow after finally)
                                // Route to catch block
                                var catchBlock = threadDefinition.Elements.FirstOrDefault(e => e.ProcessElementKey == catchBlockKey);
                                if (catchBlock != null)
                                {
                                    executionStack.Push(catchBlock);
                                    executionContext.CompletedNodeCount++;

                                    // Queue finally block to execute after catch (if it exists)
                                    if (!string.IsNullOrEmpty(exceptionContext.FinallyBlockKey))
                                    {
                                        var finallyBlock = threadDefinition.Elements.FirstOrDefault(
                                            e => e.ProcessElementKey == exceptionContext.FinallyBlockKey);
                                        if (finallyBlock != null)
                                        {
                                            executionStack.Push(finallyBlock);
                                        }
                                    }
                                    continue; // Skip normal error handling, proceed to catch block execution
                                }
                            }
                            else
                            {
                                // No catch handler matched - queue finally and rethrow
                                _logger.LogInformation("No matching catch handler, queuing finally block and rethrowing");

                                executionContext.Memory.CurrentException = ex;
                                exceptionContext.ExceptionOccurred = true;
                                exceptionContext.Exception = ex;

                                // Queue finally block if it exists
                                if (!string.IsNullOrEmpty(exceptionContext.FinallyBlockKey))
                                {
                                    var finallyBlock = threadDefinition.Elements.FirstOrDefault(
                                        e => e.ProcessElementKey == exceptionContext.FinallyBlockKey);
                                    if (finallyBlock != null)
                                    {
                                        executionStack.Push(finallyBlock);
                                        executionContext.CompletedNodeCount++;
                                        continue; // Execute finally block, then exception will propagate
                                    }
                                }

                                // No finally block - just rethrow
                                throw;
                            }
                        }

                        // Not in try-catch-finally context - use standard error handling with retry
                        _logger.LogWarning(
                            ex,
                            "Exception in element {ElementKey}: {ExceptionType}",
                            currentElement.ProcessElementKey, ex.GetType().Name);

                        // Create error context
                        var errorContext = ExecutionErrorContext.CreateFromException(
                            currentElement.ProcessElementID,
                            currentElement.ProcessElementKey,
                            currentElement.ProcessElementTypeName ?? "Unknown",
                            ex,
                            currentAttempt: 0,
                            maxRetries: 3);

                        _errorHandler.LogError(errorContext);

                        // Attempt to handle with retry
                        var retryPolicy = RetryPolicy.DefaultActionPolicy();
                        var errorResult = await _errorHandler.HandleErrorAsync(
                            errorContext,
                            retryPolicy,
                            async (attemptNumber) =>
                            {
                                // Re-create element context for retry
                                var retryElementContext = CreateElementExecutionContext(currentElement, executionContext);
                                var result = await _elementExecutor.ExecuteAsync(retryElementContext, cancellationToken);
                                nodeResults.Add(result);
                                executionContext.Memory.SetNodeOutput(currentElement.ProcessElementKey, result.OutputData);

                                // Route next nodes
                                var nextNodes = _executionRouter.GetDownstreamNodesForOutputPort(
                                    currentElement, result.OutputPortKey, threadDefinition);

                                foreach (var nextNode in nextNodes)
                                {
                                    executionStack.Push(nextNode);
                                }

                                executionContext.CompletedNodeCount++;
                            },
                            cancellationToken);

                        if (!errorResult.ShouldContinue)
                        {
                            // Error cannot be recovered - stop execution
                            executionContext.State = eExecutionState.Failed;
                            throw new InvalidOperationException(
                                $"Execution failed at element {currentElement.ProcessElementKey}: {errorContext.ErrorMessage}",
                                errorResult.FinalException);
                        }
                    }
                }

                // If not paused or cancelled, mark as completed
                if (executionContext.State == eExecutionState.Running)
                {
                    executionContext.State = eExecutionState.Completed;
                }
            }
            finally
            {
                // Cleanup active execution tracking (unless paused)
                if (executionContext.State != eExecutionState.Paused)
                {
                    _activeExecutions.Remove(processExecutionID);
                    _pauseSignals.Remove(processExecutionID);
                }
            }

            // Create ProcessThreadExecution entity to persist
            var threadExecution = new ProcessThreadExecution
            {
                ProcessThreadID = processThreadID,
                ProcessThreadVersionID = processThreadVersionID,
                ExecutionModeID = 1, // TODO: Map from context
                ExecutionStatusID = MapExecutionStateToStatusID(executionContext.State),
                StartedAt = executionContext.StartedAtUtc,
                StoppedAt = executionContext.State == eExecutionState.Running ? null : DateTime.UtcNow,
                Duration = executionContext.State == eExecutionState.Running ? null : (int?)(DateTime.UtcNow - executionContext.StartedAtUtc).TotalMilliseconds,
                InputData = System.Text.Json.JsonSerializer.Serialize(executionContext.InputData),
                OutputData = System.Text.Json.JsonSerializer.Serialize(executionContext.OutputData),
                TotalNodes = executionContext.TotalNodeCount,
                CompletedNodes = executionContext.CompletedNodeCount
            };

            // Notify event handlers that workflow has completed
            await RaiseWorkflowCompletedEventAsync(threadExecution, executionContext, cancellationToken);

            // Complete execution trace
            var traceStatus = executionContext.State switch
            {
                eExecutionState.Completed => "Completed",
                eExecutionState.Failed => "Failed",
                eExecutionState.Cancelled => "Cancelled",
                eExecutionState.Paused => "Paused",
                _ => "Unknown"
            };
            await _tracingService.CompleteTraceAsync(executionTrace.TraceId, traceStatus, cancellationToken);

            return threadExecution;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing ProcessThread {ProcessThreadID}", processThreadID);
            executionContext.State = eExecutionState.Failed;

            // Record error trace and complete execution trace
            try
            {
                var errorTrace = new ErrorTrace
                {
                    ErrorType = ex.GetType().Name,
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    Severity = "Critical"
                };
                await _tracingService.RecordErrorTraceAsync(executionTrace.TraceId, errorTrace, cancellationToken);
                await _tracingService.CompleteTraceAsync(executionTrace.TraceId, "Failed", cancellationToken);
            }
            catch
            {
                // Don't let tracing failures prevent error propagation
            }

            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> PauseExecutionAsync(
        int processExecutionID,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Pausing execution {ProcessExecutionID}", processExecutionID);

        try
        {
            // Check if this execution is currently active
            if (!_activeExecutions.ContainsKey(processExecutionID))
            {
                _logger.LogWarning("Execution {ProcessExecutionID} is not currently active", processExecutionID);
                return await Task.FromResult(false);
            }

            // Set pause signal - the execution loop will detect this and save state
            _pauseSignals[processExecutionID] = true;

            _logger.LogInformation("Pause signal set for execution {ProcessExecutionID}, waiting for graceful pause", processExecutionID);

            // Wait briefly for the execution loop to process the pause signal
            // In a production system, this could be replaced with a completion signal
            await Task.Delay(100, cancellationToken);

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pause execution {ProcessExecutionID}", processExecutionID);
            return await Task.FromResult(false);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ResumeExecutionAsync(
        int processExecutionID,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Resuming execution {ProcessExecutionID}", processExecutionID);

        try
        {
            // Load the ProcessThreadExecution for this process
            var request = new BizFirst.Ai.Process.Domain.WebRequests.ProcessThreadExecution.GetByProcessExecutionSearchRequest
            {
                ProcessExecutionID = processExecutionID
            };
            var response = await _threadExecutionService.GetByProcessExecutionAsync(request, cancellationToken);

            if (response?.Data is not IEnumerable<object> threadList || !threadList.Any())
            {
                _logger.LogWarning("No thread execution found for ProcessExecutionID {ProcessExecutionID}", processExecutionID);
                return false;
            }

            if (threadList.FirstOrDefault() is not ProcessThreadExecution threadExecution)
            {
                return false;
            }

            _logger.LogDebug("Loading saved execution state for ProcessThreadExecutionID {ThreadExecutionID}",
                threadExecution.ProcessThreadExecutionID);

            // Load workflow definition
            var threadDefinition = await _definitionLoader.LoadProcessThreadAsync(
                threadExecution.ProcessThreadVersionID.Value, cancellationToken);

            // Load saved stack state
            var restoredStack = await _executionStateService.LoadStackStateAsync(
                threadExecution.ProcessThreadExecutionID,
                threadDefinition,
                cancellationToken);

            // Load saved memory state
            var restoredMemory = await _executionStateService.LoadMemoryStateAsync(
                threadExecution.ProcessThreadExecutionID,
                cancellationToken);

            // Store restored state for execution
            _currentExecutionStack = restoredStack;
            _currentExecutionMemory = restoredMemory;

            // Update status to running
            threadExecution.ExecutionStatusID = 1;  // Running status ID
            var updateRequest = new BizFirstFi.Go.Essentials.Domain.Requests.UpdateWebRequest { Data = threadExecution };
            await _threadExecutionService.UpdateAsync(updateRequest, cancellationToken);

            _logger.LogInformation(
                "Execution {ProcessExecutionID} resumed successfully with {StackDepth} pending nodes",
                processExecutionID, restoredStack.Count);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resume execution {ProcessExecutionID}", processExecutionID);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> CancelExecutionAsync(
        int processExecutionID,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cancelling execution {ProcessExecutionID}", processExecutionID);

        try
        {
            // Load the ProcessThreadExecution for this process
            var request = new BizFirst.Ai.Process.Domain.WebRequests.ProcessThreadExecution.GetByProcessExecutionSearchRequest
            {
                ProcessExecutionID = processExecutionID
            };
            var response = await _threadExecutionService.GetByProcessExecutionAsync(request, cancellationToken);

            if (response?.Data is not IEnumerable<object> threadList || !threadList.Any())
            {
                _logger.LogWarning("No thread execution found for ProcessExecutionID {ProcessExecutionID}", processExecutionID);
                return false;
            }

            if (threadList.FirstOrDefault() is not ProcessThreadExecution threadExecution)
            {
                return false;
            }

            // Update status to cancelled
            threadExecution.ExecutionStatusID = 5;  // Cancelled status ID
            threadExecution.StoppedAt = DateTime.UtcNow;
            var updateRequest = new BizFirstFi.Go.Essentials.Domain.Requests.UpdateWebRequest { Data = threadExecution };
            await _threadExecutionService.UpdateAsync(updateRequest, cancellationToken);

            // Mark saved state as inactive (cleanup)
            await _executionStateService.MarkStateInactiveAsync(
                threadExecution.ProcessThreadExecutionID,
                cancellationToken);

            // Clear execution stack and memory
            _currentExecutionStack?.Clear();
            _currentExecutionMemory = new ExecutionMemory(new Dictionary<string, object>());

            _logger.LogInformation("Execution {ProcessExecutionID} cancelled successfully", processExecutionID);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel execution {ProcessExecutionID}", processExecutionID);
            return false;
        }
    }

    /// <summary>Maps execution state to database ExecutionStatusID.</summary>
    private int MapExecutionStateToStatusID(eExecutionState state)
    {
        // Status ID mapping:
        // 1 = Running, 2 = Paused, 3 = Completed, 4 = Failed, 5 = Cancelled
        return state switch
        {
            eExecutionState.Running => 1,
            eExecutionState.Paused => 2,
            eExecutionState.Completed => 3,
            eExecutionState.Failed => 4,
            eExecutionState.Cancelled => 5,
            _ => 1  // Default to Running
        };
    }

    /// <summary>Creates an execution context for a process element.</summary>
    private ProcessElementExecutionContext CreateElementExecutionContext(
        ProcessElementDefinition elementDefinition,
        ProcessThreadExecutionContext threadContext)
    {
        return new ProcessElementExecutionContext
        {
            ProcessElementID = elementDefinition.ProcessElementID,
            ProcessElementKey = elementDefinition.ProcessElementKey,
            ProcessThreadExecutionID = threadContext.ProcessThreadExecutionID,
            ParentThreadContext = threadContext,
            ExecutionOrder = threadContext.CompletedNodeCount + 1,
            ElementDefinition = elementDefinition,
            InputData = new Dictionary<string, object>()
        };
    }

    /// <summary>Raises the workflow starting event for all registered event handlers.</summary>
    private async Task RaiseWorkflowStartingEventAsync(
        ProcessThreadExecutionContext context,
        CancellationToken cancellationToken)
    {
        foreach (var handler in _eventHandlers)
        {
            try
            {
                await handler.OnWorkflowStartingAsync(context, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Event handler failed in OnWorkflowStartingAsync: {Handler}", handler.GetType().Name);
            }
        }
    }

    /// <summary>Raises the node executing event for all registered event handlers.</summary>
    private async Task RaiseNodeExecutingEventAsync(
        ProcessElementExecutionContext context,
        CancellationToken cancellationToken)
    {
        foreach (var handler in _eventHandlers)
        {
            try
            {
                await handler.OnNodeExecutingAsync(context, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Event handler failed in OnNodeExecutingAsync: {Handler}", handler.GetType().Name);
            }
        }
    }

    /// <summary>Raises the node executed event for all registered event handlers.</summary>
    private async Task RaiseNodeExecutedEventAsync(
        NodeExecutionResult result,
        ProcessElementExecutionContext context,
        CancellationToken cancellationToken)
    {
        foreach (var handler in _eventHandlers)
        {
            try
            {
                await handler.OnNodeExecutedAsync(result, context, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Event handler failed in OnNodeExecutedAsync: {Handler}", handler.GetType().Name);
            }
        }
    }

    /// <summary>Raises the error event for all registered event handlers.</summary>
    private async Task RaiseErrorEventAsync(
        ProcessElementExecutionContext context,
        Exception error,
        CancellationToken cancellationToken)
    {
        foreach (var handler in _eventHandlers)
        {
            try
            {
                await handler.OnErrorAsync(context, error, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Event handler failed in OnErrorAsync: {Handler}", handler.GetType().Name);
            }
        }
    }

    /// <summary>Raises the workflow completed event for all registered event handlers.</summary>
    private async Task RaiseWorkflowCompletedEventAsync(
        ProcessThreadExecution execution,
        ProcessThreadExecutionContext context,
        CancellationToken cancellationToken)
    {
        foreach (var handler in _eventHandlers)
        {
            try
            {
                await handler.OnWorkflowCompletedAsync(execution, context, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Event handler failed in OnWorkflowCompletedAsync: {Handler}", handler.GetType().Name);
            }
        }
    }
}
