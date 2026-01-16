using System.ComponentModel.DataAnnotations;

namespace FWH.Location.Api.Data;

/// <summary>
/// Entity for storing device location updates.
/// </summary>
public class DeviceLocation
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string DeviceId { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public double? AccuracyMeters { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    public DateTimeOffset RecordedAt { get; set; }
}
