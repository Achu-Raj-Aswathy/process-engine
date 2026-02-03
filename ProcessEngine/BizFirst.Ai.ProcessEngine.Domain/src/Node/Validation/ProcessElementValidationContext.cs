namespace BizFirst.Ai.ProcessEngine.Domain.Node.Validation;

using BizFirst.Ai.ProcessEngine.Domain.Definition;
using BizFirst.Ai.ProcessEngine.Domain.Execution.Context;

/// <summary>
/// Context information for validating a process element.
/// Provides access to element definition and execution context for validation.
/// </summary>
public class ProcessElementValidationContext
{
    /// <summary>Definition of the element being validated.</summary>
    public ProcessElementDefinition Definition { get; set; } = null!;

    /// <summary>Execution context if validation happens during execution.</summary>
    public ProcessElementExecutionContext? ExecutionContext { get; set; }
}
