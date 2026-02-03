# Critical Features 3-6 Implementation Plan

## ✅ Completed
- **#1 Timeout Handling** - DONE
- **#2 Loop Control** - DONE

## Implementation Queue

### Feature #3: Try-Catch-Finally (6-8 hours)
**Concept**: Scope-based exception routing

**Files to Create**:
1. `TryBlockNode.cs` - Marks try scope start
2. `CatchBlockNode.cs` - Exception handler
3. `FinallyBlockNode.cs` - Cleanup handler
4. Exception routing in OrchestrationProcessor

**Key Logic**:
```csharp
// In OrchestrationProcessor
try {
  // Execute nodes in try scope
} catch (Exception ex) {
  // Route to catch block
  // Determine which catch handles this exception type
} finally {
  // Always execute finally block
  // Clean up resources
}
```

**Dependencies**:
- Extend ExecutionMemory with exception context stack
- Add scope tracking to execution flow
- Exception type filtering

---

### Feature #4: Sub-Workflow Execution (6-8 hours)
**Concept**: Recursive workflow invocation

**Files to Create**:
1. `SubWorkflowExecutor.cs` - Main executor
2. Nested context management in ProcessThreadExecutionContext
3. Data mapping between parent and child workflows

**Key Logic**:
```csharp
public class SubWorkflowExecutor : IActionNodeExecution
{
    public async Task<NodeExecutionResult> ExecuteAsync(...)
    {
        // Get sub-workflow ID from config
        var subWorkflowId = GetSubWorkflowId(context);

        // Create child context
        var childContext = CreateChildContext(context);

        // Execute sub-workflow
        var result = await _orchestrationProcessor.ExecuteProcessThreadAsync(
            subWorkflowId, version, childContext, cancellation Token);

        // Map output back to parent
        return MapResult(result);
    }
}
```

**Changes**:
- Add parentContext reference to ProcessThreadExecutionContext
- Add depth/nesting level tracking
- Max nesting depth validation

---

### Feature #5: Event/Hook System (3-4 hours)
**Concept**: Extensibility through lifecycle events

**Files to Create**:
1. `IExecutionEventHandler.cs` - Event handler interface
2. `ExecutionEventPublisher.cs` - Event dispatcher
3. Event registrations in DependencyInjection

**Key Events**:
```csharp
public interface IExecutionEventHandler
{
    Task OnWorkflowStartingAsync(ProcessThreadExecutionContext context);
    Task OnNodeExecutingAsync(ProcessElementExecutionContext context);
    Task OnNodeExecutedAsync(NodeExecutionResult result);
    Task OnErrorAsync(ExecutionErrorContext error);
    Task OnWorkflowCompletedAsync(ProcessThreadExecution execution);
}
```

**Changes**:
- Add event publishing at key execution points
- Register handlers in DI
- Optional async event handling

---

### Feature #6: Data Transformation Nodes (4-6 hours)
**Concept**: Built-in data processing nodes

**Files to Create**:
1. `VariableAssignmentExecutor.cs` - Set variables
2. `JsonTransformExecutor.cs` - JSON manipulation
3. `DataMappingExecutor.cs` - Field mapping
4. `CollectionOperationExecutor.cs` - Array operations

**Key Executors**:
```csharp
// Variable Assignment
public class VariableAssignmentExecutor : IActionNodeExecution
{
    // Config: { "variable_name": "x", "value_expression": "input.value * 2" }
    // Sets: Memory.Variables["x"] = evaluated_value
}

// JSON Transform
public class JsonTransformExecutor : IActionNodeExecution
{
    // Config: { "source": "node_output", "jql": "$.data[*].name" }
    // Returns: Transformed JSON
}

// Data Mapping
public class DataMappingExecutor : IActionNodeExecution
{
    // Config: { "mappings": [{ "source": "field1", "target": "newName" }] }
    // Maps fields between objects
}

// Collection Operations
public class CollectionOperationExecutor : IActionNodeExecution
{
    // Config: { "operation": "filter|map|reduce", "expression": "..." }
    // Performs operations on collections
}
```

---

## Implementation Sequence

**Day 1-2**: Try-Catch-Finally (Core exception handling)
- Create scope tracking
- Exception routing
- Finally guarantee

**Day 3-4**: Sub-Workflows (Enabler for complex workflows)
- Context nesting
- Data mapping
- Depth tracking

**Day 5-6**: Event/Hook System (Extensibility)
- Event interfaces
- Hook registration
- Publisher integration

**Day 7**: Data Transformation Nodes (Enable workflows)
- Variable assignment
- JSON transform
- Collection operations

---

## Testing Strategy

### Try-Catch-Finally
- [ ] Normal flow (no exception)
- [ ] Exception in try → routed to catch
- [ ] Multiple catch blocks
- [ ] Finally executes always
- [ ] Nested try-catch

### Sub-Workflows
- [ ] Basic invocation
- [ ] Input data passed correctly
- [ ] Output returned properly
- [ ] Error propagation
- [ ] Nested sub-workflows (depth limit)

### Event/Hook System
- [ ] Events fire at correct points
- [ ] Handlers can modify context
- [ ] Async handlers completed
- [ ] Exception in handler handled

### Data Transformation
- [ ] Variables assigned correctly
- [ ] JSON transformation works
- [ ] Data mapping accurate
- [ ] Collection operations function

---

## Success Criteria

After all 6 features:
- ✅ Production-ready engine (95%+ complete)
- ✅ Build succeeds with 0 errors
- ✅ Comprehensive error handling
- ✅ Workflow composition possible
- ✅ Data processing enabled
- ✅ Extensible architecture

---

## Risk Mitigation

**Try-Catch-Finally Risk**: Exception routing complexity
- **Mitigation**: Start with single exception type, add filtering later

**Sub-Workflow Risk**: Infinite recursion
- **Mitigation**: Add nesting depth limit (max 10)

**Event System Risk**: Performance impact
- **Mitigation**: Make events async, optimize critical path

**Data Transformation Risk**: Performance on large datasets
- **Mitigation**: Lazy evaluation, streaming for large collections

---

**Ready to Begin**: Feature #3 - Try-Catch-Finally
