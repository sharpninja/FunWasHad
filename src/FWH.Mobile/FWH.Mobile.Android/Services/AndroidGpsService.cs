using Android.Content;
using Android.Content.PM;
using Android.Locations;
using Android.OS;
using AndroidX.Core.Content;
using FWH.Common.Location;
using FWH.Common.Location.Models;
using Microsoft.Extensions.Logging;

namespace FWH.Mobile.Android.Services;

/// <summary>
/// Android implementation of GPS service using LocationManager.
/// </summary>
public class AndroidGpsService : Java.Lang.Object, IGpsService, ILocationListener
{
    private readonly LocationManager? _locationManager;
    private readonly Handler _mainHandler;
    private readonly ILogger<AndroidGpsService>? _logger;
    private TaskCompletionSource<GpsCoordinates?>? _locationTcs;
    private const int LocationTimeoutSeconds = 30;

    public AndroidGpsService(ILogger<AndroidGpsService>? logger = null)
    {
        var context = global::Android.App.Application.Context;
        _locationManager = context.GetSystemService(Context.LocationService) as LocationManager;
        _mainHandler = new Handler(Looper.MainLooper!);
        _logger = logger;
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

    public Task<bool> RequestLocationPermissionAsync()
    {
        var context = global::Android.App.Application.Context;

        // Check if permission is already granted
        var permission = ContextCompat.CheckSelfPermission(context, global::Android.Manifest.Permission.AccessFineLocation);
        if (permission == Permission.Granted)
            return Task.FromResult(true);

        // Note: Actual permission request must be done from an Activity
        // This method just checks current status
        // The MainActivity should handle permission requests
        return Task.FromResult(false);
    }

    public async Task<GpsCoordinates?> GetCurrentLocationAsync(CancellationToken cancellationToken = default)
    {
        var context = global::Android.App.Application.Context;
        var diagnostics = new Dictionary<string, object?>();

        try
        {
            // Collect diagnostic information
            diagnostics["LocationManagerAvailable"] = _locationManager != null;
            diagnostics["IsLocationAvailable"] = IsLocationAvailable;

            if (_locationManager == null)
            {
                diagnostics["Error"] = "LocationManager is null - system service unavailable";
                throw new LocationServicesException(
                    "Android",
                    "GetCurrentLocationAsync",
                    "LocationManager system service is not available",
                    diagnostics);
            }

            var permission = ContextCompat.CheckSelfPermission(context, global::Android.Manifest.Permission.AccessFineLocation);
            diagnostics["PermissionStatus"] = permission.ToString();
            diagnostics["PermissionGranted"] = permission == Permission.Granted;

            if (permission != Permission.Granted)
            {
                diagnostics["Error"] = "Location permission not granted";
                throw new LocationServicesException(
                    "Android",
                    "GetCurrentLocationAsync",
                    "Location permission is not granted",
                    diagnostics);
            }

            // Check provider availability
            var isGpsEnabled = _locationManager.IsProviderEnabled(LocationManager.GpsProvider);
            var isNetworkEnabled = _locationManager.IsProviderEnabled(LocationManager.NetworkProvider);
            var isPassiveEnabled = _locationManager.IsProviderEnabled(LocationManager.PassiveProvider);

            diagnostics["GpsProviderEnabled"] = isGpsEnabled;
            diagnostics["NetworkProviderEnabled"] = isNetworkEnabled;
            diagnostics["PassiveProviderEnabled"] = isPassiveEnabled;

            if (!isGpsEnabled && !isNetworkEnabled)
            {
                diagnostics["Error"] = "No location providers are enabled";
                throw new LocationServicesException(
                    "Android",
                    "GetCurrentLocationAsync",
                    "No location providers (GPS or Network) are enabled on the device",
                    diagnostics);
            }

            _locationTcs = new TaskCompletionSource<GpsCoordinates?>();

            // Try to get last known location first (faster)
            var lastKnownLocation = GetLastKnownLocation();
            diagnostics["LastKnownLocationAvailable"] = lastKnownLocation != null;
            if (lastKnownLocation != null)
            {
                diagnostics["LastKnownLocationAge"] = (DateTimeOffset.UtcNow - lastKnownLocation.Timestamp).TotalMinutes;
                diagnostics["LastKnownLocationRecent"] = IsLocationRecent(lastKnownLocation);
            }

            if (lastKnownLocation != null && IsLocationRecent(lastKnownLocation))
            {
                return lastKnownLocation;
            }

            // Request location updates
            var provider = isGpsEnabled
                ? LocationManager.GpsProvider
                : LocationManager.NetworkProvider;

            diagnostics["SelectedProvider"] = provider;
            diagnostics["TimeoutSeconds"] = LocationTimeoutSeconds;

            // RequestLocationUpdates must be called on a thread with a Looper (main thread)
            var requestLocationTcs = new TaskCompletionSource<bool>();
            _mainHandler.Post(() =>
            {
                try
                {
                    _locationManager.RequestLocationUpdates(
                        provider,
                        minTimeMs: 0,
                        minDistanceM: 0,
                        listener: this);
                    requestLocationTcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    requestLocationTcs.SetException(ex);
                }
            });

            // Wait for the request to be posted (should be immediate)
            await requestLocationTcs.Task.ConfigureAwait(false);

            // Set up timeout
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(LocationTimeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            linkedCts.Token.Register(() =>
            {
                if (!_locationTcs.Task.IsCompleted)
                {
                    diagnostics["TimedOut"] = true;
                    diagnostics["FallbackToLastKnown"] = lastKnownLocation != null;
                    _locationTcs?.TrySetResult(lastKnownLocation);
                }
            });

            var result = await _locationTcs.Task.ConfigureAwait(false);

            // Clean up - RemoveUpdates must also be called on a thread with a Looper
            var removeUpdatesTcs = new TaskCompletionSource<bool>();
            _mainHandler.Post(() =>
            {
                try
                {
                    _locationManager.RemoveUpdates(this);
                    removeUpdatesTcs.SetResult(true);
                }
                catch (Exception cleanupEx)
                {
                    diagnostics["CleanupError"] = cleanupEx.Message;
                    removeUpdatesTcs.SetResult(false);
                }
            });

            // Wait for cleanup to complete (should be immediate)
            await removeUpdatesTcs.Task.ConfigureAwait(false);

            if (result == null)
            {
                diagnostics["Error"] = "Location request completed but returned null";
                throw new LocationServicesException(
                    "Android",
                    "GetCurrentLocationAsync",
                    "Location request completed but no location was obtained",
                    diagnostics);
            }

            return result;
        }
        catch (LocationServicesException)
        {
            throw; // Re-throw our custom exception as-is
        }
        catch (Exception ex)
        {
            diagnostics["ExceptionType"] = ex.GetType().Name;
            diagnostics["ExceptionMessage"] = ex.Message;
            diagnostics["StackTrace"] = ex.StackTrace;

            throw new LocationServicesException(
                "Android",
                "GetCurrentLocationAsync",
                $"Unexpected error getting GPS location: {ex.Message}",
                diagnostics,
                ex);
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
                    SpeedMetersPerSecond = bestLocation.HasSpeed ? bestLocation.Speed : null,
                    Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(bestLocation.Time)
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting last known location");
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
        ArgumentNullException.ThrowIfNull(location);
        if (_locationTcs != null && !_locationTcs.Task.IsCompleted)
        {
            var coordinates = new GpsCoordinates(
                location.Latitude,
                location.Longitude,
                location.Accuracy)
            {
                AltitudeMeters = location.HasAltitude ? location.Altitude : null,
                SpeedMetersPerSecond = location.HasSpeed ? location.Speed : null,
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(location.Time)
            };

            _locationTcs.TrySetResult(coordinates);
        }
    }

    public void OnProviderDisabled(string provider)
    {
        _logger?.LogInformation("Location provider disabled: {Provider}", provider);
    }

    public void OnProviderEnabled(string provider)
    {
        _logger?.LogInformation("Location provider enabled: {Provider}", provider);
    }

    public void OnStatusChanged(string? provider, Availability status, Bundle? extras)
    {
        _logger?.LogDebug("Location provider status changed: {Provider} - {Status}", provider, status);
    }
}
