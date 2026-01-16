using FWH.Common.Location.Models;

namespace FWH.Common.Location;

/// <summary>
/// Service for retrieving GPS coordinates from the device.
/// Platform-specific implementations required for mobile platforms.
/// </summary>
public interface IGpsService
{
    /// <summary>
    /// Gets the current GPS coordinates from the device.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>GpsCoordinates containing latitude and longitude, or null if location unavailable.</returns>
    Task<GpsCoordinates?> GetCurrentLocationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if GPS/location services are available and enabled on the device.
    /// </summary>
    bool IsLocationAvailable { get; }

    /// <summary>
    /// Requests location permissions from the user.
    /// Returns true if permissions are granted.
    /// </summary>
    Task<bool> RequestLocationPermissionAsync();
}
