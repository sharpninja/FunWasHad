using System.Security;
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
public partial class AndroidGpsService : Java.Lang.Object, IGpsService, ILocationListener
{
    private readonly LocationManager? _locationManager;
    private readonly Handler _mainHandler;
    private readonly ILogger<AndroidGpsService>? _logger;
    private readonly object _syncRoot = new();
    private TaskCompletionSource<GpsCoordinates?>? _locationTcs;
    private GpsCoordinates? _latestLocation;
    private bool _isListening;
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
            {
                return false;
            }

            try
            {
                var context = global::Android.App.Application.Context;
                var permission = ContextCompat.CheckSelfPermission(context, global::Android.Manifest.Permission.AccessFineLocation);

                if (permission != Permission.Granted)
                {
                    return false;
                }

                var isGpsEnabled = _locationManager.IsProviderEnabled(LocationManager.GpsProvider);
                var isNetworkEnabled = _locationManager.IsProviderEnabled(LocationManager.NetworkProvider);

                return isGpsEnabled || isNetworkEnabled;
            }
            catch (SecurityException)
            {
                return false;
            }
            catch (global::Java.Lang.IllegalArgumentException)
            {
                return false;
            }
        }
    }

    private Task EnsureListeningAsync(string provider, IDictionary<string, object?> diagnostics, CancellationToken cancellationToken)
    {
        if (_locationManager == null)
        {
            return Task.CompletedTask;
        }

        lock (_syncRoot)
        {
            if (_isListening)
            {
                return Task.CompletedTask;
            }
        }

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        _mainHandler.Post(() =>
        {
            try
            {
                // Use a persistent listener with no minimum time/distance; higher layers decide how often to dispatch.
                _locationManager.RequestLocationUpdates(
                    provider,
                    minTimeMs: 0,
                    minDistanceM: 0,
                    listener: this);

                lock (_syncRoot)
                {
                    _isListening = true;
                }

                tcs.SetResult(true);
            }
            catch (SecurityException ex)
            {
                diagnostics["ListenerError"] = ex.Message;
                tcs.SetException(ex);
            }
            catch (global::Java.Lang.IllegalArgumentException ex)
            {
                diagnostics["ListenerError"] = ex.Message;
                tcs.SetException(ex);
            }
        });

        using (cancellationToken.Register(() =>
               {
                   if (!tcs.Task.IsCompleted)
                   {
                       tcs.TrySetCanceled(cancellationToken);
                   }
               }))
        {
            return tcs.Task;
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

    private static partial class Log
    {
        [LoggerMessage(EventId = 300, Level = LogLevel.Warning, Message = "Dispose failed to remove location updates")]
        public static partial void DisposeRemoveUpdatesFailed(ILogger? logger, Exception exception);

        [LoggerMessage(EventId = 301, Level = LogLevel.Warning, Message = "Dispose failed to remove handler callbacks")]
        public static partial void DisposeHandlerCallbacksFailed(ILogger? logger, Exception exception);

        [LoggerMessage(EventId = 302, Level = LogLevel.Warning, Message = "Permission issue getting last known location")]
        public static partial void LastKnownLocationPermission(ILogger? logger, Exception exception);

        [LoggerMessage(EventId = 303, Level = LogLevel.Warning, Message = "Invalid provider when getting last known location")]
        public static partial void LastKnownLocationProvider(ILogger? logger, Exception exception);

        [LoggerMessage(EventId = 304, Level = LogLevel.Information, Message = "Location provider disabled: {Provider}")]
        public static partial void ProviderDisabled(ILogger? logger, string provider);

        [LoggerMessage(EventId = 305, Level = LogLevel.Information, Message = "Location provider enabled: {Provider}")]
        public static partial void ProviderEnabled(ILogger? logger, string provider);

        [LoggerMessage(EventId = 306, Level = LogLevel.Debug, Message = "Location provider status changed: {Provider} - {Status}")]
        public static partial void ProviderStatusChanged(ILogger? logger, string? provider, Availability status);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_locationManager != null)
            {
                try
                {
                    if (_isListening)
                    {
                        _locationManager.RemoveUpdates(this);
                    }
                }
                catch (SecurityException ex)
                {
                    Log.DisposeRemoveUpdatesFailed(_logger, ex);
                }
                catch (global::Java.Lang.IllegalArgumentException ex)
                {
                    Log.DisposeRemoveUpdatesFailed(_logger, ex);
                }

                _locationManager.Dispose();
            }

            if (_mainHandler != null)
            {
                try
                {
                    _mainHandler.RemoveCallbacksAndMessages(null);
                }
                catch (InvalidOperationException ex)
                {
                    Log.DisposeHandlerCallbacksFailed(_logger, ex);
                }

                _mainHandler.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    public async Task<GpsCoordinates?> GetCurrentLocationAsync(CancellationToken cancellationToken = default)
    {
        var context = global::Android.App.Application.Context;
        var diagnostics = new Dictionary<string, object?>();

        try
        {
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

            var provider = isGpsEnabled
                ? LocationManager.GpsProvider
                : LocationManager.NetworkProvider;

            diagnostics["SelectedProvider"] = provider;
            diagnostics["TimeoutSeconds"] = LocationTimeoutSeconds;

            // Ensure we have a persistent listener running so that we can serve
            // requests from the latest in-memory fix instead of repeatedly
            // subscribing/unsubscribing from Android's LocationManager.
            await EnsureListeningAsync(provider, diagnostics, cancellationToken).ConfigureAwait(false);

            // First, prefer a recent location from the persistent listener.
            GpsCoordinates? listenerLocation;
            lock (_syncRoot)
            {
                listenerLocation = _latestLocation;
            }

            diagnostics["ListenerLocationAvailable"] = listenerLocation != null;
            if (listenerLocation != null)
            {
                diagnostics["ListenerLocationAgeMinutes"] =
                    (DateTimeOffset.UtcNow - listenerLocation.Timestamp).TotalMinutes;
                diagnostics["ListenerLocationRecent"] = IsLocationRecent(listenerLocation);
            }

            if (listenerLocation != null && IsLocationRecent(listenerLocation))
            {
                return listenerLocation;
            }

            // Fallback to last known location from system providers.
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

            // No recent location yet; wait for the next fix from the listener.
            _locationTcs = new TaskCompletionSource<GpsCoordinates?>(TaskCreationOptions.RunContinuationsAsynchronously);

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(LocationTimeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            linkedCts.Token.Register(() =>
            {
                if (_locationTcs != null && !_locationTcs.Task.IsCompleted)
                {
                    diagnostics["TimedOut"] = true;

                    GpsCoordinates? fallback;
                    lock (_syncRoot)
                    {
                        // Prefer any listener location we may have received while waiting;
                        // otherwise fall back to earlier last-known value (which may be null).
                        fallback = _latestLocation ?? lastKnownLocation;
                    }

                    diagnostics["FallbackToLastKnown"] = fallback != null;
                    _locationTcs.TrySetResult(fallback);
                }
            });

            var result = await _locationTcs.Task.ConfigureAwait(false);

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
            throw;
        }
        catch (global::System.OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (SecurityException ex)
        {
            diagnostics["ExceptionType"] = ex.GetType().Name;
            diagnostics["ExceptionMessage"] = ex.Message;
            diagnostics["StackTrace"] = ex.StackTrace;
            throw new LocationServicesException(
                "Android",
                "GetCurrentLocationAsync",
                "Security exception while getting GPS location",
                diagnostics,
                ex);
        }
    }

    private GpsCoordinates? GetLastKnownLocation()
    {
        if (_locationManager == null)
        {
            return null;
        }

        try
        {
            Location? bestLocation = null;

            var providers = _locationManager.GetProviders(enabledOnly: true);
            if (providers == null)
            {
                return null;
            }

            foreach (var provider in providers)
            {
                var location = _locationManager.GetLastKnownLocation(provider);
                if (location != null && (bestLocation == null || location.Accuracy < bestLocation.Accuracy))
                {
                    bestLocation = location;
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
                    // Android often reports HasSpeed true but Speed 0; treat 0 as unavailable so caller can use calculated speed
                    SpeedMetersPerSecond = bestLocation.HasSpeed && bestLocation.Speed > 0 ? bestLocation.Speed : null,
                    Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(bestLocation.Time)
                };
            }

            return null;
        }
        catch (SecurityException ex)
        {
            Log.LastKnownLocationPermission(_logger, ex);
            return null;
        }
        catch (global::Java.Lang.IllegalArgumentException ex)
        {
            Log.LastKnownLocationProvider(_logger, ex);
            return null;
        }
    }

    private static bool IsLocationRecent(GpsCoordinates location)
    {
        var age = DateTimeOffset.UtcNow - location.Timestamp;
        return age.TotalMinutes < 5; // Consider location recent if less than 5 minutes old
    }

    // ILocationListener implementation
    public void OnLocationChanged(Location location)
    {
        ArgumentNullException.ThrowIfNull(location);
        var coordinates = new GpsCoordinates(
            location.Latitude,
            location.Longitude,
            location.Accuracy)
        {
            AltitudeMeters = location.HasAltitude ? location.Altitude : null,
            // Android often reports HasSpeed true but Speed 0; treat 0 as unavailable so caller can use calculated speed
            SpeedMetersPerSecond = location.HasSpeed && location.Speed > 0 ? location.Speed : null,
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(location.Time)
        };

        lock (_syncRoot)
        {
            _latestLocation = coordinates;
        }

        if (_locationTcs != null && !_locationTcs.Task.IsCompleted)
        {
            _locationTcs.TrySetResult(coordinates);
        }
    }

    public void OnProviderDisabled(string provider)
    {
        Log.ProviderDisabled(_logger, provider);
    }

    public void OnProviderEnabled(string provider)
    {
        Log.ProviderEnabled(_logger, provider);
    }

    public void OnStatusChanged(string? provider, Availability status, Bundle? extras)
    {
        Log.ProviderStatusChanged(_logger, provider, status);
    }
}
