using System.ComponentModel.DataAnnotations;

namespace FWH.Location.Api.Data;

internal class LocationConfirmation
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(300)]
    public string BusinessName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? BusinessAddress { get; set; }

    [MaxLength(100)]
    public string? BusinessCategory { get; set; }

    public double BusinessLatitude { get; set; }

    public double BusinessLongitude { get; set; }

    public double UserLatitude { get; set; }

    public double UserLongitude { get; set; }

    public DateTimeOffset ConfirmedAt { get; set; }
}
