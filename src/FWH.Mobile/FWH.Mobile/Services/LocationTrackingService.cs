using FWH.Common.Location;
using FWH.Common.Location.Models;
using FWH.Mobile.Configuration;
using FWH.Mobile.Data.Data;
using FWH.Mobile.Data.Entities;
using Microsoft.Extensions.Logging;

namespace FWH.Mobile.Services;

/// <summary>
/// Implementation of location tracking service that monitors device location
/// and stores updates in local SQLite database (NOT sent to API).
/// Also tracks movement state transitions (stationary ↔ walking ↔ riding).
/// Monitors for address changes when device remains stationary.
/// TR-MOBILE-001: Device location is tracked locally only, never sent to API.
/// </summary>
public class LocationTrackingService : ILocationTrackingService, IDisposable
{
    private readonly IGpsService _gpsService;
    private readonly NotesDbContext _dbContext;
    private readonly ILocationService _locationService;
    private readonly LocationWorkflowService? _locationWorkflowService;
    private readonly LocationSettings _locationSettings;
    private readonly ILogger<LocationTrackingService> _logger;
    private readonly IImageService? _imageService;
    private readonly IThemeService? _themeService;
    private readonly string _deviceId;
    private CancellationTokenSource? _trackingCts;
    private Task? _trackingTask;
    private GpsCoordinates? _lastKnownLocation;
    private GpsCoordinates? _lastReportedLocation;

    // Movement state tracking
    private MovementState _currentMovementState = MovementState.Stationary;
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
        LocationSettings locationSettings,
        ILogger<LocationTrackingService> logger,
        LocationWorkflowService? locationWorkflowService = null,
        IImageService? imageService = null,
        IThemeService? themeService = null)
    {
        _gpsService = gpsService ?? throw new ArgumentNullException(nameof(gpsService));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
        _locationSettings = locationSettings ?? throw new ArgumentNullException(nameof(locationSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _locationWorkflowService = locationWorkflowService;
        _imageService = imageService;
        _themeService = themeService;

        _deviceId = Guid.NewGuid().ToString();

        MinimumDistanceMeters = 50.0;
        PollingInterval = _locationSettings.GetPollingInterval();
        // Dispatch interval (UI/event cadence) is configurable via the same setting;
        // default is once per second when PollingIntervalMode is "normal".
        DispatchInterval = _locationSettings.GetPollingInterval();
        StationaryThresholdDuration = TimeSpan.FromMinutes(3);
        StationaryDistanceThresholdMeters = 10.0;
        WalkingRidingSpeedThresholdMph = 5.0;
        StationaryAddressCheckDelay = TimeSpan.FromMinutes(1);

        // Subscribe to NewLocationAddress event to trigger workflow
        NewLocationAddress += OnNewLocationAddress;

        if (!_locationSettings.IsTrackingEnabled)
        {
            _logger.LogWarning("Location tracking is configured as 'off' (PollingIntervalMode='off'). Tracking will be disabled.");
        }
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
    public string? CurrentAddress => _lastKnownAddress;
    public double MinimumDistanceMeters { get; set; }
    public TimeSpan PollingInterval { get; set; }
    /// <summary>
    /// How often to dispatch location and movement state updates to listeners.
    /// This is separate from provider sampling and distance thresholds so the UI
    /// can update smoothly (e.g., once per second) while storage still honors
    /// <see cref="MinimumDistanceMeters"/>.
    /// </summary>
    public TimeSpan DispatchInterval { get; set; } = TimeSpan.FromSeconds(1);
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

        if (!_locationSettings.IsTrackingEnabled)
        {
            _logger.LogWarning("Location tracking is disabled (PollingIntervalMode='off'). Cannot start tracking.");
            return;
        }

        if (!_gpsService.IsLocationAvailable)
        {
            _logger.LogWarning("GPS service is not available");
            var hasPermission = await _gpsService.RequestLocationPermissionAsync().ConfigureAwait(false);
            if (!hasPermission)
            {
                _logger.LogWarning("Location permission not yet granted, tracking will start when permission is available");
                // Don't throw - allow tracking to start and it will work once permission is granted
                // The tracking loop will handle the case where location is not available
            }
        }

        _logger.LogDebug("Starting location tracking with {Distance}m threshold and {Speed} mph walking/riding threshold",
            MinimumDistanceMeters, WalkingRidingSpeedThresholdMph);

        _trackingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _trackingTask = TrackLocationLoopAsync(_trackingCts.Token);

        // Initialize with current location asynchronously (non-blocking)
        // Don't await this to prevent blocking UI thread during app startup
        _ = Task.Run(async () =>
        {
            try
            {
                // Use a shorter timeout for initial location to avoid ANR
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var initialLocation = await _gpsService.GetCurrentLocationAsync(timeoutCts.Token).ConfigureAwait(false);

                if (initialLocation != null && initialLocation.IsValid())
                {
                    _lastKnownLocation = initialLocation;
                    _lastReportedLocation = initialLocation;
                    _lastLocationTime = initialLocation.Timestamp;
                    _logger.LogInformation("Location tracking initialized with current location: {Latitude:F6}, {Longitude:F6}",
                        initialLocation.Latitude, initialLocation.Longitude);

                    // Always determine movement state on startup. Stationary is the default.
                    MovementState initialState;
                    if (initialLocation.SpeedMetersPerSecond.HasValue)
                    {
                        var speed = initialLocation.SpeedMetersPerSecond.Value;
                        _currentSpeedMetersPerSecond = speed;
                        initialState = DetermineMovementStateFromSpeed(speed);
                        _logger.LogInformation(
                            "Initial movement state determined from GPS speed: {State} (speed: {Speed:F1} mph)",
                            initialState,
                            GpsCalculator.MetersPerSecondToMph(speed));
                    }
                    else
                    {
                        initialState = MovementState.Stationary;
                        _logger.LogInformation("Initial movement state set to Stationary (no speed data yet)");
                    }

                    _currentMovementState = initialState;
                    _lastStateChangeTime = DateTimeOffset.UtcNow;
                    var initialEventArgs = new MovementStateChangedEventArgs(
                        MovementState.Stationary,
                        initialState,
                        DateTimeOffset.UtcNow,
                        null,
                        TimeSpan.Zero,
                        _currentSpeedMetersPerSecond);
                    MovementStateChanged?.Invoke(this, initialEventArgs);

                    // Immediately get address for initial location
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await CheckForAddressChangeAsync(initialLocation).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error getting initial address");
                        }
                    }, cancellationToken);

                    // Trigger location updated event for initial location
                    LocationUpdated?.Invoke(this, initialLocation);
                }
                else
                {
                    _logger.LogDebug("Could not get initial location, tracking loop will obtain it");
                }
            }
            catch (LocationServicesException ex)
            {
                _logger.LogWarning(ex,
                    "Location service error during initialization. Platform: {Platform}, Operation: {Operation}. Tracking loop will retry.",
                    ex.Platform,
                    ex.Operation);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Initial location request timed out, tracking loop will obtain location");
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Initial location request completed, tracking loop will obtain location");
            }
        }, cancellationToken);
    }

    public async Task StopTrackingAsync()
    {
        if (!IsTracking)
        {
            return;
        }

        _logger.LogDebug("Stopping location tracking");

        ResetStationaryCountdown();

        _trackingCts?.Cancel();

        if (_trackingTask != null)
        {
            try
            {
                await _trackingTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
            }
        }

        _trackingCts?.Dispose();
        _trackingCts = null;
        _trackingTask = null;

        _logger.LogDebug("Location tracking stopped");
    }

    private async Task TrackLocationLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Get current location
                var currentLocation = await _gpsService.GetCurrentLocationAsync(cancellationToken).ConfigureAwait(false);

                if (currentLocation == null || !currentLocation.IsValid())
                {
                    _logger.LogWarning("Failed to get valid GPS coordinates");
                    await Task.Delay(DispatchInterval, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                var previousKnownLocation = _lastKnownLocation;
                _lastKnownLocation = currentLocation;
                var currentTime = currentLocation.Timestamp;

                // Use device's instant speed if available and non-zero; otherwise calculate from distance/time.
                // On Android, device speed is often 0 even when moving, so treat 0 as unavailable.
                var deviceSpeed = currentLocation.SpeedMetersPerSecond;
                double? speed = deviceSpeed is > 0 ? deviceSpeed : null;

                // Calculate step distance from previous known location (per-sample),
                // separate from distance used for persistence thresholds.
                double? stepDistance = null;

                if (previousKnownLocation != null && _lastLocationTime.HasValue)
                {
                    stepDistance = GpsCalculator.CalculateDistance(
                        previousKnownLocation.Latitude,
                        previousKnownLocation.Longitude,
                        currentLocation.Latitude,
                        currentLocation.Longitude);

                    // If device didn't provide usable speed, calculate it from step distance/time
                    if (!speed.HasValue)
                    {
                        speed = GpsCalculator.CalculateSpeed(
                            previousKnownLocation.Latitude,
                            previousKnownLocation.Longitude,
                            _lastLocationTime.Value,
                            currentLocation.Latitude,
                            currentLocation.Longitude,
                            currentTime);
                    }

                    // Update current speed tracking
                    if (speed.HasValue && speed.Value >= 0)
                    {
                        _currentSpeedMetersPerSecond = speed.Value;

                        _logger.LogDebug(
                            "Speed: {SpeedMph:F1} mph ({SpeedKmh:F1} km/h, {SpeedMs:F2} m/s) {Source}",
                            GpsCalculator.MetersPerSecondToMph(speed.Value),
                            GpsCalculator.MetersPerSecondToKmh(speed.Value),
                            speed.Value,
                            deviceSpeed is > 0 ? "[device]" : "[calculated]");
                    }

                    // Track recent movements for state detection using step distance
                    TrackMovement(currentTime, stepDistance.Value, speed);

                    // Detect and update movement state from step distance
                    UpdateMovementState(stepDistance.Value, speed);
                }
                else
                {
                    // First location reading in loop (startup may not have run or may have failed).
                    // Determine state from speed; default is Stationary.
                    _lastLocationTime = currentTime;
                    var deviceSpeedMps = currentLocation.SpeedMetersPerSecond;
                    var stateFromSpeed = (deviceSpeedMps.HasValue && deviceSpeedMps.Value > 0)
                        ? DetermineMovementStateFromSpeed(deviceSpeedMps.Value)
                        : MovementState.Stationary;
                    _currentMovementState = stateFromSpeed;
                    _lastStateChangeTime = DateTimeOffset.UtcNow;
                    _logger.LogInformation("Movement state set on first fix: {State}", stateFromSpeed);
                    try
                    {
                        MovementStateChanged?.Invoke(this, new MovementStateChangedEventArgs(
                            MovementState.Stationary,
                            stateFromSpeed,
                            DateTimeOffset.UtcNow,
                            null,
                            TimeSpan.Zero,
                            currentLocation.SpeedMetersPerSecond));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "MovementStateChanged handler threw: {Type} - {Message}", ex.GetType().Name, ex.Message);
                    }
                }

                // Always notify UI of current location so coordinates display updates in real time.
                // Persistence to DB (SendLocationUpdateAsync) is still gated by minimum distance.
                try
                {
                    LocationUpdated?.Invoke(this, currentLocation);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "LocationUpdated handler threw: {Type} - {Message}", ex.GetType().Name, ex.Message);
                }

                // Check if we should persist update to local DB
                if (ShouldSendLocationUpdate(currentLocation))
                {
                    await SendLocationUpdateAsync(currentLocation, cancellationToken).ConfigureAwait(false);
                    _lastLocationTime = currentTime;
                }

                // Wait for next polling interval
                await Task.Delay(DispatchInterval, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation
                throw;
            }
            catch (LocationServicesException ex)
            {
                // Log detailed location service exception with all diagnostics
                _logger.LogError(ex,
                    "Location service error in tracking loop. Platform: {Platform}, Operation: {Operation}, Diagnostics: {Diagnostics}",
                    ex.Platform,
                    ex.Operation,
                    string.Join(", ", ex.Diagnostics.Select(kvp => $"{kvp.Key}={kvp.Value}")));
                LocationUpdateFailed?.Invoke(this, ex);

                // Wait before retrying
                try
                {
                    await Task.Delay(DispatchInterval, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in location tracking loop: {Type} - {Message}. Will retry after delay.",
                    ex.GetType().Name,
                    ex.Message);
                LocationUpdateFailed?.Invoke(this, ex);

                // Wait before retrying
                try
                {
                    await Task.Delay(DispatchInterval, cancellationToken).ConfigureAwait(false);
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
        if (newState != _currentMovementState)
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

            try
            {
                MovementStateChanged?.Invoke(this, eventArgs);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MovementStateChanged handler threw: {Type} - {Message}", ex.GetType().Name, ex.Message);
            }
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

        _logger.LogDebug("Device became stationary, starting {Delay} countdown for address change check",
            StationaryAddressCheckDelay);

        _stationaryLocationForAddressCheck = _lastKnownLocation;
        _stationaryCountdownCts = new CancellationTokenSource();

        // Start countdown task
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(StationaryAddressCheckDelay, _stationaryCountdownCts.Token).ConfigureAwait(false);

                // Countdown expired, check for address change
                if (_stationaryLocationForAddressCheck != null)
                {
                    await CheckForAddressChangeAsync(_stationaryLocationForAddressCheck).ConfigureAwait(false);
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

            // Try to get address from location service (reverse geocoding)
            var currentAddress = await _locationService.GetAddressAsync(
                location.Latitude,
                location.Longitude,
                maxDistanceMeters: 500, // Check within 500m for address data
                cancellationToken: default).ConfigureAwait(false);

            // Try to get closest business
            BusinessLocation? closestBusiness = null;
            try
            {
                closestBusiness = await _locationService.GetClosestBusinessAsync(
                    location.Latitude,
                    location.Longitude,
                    maxDistanceMeters: 100, // Check within 100m for businesses
                    cancellationToken: default).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error getting closest business, continuing without business info");
            }

            // Fallback to coordinates if no address found
            if (string.IsNullOrEmpty(currentAddress))
            {
                currentAddress = $"{location.Latitude:F6}, {location.Longitude:F6}";
                _logger.LogDebug("No address found, using coordinates as fallback");
            }

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

            // Save place where user became stationary (only if address changed or this is first time)
            if (_lastKnownAddress != currentAddress || _lastKnownAddress == null)
            {
                await SaveStationaryPlaceAsync(location, currentAddress, closestBusiness).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for address change");
        }
    }

    private async Task SaveStationaryPlaceAsync(
        GpsCoordinates location,
        string address,
        BusinessLocation? business)
    {
        try
        {
            var place = new StationaryPlaceEntity
            {
                DeviceId = _deviceId,
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                Address = address,
                StationaryAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow
            };

            // Add business information if available
            if (business != null)
            {
                place.BusinessName = business.Name;
                place.Category = business.Category;
                // Use business address if available, otherwise use the resolved address
                if (!string.IsNullOrEmpty(business.Address))
                {
                    place.Address = business.Address;
                }
            }

            _dbContext.StationaryPlaces.Add(place);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Saved stationary place: {BusinessName} at {Address}",
                place.BusinessName ?? "Unknown", place.Address);

            // Apply business theme if available (only when business has an Id, e.g. from Marketing DB)
            if (business != null && business.Id != null && _themeService != null)
            {
                var businessId = business.Id.Value;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var applied = await _themeService.ApplyBusinessThemeAsync(businessId).ConfigureAwait(false);
                        if (applied)
                        {
                            _logger.LogInformation("Applied business theme for business {BusinessId}", businessId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to apply business theme for business {BusinessId}", businessId);
                    }
                });
            }
            else if (business == null && _themeService != null)
            {
                // No business detected, check for city theme
                var (city, state, country) = ExtractCityStateCountryFromAddress(address);
                if (!string.IsNullOrEmpty(city))
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var applied = await _themeService.ApplyCityThemeAsync(city, state, country).ConfigureAwait(false);
                            if (applied)
                            {
                                _logger.LogInformation("Applied city theme for city {CityName}", city);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to apply city theme for city {CityName}", city);
                        }
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving stationary place");
        }
    }

    /// <summary>
    /// Extracts city, state, and country from an address string.
    /// </summary>
    private static (string? city, string? state, string? country) ExtractCityStateCountryFromAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return (null, null, null);

        // Simple parsing: look for common patterns
        // Format: "Street, City, State Country" or "Street, City, State, Country"
        var parts = address.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length < 2)
            return (null, null, null);

        // Last part might be "State Country" or just "Country"
        var lastPart = parts[^1];
        var secondLastPart = parts.Length >= 3 ? parts[^2] : null;

        string? city = null;
        string? state = null;
        string? country = null;

        // Try to extract city (usually second-to-last or third-to-last)
        if (parts.Length >= 2)
        {
            city = parts[^2];
        }

        // Try to extract state and country from last part
        var lastPartWords = lastPart.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (lastPartWords.Length >= 2)
        {
            // Assume format: "State Country"
            state = string.Join(" ", lastPartWords.Take(lastPartWords.Length - 1));
            country = lastPartWords[^1];
        }
        else if (lastPartWords.Length == 1)
        {
            // Could be either state or country - assume country if it's a known country code/name
            var possibleCountry = lastPartWords[0];
            if (possibleCountry.Length == 2 || possibleCountry.Equals("USA", StringComparison.OrdinalIgnoreCase) ||
                possibleCountry.Equals("United States", StringComparison.OrdinalIgnoreCase))
            {
                country = possibleCountry;
            }
            else
            {
                state = possibleCountry;
            }
        }

        return (city, state, country);
    }

    /// <summary>
    /// Determines movement state from speed alone (for initial location when distance is not available).
    /// Invalid or negative speed is treated as Stationary.
    /// </summary>
    private MovementState DetermineMovementStateFromSpeed(double speedMetersPerSecond)
    {
        if (speedMetersPerSecond < 0)
        {
            return MovementState.Stationary; // Invalid reading, assume not moving
        }

        var speedMph = GpsCalculator.MetersPerSecondToMph(speedMetersPerSecond);
        
        // Use a small threshold to account for GPS noise (e.g., 0.5 mph)
        const double stationaryThresholdMph = 0.5;
        
        if (speedMph < stationaryThresholdMph)
        {
            return MovementState.Stationary;
        }
        else if (speedMph >= WalkingRidingSpeedThresholdMph)
        {
            return MovementState.Riding;
        }
        else
        {
            return MovementState.Walking;
        }
    }

    private MovementState DetermineMovementState(double currentDistance, double? currentSpeed)
    {
        if (_recentMovements.Count == 0)
        {
            return MovementState.Stationary;
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

            await _dbContext.DeviceLocationHistory.AddAsync(locationEntity, cancellationToken).ConfigureAwait(false);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("Location stored locally (ID: {LocationId})", locationEntity.Id);
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
            _logger.LogDebug("NewLocationAddress event: {Address}", e.CurrentAddress);

            if (_locationWorkflowService != null)
            {
                await _locationWorkflowService.HandleNewLocationAddressAsync(e).ConfigureAwait(false);
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

    /// <summary>
    /// Disposes of all resources, ensuring proper cleanup of CancellationTokenSource instances.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected dispose method to handle resource cleanup.
    /// </summary>
    /// <param name="disposing">True if called from Dispose(), false if from finalizer</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Stop tracking if still active
            if (IsTracking)
            {
                try
                {
                    // Use synchronous wait with timeout to avoid blocking indefinitely
                    var stopTask = Task.Run(async () => await StopTrackingAsync().ConfigureAwait(false));
                    stopTask.Wait(TimeSpan.FromSeconds(5));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error stopping tracking during disposal");
                }
            }

            // Ensure stationary countdown is reset
            ResetStationaryCountdown();

            // Dispose tracking cancellation token source if still exists
            if (_trackingCts != null)
            {
                try
                {
                    _trackingCts.Cancel();
                    _trackingCts.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing tracking cancellation token source");
                }
                finally
                {
                    _trackingCts = null;
                }
            }
        }
    }
}
