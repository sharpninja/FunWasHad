using FWH.Mobile.Services;
using FWH.Common.Chat.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace FWH.Mobile.Services;

/// <summary>
/// Service that tracks user activities based on movement state transitions.
/// Provides statistics and notifications for walking and riding activities.
/// </summary>
public class ActivityTrackingService
{
    private readonly ILocationTrackingService _locationTrackingService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ActivityTrackingService> _logger;

    // Activity tracking state
    private bool _isTrackingActivity;
    private DateTimeOffset _activityStartTime;
    private MovementState _currentActivityType = MovementState.Unknown;
    private double _totalDistanceMeters;
    private double _maxSpeedMph;
    private int _transitionCount;

    public ActivityTrackingService(
        ILocationTrackingService locationTrackingService,
        INotificationService notificationService,
        ILogger<ActivityTrackingService> logger)
    {
        _locationTrackingService = locationTrackingService ?? throw new ArgumentNullException(nameof(locationTrackingService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets whether activity tracking is currently active.
    /// </summary>
    public bool IsTrackingActivity => _isTrackingActivity;

    /// <summary>
    /// Gets the current activity type being tracked.
    /// </summary>
    public MovementState CurrentActivityType => _currentActivityType;

    /// <summary>
    /// Gets the total distance traveled in the current activity (meters).
    /// </summary>
    public double TotalDistanceMeters => _totalDistanceMeters;

    /// <summary>
    /// Gets the total distance traveled in miles.
    /// </summary>
    public double TotalDistanceMiles => _totalDistanceMeters / 1609.34;

    /// <summary>
    /// Gets the maximum speed reached during the current activity (mph).
    /// </summary>
    public double MaxSpeedMph => _maxSpeedMph;

    /// <summary>
    /// Gets the duration of the current activity.
    /// </summary>
    public TimeSpan ActivityDuration => _isTrackingActivity 
        ? DateTimeOffset.UtcNow - _activityStartTime 
        : TimeSpan.Zero;

    /// <summary>
    /// Gets the average speed for the current activity (mph).
    /// </summary>
    public double AverageSpeedMph
    {
        get
        {
            var duration = ActivityDuration;
            if (duration.TotalSeconds <= 0 || _totalDistanceMeters <= 0)
                return 0;

            var avgSpeedMps = _totalDistanceMeters / duration.TotalSeconds;
            return GpsCalculator.MetersPerSecondToMph(avgSpeedMps);
        }
    }

    /// <summary>
    /// Starts monitoring movement state transitions and tracking activities.
    /// </summary>
    public void StartMonitoring()
    {
        _locationTrackingService.MovementStateChanged += OnMovementStateChanged;
        _locationTrackingService.LocationUpdated += OnLocationUpdated;
        
        _logger.LogInformation("Activity tracking monitoring started");
    }

    /// <summary>
    /// Stops monitoring movement state transitions.
    /// </summary>
    public void StopMonitoring()
    {
        _locationTrackingService.MovementStateChanged -= OnMovementStateChanged;
        _locationTrackingService.LocationUpdated -= OnLocationUpdated;
        
        if (_isTrackingActivity)
        {
            EndActivity();
        }
        
        _logger.LogInformation("Activity tracking monitoring stopped");
    }

    private void OnMovementStateChanged(object? sender, MovementStateChangedEventArgs e)
    {
        try
        {
            _logger.LogInformation(
                "Movement state transition: {Previous} → {Current}, Speed: {Speed:F1} mph",
                e.PreviousState,
                e.CurrentState,
                e.CurrentSpeedMph ?? 0);

            // Handle transitions from stationary
            if (e.PreviousState == MovementState.Stationary || e.PreviousState == MovementState.Unknown)
            {
                if (e.CurrentState == MovementState.Walking)
                {
                    StartWalkingActivity(e);
                }
                else if (e.CurrentState == MovementState.Riding)
                {
                    StartRidingActivity(e);
                }
            }
            // Handle transitions between activities
            else if (e.PreviousState == MovementState.Walking && e.CurrentState == MovementState.Riding)
            {
                TransitionToRiding(e);
            }
            else if (e.PreviousState == MovementState.Riding && e.CurrentState == MovementState.Walking)
            {
                TransitionToWalking(e);
            }
            // Handle transitions to stationary
            else if (e.CurrentState == MovementState.Stationary)
            {
                if (e.PreviousState == MovementState.Walking || e.PreviousState == MovementState.Riding)
                {
                    EndActivity(e);
                }
            }

            // Track max speed
            if (e.CurrentSpeedMph.HasValue && e.CurrentSpeedMph.Value > _maxSpeedMph)
            {
                _maxSpeedMph = e.CurrentSpeedMph.Value;
            }

            _transitionCount++;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling movement state change");
        }
    }

    private void OnLocationUpdated(object? sender, FWH.Common.Location.Models.GpsCoordinates e)
    {
        // Distance tracking is handled in the location tracking service
        // We could add additional tracking here if needed
    }

    private void StartWalkingActivity(MovementStateChangedEventArgs e)
    {
        StartActivity(MovementState.Walking);
        
        _notificationService.ShowSuccess(
            "Your walking activity is now being tracked",
            "Walking started");
        
        _logger.LogInformation("Started walking activity");
    }

    private void StartRidingActivity(MovementStateChangedEventArgs e)
    {
        StartActivity(MovementState.Riding);
        
        var speedText = e.CurrentSpeedMph.HasValue 
            ? $" at {e.CurrentSpeedMph:F1} mph" 
            : "";
        
        _notificationService.ShowSuccess(
            $"Your riding activity is now being tracked{speedText}",
            "Riding started");
        
        _logger.LogInformation("Started riding activity at {Speed:F1} mph", e.CurrentSpeedMph ?? 0);
    }

    private void TransitionToRiding(MovementStateChangedEventArgs e)
    {
        _currentActivityType = MovementState.Riding;
        
        var speedText = e.CurrentSpeedMph.HasValue 
            ? $"{e.CurrentSpeedMph:F1} mph" 
            : "riding speed";
        
        _notificationService.ShowInfo(
            $"You're now traveling at {speedText}",
            "Now riding");
        
        _logger.LogInformation("Transitioned from walking to riding at {Speed:F1} mph", e.CurrentSpeedMph ?? 0);
    }

    private void TransitionToWalking(MovementStateChangedEventArgs e)
    {
        _currentActivityType = MovementState.Walking;
        
        _notificationService.ShowInfo(
            "You've slowed down to walking pace",
            "Now walking");
        
        _logger.LogInformation("Transitioned from riding to walking");
    }

    private void EndActivity(MovementStateChangedEventArgs e)
    {
        var duration = ActivityDuration;
        var distance = TotalDistanceMiles;
        var avgSpeed = AverageSpeedMph;
        var activityType = _currentActivityType == MovementState.Walking ? "Walking" : "Riding";
        
        _notificationService.ShowSuccess(
            $"Duration: {duration.TotalMinutes:F0} min\n" +
            $"Distance: {distance:F2} miles\n" +
            $"Avg Speed: {avgSpeed:F1} mph\n" +
            $"Max Speed: {_maxSpeedMph:F1} mph",
            $"{activityType} activity completed");
        
        _logger.LogInformation(
            "{Activity} activity ended - Duration: {Duration:F0} min, Distance: {Distance:F2} miles, Avg Speed: {AvgSpeed:F1} mph",
            activityType,
            duration.TotalMinutes,
            distance,
            avgSpeed);
        
        EndActivity();
    }

    private void StartActivity(MovementState activityType)
    {
        _isTrackingActivity = true;
        _activityStartTime = DateTimeOffset.UtcNow;
        _currentActivityType = activityType;
        _totalDistanceMeters = 0;
        _maxSpeedMph = 0;
        _transitionCount = 0;
    }

    private void EndActivity()
    {
        _isTrackingActivity = false;
        _currentActivityType = MovementState.Unknown;
    }

    /// <summary>
    /// Gets a summary of the current activity.
    /// </summary>
    public string GetActivitySummary()
    {
        if (!_isTrackingActivity)
            return "No active activity";

        var activityName = _currentActivityType == MovementState.Walking ? "Walking" : "Riding";
        var duration = ActivityDuration;
        
        return $"{activityName}\n" +
               $"Duration: {duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}\n" +
               $"Distance: {TotalDistanceMiles:F2} miles\n" +
               $"Current Speed: {_locationTrackingService.CurrentSpeedMph:F1} mph\n" +
               $"Avg Speed: {AverageSpeedMph:F1} mph\n" +
               $"Max Speed: {MaxSpeedMph:F1} mph\n" +
               $"Transitions: {_transitionCount}";
    }
}
