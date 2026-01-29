using CoreLocation;
using FWH.Common.Location;
using FWH.Common.Location.Models;
using Microsoft.Extensions.Logging;

namespace FWH.Mobile.iOS.Services;

/// <summary>
/// iOS implementation of GPS service using CLLocationManager.
/// </summary>
internal class iOSGpsService : IGpsService
{
    private readonly CLLocationManager _locationManager;
    private readonly ILogger<iOSGpsService>? _logger;
    private TaskCompletionSource<GpsCoordinates?>? _locationTcs;
    private const int LocationTimeoutSeconds = 30;

    public iOSGpsService(ILogger<iOSGpsService>? logger = null)
    {
        _locationManager = new CLLocationManager
        {
            DesiredAccuracy = CLLocation.AccuracyBest,
            DistanceFilter = CLLocationDistance.FilterNone
        };

        _locationManager.LocationsUpdated += OnLocationsUpdated;
        _locationManager.Failed += OnLocationFailed;
        _logger = logger;
    }

    public bool IsLocationAvailable
    {
        get
        {
            var status = CLLocationManager.Status;
            return status == CLAuthorizationStatus.AuthorizedAlways ||
                   status == CLAuthorizationStatus.AuthorizedWhenInUse;
        }
    }

    public async Task<bool> RequestLocationPermissionAsync()
    {
        var status = CLLocationManager.Status;

        if (status == CLAuthorizationStatus.AuthorizedAlways ||
            status == CLAuthorizationStatus.AuthorizedWhenInUse)
        {
            return true;
        }

        if (status == CLAuthorizationStatus.NotDetermined)
        {
            var tcs = new TaskCompletionSource<bool>();

            void handler(object? sender, CLAuthorizationChangedEventArgs e)
            {
                _locationManager.AuthorizationChanged -= handler;
                var authorized = e.Status == CLAuthorizationStatus.AuthorizedAlways ||
                               e.Status == CLAuthorizationStatus.AuthorizedWhenInUse;
                tcs.TrySetResult(authorized);
            }

            _locationManager.AuthorizationChanged += handler;
            _locationManager.RequestWhenInUseAuthorization();

            // Timeout after 10 seconds
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            cts.Token.Register(() => tcs.TrySetResult(false));

            return await tcs.Task.ConfigureAwait(false);
        }

        return false;
    }

    public async Task<GpsCoordinates?> GetCurrentLocationAsync(CancellationToken cancellationToken = default)
    {
        var diagnostics = new Dictionary<string, object?>();

        try
        {
            var status = CLLocationManager.Status;
            diagnostics["AuthorizationStatus"] = status.ToString();
            diagnostics["IsLocationAvailable"] = IsLocationAvailable;
            diagnostics["TimeoutSeconds"] = LocationTimeoutSeconds;
            diagnostics["DesiredAccuracy"] = _locationManager.DesiredAccuracy.ToString();
            diagnostics["DistanceFilter"] = _locationManager.DistanceFilter.ToString();

            if (!IsLocationAvailable)
            {
                // Try to request permission
                var granted = await RequestLocationPermissionAsync().ConfigureAwait(false);
                diagnostics["PermissionRequested"] = true;
                diagnostics["PermissionGranted"] = granted;

                var newStatus = CLLocationManager.Status;
                diagnostics["AuthorizationStatusAfterRequest"] = newStatus.ToString();

                if (!granted)
                {
                    diagnostics["Error"] = "Location permission not granted";
                    throw new LocationServicesException(
                        "iOS",
                        "GetCurrentLocationAsync",
                        $"Location permission is not granted (status: {status})",
                        diagnostics);
                }

                // Re-check availability after permission request
                if (!IsLocationAvailable)
                {
                    diagnostics["Error"] = "Location still not available after permission granted";
                    throw new LocationServicesException(
                        "iOS",
                        "GetCurrentLocationAsync",
                        "Location services are not available even after permission was granted",
                        diagnostics);
                }
            }

            _locationTcs = new TaskCompletionSource<GpsCoordinates?>();

            // Start location updates
            _locationManager.StartUpdatingLocation();
            diagnostics["LocationUpdatesStarted"] = true;

            // Set up timeout
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(LocationTimeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            linkedCts.Token.Register(() =>
            {
                if (!_locationTcs.Task.IsCompleted)
                {
                    diagnostics["TimedOut"] = true;
                    _locationTcs?.TrySetResult(null);
                }
            });

            var result = await _locationTcs.Task.ConfigureAwait(false);

            // Stop location updates
            _locationManager.StopUpdatingLocation();

            if (result == null)
            {
                diagnostics["Error"] = "Location request completed but returned null";
                throw new LocationServicesException(
                    "iOS",
                    "GetCurrentLocationAsync",
                    "Location request completed but no location was obtained",
                    diagnostics);
            }

            diagnostics["PositionObtained"] = true;
            diagnostics["Accuracy"] = result.AccuracyMeters;
            return result;
        }
        catch (LocationServicesException)
        {
            // Ensure location updates are stopped
            try
            {
                _locationManager.StopUpdatingLocation();
            }
            catch { }
            throw; // Re-throw our custom exception as-is
        }
        catch (Exception ex)
        {
            // Ensure location updates are stopped
            try
            {
                _locationManager.StopUpdatingLocation();
            }
            catch { }

            diagnostics["ExceptionType"] = ex.GetType().Name;
            diagnostics["ExceptionMessage"] = ex.Message;
            diagnostics["StackTrace"] = ex.StackTrace;

            throw new LocationServicesException(
                "iOS",
                "GetCurrentLocationAsync",
                $"Unexpected error getting GPS location: {ex.Message}",
                diagnostics,
                ex);
        }
    }

    private void OnLocationsUpdated(object? sender, CLLocationsUpdatedEventArgs e)
    {
        if (_locationTcs != null && !_locationTcs.Task.IsCompleted && e.Locations.Length > 0)
        {
            var location = e.Locations[e.Locations.Length - 1]; // Get most recent location

            var coordinates = new GpsCoordinates(
                location.Coordinate.Latitude,
                location.Coordinate.Longitude,
                location.HorizontalAccuracy)
            {
                AltitudeMeters = location.Altitude,
                SpeedMetersPerSecond = location.Speed >= 0 ? location.Speed : null,
                Timestamp = DateTimeOffset.FromUnixTimeSeconds((long)location.Timestamp.SecondsSinceReferenceDate)
            };

            _locationTcs.TrySetResult(coordinates);
        }
    }

    private void OnLocationFailed(object? sender, NSErrorEventArgs e)
    {
        _logger?.LogError("Location manager failed: {Error}", e.Error);
        // Set result to null - the calling method will throw LocationServicesException
        _locationTcs?.TrySetResult(null);
    }
}
