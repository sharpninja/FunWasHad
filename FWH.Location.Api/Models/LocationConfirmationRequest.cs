using System.ComponentModel.DataAnnotations;
using FWH.Common.Location.Models;

namespace FWH.Location.Api.Models;

public sealed record LocationConfirmationRequest
{
    [Required]
    public required BusinessLocation Business { get; init; }

    [Range(-90, 90)]
    public double Latitude { get; init; }

    [Range(-180, 180)]
    public double Longitude { get; init; }
}
