namespace FWH.Mobile.Data.Entities;

/// <summary>
/// Entity for storing device location history in local SQLite database.
/// Device location is tracked locally and NEVER sent to the API.
/// TR-MOBILE-001: Local-only device location tracking
/// </summary>
public class DeviceLocationEntity
{
    /// <summary>
    /// Primary key for the location record
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Device identifier (generated GUID)
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Latitude in decimal degrees
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// Longitude in decimal degrees
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// Accuracy of the location in meters (optional)
    /// </summary>
    public double? AccuracyMeters { get; set; }

    /// <summary>
    /// Altitude above sea level in meters (optional)
    /// </summary>
    public double? AltitudeMeters { get; set; }

    /// <summary>
    /// Speed in meters per second (optional)
    /// </summary>
    public double? SpeedMetersPerSecond { get; set; }

    /// <summary>
    /// Heading/bearing in degrees (0-360, optional)
    /// </summary>
    public double? HeadingDegrees { get; set; }

    /// <summary>
    /// Movement state at time of recording (Stationary, Walking, Riding, Moving)
    /// </summary>
    public string MovementState { get; set; } = "Stationary";

    /// <summary>
    /// Timestamp when the location was recorded
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Address at this location (if reverse geocoded)
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// When this record was created in the database
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
