namespace FWH.Common.Location.Configuration;

/// <summary>
/// Configuration options for the location service.
/// </summary>
public class LocationServiceOptions
{
    /// <summary>
    /// Default search radius in meters if not specified in query.
    /// Default: 30 meters.
    /// </summary>
    public int DefaultRadiusMeters { get; set; } = 30;

    /// <summary>
    /// Maximum allowed search radius in meters.
    /// Default: 5000 meters (5 km) for optimal Overpass API performance.
    /// </summary>
    public int MaxRadiusMeters { get; set; } = 5000;

    /// <summary>
    /// Minimum allowed search radius in meters.
    /// Default: 50 meters.
    /// </summary>
    public int MinRadiusMeters { get; set; } = 50;

    /// <summary>
    /// HTTP timeout for Overpass API requests in seconds.
    /// Default: 30 seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// User agent string for HTTP requests.
    /// Default: "FunWasHad/1.0".
    /// </summary>
    public string UserAgent { get; set; } = "FunWasHad/1.0";

    /// <summary>
    /// Base URL for the Overpass API.
    /// Default: "https://overpass-api.de/api/interpreter".
    /// </summary>
    public string OverpassApiUrl { get; set; } = "https://overpass-api.de/api/interpreter";

    /// <summary>
    /// Validates the radius against configured min/max limits and returns a clamped value.
    /// </summary>
    /// <param name="requestedRadius">The requested radius in meters.</param>
    /// <returns>The clamped radius within valid bounds.</returns>
    public int ValidateAndClampRadius(int requestedRadius)
    {
        if (requestedRadius < MinRadiusMeters)
            return MinRadiusMeters;

        if (requestedRadius > MaxRadiusMeters)
            return MaxRadiusMeters;

        return requestedRadius;
    }
}
