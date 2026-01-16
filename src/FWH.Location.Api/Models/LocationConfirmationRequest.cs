using System.ComponentModel.DataAnnotations;
using FWH.Common.Location.Models;

namespace FWH.Location.Api.Models;

/// <summary>
/// Request model for confirming a business location with user GPS coordinates.
/// Implements TR-API-005: Location API Endpoints request validation.
/// </summary>
/// <remarks>
/// Used to record when a user confirms a business location with their current GPS coordinates.
/// Implements TR-SEC-001 (data validation) with coordinate range validation.
/// </remarks>
public sealed record LocationConfirmationRequest
{
    /// <summary>
    /// Business location information being confirmed.
    /// </summary>
    [Required]
    public required BusinessLocation Business { get; init; }

    /// <summary>
    /// User's latitude coordinate when confirming the location.
    /// Must be between -90 and 90 degrees.
    /// </summary>
    [Required]
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90 degrees.")]
    public double Latitude { get; init; }

    /// <summary>
    /// User's longitude coordinate when confirming the location.
    /// Must be between -180 and 180 degrees.
    /// </summary>
    [Required]
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180 degrees.")]
    public double Longitude { get; init; }
}
