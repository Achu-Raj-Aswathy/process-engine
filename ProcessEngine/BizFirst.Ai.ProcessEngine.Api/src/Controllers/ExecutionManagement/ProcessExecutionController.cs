namespace BizFirst.Ai.ProcessEngine.Api.Controllers.ExecutionManagement;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BizFirst.Ai.ProcessEngine.Api.Base.Controllers;
using BizFirst.Ai.ProcessEngine.Service.Orchestration;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Context;
using BizFirst.Ai.Process.Domain.Entities;
using BizFirst.Ai.Process.Domain.Interfaces.Services;
using BizFirstFi.Go.Essentials.Domain.Requests;

/// <summary>
/// API controller for process execution endpoints.
/// Handles execution of complete processes.
/// </summary>
[ApiController]
[Route("api/v1/process-engine/executions/processes")]
public class ProcessExecutionController : BaseProcessExecutionController
{
    private readonly IOrchestrationProcessor _orchestrationProcessor;
    private readonly IProcessExecutionService _processExecutionService;
    private readonly IProcessThreadExecutionService _threadExecutionService;
    private readonly IExecutionStatusService _statusService;
    private readonly ILogger<ProcessExecutionController> _logger;

    /// <summary>Initializes a new instance.</summary>
    public ProcessExecutionController(
        IOrchestrationProcessor orchestrationProcessor,
        IProcessExecutionService processExecutionService,
        IProcessThreadExecutionService threadExecutionService,
        IExecutionStatusService statusService,
        ILogger<ProcessExecutionController> logger)
    {
        _orchestrationProcessor = orchestrationProcessor ?? throw new ArgumentNullException(nameof(orchestrationProcessor));
        _processExecutionService = processExecutionService ?? throw new ArgumentNullException(nameof(processExecutionService));
        _threadExecutionService = threadExecutionService ?? throw new ArgumentNullException(nameof(threadExecutionService));
        _statusService = statusService ?? throw new ArgumentNullException(nameof(statusService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Execute a process.
    /// </summary>
    /// <param name="processID">ID of the process to execute.</param>
    /// <param name="inputData">Input data for the process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Execution result.</returns>
    [HttpPost("execute")]
    public async Task<IActionResult> ExecuteProcessAsync(
        [FromQuery] int processID,
        [FromBody] Dictionary<string, object>? inputData = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Received request to execute Process {ProcessID}", processID);

        try
        {
            // Create execution context
            var executionContext = new ProcessExecutionContext
            {
                ProcessID = processID,
                InputData = inputData ?? new Dictionary<string, object>(),
                ProcessExecutionID = Guid.NewGuid().GetHashCode() // Placeholder
            };

            // Execute process
            var result = await _orchestrationProcessor.ExecuteProcessAsync(
                processID,
                executionContext,
                cancellationToken);

            return ApiResponse(result, "Process execution completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Process {ProcessID}", processID);
            return ApiError($"Error executing process: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get execution status for a process.
    /// </summary>
    /// <param name="processExecutionID">ID of the execution to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Execution status.</returns>
    [HttpGet("{processExecutionID}/status")]
    public async Task<IActionResult> GetExecutionStatusAsync(
        int processExecutionID,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Received request to get execution status for {ExecutionID}", processExecutionID);

        try
        {
            // Load ProcessExecution
            var executionRequest = new GetByIdWebRequest
            {
                ID = new IDInfo { ID = processExecutionID }
            };
            var executionResponse = await _processExecutionService.GetByIdAsync(
                executionRequest, cancellationToken);

            if (executionResponse?.Data is not ProcessExecution execution)
            {
                _logger.LogWarning("ProcessExecution {ExecutionID} not found", processExecutionID);
                return ApiError("Execution not found", 404);
            }

            // Get execution status name
            var statusRequest = new GetByIdWebRequest
            {
                ID = new IDInfo { ID = execution.ExecutionStatusID }
            };
            var statusResponse = await _statusService.GetByIdAsync(
                statusRequest, cancellationToken);

            var statusName = (statusResponse?.Data as ExecutionStatus)?.Code ?? "unknown";

            // Load thread executions for progress
            var threadRequest = new BizFirst.Ai.Process.Domain.WebRequests.ProcessThreadExecution.GetByProcessExecutionSearchRequest
            {
                ProcessExecutionID = processExecutionID
            };
            var threadResponse = await _threadExecutionService.GetByProcessExecutionAsync(
                threadRequest, cancellationToken);

            var threads = threadResponse?.Data as IEnumerable<object> ?? Enumerable.Empty<object>();

            // Build status response
            var executionStatus = new
            {
                executionID = processExecutionID,
                processID = execution.ProcessID,
                status = statusName,
                isActive = statusName is "running" or "paused" or "waiting",
                progress = new
                {
                    totalNodes = execution.TotalNodes ?? 0,
                    completedNodes = execution.CompletedNodes ?? 0,
                    failedNodes = execution.FailedNodes ?? 0,
                    skippedNodes = execution.SkippedNodes ?? 0,
                    percentage = execution.TotalNodes.HasValue && execution.TotalNodes > 0
                        ? (decimal)(execution.CompletedNodes ?? 0) / execution.TotalNodes.Value * 100
                        : 0
                },
                timing = new
                {
                    startedAt = execution.StartedAt,
                    stoppedAt = execution.StoppedAt,
                    durationMs = execution.Duration
                },
                threads = threads.Cast<ProcessThreadExecution>().Select(t => new
                {
                    threadExecutionID = t.ProcessThreadExecutionID,
                    processThreadID = t.ProcessThreadID,
                    status = t.ExecutionStatusID,
                    completedNodes = t.CompletedNodes ?? 0,
                    totalNodes = t.TotalNodes ?? 0
                }).ToList(),
                errorInfo = !string.IsNullOrEmpty(execution.ErrorMessage) ? new
                {
                    message = execution.ErrorMessage,
                    stackTrace = execution.ErrorStack,
                    nodeID = execution.ErrorNodeID
                } : null
            };

            return ApiResponse(executionStatus, "Execution status retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting execution status for {ExecutionID}", processExecutionID);
            return ApiError($"Error getting execution status: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Pause a running process execution.
    /// </summary>
    /// <param name="processExecutionID">ID of the execution to pause.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of pause operation.</returns>
    [HttpPost("{processExecutionID}/pause")]
    public async Task<IActionResult> PauseExecutionAsync(
        int processExecutionID,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Received request to pause execution {ExecutionID}", processExecutionID);

        try
        {
            var result = await _orchestrationProcessor.PauseExecutionAsync(
                processExecutionID,
                cancellationToken);

            if (result)
            {
                return ApiResponse(new { executionID = processExecutionID, status = "paused" },
                    "Execution paused successfully");
            }
            else
            {
                return ApiError("Failed to pause execution", 400);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing execution {ExecutionID}", processExecutionID);
            return ApiError($"Error pausing execution: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Resume a paused process execution.
    /// </summary>
    /// <param name="processExecutionID">ID of the execution to resume.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of resume operation.</returns>
    [HttpPost("{processExecutionID}/resume")]
    public async Task<IActionResult> ResumeExecutionAsync(
        int processExecutionID,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Received request to resume execution {ExecutionID}", processExecutionID);

        try
        {
            var result = await _orchestrationProcessor.ResumeExecutionAsync(
                processExecutionID,
                cancellationToken);

            if (result)
            {
                return ApiResponse(new { executionID = processExecutionID, status = "running" },
                    "Execution resumed successfully");
            }
            else
            {
                return ApiError("Failed to resume execution", 400);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming execution {ExecutionID}", processExecutionID);
            return ApiError($"Error resuming execution: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Cancel a process execution.
    /// </summary>
    /// <param name="processExecutionID">ID of the execution to cancel.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of cancel operation.</returns>
    [HttpPost("{processExecutionID}/cancel")]
    public async Task<IActionResult> CancelExecutionAsync(
        int processExecutionID,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Received request to cancel execution {ExecutionID}", processExecutionID);

        try
        {
            var result = await _orchestrationProcessor.CancelExecutionAsync(
                processExecutionID,
                cancellationToken);

            if (result)
            {
                return ApiResponse(new { executionID = processExecutionID, status = "cancelled" },
                    "Execution cancelled successfully");
            }
            else
            {
                return ApiError("Failed to cancel execution", 400);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling execution {ExecutionID}", processExecutionID);
            return ApiError($"Error cancelling execution: {ex.Message}", 500);
        }
    }
}
