using FWH.Common.Location.Models;

namespace FWH.Common.Location;

/// <summary>
/// Service for retrieving nearby businesses and points of interest.
/// Single Responsibility: Location-based business search.
/// </summary>
public interface ILocationService
{
    /// <summary>
    /// Gets nearby businesses within the specified radius.
    /// </summary>
    /// <param name="latitude">The latitude of the center point.</param>
    /// <param name="longitude">The longitude of the center point.</param>
    /// <param name="radiusMeters">The search radius in meters.</param>
    /// <param name="categories">Optional categories to filter by (e.g., "restaurant", "cafe", "shop").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of nearby businesses.</returns>
    Task<IEnumerable<BusinessLocation>> GetNearbyBusinessesAsync(
        double latitude,
        double longitude,
        int radiusMeters,
        IEnumerable<string>? categories = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the closest business to the specified coordinates.
    /// </summary>
    /// <param name="latitude">The latitude of the search point.</param>
    /// <param name="longitude">The longitude of the search point.</param>
    /// <param name="maxDistanceMeters">Maximum distance to search in meters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The closest business or null if none found.</returns>
    Task<BusinessLocation?> GetClosestBusinessAsync(
        double latitude,
        double longitude,
        int maxDistanceMeters = 1000,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reverse geocodes GPS coordinates to get a human-readable address.
    /// </summary>
    /// <param name="latitude">The latitude of the location.</param>
    /// <param name="longitude">The longitude of the location.</param>
    /// <param name="maxDistanceMeters">Maximum distance to search for address data in meters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The address string, or null if no address could be determined.</returns>
    Task<string?> GetAddressAsync(
        double latitude,
        double longitude,
        int maxDistanceMeters = 500,
        CancellationToken cancellationToken = default);
}
