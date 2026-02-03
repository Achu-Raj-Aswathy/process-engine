namespace BizFirst.Ai.ProcessEngine.Domain.Exceptions;

using System;

/// <summary>
/// Thrown when execution context cannot be accessed.
/// Usually means ExecutionContextMiddleware did not run.
/// </summary>
public class ContextAccessException : Exception
{
    /// <summary>Initializes a new instance.</summary>
    public ContextAccessException(string message) : base(message)
    {
    }

    /// <summary>Initializes a new instance with inner exception.</summary>
    public ContextAccessException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
