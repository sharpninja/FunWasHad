using FWH.Common.Location.Models;

namespace FWH.Common.Location.Services;

/// <summary>
/// Fallback GPS service for platforms without GPS support (desktop, browser).
/// Returns null for all location requests.
/// </summary>
public class NoGpsService : IGpsService
{
    public bool IsLocationAvailable => false;

    public Task<GpsCoordinates?> GetCurrentLocationAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<GpsCoordinates?>(null);
    }

    public Task<bool> RequestLocationPermissionAsync()
    {
        return Task.FromResult(false);
    }
}
