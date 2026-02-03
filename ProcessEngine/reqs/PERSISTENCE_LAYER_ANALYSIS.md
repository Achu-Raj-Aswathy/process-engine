# Persistence Layer for Execution Control - Analysis & Design

**Date:** 2026-02-02
**Purpose:** Define data structures and persistence strategy for pause/resume/cancel operations

---

## Executive Summary

The ProcessEngine needs to persist execution state to support pause/resume/cancel operations. The required data structures already exist in the Process module database with some extensions needed for capturing execution stack and memory state.

### Key Components:
1. **Existing Tables** - Use ProcessExecution, ProcessThreadExecution, ProcessElementExecution
2. **New Tables** - ExecutionStackState, ExecutionMemoryState (proposed)
3. **Status Values** - Already support Paused, Running, Cancelled statuses
4. **State Serialization** - JSON-based storage for stack and memory

---

## Current Database Schema

### 1. ProcessExecution Table
**Location:** `Process_ProcessExecutions`
**Mapped Entity:** `ProcessExecution.cs`

```csharp
public class ProcessExecution : BaseEntity
{
    [Key]
    public int ProcessExecutionID { get; set; }

    [Required]
    public int ProcessID { get; set; }

    [Required]
    public int ExecutionModeID { get; set; }        // Sync/Async/Scheduled

    [Required]
    public int ExecutionStatusID { get; set; }     // Running, Paused, Completed, Failed, Cancelled

    [Required]
    public DateTime StartedAt { get; set; }

    public DateTime? StoppedAt { get; set; }       // Set when paused or completed

    public int? Duration { get; set; }              // In milliseconds

    [MaxLength(4000)]
    public string? InputData { get; set; }          // JSON input parameters

    [MaxLength(4000)]
    public string? OutputData { get; set; }         // JSON output data

    public int? TotalNodes { get; set; }            // Total workflow nodes

    public int? CompletedNodes { get; set; }       // Nodes executed so far

    public int? FailedNodes { get; set; }
    public int? SkippedNodes { get; set; }
}
```

**Purpose:** Top-level execution tracking for entire process
**Use for:** Overall progress, status, input/output

---

### 2. ProcessThreadExecution Table
**Location:** `Process_ProcessThreadExecutions`
**Mapped Entity:** `ProcessThreadExecution.cs`

```csharp
public class ProcessThreadExecution : BaseEntity
{
    [Key]
    public int ProcessThreadExecutionID { get; set; }

    public int? ProcessExecutionID { get; set; }     // Link to parent process

    [Required]
    public int ProcessThreadID { get; set; }

    public int? ProcessThreadVersionID { get; set; }

    [Required]
    public int ExecutionModeID { get; set; }

    [Required]
    public int ExecutionStatusID { get; set; }      // Same statuses as ProcessExecution

    [Required]
    public DateTime StartedAt { get; set; }

    public DateTime? StoppedAt { get; set; }

    [MaxLength(4000)]
    public string? InputData { get; set; }

    [MaxLength(4000)]
    public string? OutputData { get; set; }

    public int? TotalNodes { get; set; }

    public int? CompletedNodes { get; set; }
}
```

**Purpose:** Thread-level (workflow) execution tracking
**Use for:** Individual workflow progress and state

---

### 3. ProcessElementExecution Table
**Location:** `Process_ProcessElementExecutions`
**Mapped Entity:** `ProcessElementExecution.cs`

```csharp
public class ProcessElementExecution : BaseEntity
{
    [Key]
    public int ProcessElementExecutionID { get; set; }

    [Required]
    public int ProcessThreadExecutionID { get; set; }  // Parent workflow execution

    [Required]
    public int ProcessElementID { get; set; }          // Node ID

    [Required]
    public int ProcessElementExecutionStatusID { get; set; }

    [Required]
    public DateTime StartedAt { get; set; }

    public DateTime? StoppedAt { get; set; }

    [MaxLength(4000)]
    public string? InputData { get; set; }             // Node input

    [MaxLength(4000)]
    public string? OutputData { get; set; }            // Node output

    public int? ExecutionOrder { get; set; }           // Order in execution sequence
}
```

**Purpose:** Individual node execution tracking
**Use for:** Detailed node-level debugging and progress

---

### 4. ExecutionStatus Table
**Location:** `Process_ExecutionStatuses`
**Mapped Entity:** `ExecutionStatus.cs`

**Current Status Codes:**
```
- 'running'    - Execution in progress
- 'paused'     - Execution paused (waiting to resume)
- 'completed'  - Execution finished successfully
- 'failed'     - Execution failed with error
- 'cancelled'  - Execution cancelled by user
- 'waiting'    - Waiting for trigger or dependency
- 'timeout'    - Execution timed out
```

**Schema:**
```csharp
public class ExecutionStatus : BaseEntity
{
    [Key]
    public int ExecutionStatusID { get; set; }

    [Required]
    [MaxLength(300)]
    public string Name { get; set; }              // Human-readable name

    [Required]
    [MaxLength(100)]
    public string Code { get; set; }              // System code ('paused', 'running', etc)

    [Required]
    public bool IsFinal { get; set; }             // Terminal state?

    [Required]
    public bool IsSuccess { get; set; }           // Success state?

    [Required]
    public bool IsError { get; set; }             // Error state?
}
```

**Existing Statuses** (can be queried via ExecutionStatusService):
- Running (1)
- Paused (2) ← Use this for pause
- Completed (3)
- Failed (4)
- Cancelled (5) ← Use this for cancel

---

### 5. ExecutionLog Table (Audit Trail)
**Location:** `Process_ExecutionLogs`
**Mapped Entity:** `ExecutionLog.cs`

```csharp
public class ExecutionLog : BaseEntity
{
    [Key]
    public int ExecutionLogID { get; set; }

    public int? ProcessThreadExecutionID { get; set; }

    public int? ProcessElementExecutionID { get; set; }

    [Required]
    public int LogLevelID { get; set; }           // Info, Warning, Error

    [Required]
    [MaxLength(2000)]
    public string Message { get; set; }

    [MaxLength(4000)]
    public string? Details { get; set; }          // JSON details

    [Required]
    public DateTime Timestamp { get; set; }

    [MaxLength(200)]
    public string? Source { get; set; }           // 'pause', 'resume', 'cancel', etc
}
```

**Use for:** Audit trail of pause/resume/cancel operations

---

## New Tables Required for Pause/Resume

### 6. ExecutionStackState Table (PROPOSED)
**Purpose:** Store execution stack state when paused
**Location:** `Process_ExecutionStackStates`

```sql
CREATE TABLE [Process_ExecutionStackStates] (
    [ExecutionStackStateID] INT PRIMARY KEY IDENTITY(1,1),
    [ProcessThreadExecutionID] INT NOT NULL,
    [StackData] NVARCHAR(MAX) NOT NULL,  -- JSON array of execution stack
    [SavedAt] DATETIME DEFAULT GETUTCDATE(),
    [IsActive] BIT DEFAULT 1,
    CONSTRAINT [FK_ExecutionStackState_ProcessThreadExecution]
        FOREIGN KEY ([ProcessThreadExecutionID])
        REFERENCES [Process_ProcessThreadExecutions]([ProcessThreadExecutionID])
);
```

**Data Structure (JSON):**
```json
{
  "execution_stack": [
    {
      "element_id": 101,
      "element_key": "node_verify_data",
      "element_type": "if-condition",
      "execution_order": 3,
      "status": "pending"
    },
    {
      "element_id": 102,
      "element_key": "node_send_email",
      "element_type": "send-email",
      "execution_order": 4,
      "status": "pending"
    }
  ],
  "saved_at": "2026-02-02T10:30:00Z",
  "paused_at_node": 100
}
```

**C# Entity:**
```csharp
public class ExecutionStackState : BaseEntity
{
    [Key]
    public int ExecutionStackStateID { get; set; }

    [Required]
    public int ProcessThreadExecutionID { get; set; }

    [Required]
    [MaxLength(4000)]  // Adjust based on stack depth
    public string StackData { get; set; }  // JSON serialized Stack<ProcessElementDefinition>

    [Required]
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public bool IsActive { get; set; } = true;
}
```

---

### 7. ExecutionMemoryState Table (PROPOSED)
**Purpose:** Store execution memory (variables) state when paused
**Location:** `Process_ExecutionMemoryStates`

```sql
CREATE TABLE [Process_ExecutionMemoryStates] (
    [ExecutionMemoryStateID] INT PRIMARY KEY IDENTITY(1,1),
    [ProcessThreadExecutionID] INT NOT NULL,
    [MemoryData] NVARCHAR(MAX) NOT NULL,  -- JSON dictionary of variables
    [SavedAt] DATETIME DEFAULT GETUTCDATE(),
    [IsActive] BIT DEFAULT 1,
    CONSTRAINT [FK_ExecutionMemoryState_ProcessThreadExecution]
        FOREIGN KEY ([ProcessThreadExecutionID])
        REFERENCES [Process_ProcessThreadExecutions]([ProcessThreadExecutionID])
);
```

**Data Structure (JSON):**
```json
{
  "variables": {
    "order_id": "ORD-12345",
    "customer_email": "customer@example.com",
    "order_total": 599.99,
    "is_premium": true
  },
  "node_outputs": {
    "node_fetch_order": {
      "status": "success",
      "data": { "id": "ORD-12345" }
    },
    "node_validate": {
      "status": "success",
      "is_valid": true
    }
  },
  "saved_at": "2026-02-02T10:30:00Z"
}
```

**C# Entity:**
```csharp
public class ExecutionMemoryState : BaseEntity
{
    [Key]
    public int ExecutionMemoryStateID { get; set; }

    [Required]
    public int ProcessThreadExecutionID { get; set; }

    [Required]
    [MaxLength(4000)]  // Adjust based on variable size
    public string MemoryData { get; set; }  // JSON serialized ExecutionMemory

    [Required]
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public bool IsActive { get; set; } = true;
}
```

---

### 8. ExecutionStateSnapshot Table (OPTIONAL, for audit/debugging)
**Purpose:** Complete execution state snapshot for debugging
**Location:** `Process_ExecutionStateSnapshots`

```sql
CREATE TABLE [Process_ExecutionStateSnapshots] (
    [ExecutionStateSnapshotID] INT PRIMARY KEY IDENTITY(1,1),
    [ProcessThreadExecutionID] INT NOT NULL,
    [SnapshotType] VARCHAR(50),  -- 'pause', 'resume', 'cancel', 'error'
    [CompleteState] NVARCHAR(MAX),  -- Complete JSON state
    [CreatedAt] DATETIME DEFAULT GETUTCDATE(),
    CONSTRAINT [FK_ExecutionStateSnapshot_ProcessThreadExecution]
        FOREIGN KEY ([ProcessThreadExecutionID])
        REFERENCES [Process_ProcessThreadExecutions]([ProcessThreadExecutionID])
);
```

**Data Structure:**
```json
{
  "snapshot_type": "pause",
  "timestamp": "2026-02-02T10:30:00Z",
  "execution_id": 123,
  "thread_execution_id": 456,
  "current_state": "running",
  "completed_node_count": 3,
  "total_node_count": 7,
  "execution_stack": [...],
  "execution_memory": {...},
  "last_completed_node": {
    "element_id": 100,
    "element_key": "node_validate",
    "completed_at": "2026-02-02T10:29:55Z"
  }
}
```

---

## Data Flow for Pause/Resume/Cancel

### Pause Operation
```
1. User calls ProcessExecutionController.PauseExecutionAsync(executionId)
   ↓
2. OrchestrationProcessor.PauseExecutionAsync()
   ↓
3. Load current ProcessThreadExecution from database
   ↓
4. Save execution stack to ExecutionStackState table
   {
     - ProcessThreadExecutionID
     - StackData (JSON serialized)
     - SavedAt = DateTime.UtcNow
     - IsActive = true
   }
   ↓
5. Save execution memory to ExecutionMemoryState table
   {
     - ProcessThreadExecutionID
     - MemoryData (JSON serialized)
     - SavedAt = DateTime.UtcNow
     - IsActive = true
   }
   ↓
6. Update ProcessThreadExecution status to "paused"
   {
     - ExecutionStatusID = 2 (Paused)
     - StoppedAt = DateTime.UtcNow
   }
   ↓
7. Log pause operation to ExecutionLog table
   {
     - ProcessThreadExecutionID
     - Message = "Execution paused"
     - Source = "pause"
   }
   ↓
8. Return success response
```

### Resume Operation
```
1. User calls ProcessExecutionController.ResumeExecutionAsync(executionId)
   ↓
2. OrchestrationProcessor.ResumeExecutionAsync()
   ↓
3. Load ProcessThreadExecution from database
   ↓
4. Validate status is "paused"
   ↓
5. Load latest ExecutionStackState
   {
     - Get most recent saved stack
     - Deserialize JSON to Stack<ProcessElementDefinition>
   }
   ↓
6. Load latest ExecutionMemoryState
   {
     - Get most recent saved memory
     - Deserialize JSON to ExecutionMemory
   }
   ↓
7. Restore execution context with loaded stack and memory
   ↓
8. Update ProcessThreadExecution status to "running"
   {
     - ExecutionStatusID = 1 (Running)
     - StartedAt stays same (don't reset)
   }
   ↓
9. Log resume operation
   ↓
10. Continue execution from restored state
```

### Cancel Operation
```
1. User calls ProcessExecutionController.CancelExecutionAsync(executionId)
   ↓
2. OrchestrationProcessor.CancelExecutionAsync()
   ↓
3. Load ProcessThreadExecution from database
   ↓
4. Update status to "cancelled"
   {
     - ExecutionStatusID = 5 (Cancelled)
     - StoppedAt = DateTime.UtcNow
   }
   ↓
5. Mark stack/memory states as inactive
   {
     - ExecutionStackState.IsActive = false
     - ExecutionMemoryState.IsActive = false
   }
   ↓
6. Cleanup any temporary resources
   ↓
7. Log cancellation
   ↓
8. Return success response
```

---

## Implementation Strategy

### Phase 1: Create Database Tables
```sql
-- Execute in Process database
CREATE TABLE Process_ExecutionStackStates (
    [ExecutionStackStateID] INT PRIMARY KEY IDENTITY(1,1),
    [ProcessThreadExecutionID] INT NOT NULL,
    [StackData] NVARCHAR(MAX) NOT NULL,
    [SavedAt] DATETIME DEFAULT GETUTCDATE(),
    [IsActive] BIT DEFAULT 1,
    FOREIGN KEY ([ProcessThreadExecutionID])
        REFERENCES [Process_ProcessThreadExecutions]([ProcessThreadExecutionID])
);

CREATE TABLE Process_ExecutionMemoryStates (
    [ExecutionMemoryStateID] INT PRIMARY KEY IDENTITY(1,1),
    [ProcessThreadExecutionID] INT NOT NULL,
    [MemoryData] NVARCHAR(MAX) NOT NULL,
    [SavedAt] DATETIME DEFAULT GETUTCDATE(),
    [IsActive] BIT DEFAULT 1,
    FOREIGN KEY ([ProcessThreadExecutionID])
        REFERENCES [Process_ProcessThreadExecutions]([ProcessThreadExecutionID])
);
```

### Phase 2: Create Entity Classes
```csharp
// In Process.Domain/Entities/
public class ExecutionStackState : BaseEntity { ... }
public class ExecutionMemoryState : BaseEntity { ... }
```

### Phase 3: Create Service Interfaces
```csharp
// In Process.Domain/Interfaces/Services/
public interface IExecutionStateService
{
    Task SaveStackStateAsync(int processThreadExecutionID,
        Stack<ProcessElementDefinition> executionStack);

    Task SaveMemoryStateAsync(int processThreadExecutionID,
        ExecutionMemory executionMemory);

    Task<Stack<ProcessElementDefinition>> LoadStackStateAsync(
        int processThreadExecutionID);

    Task<ExecutionMemory> LoadMemoryStateAsync(
        int processThreadExecutionID);

    Task MarkStateInactiveAsync(int processThreadExecutionID);
}
```

### Phase 4: Create Service Implementations
```csharp
// In Process.Service/
public class ExecutionStateService : IExecutionStateService
{
    private readonly IExecutionStackStateRepository _stackRepo;
    private readonly IExecutionMemoryStateRepository _memoryRepo;

    public async Task SaveStackStateAsync(...)
    {
        var stackJson = JsonSerializer.Serialize(executionStack);
        var state = new ExecutionStackState
        {
            ProcessThreadExecutionID = processThreadExecutionID,
            StackData = stackJson,
            SavedAt = DateTime.UtcNow,
            IsActive = true
        };
        await _stackRepo.AddAsync(state);
    }

    public async Task<Stack<ProcessElementDefinition>> LoadStackStateAsync(...)
    {
        var state = await _stackRepo
            .GetLatestActiveStateAsync(processThreadExecutionID);

        var stack = JsonSerializer.Deserialize<Stack<ProcessElementDefinition>>(
            state.StackData);

        return stack ?? new Stack<ProcessElementDefinition>();
    }

    // ... other methods
}
```

### Phase 5: Integrate with OrchestrationProcessor
```csharp
public class OrchestrationProcessor
{
    private readonly IExecutionStateService _stateService;

    public async Task<bool> PauseExecutionAsync(
        int processExecutionID,
        CancellationToken cancellationToken = default)
    {
        // Save stack state
        await _stateService.SaveStackStateAsync(
            processThreadExecutionID,
            executionStack);

        // Save memory state
        await _stateService.SaveMemoryStateAsync(
            processThreadExecutionID,
            executionMemory);

        // Update ProcessThreadExecution status
        await _threadExecutionService.UpdateStatusAsync(
            processThreadExecutionID,
            ExecutionStatusCode.Paused);

        return true;
    }

    public async Task<bool> ResumeExecutionAsync(...)
    {
        // Load stack state
        var restoredStack = await _stateService.LoadStackStateAsync(
            processThreadExecutionID);

        // Load memory state
        var restoredMemory = await _stateService.LoadMemoryStateAsync(
            processThreadExecutionID);

        // Restore execution context
        executionContext.ExecutionStack = restoredStack;
        executionContext.Memory = restoredMemory;

        // Update status to running
        await _threadExecutionService.UpdateStatusAsync(
            processThreadExecutionID,
            ExecutionStatusCode.Running);

        // Continue execution
        return true;
    }
}
```

---

## Data Serialization Strategy

### Stack Serialization
```csharp
// Serialize
var stackJson = JsonSerializer.Serialize(new
{
    execution_stack = executionStack.Select(elem => new
    {
        element_id = elem.ProcessElementID,
        element_key = elem.ProcessElementKey,
        element_type = elem.ElementTypeName,
        execution_order = executionContext.CompletedNodeCount
    }).ToList()
});

// Deserialize
var stackData = JsonSerializer.Deserialize<dynamic>(stackJson);
var restoredStack = new Stack<ProcessElementDefinition>();
foreach (var item in stackData["execution_stack"])
{
    var element = threadDefinition.Elements
        .FirstOrDefault(e => e.ProcessElementID == item["element_id"]);
    if (element != null)
        restoredStack.Push(element);
}
```

### Memory Serialization
```csharp
// Serialize
var memoryJson = JsonSerializer.Serialize(new
{
    variables = executionContext.Memory.Variables,
    node_outputs = executionContext.Memory.NodeOutputs
});

// Deserialize
var memoryData = JsonSerializer.Deserialize<dynamic>(memoryJson);
var restoredMemory = new ExecutionMemory();
foreach (var kvp in memoryData["variables"])
{
    restoredMemory.Variables[kvp.Key] = kvp.Value;
}
```

---

## API Responses

### Pause Response
```json
{
  "success": true,
  "executionID": 123,
  "status": "paused",
  "pausedAt": "2026-02-02T10:30:00Z",
  "completedNodes": 3,
  "totalNodes": 7,
  "message": "Execution paused successfully"
}
```

### Resume Response
```json
{
  "success": true,
  "executionID": 123,
  "status": "running",
  "resumedAt": "2026-02-02T10:35:00Z",
  "completedNodes": 3,
  "totalNodes": 7,
  "message": "Execution resumed successfully"
}
```

### Cancel Response
```json
{
  "success": true,
  "executionID": 123,
  "status": "cancelled",
  "cancelledAt": "2026-02-02T10:32:00Z",
  "completedNodes": 3,
  "totalNodes": 7,
  "message": "Execution cancelled successfully"
}
```

---

## Status Codes Reference

| Code | ID | IsFinal | IsSuccess | IsError | Use Case |
|------|-----|---------|-----------|---------|----------|
| running | 1 | false | false | false | Execution in progress |
| paused | 2 | false | false | false | Paused, waiting for resume |
| completed | 3 | true | true | false | Finished successfully |
| failed | 4 | true | false | true | Failed with error |
| cancelled | 5 | true | false | false | User cancelled |
| waiting | 6 | false | false | false | Waiting for trigger |
| timeout | 7 | true | false | true | Execution timed out |

---

## Summary

| Component | Location | Purpose |
|-----------|----------|---------|
| **ProcessExecution** | Process_ProcessExecutions | Top-level execution tracking |
| **ProcessThreadExecution** | Process_ProcessThreadExecutions | Workflow-level execution tracking |
| **ProcessElementExecution** | Process_ProcessElementExecutions | Node-level execution tracking |
| **ExecutionLog** | Process_ExecutionLogs | Audit trail |
| **ExecutionStackState** | Process_ExecutionStackStates | Saved execution stack (NEW) |
| **ExecutionMemoryState** | Process_ExecutionMemoryStates | Saved execution memory (NEW) |

**Status:** Ready for implementation in Phase 1-5 above
