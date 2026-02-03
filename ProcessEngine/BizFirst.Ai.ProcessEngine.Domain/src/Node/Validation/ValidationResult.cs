namespace BizFirst.Ai.ProcessEngine.Domain.Node.Validation;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Result of validating a process element.
/// Contains validation errors if validation failed.
/// </summary>
public class ValidationResult
{
    /// <summary>Whether validation passed.</summary>
    public bool IsValid { get; set; }

    /// <summary>Validation errors, if any.</summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>Gets the first error message.</summary>
    public string? FirstError => Errors.FirstOrDefault();

    /// <summary>Creates a successful validation result.</summary>
    public static ValidationResult Success() => new() { IsValid = true };

    /// <summary>Creates a failed validation result.</summary>
    public static ValidationResult Failure(params string[] errors)
    {
        return new() { IsValid = false, Errors = errors.ToList() };
    }
}
