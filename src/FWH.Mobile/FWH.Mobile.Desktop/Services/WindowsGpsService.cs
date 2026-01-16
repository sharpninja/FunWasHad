using Windows.Devices.Geolocation;
using FWH.Common.Location;
using FWH.Common.Location.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FWH.Mobile.Desktop.Services;

/// <summary>
/// Windows implementation of GPS service using Windows.Devices.Geolocation.
/// Requires Windows 10/11 and appropriate capabilities in Package.appxmanifest.
/// </summary>
public class WindowsGpsService : IGpsService
{
    private readonly Geolocator _geolocator;
    private const int LocationTimeoutSeconds = 30;

    public WindowsGpsService()
    {
        _geolocator = new Geolocator
        {
            DesiredAccuracy = PositionAccuracy.High,
            MovementThreshold = 0, // Report all changes
            ReportInterval = 0 // Fastest possible updates
        };
    }

    public bool IsLocationAvailable
    {
        get
        {
            try
            {
                var status = _geolocator.LocationStatus;
                return status == PositionStatus.Ready || 
                       status == PositionStatus.Initializing;
            }
            catch (UnauthorizedAccessException)
            {
                // Location capability not declared or permission denied
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking location availability: {ex}");
                return false;
            }
        }
    }

    public async Task<bool> RequestLocationPermissionAsync()
    {
        try
        {
            var accessStatus = await Geolocator.RequestAccessAsync();
            return accessStatus == GeolocationAccessStatus.Allowed;
        }
        catch (UnauthorizedAccessException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Location permission denied: {ex}");
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error requesting location permission: {ex}");
            return false;
        }
    }

    public async Task<GpsCoordinates?> GetCurrentLocationAsync(
        CancellationToken cancellationToken = default)
    {
        // Check if location is available first
        if (!IsLocationAvailable)
        {
            // Try to request permission
            var granted = await RequestLocationPermissionAsync();
            if (!granted)
            {
                System.Diagnostics.Debug.WriteLine("Location permission not granted");
                return null;
            }
        }

        try
        {
            // Create timeout cancellation token
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(LocationTimeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutCts.Token);

            // Get current position with cancellation support
            var position = await _geolocator.GetGeopositionAsync()
                .AsTask(linkedCts.Token);

            if (position?.Coordinate == null)
            {
                System.Diagnostics.Debug.WriteLine("Position or coordinate is null");
                return null;
            }

            var coordinate = position.Coordinate;
            
            return new GpsCoordinates(
                coordinate.Point.Position.Latitude,
                coordinate.Point.Position.Longitude,
                coordinate.Accuracy)
            {
                AltitudeMeters = coordinate.Point.Position.Altitude,
                Timestamp = coordinate.Timestamp
            };
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine("GPS location request cancelled or timed out");
            return null;
        }
        catch (UnauthorizedAccessException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Unauthorized access to location: {ex}");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting GPS location: {ex}");
            return null;
        }
    }

    /// <summary>
    /// Gets the last known position (cached location).
    /// This is faster than GetCurrentLocationAsync but may return stale data.
    /// </summary>
    public async Task<GpsCoordinates?> GetLastKnownLocationAsync()
    {
        try
        {
            var position = await _geolocator.GetGeopositionAsync(
                maximumAge: TimeSpan.FromMinutes(5),
                timeout: TimeSpan.FromSeconds(5));

            if (position?.Coordinate == null)
                return null;

            var coordinate = position.Coordinate;
            
            return new GpsCoordinates(
                coordinate.Point.Position.Latitude,
                coordinate.Point.Position.Longitude,
                coordinate.Accuracy)
            {
                AltitudeMeters = coordinate.Point.Position.Altitude,
                Timestamp = coordinate.Timestamp
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting last known location: {ex}");
            return null;
        }
    }
}
