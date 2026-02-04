namespace BizFirst.Ai.ProcessEngine.JS.Services.Security;

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BizFirst.Ai.ProcessEngine.Domain.Definition;

/// <summary>
/// Validates node certification from configuration JSON.
/// Determines JavaScript isolation mode based on certificate validity.
/// </summary>
public class NodeCertificationService : INodeCertificationService
{
    private readonly ILogger<NodeCertificationService> _logger;

    /// <summary>Initializes a new instance.</summary>
    public NodeCertificationService(ILogger<NodeCertificationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<JavaScriptIsolationMode> GetIsolationModeAsync(
        ProcessElementDefinition? elementDefinition,
        CancellationToken cancellationToken = default)
    {
        if (IsCertified(elementDefinition))
        {
            _logger.LogDebug("Node {ElementKey} is certified, using LowIsolation mode",
                elementDefinition?.ProcessElementKey ?? "unknown");
            return JavaScriptIsolationMode.LowIsolation;
        }

        _logger.LogDebug("Node {ElementKey} is not certified, using HighIsolation mode (default)",
            elementDefinition?.ProcessElementKey ?? "unknown");
        return JavaScriptIsolationMode.HighIsolation;
    }

    public bool IsCertified(ProcessElementDefinition? elementDefinition)
    {
        var certificate = GetCertificate(elementDefinition);
        return certificate?.IsValid() ?? false;
    }

    public NodeCertificate? GetCertificate(ProcessElementDefinition? elementDefinition)
    {
        if (elementDefinition?.Configuration == null)
            return null;

        try
        {
            using (var jsonDoc = JsonDocument.Parse(elementDefinition.Configuration))
            {
                var root = jsonDoc.RootElement;

                // Look for "certificate" object in configuration
                if (!root.TryGetProperty("certificate", out var certElement))
                {
                    _logger.LogDebug("No certificate found in configuration for element {ElementKey}",
                        elementDefinition.ProcessElementKey);
                    return null;
                }

                // Parse certificate object
                var certificate = new NodeCertificate();

                if (certElement.TryGetProperty("issued", out var issuedProp))
                {
                    certificate.Issued = issuedProp.GetBoolean();
                }

                if (certElement.TryGetProperty("issuedBy", out var issuedByProp))
                {
                    certificate.IssuedBy = issuedByProp.GetString();
                }

                if (certElement.TryGetProperty("certificateType", out var certTypeProp))
                {
                    certificate.CertificateType = certTypeProp.GetString();
                }

                if (certElement.TryGetProperty("certificateHash", out var hashProp))
                {
                    certificate.CertificateHash = hashProp.GetString();
                }

                if (certElement.TryGetProperty("issuedAt", out var issuedAtProp))
                {
                    certificate.IssuedAt = issuedAtProp.GetString();
                }

                if (certElement.TryGetProperty("expiresAt", out var expiresProp))
                {
                    certificate.ExpiresAt = expiresProp.GetString();
                }

                _logger.LogDebug(
                    "Parsed certificate for element {ElementKey}: Issued={Issued}, IssuedBy={IssuedBy}, Type={Type}",
                    elementDefinition.ProcessElementKey,
                    certificate.Issued,
                    certificate.IssuedBy ?? "N/A",
                    certificate.CertificateType ?? "N/A");

                return certificate;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse certificate from configuration for element {ElementKey}",
                elementDefinition.ProcessElementKey);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting certificate for element {ElementKey}",
                elementDefinition.ProcessElementKey);
            return null;
        }
    }
}
