using FWH.Common.Location.Models;

namespace FWH.Mobile.Services;

/// <summary>
/// Service for tracking device location changes and updating the backend API.
/// Monitors location changes and sends updates when device moves more than threshold distance.
/// </summary>
public interface ILocationTrackingService
{
    /// <summary>
    /// Starts tracking location changes.
    /// </summary>
    Task StartTrackingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops tracking location changes.
    /// </summary>
    Task StopTrackingAsync();

    /// <summary>
    /// Gets whether location tracking is currently active.
    /// </summary>
    bool IsTracking { get; }

    /// <summary>
    /// Gets the last known location.
    /// </summary>
    GpsCoordinates? LastKnownLocation { get; }

    /// <summary>
    /// Gets the current movement state of the device.
    /// </summary>
    MovementState CurrentMovementState { get; }

    /// <summary>
    /// Gets the current speed in meters per second (if available).
    /// </summary>
    double? CurrentSpeedMetersPerSecond { get; }

    /// <summary>
    /// Gets the current speed in miles per hour (if available).
    /// </summary>
    double? CurrentSpeedMph { get; }

    /// <summary>
    /// Gets the current speed in kilometers per hour (if available).
    /// </summary>
    double? CurrentSpeedKmh { get; }

    /// <summary>
    /// Gets or sets the minimum distance in meters that triggers a location update.
    /// Default: 50 meters.
    /// </summary>
    double MinimumDistanceMeters { get; set; }

    /// <summary>
    /// Gets or sets the polling interval for checking location changes.
    /// Default: 30 seconds.
    /// </summary>
    TimeSpan PollingInterval { get; set; }

    /// <summary>
    /// Gets or sets the minimum duration a device must remain stationary before being considered "Stationary".
    /// Default: 3 minutes (3 consecutive polls with no significant movement).
    /// </summary>
    TimeSpan StationaryThresholdDuration { get; set; }

    /// <summary>
    /// Gets or sets the distance threshold for considering device as stationary.
    /// Movement less than this distance is considered stationary.
    /// Default: 10 meters.
    /// </summary>
    double StationaryDistanceThresholdMeters { get; set; }

    /// <summary>
    /// Gets or sets the speed threshold in MPH for distinguishing walking from riding.
    /// Speed below this threshold is considered walking, at or above is considered riding.
    /// Default: 5.0 mph.
    /// </summary>
    double WalkingRidingSpeedThresholdMph { get; set; }

    /// <summary>
    /// Gets or sets the countdown duration before checking for address change when device becomes stationary.
    /// Default: 1 minute.
    /// </summary>
    TimeSpan StationaryAddressCheckDelay { get; set; }

    /// <summary>
    /// Event raised when location is updated.
    /// </summary>
    event EventHandler<GpsCoordinates>? LocationUpdated;

    /// <summary>
    /// Event raised when location update fails.
    /// </summary>
    event EventHandler<Exception>? LocationUpdateFailed;

    /// <summary>
    /// Event raised when movement state changes (stationary to moving or vice versa).
    /// </summary>
    event EventHandler<MovementStateChangedEventArgs>? MovementStateChanged;

    /// <summary>
    /// Event raised when the device address changes after remaining stationary for the countdown duration.
    /// </summary>
    event EventHandler<LocationAddressChangedEventArgs>? NewLocationAddress;
}

/// <summary>
/// Event args for when a location address changes.
/// </summary>
public class LocationAddressChangedEventArgs : EventArgs
{
    public string? PreviousAddress { get; }
    public string CurrentAddress { get; }
    public GpsCoordinates Location { get; }
    public DateTimeOffset Timestamp { get; }

    public LocationAddressChangedEventArgs(
        string? previousAddress,
        string currentAddress,
        GpsCoordinates location,
        DateTimeOffset timestamp)
    {
        PreviousAddress = previousAddress;
        CurrentAddress = currentAddress;
        Location = location;
        Timestamp = timestamp;
    }
}
