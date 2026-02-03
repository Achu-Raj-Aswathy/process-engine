# Implementation Guide: Persistence Layer & Status API

**Date:** 2026-02-02
**Scope:** Pause/Resume/Cancel + Execution Status API
**Dependencies:** Existing Process module services

---

## Architecture Overview

```
ProcessEngine (Orchestration)
    ↓
IOrchestrationProcessor (Interface)
    ↓
OrchestrationProcessor (Implementation)
    ├─ Uses: IProcessThreadLoader (load definitions)
    ├─ Uses: IProcessElementExecutor (execute nodes)
    ├─ Uses: IExecutionRouter (route nodes)
    └─ NEW: Uses: IExecutionStateService (save/load state)
              Uses: IProcessThreadExecutionService (update status)
              Uses: IProcessElementExecutionService (query node executions)
```

---

## Step 1: Create ExecutionStateService

### File: `BizFirst.Ai.ProcessEngine.Service/src/Persistence/ExecutionStateService.cs`

```csharp
namespace BizFirst.Ai.ProcessEngine.Service.Persistence;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BizFirst.Ai.ProcessEngine.Domain.Definition;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Context;

/// <summary>
/// Service for persisting and restoring execution state (stack and memory).
/// Handles pause/resume/cancel state management.
/// </summary>
public interface IExecutionStateService
{
    /// <summary>Save execution stack to database when pausing.</summary>
    Task SaveStackStateAsync(
        int processThreadExecutionID,
        Stack<ProcessElementDefinition> executionStack,
        CancellationToken cancellationToken = default);

    /// <summary>Save execution memory to database when pausing.</summary>
    Task SaveMemoryStateAsync(
        int processThreadExecutionID,
        ExecutionMemory executionMemory,
        CancellationToken cancellationToken = default);

    /// <summary>Load previously saved execution stack.</summary>
    Task<Stack<ProcessElementDefinition>> LoadStackStateAsync(
        int processThreadExecutionID,
        ProcessThreadDefinition threadDefinition,
        CancellationToken cancellationToken = default);

    /// <summary>Load previously saved execution memory.</summary>
    Task<ExecutionMemory> LoadMemoryStateAsync(
        int processThreadExecutionID,
        CancellationToken cancellationToken = default);

    /// <summary>Mark saved state as inactive (cleanup on cancel).</summary>
    Task MarkStateInactiveAsync(
        int processThreadExecutionID,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of execution state persistence service.
/// </summary>
public class ExecutionStateService : IExecutionStateService
{
    private readonly ILogger<ExecutionStateService> _logger;

    // TODO: Inject repositories when tables are created
    // private readonly IExecutionStackStateRepository _stackRepository;
    // private readonly IExecutionMemoryStateRepository _memoryRepository;

    public ExecutionStateService(ILogger<ExecutionStateService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Save execution stack as JSON.</summary>
    public async Task SaveStackStateAsync(
        int processThreadExecutionID,
        Stack<ProcessElementDefinition> executionStack,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Saving execution stack for ProcessThreadExecutionID {ThreadExecutionID}",
                processThreadExecutionID);

            // Serialize stack to JSON
            var stackList = new List<object>();
            foreach (var element in executionStack)
            {
                stackList.Add(new
                {
                    element.ProcessElementID,
                    element.ProcessElementKey,
                    element.ElementTypeName,
                    element.Configuration
                });
            }

            var stackJson = JsonSerializer.Serialize(new
            {
                execution_stack = stackList,
                saved_at = DateTime.UtcNow,
                stack_depth = stackList.Count
            });

            _logger.LogDebug(
                "Serialized execution stack ({StackDepth} items): {StackJson}",
                stackList.Count, stackJson);

            // TODO: Save to ExecutionStackState table
            // await _stackRepository.AddAsync(new ExecutionStackState
            // {
            //     ProcessThreadExecutionID = processThreadExecutionID,
            //     StackData = stackJson,
            //     SavedAt = DateTime.UtcNow,
            //     IsActive = true
            // });

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving execution stack");
            throw;
        }
    }

    /// <summary>Save execution memory as JSON.</summary>
    public async Task SaveMemoryStateAsync(
        int processThreadExecutionID,
        ExecutionMemory executionMemory,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Saving execution memory for ProcessThreadExecutionID {ThreadExecutionID}",
                processThreadExecutionID);

            var memoryJson = JsonSerializer.Serialize(new
            {
                variables = executionMemory.Variables,
                node_outputs = executionMemory.NodeOutputs,
                saved_at = DateTime.UtcNow,
                variable_count = executionMemory.Variables.Count
            });

            _logger.LogDebug(
                "Serialized execution memory ({VariableCount} variables)",
                executionMemory.Variables.Count);

            // TODO: Save to ExecutionMemoryState table
            // await _memoryRepository.AddAsync(new ExecutionMemoryState
            // {
            //     ProcessThreadExecutionID = processThreadExecutionID,
            //     MemoryData = memoryJson,
            //     SavedAt = DateTime.UtcNow,
            //     IsActive = true
            // });

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving execution memory");
            throw;
        }
    }

    /// <summary>Load saved execution stack.</summary>
    public async Task<Stack<ProcessElementDefinition>> LoadStackStateAsync(
        int processThreadExecutionID,
        ProcessThreadDefinition threadDefinition,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Loading execution stack for ProcessThreadExecutionID {ThreadExecutionID}",
                processThreadExecutionID);

            // TODO: Load from ExecutionStackState table
            // var stackState = await _stackRepository.GetLatestActiveStateAsync(
            //     processThreadExecutionID);

            var restoredStack = new Stack<ProcessElementDefinition>();

            // TODO: Deserialize and restore
            // var stackData = JsonSerializer.Deserialize<dynamic>(stackState.StackData);
            // foreach (var item in stackData["execution_stack"])
            // {
            //     var elementId = (int)item["ProcessElementID"];
            //     var element = threadDefinition.Elements
            //         .FirstOrDefault(e => e.ProcessElementID == elementId);
            //     if (element != null)
            //         restoredStack.Push(element);
            // }

            _logger.LogInformation(
                "Loaded execution stack with {StackDepth} items",
                restoredStack.Count);

            return await Task.FromResult(restoredStack);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading execution stack");
            throw;
        }
    }

    /// <summary>Load saved execution memory.</summary>
    public async Task<ExecutionMemory> LoadMemoryStateAsync(
        int processThreadExecutionID,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Loading execution memory for ProcessThreadExecutionID {ThreadExecutionID}",
                processThreadExecutionID);

            var restoredMemory = new ExecutionMemory();

            // TODO: Load from ExecutionMemoryState table
            // var memoryState = await _memoryRepository.GetLatestActiveStateAsync(
            //     processThreadExecutionID);
            // var memoryData = JsonSerializer.Deserialize<dynamic>(memoryState.MemoryData);
            //
            // foreach (var kvp in memoryData["variables"])
            // {
            //     restoredMemory.Variables[kvp.Key] = kvp.Value;
            // }

            _logger.LogInformation(
                "Loaded execution memory with {VariableCount} variables",
                restoredMemory.Variables.Count);

            return await Task.FromResult(restoredMemory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading execution memory");
            throw;
        }
    }

    /// <summary>Mark state as inactive.</summary>
    public async Task MarkStateInactiveAsync(
        int processThreadExecutionID,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Marking execution state inactive for ProcessThreadExecutionID {ThreadExecutionID}",
                processThreadExecutionID);

            // TODO: Update in database
            // await _stackRepository.MarkInactiveAsync(processThreadExecutionID);
            // await _memoryRepository.MarkInactiveAsync(processThreadExecutionID);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking state inactive");
            throw;
        }
    }
}
```

---

## Step 2: Update OrchestrationProcessor

### File: `BizFirst.Ai.ProcessEngine.Service/src/Orchestration/OrchestrationProcessor.cs`

#### Add to Constructor:
```csharp
private readonly IExecutionStateService _executionStateService;
private readonly IProcessThreadExecutionService _threadExecutionService;
private readonly IExecutionStatusService _statusService;

public OrchestrationProcessor(
    IProcessThreadLoader definitionLoader,
    IProcessElementExecutor elementExecutor,
    IExecutionRouter executionRouter,
    IExecutionStateService executionStateService,
    IProcessThreadExecutionService threadExecutionService,
    IExecutionStatusService statusService,
    ILogger<OrchestrationProcessor> logger)
{
    _definitionLoader = definitionLoader ?? throw new ArgumentNullException(nameof(definitionLoader));
    _elementExecutor = elementExecutor ?? throw new ArgumentNullException(nameof(elementExecutor));
    _executionRouter = executionRouter ?? throw new ArgumentNullException(nameof(executionRouter));
    _executionStateService = executionStateService ?? throw new ArgumentNullException(nameof(executionStateService));
    _threadExecutionService = threadExecutionService ?? throw new ArgumentNullException(nameof(threadExecutionService));
    _statusService = statusService ?? throw new ArgumentNullException(nameof(statusService));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
}
```

#### Implement PauseExecutionAsync:
```csharp
/// <inheritdoc/>
public async Task<bool> PauseExecutionAsync(
    int processExecutionID,
    CancellationToken cancellationToken = default)
{
    _logger.LogInformation("Pausing execution {ProcessExecutionID}", processExecutionID);

    try
    {
        // Get the currently running thread execution
        var threadExecutionRequest = new GetByProcessExecutionSearchRequest
        {
            ProcessExecutionID = processExecutionID
        };
        var threadExecutions = await _threadExecutionService.GetByProcessExecutionAsync(
            threadExecutionRequest, cancellationToken);

        if (threadExecutions?.Data is not IEnumerable<object> threadList || !threadList.Any())
        {
            _logger.LogWarning(
                "No active thread execution found for ProcessExecutionID {ProcessExecutionID}",
                processExecutionID);
            return false;
        }

        // Get first active execution (assuming single threaded for now)
        if (threadList.FirstOrDefault() is not ProcessThreadExecution threadExecution)
        {
            return false;
        }

        // Get the paused status ID
        var pausedStatus = await _statusService.GetStatusByCodeAsync("paused", cancellationToken);

        // Save execution stack
        _logger.LogDebug("Saving execution stack for thread {ThreadExecutionID}",
            threadExecution.ProcessThreadExecutionID);
        await _executionStateService.SaveStackStateAsync(
            threadExecution.ProcessThreadExecutionID,
            _executionStack,  // The current execution stack
            cancellationToken);

        // Save execution memory
        _logger.LogDebug("Saving execution memory for thread {ThreadExecutionID}",
            threadExecution.ProcessThreadExecutionID);
        await _executionStateService.SaveMemoryStateAsync(
            threadExecution.ProcessThreadExecutionID,
            _executionMemory,  // The current execution memory
            cancellationToken);

        // Update status to paused
        threadExecution.ExecutionStatusID = pausedStatus.ExecutionStatusID;
        threadExecution.StoppedAt = DateTime.UtcNow;
        await _threadExecutionService.UpdateAsync(threadExecution, cancellationToken);

        _logger.LogInformation(
            "Execution {ProcessExecutionID} paused successfully",
            processExecutionID);

        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to pause execution {ProcessExecutionID}", processExecutionID);
        return false;
    }
}
```

#### Implement ResumeExecutionAsync:
```csharp
/// <inheritdoc/>
public async Task<bool> ResumeExecutionAsync(
    int processExecutionID,
    CancellationToken cancellationToken = default)
{
    _logger.LogInformation("Resuming execution {ProcessExecutionID}", processExecutionID);

    try
    {
        // Get the paused thread execution
        var threadExecutionRequest = new GetByProcessExecutionSearchRequest
        {
            ProcessExecutionID = processExecutionID
        };
        var threadExecutions = await _threadExecutionService.GetByProcessExecutionAsync(
            threadExecutionRequest, cancellationToken);

        if (threadExecutions?.Data is not IEnumerable<object> threadList || !threadList.Any())
        {
            _logger.LogWarning(
                "No paused execution found for ProcessExecutionID {ProcessExecutionID}",
                processExecutionID);
            return false;
        }

        if (threadList.FirstOrDefault() is not ProcessThreadExecution threadExecution)
        {
            return false;
        }

        // Validate status is paused
        var pausedStatus = await _statusService.GetStatusByCodeAsync("paused", cancellationToken);
        if (threadExecution.ExecutionStatusID != pausedStatus.ExecutionStatusID)
        {
            _logger.LogWarning(
                "Thread execution {ThreadExecutionID} is not in paused state",
                threadExecution.ProcessThreadExecutionID);
            return false;
        }

        // Load the workflow definition
        var threadDefinition = await _definitionLoader.LoadProcessThreadAsync(
            threadExecution.ProcessThreadVersionID.Value,
            cancellationToken);

        // Load saved execution stack
        _logger.LogDebug("Loading saved execution stack for thread {ThreadExecutionID}",
            threadExecution.ProcessThreadExecutionID);
        var restoredStack = await _executionStateService.LoadStackStateAsync(
            threadExecution.ProcessThreadExecutionID,
            threadDefinition,
            cancellationToken);

        // Load saved execution memory
        _logger.LogDebug("Loading saved execution memory for thread {ThreadExecutionID}",
            threadExecution.ProcessThreadExecutionID);
        var restoredMemory = await _executionStateService.LoadMemoryStateAsync(
            threadExecution.ProcessThreadExecutionID,
            cancellationToken);

        // Restore execution context
        _executionStack = restoredStack;
        _executionMemory = restoredMemory;

        // Update status to running
        var runningStatus = await _statusService.GetStatusByCodeAsync("running", cancellationToken);
        threadExecution.ExecutionStatusID = runningStatus.ExecutionStatusID;
        threadExecution.StartedAt = DateTime.UtcNow;  // Update start time to reflect resume
        await _threadExecutionService.UpdateAsync(threadExecution, cancellationToken);

        _logger.LogInformation(
            "Execution {ProcessExecutionID} resumed successfully with {StackDepth} pending nodes",
            processExecutionID, restoredStack.Count);

        // Continue execution from restored state
        // (Execution loop will pick up from restored stack)
        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to resume execution {ProcessExecutionID}", processExecutionID);
        return false;
    }
}
```

#### Implement CancelExecutionAsync:
```csharp
/// <inheritdoc/>
public async Task<bool> CancelExecutionAsync(
    int processExecutionID,
    CancellationToken cancellationToken = default)
{
    _logger.LogInformation("Cancelling execution {ProcessExecutionID}", processExecutionID);

    try
    {
        // Get the thread execution
        var threadExecutionRequest = new GetByProcessExecutionSearchRequest
        {
            ProcessExecutionID = processExecutionID
        };
        var threadExecutions = await _threadExecutionService.GetByProcessExecutionAsync(
            threadExecutionRequest, cancellationToken);

        if (threadExecutions?.Data is not IEnumerable<object> threadList || !threadList.Any())
        {
            _logger.LogWarning(
                "No execution found for ProcessExecutionID {ProcessExecutionID}",
                processExecutionID);
            return false;
        }

        if (threadList.FirstOrDefault() is not ProcessThreadExecution threadExecution)
        {
            return false;
        }

        // Get cancelled status
        var cancelledStatus = await _statusService.GetStatusByCodeAsync("cancelled", cancellationToken);

        // Update status to cancelled
        threadExecution.ExecutionStatusID = cancelledStatus.ExecutionStatusID;
        threadExecution.StoppedAt = DateTime.UtcNow;
        await _threadExecutionService.UpdateAsync(threadExecution, cancellationToken);

        // Mark saved state as inactive (cleanup)
        await _executionStateService.MarkStateInactiveAsync(
            threadExecution.ProcessThreadExecutionID,
            cancellationToken);

        // Clear execution stack and memory
        _executionStack.Clear();
        _executionMemory = new ExecutionMemory();

        _logger.LogInformation(
            "Execution {ProcessExecutionID} cancelled successfully",
            processExecutionID);

        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to cancel execution {ProcessExecutionID}", processExecutionID);
        return false;
    }
}
```

---

## Step 3: Implement GetExecutionStatusAsync

### File: `BizFirst.Ai.ProcessEngine.Api/src/Controllers/ExecutionManagement/ProcessExecutionController.cs`

```csharp
/// <summary>
/// Get execution status for a process.
/// </summary>
[HttpGet("{processExecutionID}/status")]
public async Task<IActionResult> GetExecutionStatusAsync(
    int processExecutionID,
    CancellationToken cancellationToken = default)
{
    _logger.LogInformation(
        "Received request to get execution status for {ExecutionID}",
        processExecutionID);

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
        var statusResponse = await _executionStatusService.GetByIdAsync(
            statusRequest, cancellationToken);

        var statusName = (statusResponse?.Data as ExecutionStatus)?.Code ?? "unknown";

        // Load thread executions for progress
        var threadRequest = new GetByProcessExecutionSearchRequest
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
```

---

## Step 4: Register Services in DependencyInjection

### File: `BizFirst.Ai.ProcessEngine.Service/src/DependencyInjection.cs`

```csharp
public static IServiceCollection AddProcessEngineServices(this IServiceCollection services)
{
    // Existing registrations...
    services.AddScoped<IProcessThreadLoader, ProcessThreadDefinitionLoader>();
    services.AddScoped<IProcessElementExecutor, ProcessElementExecutor>();
    services.AddScoped<IExecutionRouter, ExecutionRouter>();
    services.AddScoped<IOrchestrationProcessor, OrchestrationProcessor>();
    services.AddScoped<IExpressionEvaluator, ExpressionEvaluator>();
    services.AddScoped<INodeExecutorFactory, NodeExecutorFactory>();

    // NEW: Persistence and status services
    services.AddScoped<IExecutionStateService, ExecutionStateService>();

    // Inject Process module services
    services.AddProcessServices();

    return services;
}
```

---

## Step 5: API Response Examples

### Pause Request
```
POST /api/v1/process-engine/executions/processes/123/pause
```

**Response (Success):**
```json
{
  "success": true,
  "data": {
    "executionID": 123,
    "status": "paused",
    "completedNodes": 3,
    "totalNodes": 7,
    "pausedAt": "2026-02-02T10:30:00Z"
  },
  "message": "Execution paused successfully"
}
```

### Resume Request
```
POST /api/v1/process-engine/executions/processes/123/resume
```

**Response (Success):**
```json
{
  "success": true,
  "data": {
    "executionID": 123,
    "status": "running",
    "resumedAt": "2026-02-02T10:35:00Z",
    "pendingNodes": 4
  },
  "message": "Execution resumed successfully"
}
```

### Get Status Request
```
GET /api/v1/process-engine/executions/processes/123/status
```

**Response:**
```json
{
  "success": true,
  "data": {
    "executionID": 123,
    "processID": 1,
    "status": "running",
    "isActive": true,
    "progress": {
      "totalNodes": 7,
      "completedNodes": 3,
      "failedNodes": 0,
      "skippedNodes": 0,
      "percentage": 42.86
    },
    "timing": {
      "startedAt": "2026-02-02T10:00:00Z",
      "stoppedAt": null,
      "durationMs": 1800000
    },
    "threads": [
      {
        "threadExecutionID": 456,
        "processThreadID": 10,
        "status": 1,
        "completedNodes": 3,
        "totalNodes": 7
      }
    ],
    "errorInfo": null
  },
  "message": "Execution status retrieved successfully"
}
```

---

## Database Setup (SQL)

```sql
-- Create ExecutionStackState table
CREATE TABLE [Process_ExecutionStackStates] (
    [ExecutionStackStateID] INT PRIMARY KEY IDENTITY(1,1),
    [ProcessThreadExecutionID] INT NOT NULL,
    [StackData] NVARCHAR(MAX) NOT NULL,
    [SavedAt] DATETIME DEFAULT GETUTCDATE(),
    [IsActive] BIT DEFAULT 1,
    [CreatedAt] DATETIME DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME DEFAULT GETUTCDATE(),
    CONSTRAINT [FK_ExecutionStackState_ProcessThreadExecution]
        FOREIGN KEY ([ProcessThreadExecutionID])
        REFERENCES [Process_ProcessThreadExecutions]([ProcessThreadExecutionID])
        ON DELETE CASCADE,
    INDEX [IX_ProcessThreadExecutionID_IsActive]
        ([ProcessThreadExecutionID], [IsActive])
);

-- Create ExecutionMemoryState table
CREATE TABLE [Process_ExecutionMemoryStates] (
    [ExecutionMemoryStateID] INT PRIMARY KEY IDENTITY(1,1),
    [ProcessThreadExecutionID] INT NOT NULL,
    [MemoryData] NVARCHAR(MAX) NOT NULL,
    [SavedAt] DATETIME DEFAULT GETUTCDATE(),
    [IsActive] BIT DEFAULT 1,
    [CreatedAt] DATETIME DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME DEFAULT GETUTCDATE(),
    CONSTRAINT [FK_ExecutionMemoryState_ProcessThreadExecution]
        FOREIGN KEY ([ProcessThreadExecutionID])
        REFERENCES [Process_ProcessThreadExecutions]([ProcessThreadExecutionID])
        ON DELETE CASCADE,
    INDEX [IX_ProcessThreadExecutionID_IsActive]
        ([ProcessThreadExecutionID], [IsActive])
);
```

---

## Implementation Checklist

- [ ] Step 1: Create ExecutionStateService interface and implementation
- [ ] Step 2: Create ExecutionStackState entity
- [ ] Step 3: Create ExecutionMemoryState entity
- [ ] Step 4: Create repository interfaces and implementations
- [ ] Step 5: Create DbSet mappings in ProcessDbContext
- [ ] Step 6: Create database migration
- [ ] Step 7: Create ExecutionStateRepository
- [ ] Step 8: Create ExecutionMemoryRepository
- [ ] Step 9: Update OrchestrationProcessor with pause/resume/cancel logic
- [ ] Step 10: Update ProcessExecutionController.GetExecutionStatusAsync
- [ ] Step 11: Register services in DependencyInjection
- [ ] Step 12: Add integration tests
- [ ] Step 13: Test pause/resume/cancel workflow
- [ ] Step 14: Test status API responses

---

## Key Integration Points

### Data Flow: Pause
```
ProcessExecutionController.PauseExecutionAsync()
    ↓
OrchestrationProcessor.PauseExecutionAsync()
    ├─ ExecutionStateService.SaveStackStateAsync()
    │   └─ ExecutionStackStateRepository.AddAsync()
    ├─ ExecutionStateService.SaveMemoryStateAsync()
    │   └─ ExecutionMemoryStateRepository.AddAsync()
    └─ ProcessThreadExecutionService.UpdateAsync()
        └─ ProcessThreadExecutionRepository.UpdateAsync()
```

### Data Flow: Resume
```
ProcessExecutionController.ResumeExecutionAsync()
    ↓
OrchestrationProcessor.ResumeExecutionAsync()
    ├─ ProcessThreadExecutionService.GetByProcessExecutionAsync()
    ├─ ProcessThreadDefinitionLoader.LoadProcessThreadAsync()
    ├─ ExecutionStateService.LoadStackStateAsync()
    │   └─ ExecutionStackStateRepository.GetLatestAsync()
    ├─ ExecutionStateService.LoadMemoryStateAsync()
    │   └─ ExecutionMemoryStateRepository.GetLatestAsync()
    └─ ProcessThreadExecutionService.UpdateAsync()
```

### Data Flow: Get Status
```
ProcessExecutionController.GetExecutionStatusAsync()
    ├─ ProcessExecutionService.GetByIdAsync()
    ├─ ExecutionStatusService.GetByIdAsync()
    ├─ ProcessThreadExecutionService.GetByProcessExecutionAsync()
    └─ Build comprehensive status response
```

---

## Notes

- **Stack Serialization**: ProcessElementDefinition objects are serialized by ID and key, then reconstructed from the workflow definition when loading
- **Memory Serialization**: ExecutionMemory variables and node outputs are serialized as JSON
- **Status Codes**: Use existing Process_ExecutionStatuses table codes: 'running', 'paused', 'completed', 'failed', 'cancelled'
- **Tenant Filtering**: BaseRepository automatically filters by TenantID (AsyncLocal context)
- **Concurrency**: Use optimistic locking if multiple resume attempts occur simultaneously

