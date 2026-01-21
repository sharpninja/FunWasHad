using FWH.Common.Location;
using FWH.Common.Location.Models;
using FWH.Mobile.Data.Data;
using FWH.Mobile.Data.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FWH.Mobile.Services;

/// <summary>
/// Implementation of location tracking service that monitors device location
/// and stores updates in local SQLite database (NOT sent to API).
/// Also tracks movement state transitions (stationary ↔ walking ↔ riding).
/// Monitors for address changes when device remains stationary.
/// TR-MOBILE-001: Device location is tracked locally only, never sent to API.
/// </summary>
public class LocationTrackingService : ILocationTrackingService
{
    private readonly IGpsService _gpsService;
    private readonly NotesDbContext _dbContext;
    private readonly ILocationService _locationService;
    private readonly LocationWorkflowService? _locationWorkflowService;
    private readonly ILogger<LocationTrackingService> _logger;
    private readonly string _deviceId;
    private CancellationTokenSource? _trackingCts;
    private Task? _trackingTask;
    private GpsCoordinates? _lastKnownLocation;
    private GpsCoordinates? _lastReportedLocation;

    // Movement state tracking
    private MovementState _currentMovementState = MovementState.Unknown;
    private DateTimeOffset _lastStateChangeTime = DateTimeOffset.UtcNow;
    private readonly Queue<(DateTimeOffset timestamp, double distance, double? speed)> _recentMovements = new();
    private const int MaxRecentMovements = 10;

    // Speed tracking
    private double? _currentSpeedMetersPerSecond;
    private DateTimeOffset? _lastLocationTime;

    // Stationary address tracking
    private CancellationTokenSource? _stationaryCountdownCts;
    private string? _lastKnownAddress;
    private GpsCoordinates? _stationaryLocationForAddressCheck;

    public LocationTrackingService(
        IGpsService gpsService,
        NotesDbContext dbContext,
        ILocationService locationService,
        ILogger<LocationTrackingService> logger,
        LocationWorkflowService? locationWorkflowService = null)
    {
        _gpsService = gpsService ?? throw new ArgumentNullException(nameof(gpsService));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _locationWorkflowService = locationWorkflowService;

        _deviceId = Guid.NewGuid().ToString();

        MinimumDistanceMeters = 50.0;
        PollingInterval = TimeSpan.FromSeconds(30);
        StationaryThresholdDuration = TimeSpan.FromMinutes(3);
        StationaryDistanceThresholdMeters = 10.0;
        WalkingRidingSpeedThresholdMph = 5.0;
        StationaryAddressCheckDelay = TimeSpan.FromMinutes(1);

        // Subscribe to NewLocationAddress event to trigger workflow
        NewLocationAddress += OnNewLocationAddress;
    }

    public bool IsTracking => _trackingTask != null && !_trackingTask.IsCompleted;
    public GpsCoordinates? LastKnownLocation => _lastKnownLocation;
    public MovementState CurrentMovementState => _currentMovementState;
    public double? CurrentSpeedMetersPerSecond => _currentSpeedMetersPerSecond;
    public double? CurrentSpeedMph => _currentSpeedMetersPerSecond.HasValue
        ? GpsCalculator.MetersPerSecondToMph(_currentSpeedMetersPerSecond.Value)
        : null;
    public double? CurrentSpeedKmh => _currentSpeedMetersPerSecond.HasValue
        ? GpsCalculator.MetersPerSecondToKmh(_currentSpeedMetersPerSecond.Value)
        : null;
    public double MinimumDistanceMeters { get; set; }
    public TimeSpan PollingInterval { get; set; }
    public TimeSpan StationaryThresholdDuration { get; set; }
    public double StationaryDistanceThresholdMeters { get; set; }
    public double WalkingRidingSpeedThresholdMph { get; set; }
    public TimeSpan StationaryAddressCheckDelay { get; set; }

    public event EventHandler<GpsCoordinates>? LocationUpdated;
    public event EventHandler<Exception>? LocationUpdateFailed;
    public event EventHandler<MovementStateChangedEventArgs>? MovementStateChanged;
    public event EventHandler<LocationAddressChangedEventArgs>? NewLocationAddress;

    public async Task StartTrackingAsync(CancellationToken cancellationToken = default)
    {
        if (IsTracking)
        {
            _logger.LogWarning("Location tracking is already active");
            return;
        }

        if (!_gpsService.IsLocationAvailable)
        {
            _logger.LogWarning("GPS service is not available");
            var hasPermission = await _gpsService.RequestLocationPermissionAsync();
            if (!hasPermission)
            {
                _logger.LogError("Location permission denied");
                throw new InvalidOperationException("Location permission is required for tracking");
            }
        }

        _logger.LogInformation("Starting location tracking with {Distance}m threshold and {Speed} mph walking/riding threshold",
            MinimumDistanceMeters, WalkingRidingSpeedThresholdMph);

        _trackingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _trackingTask = TrackLocationLoopAsync(_trackingCts.Token);
    }

    public async Task StopTrackingAsync()
    {
        if (!IsTracking)
        {
            return;
        }

        _logger.LogInformation("Stopping location tracking");

        ResetStationaryCountdown();

        _trackingCts?.Cancel();

        if (_trackingTask != null)
        {
            try
            {
                await _trackingTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
            }
        }

        _trackingCts?.Dispose();
        _trackingCts = null;
        _trackingTask = null;

        _logger.LogInformation("Location tracking stopped");
    }

    private async Task TrackLocationLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Get current location
                var currentLocation = await _gpsService.GetCurrentLocationAsync(cancellationToken);

                if (currentLocation == null || !currentLocation.IsValid())
                {
                    _logger.LogWarning("Failed to get valid GPS coordinates");
                    await Task.Delay(PollingInterval, cancellationToken);
                    continue;
                }

                _lastKnownLocation = currentLocation;
                var currentTime = currentLocation.Timestamp;

                // Calculate distance and speed from last known location
                double? distanceMoved = null;
                double? speed = null;

                if (_lastReportedLocation != null && _lastLocationTime.HasValue)
                {
                    distanceMoved = GpsCalculator.CalculateDistance(
                        _lastReportedLocation.Latitude,
                        _lastReportedLocation.Longitude,
                        currentLocation.Latitude,
                        currentLocation.Longitude);

                    // Calculate speed
                    speed = GpsCalculator.CalculateSpeed(
                        _lastReportedLocation.Latitude,
                        _lastReportedLocation.Longitude,
                        _lastLocationTime.Value,
                        currentLocation.Latitude,
                        currentLocation.Longitude,
                        currentTime);

                    if (speed.HasValue && speed.Value >= 0)
                    {
                        _currentSpeedMetersPerSecond = speed.Value;

                        _logger.LogDebug(
                            "Speed: {SpeedMph:F1} mph ({SpeedKmh:F1} km/h, {SpeedMs:F2} m/s)",
                            GpsCalculator.MetersPerSecondToMph(speed.Value),
                            GpsCalculator.MetersPerSecondToKmh(speed.Value),
                            speed.Value);
                    }

                    // Track recent movements for state detection
                    TrackMovement(currentTime, distanceMoved.Value, speed);

                    // Detect and update movement state
                    UpdateMovementState(distanceMoved.Value, speed);
                }
                else
                {
                    // First location reading
                    _lastLocationTime = currentTime;
                }

                // Check if we should send update
                if (ShouldSendLocationUpdate(currentLocation))
                {
                    await SendLocationUpdateAsync(currentLocation, cancellationToken);
                    _lastLocationTime = currentTime;
                }

                // Wait for next polling interval
                await Task.Delay(PollingInterval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in location tracking loop");
                LocationUpdateFailed?.Invoke(this, ex);

                // Wait before retrying
                try
                {
                    await Task.Delay(PollingInterval, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
            }
        }
    }

    private void TrackMovement(DateTimeOffset timestamp, double distance, double? speed)
    {
        // Add new movement data
        _recentMovements.Enqueue((timestamp, distance, speed));

        // Keep only recent movements
        while (_recentMovements.Count > MaxRecentMovements)
        {
            _recentMovements.Dequeue();
        }
    }

    private void UpdateMovementState(double distanceMoved, double? speed)
    {
        var newState = DetermineMovementState(distanceMoved, speed);

        // Check if state has changed
        if (newState != _currentMovementState && newState != MovementState.Unknown)
        {
            var previousState = _currentMovementState;
            var transitionTime = DateTimeOffset.UtcNow;
            var durationInPreviousState = transitionTime - _lastStateChangeTime;

            _logger.LogInformation(
                "Movement state changed: {Previous} → {Current} (duration: {Duration:F0}s, distance: {Distance:F1}m, speed: {Speed:F1} mph)",
                previousState,
                newState,
                durationInPreviousState.TotalSeconds,
                distanceMoved,
                CurrentSpeedMph ?? 0);

            _currentMovementState = newState;
            _lastStateChangeTime = transitionTime;

            // Handle stationary state transition
            if (newState == MovementState.Stationary)
            {
                StartStationaryCountdown();
            }
            else if (previousState == MovementState.Stationary)
            {
                // Movement detected, cancel countdown
                ResetStationaryCountdown();
            }

            // Raise state change event
            var eventArgs = new MovementStateChangedEventArgs(
                previousState,
                newState,
                transitionTime,
                distanceMoved,
                durationInPreviousState,
                _currentSpeedMetersPerSecond);

            MovementStateChanged?.Invoke(this, eventArgs);
        }
        else if (_currentMovementState == MovementState.Stationary && _stationaryCountdownCts != null)
        {
            // Still stationary and countdown is active, reset it on any location change
                _logger.LogTrace("Location changed while stationary, resetting address check countdown");
            ResetStationaryCountdown();
            StartStationaryCountdown();
        }
    }

    private void StartStationaryCountdown()
    {
        // Cancel any existing countdown
        ResetStationaryCountdown();

        if (_lastKnownLocation == null)
            return;

        _logger.LogInformation("Device became stationary, starting {Delay} countdown for address change check",
            StationaryAddressCheckDelay);

        _stationaryLocationForAddressCheck = _lastKnownLocation;
        _stationaryCountdownCts = new CancellationTokenSource();

        // Start countdown task
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(StationaryAddressCheckDelay, _stationaryCountdownCts.Token);

                // Countdown expired, check for address change
                if (_stationaryLocationForAddressCheck != null)
                {
                    await CheckForAddressChangeAsync(_stationaryLocationForAddressCheck);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Stationary countdown cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during stationary address check");
            }
        }, _stationaryCountdownCts.Token);
    }

    private void ResetStationaryCountdown()
    {
        if (_stationaryCountdownCts != null)
        {
            _logger.LogDebug("Cancelling stationary address check countdown");
            _stationaryCountdownCts.Cancel();
            _stationaryCountdownCts.Dispose();
            _stationaryCountdownCts = null;
            _stationaryLocationForAddressCheck = null;
        }
    }

    private async Task CheckForAddressChangeAsync(GpsCoordinates location)
    {
        try
        {
            _logger.LogDebug("Checking for address change at ({Lat:F6}, {Lon:F6})",
                location.Latitude, location.Longitude);

            // Get closest business/POI to determine address
            var closestBusiness = await _locationService.GetClosestBusinessAsync(
                location.Latitude,
                location.Longitude,
                maxDistanceMeters: 100); // Check within 100m

            var currentAddress = closestBusiness?.Address ??
                                $"{location.Latitude:F6}, {location.Longitude:F6}";

            _logger.LogTrace("Current address: {Address}, Previous address: {PreviousAddress}",
                currentAddress, _lastKnownAddress ?? "none");

            // Check if address has changed
            if (_lastKnownAddress != currentAddress)
            {
                _logger.LogInformation("Address changed: {Previous} → {Current}",
                    _lastKnownAddress ?? "none", currentAddress);

                var eventArgs = new LocationAddressChangedEventArgs(
                    _lastKnownAddress,
                    currentAddress,
                    location,
                    DateTimeOffset.UtcNow);

                _lastKnownAddress = currentAddress;

                // Raise event
                NewLocationAddress?.Invoke(this, eventArgs);
            }
            else
            {
                _logger.LogDebug("Address unchanged: {Address}", currentAddress);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for address change");
        }
    }

    private MovementState DetermineMovementState(double currentDistance, double? currentSpeed)
    {
        if (_recentMovements.Count == 0)
        {
            return MovementState.Unknown;
        }

        // Get movements within the stationary threshold duration
        var thresholdTime = DateTimeOffset.UtcNow - StationaryThresholdDuration;
        var recentMovementsInWindow = _recentMovements
            .Where(m => m.timestamp >= thresholdTime)
            .ToList();

        // Need sufficient data points to make determination
        var requiredSamples = Math.Max(3, (int)(StationaryThresholdDuration.TotalSeconds / PollingInterval.TotalSeconds));
        if (recentMovementsInWindow.Count < requiredSamples)
        {
            // Not enough data yet, but check if currently moving with valid speed
            if (currentSpeed.HasValue && currentDistance >= MinimumDistanceMeters)
            {
                var speedMph = GpsCalculator.MetersPerSecondToMph(currentSpeed.Value);
                if (speedMph >= WalkingRidingSpeedThresholdMph)
                {
                    return MovementState.Riding;
                }
                else if (speedMph > 0)
                {
                    return MovementState.Walking;
                }
            }

            return _currentMovementState;
        }

        // Calculate statistics
        var avgDistance = recentMovementsInWindow.Average(m => m.distance);
        var maxDistance = recentMovementsInWindow.Max(m => m.distance);

        // Calculate average speed from movements with speed data
        var movementsWithSpeed = recentMovementsInWindow.Where(m => m.speed.HasValue).ToList();
        double? avgSpeed = movementsWithSpeed.Any()
            ? movementsWithSpeed.Average(m => m.speed!.Value)
            : null;

        _logger.LogDebug(
            "Movement analysis: avg={AvgDist:F1}m, max={MaxDist:F1}m, avgSpeed={AvgSpeed:F2} m/s ({AvgSpeedMph:F1} mph), samples={Count}",
            avgDistance,
            maxDistance,
            avgSpeed ?? 0,
            avgSpeed.HasValue ? GpsCalculator.MetersPerSecondToMph(avgSpeed.Value) : 0,
            recentMovementsInWindow.Count);

        // Determine state based on movement patterns and speed

        // Check for stationary state
        if (maxDistance < StationaryDistanceThresholdMeters && avgDistance < StationaryDistanceThresholdMeters / 2)
        {
            return MovementState.Stationary;
        }

        // Check for continuous motion (walking or riding)
        if (currentDistance >= MinimumDistanceMeters || avgDistance >= StationaryDistanceThresholdMeters * 2 || maxDistance >= MinimumDistanceMeters)
        {
            // Device is moving, now determine if walking or riding based on speed
            double? speedToCheck = currentSpeed ?? avgSpeed;

            if (speedToCheck.HasValue)
            {
                var speedMph = GpsCalculator.MetersPerSecondToMph(speedToCheck.Value);

                if (speedMph >= WalkingRidingSpeedThresholdMph)
                {
                    return MovementState.Riding;
                }
                else if (speedMph > 0)
                {
                    return MovementState.Walking;
                }
            }

            // Speed not available or invalid, use legacy Moving state
            return MovementState.Moving;
        }

        // Maintain current state if unclear
        return _currentMovementState;
    }

    private bool ShouldSendLocationUpdate(GpsCoordinates currentLocation)
    {
        // Always send first location
        if (_lastReportedLocation == null)
        {
            return true;
        }

        // Calculate distance from last reported location
        var distance = GpsCalculator.CalculateDistance(
            _lastReportedLocation.Latitude,
            _lastReportedLocation.Longitude,
            currentLocation.Latitude,
            currentLocation.Longitude);

        _logger.LogDebug("Distance from last reported location: {Distance:F2}m", distance);

        return distance >= MinimumDistanceMeters;
    }

    private async Task SendLocationUpdateAsync(GpsCoordinates location, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug(
                "Storing location locally: ({Lat:F6}, {Lon:F6}) - State: {State}, Speed: {Speed:F1} mph",
                location.Latitude,
                location.Longitude,
                _currentMovementState,
                CurrentSpeedMph ?? 0);

            // Store location in local SQLite database (never sent to API)
            var locationEntity = new DeviceLocationEntity
            {
                DeviceId = _deviceId,
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                AccuracyMeters = location.AccuracyMeters,
                AltitudeMeters = location.AltitudeMeters,
                SpeedMetersPerSecond = _currentSpeedMetersPerSecond,
                HeadingDegrees = null, // Heading not available in GpsCoordinates
                MovementState = _currentMovementState.ToString(),
                Timestamp = location.Timestamp,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _dbContext.DeviceLocationHistory.AddAsync(locationEntity, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Location stored locally (ID: {LocationId})", locationEntity.Id);
            _lastReportedLocation = location;
            LocationUpdated?.Invoke(this, location);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store location locally");
            LocationUpdateFailed?.Invoke(this, ex);
        }
    }

    private async void OnNewLocationAddress(object? sender, LocationAddressChangedEventArgs e)
    {
        try
        {
            _logger.LogInformation("NewLocationAddress event: {Address}", e.CurrentAddress);

            if (_locationWorkflowService != null)
            {
                await _locationWorkflowService.HandleNewLocationAddressAsync(e);
            }
            else
            {
                _logger.LogWarning("LocationWorkflowService not available, cannot start location workflow");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling NewLocationAddress event");
        }
    }
}
