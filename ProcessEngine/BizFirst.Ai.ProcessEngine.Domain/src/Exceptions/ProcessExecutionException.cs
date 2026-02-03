namespace BizFirst.Ai.ProcessEngine.Domain.Exceptions;

using System;

/// <summary>
/// Thrown when an error occurs during process execution.
/// </summary>
public class ProcessExecutionException : Exception
{
    /// <summary>Process ID where error occurred.</summary>
    public int? ProcessID { get; set; }

    /// <summary>Process execution ID.</summary>
    public int? ProcessExecutionID { get; set; }

    /// <summary>Initializes a new instance.</summary>
    public ProcessExecutionException(string message) : base(message)
    {
    }

    /// <summary>Initializes a new instance with process context.</summary>
    public ProcessExecutionException(string message, int processID, int processExecutionID) : base(message)
    {
        ProcessID = processID;
        ProcessExecutionID = processExecutionID;
    }

    /// <summary>Initializes a new instance with inner exception.</summary>
    public ProcessExecutionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
