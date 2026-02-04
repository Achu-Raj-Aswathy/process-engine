namespace BizFirst.Ai.ProcessEngine.JS.Services.Security;

/// <summary>
/// JavaScript isolation modes for expression execution.
/// Determines sandbox restrictions based on node certification.
/// </summary>
public enum JavaScriptIsolationMode
{
    /// <summary>
    /// High isolation mode (DEFAULT for uncertified nodes).
    /// Sandbox with strict restrictions: 5s timeout, 10MB memory, no file/network access.
    /// Used for untrusted or uncertified JavaScript expressions.
    /// </summary>
    HighIsolation = 0,

    /// <summary>
    /// Low isolation mode (for certified nodes only).
    /// More permissive: 30s timeout, 50MB memory, controlled API access.
    /// Only available if node is certified by the provider.
    /// </summary>
    LowIsolation = 1
}
