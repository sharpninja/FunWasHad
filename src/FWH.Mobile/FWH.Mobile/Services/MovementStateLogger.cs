using FWH.Mobile.Services;
using Microsoft.Extensions.Logging;
using System;

namespace FWH.Mobile.Services;

/// <summary>
/// Demonstration service that logs movement state transitions to console.
/// This shows how to use the walking/riding detection feature.
/// </summary>
public class MovementStateLogger
{
    private readonly ILocationTrackingService _locationTrackingService;
    private readonly ILogger<MovementStateLogger> _logger;

    public MovementStateLogger(
        ILocationTrackingService locationTrackingService,
        ILogger<MovementStateLogger> logger)
    {
        _locationTrackingService = locationTrackingService ?? throw new ArgumentNullException(nameof(locationTrackingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Starts logging movement state transitions.
    /// </summary>
    public void StartLogging()
    {
        _locationTrackingService.MovementStateChanged += OnMovementStateChanged;
        _locationTrackingService.LocationUpdated += OnLocationUpdated;
        
        _logger.LogDebug("Movement state logging started");
        LogCurrentState();
    }

    /// <summary>
    /// Stops logging movement state transitions.
    /// </summary>
    public void StopLogging()
    {
        _locationTrackingService.MovementStateChanged -= OnMovementStateChanged;
        _locationTrackingService.LocationUpdated -= OnLocationUpdated;
        
        _logger.LogDebug("Movement state logging stopped");
    }

    private void OnMovementStateChanged(object? sender, MovementStateChangedEventArgs e)
    {
        var message = $"""
            ================================================================================
            MOVEMENT STATE CHANGED
            ================================================================================
            Previous State:    {e.PreviousState}
            Current State:     {e.CurrentState}
            Transition Time:   {e.TransitionTime:yyyy-MM-dd HH:mm:ss}
            Duration in State: {e.DurationInPreviousState.TotalMinutes:F1} minutes
            """;

        if (e.TriggerDistanceMeters.HasValue)
        {
            message += $"\nTrigger Distance:  {e.TriggerDistanceMeters:F1} meters ({e.TriggerDistanceMeters / 1609.34:F2} miles)";
        }

        if (e.CurrentSpeedMph.HasValue)
        {
            message += $"""
                
                Current Speed:     {e.CurrentSpeedMph:F1} mph ({e.CurrentSpeedKmh:F1} km/h)
                Speed (m/s):       {e.CurrentSpeedMetersPerSecond:F2}
                """;
        }

        message += "\n================================================================================";

        _logger.LogInformation(message);

        // Log specific transition messages
        LogTransitionMessage(e);
    }

    private void LogTransitionMessage(MovementStateChangedEventArgs e)
    {
        var message = e switch
        {
            { PreviousState: MovementState.Stationary, CurrentState: MovementState.Walking }
                => "🚶 Started walking",
            
            { PreviousState: MovementState.Stationary, CurrentState: MovementState.Riding }
                => $"🚴 Started riding at {e.CurrentSpeedMph:F1} mph",
            
            { PreviousState: MovementState.Walking, CurrentState: MovementState.Riding }
                => $"⬆️  Accelerated from walking to riding ({e.CurrentSpeedMph:F1} mph)",
            
            { PreviousState: MovementState.Riding, CurrentState: MovementState.Walking }
                => "⬇️  Slowed down from riding to walking",
            
            { PreviousState: MovementState.Walking, CurrentState: MovementState.Stationary }
                => $"⏸️  Stopped walking (walked for {e.DurationInPreviousState.TotalMinutes:F1} minutes)",
            
            { PreviousState: MovementState.Riding, CurrentState: MovementState.Stationary }
                => $"⏸️  Stopped riding (rode for {e.DurationInPreviousState.TotalMinutes:F1} minutes)",
            
            _ => $"State transition: {e.PreviousState} → {e.CurrentState}"
        };

        _logger.LogInformation(message);
    }

    private void OnLocationUpdated(object? sender, FWH.Common.Location.Models.GpsCoordinates e)
    {
        var state = _locationTrackingService.CurrentMovementState;
        var speed = _locationTrackingService.CurrentSpeedMph;
        
        _logger.LogDebug(
            "Location updated: ({Lat:F6}, {Lon:F6}) - State: {State}, Speed: {Speed:F1} mph",
            e.Latitude,
            e.Longitude,
            state,
            speed ?? 0);
    }

    private void LogCurrentState()
    {
        var state = _locationTrackingService.CurrentMovementState;
        var speed = _locationTrackingService.CurrentSpeedMph;
        
        _logger.LogInformation(
            "Current Movement State: {State}, Speed: {Speed:F1} mph",
            state,
            speed ?? 0);
    }
}
