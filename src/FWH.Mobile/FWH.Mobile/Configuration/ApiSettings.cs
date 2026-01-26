namespace FWH.Mobile.Configuration;

/// <summary>
/// API settings loaded from appsettings.json
/// </summary>
public sealed class ApiSettings
{
    /// <summary>
    /// The IP address of the host machine running the APIs.
    /// Defaults to 10.0.2.2 (Android emulator alias for localhost)
    /// </summary>
    public string HostIpAddress { get; set; } = "10.0.2.2";

    /// <summary>
    /// Port for the Location API
    /// </summary>
    public int LocationApiPort { get; set; } = 4748;

    /// <summary>
    /// Port for the Marketing API
    /// </summary>
    public int MarketingApiPort { get; set; } = 4749;

    /// <summary>
    /// Whether to use HTTPS for API connections
    /// </summary>
    public bool UseHttps { get; set; }

    /// <summary>
    /// Full base URL for the Location API (overrides HostIpAddress/Port if set).
    /// Used for staging/production environments with full URLs (e.g., Railway).
    /// </summary>
    public string? LocationApiBaseUrl { get; set; }

    /// <summary>
    /// Full base URL for the Marketing API (overrides HostIpAddress/Port if set).
    /// Used for staging/production environments with full URLs (e.g., Railway).
    /// </summary>
    public string? MarketingApiBaseUrl { get; set; }

    /// <summary>
    /// Gets the resolved base URI for the Location API (from override or from Host+Port).
    /// </summary>
    public Uri GetResolvedLocationApiBaseUrl()
    {
        // If full URL is provided (e.g., for staging), use it directly
        if (!string.IsNullOrWhiteSpace(LocationApiBaseUrl))
        {
            var s = LocationApiBaseUrl.EndsWith('/') ? LocationApiBaseUrl : LocationApiBaseUrl + "/";
            return new Uri(s, UriKind.Absolute);
        }

        // Otherwise, construct from HostIpAddress and Port
        var protocol = UseHttps ? "https" : "http";
        return new Uri($"{protocol}://{HostIpAddress}:{LocationApiPort}/", UriKind.Absolute);
    }

    /// <summary>
    /// Gets the resolved base URI for the Marketing API (from override or from Host+Port).
    /// </summary>
    public Uri GetResolvedMarketingApiBaseUrl()
    {
        // If full URL is provided (e.g., for staging), use it directly
        if (!string.IsNullOrWhiteSpace(MarketingApiBaseUrl))
        {
            var s = MarketingApiBaseUrl.EndsWith('/') ? MarketingApiBaseUrl : MarketingApiBaseUrl + "/";
            return new Uri(s, UriKind.Absolute);
        }

        // Otherwise, construct from HostIpAddress and Port
        var protocol = UseHttps ? "https" : "http";
        return new Uri($"{protocol}://{HostIpAddress}:{MarketingApiPort}/", UriKind.Absolute);
    }
}
