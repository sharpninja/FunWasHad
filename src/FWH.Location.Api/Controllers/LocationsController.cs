using FWH.Common.Location;
using FWH.Common.Location.Models;
using FWH.Location.Api.Data;
using FWH.Location.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace FWH.Location.Api.Controllers;

    /// <summary>
    /// REST API surface for the shared location service.
    /// Implements TR-API-005: Location API Endpoints.
    /// </summary>
    /// <remarks>
    /// This controller provides endpoints for:
    /// - Nearby business discovery (TR-API-002)
    /// - Location confirmations
    /// </remarks>
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
    /// Implements TR-API-002: Marketing endpoints - nearby business discovery.
    /// </summary>
    /// <param name="latitude">Latitude coordinate (-90 to 90)</param>
    /// <param name="longitude">Longitude coordinate (-180 to 180)</param>
    /// <param name="radiusMeters">Search radius in meters (default: 30)</param>
    /// <param name="categories">Optional category filters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of nearby business locations</returns>
    /// <exception cref="BadRequestResult">Thrown when coordinates are invalid or radius is invalid</exception>
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
    /// <param name="latitude">Latitude coordinate (-90 to 90)</param>
    /// <param name="longitude">Longitude coordinate (-180 to 180)</param>
    /// <param name="maxDistanceMeters">Maximum distance to search in meters (default: 1000)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The closest business location, or null if none found</returns>
    /// <exception cref="BadRequestResult">Thrown when coordinates are invalid or max distance is invalid</exception>
    /// <exception cref="NotFoundResult">Thrown when no business is found within the specified distance</exception>
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
            _logger.LogDebug("No business found within {MaxDistance}m of ({Latitude}, {Longitude})", maxDistanceMeters, latitude, longitude);
            return NotFound();
        }

        return Ok(result);
    }

    /// <summary>
    /// Records a confirmed business location with the reporting GPS coordinates.
    /// </summary>
    /// <param name="request">Location confirmation request with business and user coordinates</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created location confirmation with ID</returns>
    /// <exception cref="BadRequestResult">Thrown when request is invalid, coordinates are out of range, or business name is missing</exception>
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

    private static bool IsValidCoordinate(double value, double min, double max) => value >= min && value <= max;
}
