using FWH.Common.Location;
using FWH.Common.Location.Models;
using Microsoft.AspNetCore.Mvc;

namespace FWH.Location.Api.Controllers;

/// <summary>
/// REST API surface for the shared location service.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class LocationsController : ControllerBase
{
    private readonly ILocationService _locationService;
    private readonly ILogger<LocationsController> _logger;

    public LocationsController(ILocationService locationService, ILogger<LocationsController> logger)
    {
        _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Returns nearby businesses for the supplied coordinates.
    /// </summary>
    [HttpGet("nearby")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<BusinessLocation>>> GetNearbyAsync(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromQuery] int radiusMeters = 30,
        [FromQuery] string[]? categories = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidCoordinate(latitude, -90, 90))
        {
            return BadRequest("Latitude must be between -90 and 90 degrees.");
        }

        if (!IsValidCoordinate(longitude, -180, 180))
        {
            return BadRequest("Longitude must be between -180 and 180 degrees.");
        }

        if (radiusMeters <= 0)
        {
            return BadRequest("Radius must be greater than zero.");
        }

        var results = await _locationService.GetNearbyBusinessesAsync(
            latitude,
            longitude,
            radiusMeters,
            categories,
            cancellationToken);

        return Ok(results);
    }

    /// <summary>
    /// Returns the closest business for the supplied coordinates.
    /// </summary>
    [HttpGet("closest")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BusinessLocation>> GetClosestAsync(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromQuery] int maxDistanceMeters = 1000,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidCoordinate(latitude, -90, 90))
        {
            return BadRequest("Latitude must be between -90 and 90 degrees.");
        }

        if (!IsValidCoordinate(longitude, -180, 180))
        {
            return BadRequest("Longitude must be between -180 and 180 degrees.");
        }

        if (maxDistanceMeters <= 0)
        {
            return BadRequest("Max distance must be greater than zero.");
        }

        var result = await _locationService.GetClosestBusinessAsync(
            latitude,
            longitude,
            maxDistanceMeters,
            cancellationToken);

        if (result == null)
        {
            _logger.LogInformation("No business found within {MaxDistance}m of ({Latitude}, {Longitude})", maxDistanceMeters, latitude, longitude);
            return NotFound();
        }

        return Ok(result);
    }

    private static bool IsValidCoordinate(double value, double min, double max) => value >= min && value <= max;
}
