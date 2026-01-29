namespace FWH.Mobile.Configuration;

/// <summary>
/// Location tracking settings loaded from appsettings.json
/// </summary>
public sealed class LocationSettings
{
    /// <summary>
    /// Speed unit preference for display.
    /// Valid values: "mph" (miles per hour) or "kmh" (kilometers per hour).
    /// Default: "mph"
    /// </summary>
    public string SpeedUnit { get; set; } = "mph";

    /// <summary>
    /// Polling interval mode for location tracking.
    /// Valid values: "fast" (0.5 seconds), "normal" (1.0 seconds), "off" (0.0 seconds - tracking disabled).
    /// Default: "normal"
    /// </summary>
    public string PollingIntervalMode { get; set; } = "normal";

    /// <summary>
    /// Gets whether speed should be displayed in miles per hour.
    /// </summary>
    public bool UseMph => SpeedUnit?.ToLowerInvariant() == "mph";

    /// <summary>
    /// Gets whether speed should be displayed in kilometers per hour.
    /// </summary>
    public bool UseKmh => SpeedUnit?.ToLowerInvariant() == "kmh" || SpeedUnit?.ToLowerInvariant() == "km/h";

    /// <summary>
    /// Gets the polling interval as a TimeSpan based on the configured mode.
    /// </summary>
    public TimeSpan GetPollingInterval()
    {
        return PollingIntervalMode?.ToLowerInvariant() switch
        {
            "fast" => TimeSpan.FromSeconds(0.5),
            "normal" => TimeSpan.FromSeconds(1.0),
            "off" => TimeSpan.Zero,
            _ => TimeSpan.FromSeconds(1.0) // Default to normal
        };
    }

    /// <summary>
    /// Gets whether location tracking is enabled (polling interval is not zero).
    /// </summary>
    public bool IsTrackingEnabled => GetPollingInterval() > TimeSpan.Zero;
}
