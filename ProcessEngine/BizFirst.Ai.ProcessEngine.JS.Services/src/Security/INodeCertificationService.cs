namespace BizFirst.Ai.ProcessEngine.JS.Services.Security;

using System.Threading;
using System.Threading.Tasks;
using BizFirst.Ai.ProcessEngine.Domain.Definition;

/// <summary>
/// Service for validating node certification for JavaScript execution.
/// Determines isolation mode based on node certificate from configuration.
/// </summary>
public interface INodeCertificationService
{
    /// <summary>
    /// Gets the JavaScript isolation mode for a node based on its certification.
    /// </summary>
    /// <param name="elementDefinition">The element definition containing certificate info.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>HighIsolation (default) or LowIsolation (if certified).</returns>
    Task<JavaScriptIsolationMode> GetIsolationModeAsync(
        ProcessElementDefinition? elementDefinition,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a node is certified for low isolation mode.
    /// </summary>
    /// <param name="elementDefinition">The element definition to check.</param>
    /// <returns>True if node is certified, false otherwise.</returns>
    bool IsCertified(ProcessElementDefinition? elementDefinition);

    /// <summary>
    /// Gets certificate details from a node's configuration.
    /// </summary>
    /// <param name="elementDefinition">The element definition.</param>
    /// <returns>Certificate object or null if not found/invalid.</returns>
    NodeCertificate? GetCertificate(ProcessElementDefinition? elementDefinition);
}

/// <summary>
/// Represents a node certificate for JavaScript execution privileges.
/// </summary>
public class NodeCertificate
{
    /// <summary>Whether the certificate is issued and active.</summary>
    public bool Issued { get; set; }

    /// <summary>The provider name that issued the certificate.</summary>
    public string? IssuedBy { get; set; }

    /// <summary>Certificate type: Trusted, Verified, or Certified.</summary>
    public string? CertificateType { get; set; }

    /// <summary>Certificate hash for validation.</summary>
    public string? CertificateHash { get; set; }

    /// <summary>Certificate issuance date.</summary>
    public string? IssuedAt { get; set; }

    /// <summary>Certificate expiration date.</summary>
    public string? ExpiresAt { get; set; }

    /// <summary>Checks if certificate is valid and not expired.</summary>
    public bool IsValid()
    {
        if (!Issued || string.IsNullOrEmpty(IssuedBy) || string.IsNullOrEmpty(CertificateType))
            return false;

        if (!IsValidCertificateType(CertificateType))
            return false;

        // Check expiration
        if (!string.IsNullOrEmpty(ExpiresAt))
        {
            if (System.DateTime.TryParse(ExpiresAt, out var expiryDate))
            {
                if (expiryDate < System.DateTime.UtcNow)
                    return false;
            }
        }

        return true;
    }

    /// <summary>Validates certificate type is one of the allowed values.</summary>
    private static bool IsValidCertificateType(string type)
    {
        return type.Equals("Trusted", System.StringComparison.OrdinalIgnoreCase) ||
               type.Equals("Verified", System.StringComparison.OrdinalIgnoreCase) ||
               type.Equals("Certified", System.StringComparison.OrdinalIgnoreCase);
    }
}
