namespace BizFirst.Ai.ProcessEngine.Domain.Definition;

using BizFirst.Ai.Process.Domain.Entities;

/// <summary>
/// Execution-specific wrapper for ProcessElement providing orchestration properties.
/// Wraps the core ProcessElement entity and adds execution-specific data.
/// </summary>
public class ProcessElementDefinition //todo: remove
{
    /// <summary>Gets or sets the underlying process element entity.</summary>
    public ProcessElement Element { get; set; }

    /// <summary>Gets the process element ID.</summary>
    public int ProcessElementID => Element.ProcessElementID;

    /// <summary>Gets the process element key (unique within workflow).</summary>
    public string ProcessElementKey => Element.ProcessElementKey;

    /// <summary>Gets the process element type ID.</summary>
    public int ProcessElementTypeID => Element.ProcessElementTypeID;

    /// <summary>Gets the process element type name (loaded from type reference).</summary>
    public string ProcessElementTypeName { get; set; }

    /// <summary>Gets whether the element is disabled.</summary>
    public bool IsDisabled => Element.IsDisabled;

    /// <summary>Gets the timeout in seconds for this element.</summary>
    public int TimeoutSeconds => Element.Timeout ?? 300; // Default 5 minutes

    /// <summary>Gets the configuration JSON for this element.</summary>
    public string Configuration => Element.Configuration;

    /// <summary>Gets whether this is a trigger element.</summary>
    public bool IsTrigger => Element.IsTrigger;

    /// <summary>Gets whether execution should continue on failure.</summary>
    public bool ContinueOnFail => Element.ContinueOnFail;

    /// <summary>Gets whether to always output data even on failure.</summary>
    public bool AlwaysOutputData => Element.AlwaysOutputData;

    /// <summary>Initializes a new instance.</summary>
    public ProcessElementDefinition(ProcessElement element, string elementTypeName)
    {
        Element = element;
        ProcessElementTypeName = elementTypeName;
    }
}
