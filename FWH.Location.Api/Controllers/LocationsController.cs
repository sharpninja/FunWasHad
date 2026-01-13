using FWH.Common.Location;
using FWH.Common.Location.Models;
using FWH.Location.Api.Data;
using FWH.Location.Api.Models;
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
    private readonly LocationDbContext _dbContext;
    private readonly ILogger<LocationsController> _logger;

    public LocationsController(
        ILocationService locationService,
        LocationDbContext dbContext,
        ILogger<LocationsController> logger)
    {
        _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
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

    /// <summary>
    /// Records a confirmed business location with the reporting GPS coordinates.
    /// </summary>
    [HttpPost("confirmed")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LocationConfirmed(
        [FromBody] LocationConfirmationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest("Request body is required.");
        }

        if (request.Business is null)
        {
            return BadRequest("Business payload is required.");
        }

        if (!IsValidCoordinate(request.Latitude, -90, 90) || !IsValidCoordinate(request.Longitude, -180, 180))
        {
            return BadRequest("Reported coordinates are out of range.");
        }

        if (!IsValidCoordinate(request.Business.Latitude, -90, 90) || !IsValidCoordinate(request.Business.Longitude, -180, 180))
        {
            return BadRequest("Business coordinates are out of range.");
        }

        if (string.IsNullOrWhiteSpace(request.Business.Name))
        {
            return BadRequest("Business name is required.");
        }

        var entity = new LocationConfirmation
        {
            BusinessName = request.Business.Name,
            BusinessAddress = request.Business.Address,
            BusinessCategory = request.Business.Category,
            BusinessLatitude = request.Business.Latitude,
            BusinessLongitude = request.Business.Longitude,
            UserLatitude = request.Latitude,
            UserLongitude = request.Longitude,
            ConfirmedAt = DateTimeOffset.UtcNow
        };

        _dbContext.LocationConfirmations.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Location confirmed: {Business} at ({BusinessLat},{BusinessLon}) reported from ({UserLat},{UserLon})",
            entity.BusinessName,
            entity.BusinessLatitude,
            entity.BusinessLongitude,
            entity.UserLatitude,
            entity.UserLongitude);

        return Created($"/api/locations/confirmed/{entity.Id}", new { entity.Id });
    }

    /// <summary>
    /// Updates the location of a device. Used for location tracking.
    /// </summary>
    [HttpPost("device")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateDeviceLocation(
        [FromBody] DeviceLocationUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest("Request body is required.");
        }

        if (string.IsNullOrWhiteSpace(request.DeviceId))
        {
            return BadRequest("Device ID is required.");
        }

        if (!IsValidCoordinate(request.Latitude, -90, 90))
        {
            return BadRequest("Latitude must be between -90 and 90 degrees.");
        }

        if (!IsValidCoordinate(request.Longitude, -180, 180))
        {
            return BadRequest("Longitude must be between -180 and 180 degrees.");
        }

        var entity = new DeviceLocation
        {
            DeviceId = request.DeviceId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            AccuracyMeters = request.AccuracyMeters,
            Timestamp = request.Timestamp ?? DateTimeOffset.UtcNow,
            RecordedAt = DateTimeOffset.UtcNow
        };

        _dbContext.DeviceLocations.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Device location updated: {DeviceId} at ({Lat:F6},{Lon:F6}) with accuracy {Accuracy:F1}m",
            entity.DeviceId,
            entity.Latitude,
            entity.Longitude,
            entity.AccuracyMeters ?? 0);

        return Ok(new { entity.Id, entity.Timestamp });
    }

    private static bool IsValidCoordinate(double value, double min, double max) => value >= min && value <= max;
}
