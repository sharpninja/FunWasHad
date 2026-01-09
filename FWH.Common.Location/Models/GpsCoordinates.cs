namespace FWH.Common.Location.Models;

/// <summary>
/// Represents GPS coordinates (latitude and longitude).
/// </summary>
public class GpsCoordinates
{
    /// <summary>
    /// Latitude in decimal degrees (-90 to 90).
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// Longitude in decimal degrees (-180 to 180).
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// Accuracy of the location in meters (optional).
    /// </summary>
    public double? AccuracyMeters { get; set; }

    /// <summary>
    /// Altitude in meters above sea level (optional).
    /// </summary>
    public double? AltitudeMeters { get; set; }

    /// <summary>
    /// Timestamp when the coordinates were obtained.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    public GpsCoordinates()
    {
    }

    public GpsCoordinates(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public GpsCoordinates(double latitude, double longitude, double accuracyMeters)
        : this(latitude, longitude)
    {
        AccuracyMeters = accuracyMeters;
    }

    /// <summary>
    /// Validates that the coordinates are within valid ranges.
    /// </summary>
    public bool IsValid()
    {
        return Latitude >= -90 && Latitude <= 90 &&
               Longitude >= -180 && Longitude <= 180;
    }

    public override string ToString()
    {
        return $"({Latitude:F6}, {Longitude:F6})";
    }
}
