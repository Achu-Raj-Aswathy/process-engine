namespace BizFirst.Ai.ProcessEngine.Domain.Exceptions;

using System;

/// <summary>
/// Thrown when an error occurs executing a specific node.
/// </summary>
public class NodeExecutionException : Exception
{
    /// <summary>ID of the node that failed.</summary>
    public int? ProcessElementID { get; set; }

    /// <summary>Key of the node that failed.</summary>
    public string? ProcessElementKey { get; set; }

    /// <summary>Node type that failed.</summary>
    public string? NodeType { get; set; }

    /// <summary>Initializes a new instance.</summary>
    public NodeExecutionException(string message) : base(message)
    {
    }

    /// <summary>Initializes a new instance with node context.</summary>
    public NodeExecutionException(string message, string nodeKey, string nodeType) : base(message)
    {
        ProcessElementKey = nodeKey;
        NodeType = nodeType;
    }

    /// <summary>Initializes a new instance with inner exception.</summary>
    public NodeExecutionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
