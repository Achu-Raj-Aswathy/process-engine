namespace BizFirst.Ai.ProcessEngine.Domain.Definition;

using System.Collections.Generic;
using System.Linq;
using BizFirst.Ai.Process.Domain.Entities;

/// <summary>
/// Execution-specific wrapper for ProcessThread providing orchestration properties.
/// Wraps the core ProcessThread entity and adds execution-specific data.
/// </summary>
public class ProcessThreadDefinition //todo: remove
{
    /// <summary>Gets or sets the underlying process thread entity.</summary>
    public ProcessThread Thread { get; set; }

    /// <summary>Gets the process thread ID.</summary>
    public int ProcessThreadID => Thread.ProcessThreadID;

    /// <summary>Gets the current version ID of the process thread.</summary>
    public int ProcessThreadVersionID => Thread.CurrentVersionID ?? 0;

    /// <summary>Gets the list of all process elements in this thread.</summary>
    public List<ProcessElementDefinition> Elements { get; set; } = new();

    /// <summary>Gets the list of all connections between elements.</summary>
    public List<ConnectionDefinition> Connections { get; set; } = new();

    /// <summary>Gets the thread name.</summary>
    public string Name => Thread.Name;

    /// <summary>Gets the thread description.</summary>
    public string Description => Thread.Description;

    /// <summary>Gets whether the thread is enabled.</summary>
    public bool IsEnabled => Thread.Enabled;

    /// <summary>Gets all trigger nodes (nodes with IsTrigger=true).</summary>
    /// <returns>List of trigger element definitions.</returns>
    public List<ProcessElementDefinition> GetTriggerNodes()
    {
        return Elements.Where(e => e.IsTrigger).ToList();
    }

    /// <summary>Initializes a new instance.</summary>
    public ProcessThreadDefinition(ProcessThread thread)
    {
        Thread = thread;
    }
}
