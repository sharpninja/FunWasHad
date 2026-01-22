using Windows.Devices.Geolocation;
using FWH.Common.Location;
using FWH.Common.Location.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
    private readonly ILogger<WindowsGpsService>? _logger;
    private const int LocationTimeoutSeconds = 30;

    public WindowsGpsService(ILogger<WindowsGpsService>? logger = null)
    {
        _geolocator = new Geolocator
        {
            DesiredAccuracy = PositionAccuracy.High,
            MovementThreshold = 0, // Report all changes
            ReportInterval = 0 // Fastest possible updates
        };
        _logger = logger;
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
                _logger?.LogError(ex, "Error checking location availability");
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
            _logger?.LogWarning(ex, "Location permission denied");
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error requesting location permission");
            return false;
        }
    }

    public async Task<GpsCoordinates?> GetCurrentLocationAsync(
        CancellationToken cancellationToken = default)
    {
        var diagnostics = new Dictionary<string, object?>();

        try
        {
            // Collect diagnostic information
            diagnostics["IsLocationAvailable"] = IsLocationAvailable;
            diagnostics["TimeoutSeconds"] = LocationTimeoutSeconds;

            // Check if location is available first
            if (!IsLocationAvailable)
            {
                try
                {
                    var status = _geolocator.LocationStatus;
                    diagnostics["LocationStatus"] = status.ToString();
                }
                catch (Exception statusEx)
                {
                    diagnostics["LocationStatusError"] = statusEx.Message;
                }

                // Try to request permission
                var granted = await RequestLocationPermissionAsync();
                diagnostics["PermissionRequested"] = true;
                diagnostics["PermissionGranted"] = granted;

                if (!granted)
                {
                    diagnostics["Error"] = "Location permission not granted";
                    throw new LocationServicesException(
                        "Windows",
                        "GetCurrentLocationAsync",
                        "Location permission is not granted",
                        diagnostics);
                }

                // Re-check availability after permission request
                if (!IsLocationAvailable)
                {
                    diagnostics["Error"] = "Location still not available after permission granted";
                    throw new LocationServicesException(
                        "Windows",
                        "GetCurrentLocationAsync",
                        "Location services are not available even after permission was granted",
                        diagnostics);
                }
            }

            try
            {
                var status = _geolocator.LocationStatus;
                diagnostics["LocationStatus"] = status.ToString();
                diagnostics["DesiredAccuracy"] = _geolocator.DesiredAccuracy.ToString();
            }
            catch (Exception statusEx)
            {
                diagnostics["StatusCheckError"] = statusEx.Message;
            }

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
                diagnostics["Error"] = "Position or coordinate is null";
                diagnostics["PositionIsNull"] = position == null;
                diagnostics["CoordinateIsNull"] = position?.Coordinate == null;
                throw new LocationServicesException(
                    "Windows",
                    "GetCurrentLocationAsync",
                    "Position or coordinate is null",
                    diagnostics);
            }

            var coordinate = position.Coordinate;
            diagnostics["PositionObtained"] = true;
            diagnostics["Accuracy"] = coordinate.Accuracy;
            diagnostics["Altitude"] = coordinate.Point.Position.Altitude;

            return new GpsCoordinates(
                coordinate.Point.Position.Latitude,
                coordinate.Point.Position.Longitude,
                coordinate.Accuracy)
            {
                AltitudeMeters = coordinate.Point.Position.Altitude,
                Timestamp = coordinate.Timestamp
            };
        }
        catch (LocationServicesException)
        {
            throw; // Re-throw our custom exception as-is
        }
        catch (OperationCanceledException ex)
        {
            diagnostics["Cancelled"] = true;
            diagnostics["ExceptionType"] = "OperationCanceledException";
            throw new LocationServicesException(
                "Windows",
                "GetCurrentLocationAsync",
                "GPS location request was cancelled or timed out",
                diagnostics,
                ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            diagnostics["Unauthorized"] = true;
            diagnostics["ExceptionType"] = "UnauthorizedAccessException";
            throw new LocationServicesException(
                "Windows",
                "GetCurrentLocationAsync",
                "Unauthorized access to location services",
                diagnostics,
                ex);
        }
        catch (Exception ex)
        {
            diagnostics["ExceptionType"] = ex.GetType().Name;
            diagnostics["ExceptionMessage"] = ex.Message;
            diagnostics["StackTrace"] = ex.StackTrace;

            throw new LocationServicesException(
                "Windows",
                "GetCurrentLocationAsync",
                $"Unexpected error getting GPS location: {ex.Message}",
                diagnostics,
                ex);
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
            _logger?.LogError(ex, "Error getting last known location");
            return null;
        }
    }
}
