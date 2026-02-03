namespace BizFirst.Ai.ProcessEngine.Domain.Definition;

using BizFirst.Ai.Process.Domain.Entities;

/// <summary>
/// Execution-specific wrapper for Connection providing orchestration properties.
/// Wraps the core Connection entity and adds execution-specific data.
/// </summary>
public class ConnectionDefinition //todo: remove
{
    /// <summary>Gets or sets the underlying connection entity.</summary>
    public Connection Connection { get; set; }

    /// <summary>Gets the source process element ID.</summary>
    public int SourceProcessElementID => Connection.SourceProcessElementID ?? 0;

    /// <summary>Gets the source port key (e.g., "success", "error", "default").</summary>
    public string SourcePortKey => Connection.SourcePortKey ?? "main";

    /// <summary>Gets the target process element ID.</summary>
    public int TargetProcessElementID => Connection.TargetProcessElementID ?? 0;

    /// <summary>Gets the target port key.</summary>
    public string TargetPortKey => Connection.TargetPortKey ?? "input";

    /// <summary>Gets the condition expression for conditional routing.</summary>
    public string Condition => Connection.Condition ?? string.Empty;

    /// <summary>Gets whether this is a conditional connection.</summary>
    public bool IsConditional => Connection.IsConditional;

    /// <summary>Initializes a new instance.</summary>
    public ConnectionDefinition(Connection connection)
    {
        Connection = connection;
    }
}
