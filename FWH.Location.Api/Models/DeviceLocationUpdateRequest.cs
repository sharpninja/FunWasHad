using System.ComponentModel.DataAnnotations;

namespace FWH.Location.Api.Models;

/// <summary>
/// Request model for updating device location.
/// </summary>
public sealed record DeviceLocationUpdateRequest
{
    /// <summary>
    /// Unique identifier for the device (e.g., device ID or user ID).
    /// </summary>
    [Required]
    [MaxLength(100)]
    public required string DeviceId { get; init; }

    /// <summary>
    /// Latitude of the device location.
    /// </summary>
    [Required]
    [Range(-90, 90)]
    public double Latitude { get; init; }

    /// <summary>
    /// Longitude of the device location.
    /// </summary>
    [Required]
    [Range(-180, 180)]
    public double Longitude { get; init; }

    /// <summary>
    /// Accuracy of the GPS reading in meters (optional).
    /// </summary>
    [Range(0, double.MaxValue)]
    public double? AccuracyMeters { get; init; }

    /// <summary>
    /// Timestamp when the location was captured.
    /// </summary>
    public DateTimeOffset? Timestamp { get; init; }
}
