namespace BizFirst.Ai.ProcessEngine.Domain.Exceptions;

using System;

/// <summary>
/// Thrown when workflow definition cannot be loaded from database.
/// </summary>
public class DefinitionLoadException : Exception
{
    /// <summary>Initializes a new instance.</summary>
    public DefinitionLoadException(string message) : base(message)
    {
    }

    /// <summary>Initializes a new instance with inner exception.</summary>
    public DefinitionLoadException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
