namespace BizFirst.Ai.ProcessEngine.Domain.Connector;

using System.Collections.Generic;

/// <summary>
/// Connector configuration data for external service integration.
/// Contains all information needed to call external APIs or services.
/// </summary>
public class ConnectorData
{
    /// <summary>ID of the connector.</summary>
    public int ConnectorID { get; set; }

    /// <summary>Display name of the connector.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Base URL for API requests.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>API endpoint path.</summary>
    public string ApiEndPoint { get; set; } = string.Empty;

    /// <summary>Timeout in seconds for requests.</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>Number of retries on failure.</summary>
    public int RetryCount { get; set; }

    /// <summary>API Key for authentication.</summary>
    public string? ApiKey { get; set; }

    /// <summary>API Secret for authentication.</summary>
    public string? ApiSecret { get; set; }

    /// <summary>Other connector-specific configuration.</summary>
    public Dictionary<string, object> Configuration { get; set; } = new();
}
