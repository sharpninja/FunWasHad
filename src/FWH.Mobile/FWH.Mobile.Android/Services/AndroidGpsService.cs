using Android.Content;
using Android.Locations;
using Android.OS;
using AndroidX.Core.Content;
using FWH.Common.Location;
using FWH.Common.Location.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.Content.PM;

namespace FWH.Mobile.Android.Services;

/// <summary>
/// Android implementation of GPS service using LocationManager.
/// </summary>
public class AndroidGpsService : Java.Lang.Object, IGpsService, ILocationListener
{
    private readonly LocationManager? _locationManager;
    private TaskCompletionSource<GpsCoordinates?>? _locationTcs;
    private const int LocationTimeoutSeconds = 30;

    public AndroidGpsService()
    {
        var context = global::Android.App.Application.Context;
        _locationManager = context.GetSystemService(Context.LocationService) as LocationManager;
    }

    public bool IsLocationAvailable
    {
        get
        {
            if (_locationManager == null)
                return false;

            try
            {
                var context = global::Android.App.Application.Context;
                var permission = ContextCompat.CheckSelfPermission(context, global::Android.Manifest.Permission.AccessFineLocation);
                
                if (permission != Permission.Granted)
                    return false;

                var isGpsEnabled = _locationManager.IsProviderEnabled(LocationManager.GpsProvider);
                var isNetworkEnabled = _locationManager.IsProviderEnabled(LocationManager.NetworkProvider);
                
                return isGpsEnabled || isNetworkEnabled;
            }
            catch
            {
                return false;
            }
        }
    }

    public async Task<bool> RequestLocationPermissionAsync()
    {
        var context = global::Android.App.Application.Context;
        
        // Check if permission is already granted
        var permission = ContextCompat.CheckSelfPermission(context, global::Android.Manifest.Permission.AccessFineLocation);
        if (permission == Permission.Granted)
            return true;

        // Note: Actual permission request must be done from an Activity
        // This method just checks current status
        // The MainActivity should handle permission requests
        return false;
    }

    public async Task<GpsCoordinates?> GetCurrentLocationAsync(CancellationToken cancellationToken = default)
    {
        if (_locationManager == null || !IsLocationAvailable)
            return null;

        try
        {
            var context = global::Android.App.Application.Context;
            var permission = ContextCompat.CheckSelfPermission(context, global::Android.Manifest.Permission.AccessFineLocation);
            
            if (permission != Permission.Granted)
                return null;

            _locationTcs = new TaskCompletionSource<GpsCoordinates?>();

            // Try to get last known location first (faster)
            var lastKnownLocation = GetLastKnownLocation();
            if (lastKnownLocation != null && IsLocationRecent(lastKnownLocation))
            {
                return lastKnownLocation;
            }

            // Request location updates
            var provider = _locationManager.IsProviderEnabled(LocationManager.GpsProvider)
                ? LocationManager.GpsProvider
                : LocationManager.NetworkProvider;

            _locationManager.RequestLocationUpdates(
                provider,
                minTimeMs: 0,
                minDistanceM: 0,
                listener: this);

            // Set up timeout
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(LocationTimeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            linkedCts.Token.Register(() =>
            {
                _locationTcs?.TrySetResult(lastKnownLocation);
            });

            var result = await _locationTcs.Task;

            // Clean up
            try
            {
                _locationManager.RemoveUpdates(this);
            }
            catch { }

            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting GPS location: {ex}");
            return null;
        }
    }

    private GpsCoordinates? GetLastKnownLocation()
    {
        if (_locationManager == null)
            return null;

        try
        {
            Location? bestLocation = null;

            var providers = _locationManager.GetProviders(enabledOnly: true);
            if (providers == null)
                return null;

            foreach (var provider in providers)
            {
                var location = _locationManager.GetLastKnownLocation(provider);
                if (location != null)
                {
                    if (bestLocation == null || location.Accuracy < bestLocation.Accuracy)
                    {
                        bestLocation = location;
                    }
                }
            }

            if (bestLocation != null)
            {
                return new GpsCoordinates(
                    bestLocation.Latitude,
                    bestLocation.Longitude,
                    bestLocation.Accuracy)
                {
                    AltitudeMeters = bestLocation.HasAltitude ? bestLocation.Altitude : null,
                    Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(bestLocation.Time)
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting last known location: {ex}");
            return null;
        }
    }

    private bool IsLocationRecent(GpsCoordinates location)
    {
        var age = DateTimeOffset.UtcNow - location.Timestamp;
        return age.TotalMinutes < 5; // Consider location recent if less than 5 minutes old
    }

    // ILocationListener implementation
    public void OnLocationChanged(Location location)
    {
        if (_locationTcs != null && !_locationTcs.Task.IsCompleted)
        {
            var coordinates = new GpsCoordinates(
                location.Latitude,
                location.Longitude,
                location.Accuracy)
            {
                AltitudeMeters = location.HasAltitude ? location.Altitude : null,
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(location.Time)
            };

            _locationTcs.TrySetResult(coordinates);
        }
    }

    public void OnProviderDisabled(string provider)
    {
        System.Diagnostics.Debug.WriteLine($"Location provider disabled: {provider}");
    }

    public void OnProviderEnabled(string provider)
    {
        System.Diagnostics.Debug.WriteLine($"Location provider enabled: {provider}");
    }

    public void OnStatusChanged(string? provider, Availability status, Bundle? extras)
    {
        System.Diagnostics.Debug.WriteLine($"Location provider status changed: {provider} - {status}");
    }
}
