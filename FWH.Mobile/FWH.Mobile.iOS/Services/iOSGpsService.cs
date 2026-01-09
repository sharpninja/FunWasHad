using CoreLocation;
using FWH.Common.Location;
using FWH.Common.Location.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FWH.Mobile.iOS.Services;

/// <summary>
/// iOS implementation of GPS service using CLLocationManager.
/// </summary>
public class iOSGpsService : IGpsService
{
    private readonly CLLocationManager _locationManager;
    private TaskCompletionSource<GpsCoordinates?>? _locationTcs;
    private const int LocationTimeoutSeconds = 30;

    public iOSGpsService()
    {
        _locationManager = new CLLocationManager
        {
            DesiredAccuracy = CLLocation.AccuracyBest,
            DistanceFilter = CLLocationDistance.FilterNone
        };

        _locationManager.LocationsUpdated += OnLocationsUpdated;
        _locationManager.Failed += OnLocationFailed;
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

            return await tcs.Task;
        }

        return false;
    }

    public async Task<GpsCoordinates?> GetCurrentLocationAsync(CancellationToken cancellationToken = default)
    {
        if (!IsLocationAvailable)
        {
            // Try to request permission
            var granted = await RequestLocationPermissionAsync();
            if (!granted)
                return null;
        }

        try
        {
            _locationTcs = new TaskCompletionSource<GpsCoordinates?>();

            // Start location updates
            _locationManager.StartUpdatingLocation();

            // Set up timeout
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(LocationTimeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            linkedCts.Token.Register(() =>
            {
                _locationTcs?.TrySetResult(null);
            });

            var result = await _locationTcs.Task;

            // Stop location updates
            _locationManager.StopUpdatingLocation();

            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting GPS location: {ex}");
            _locationManager.StopUpdatingLocation();
            return null;
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
                Timestamp = DateTimeOffset.FromUnixTimeSeconds((long)location.Timestamp.SecondsSinceReferenceDate)
            };

            _locationTcs.TrySetResult(coordinates);
        }
    }

    private void OnLocationFailed(object? sender, NSErrorEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"Location manager failed: {e.Error}");
        _locationTcs?.TrySetResult(null);
    }
}
