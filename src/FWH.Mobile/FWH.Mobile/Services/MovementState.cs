namespace FWH.Mobile.Services;

/// <summary>
/// Represents the current movement state of the device.
/// </summary>
public enum MovementState
{
    /// <summary>
    /// State is unknown or not yet determined.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Device is stationary (not moving significantly).
    /// </summary>
    Stationary = 1,

    /// <summary>
    /// Device is moving at walking speed (< 5 mph / 8 km/h).
    /// Continuous motion with speed less than 5 mph.
    /// </summary>
    Walking = 2,

    /// <summary>
    /// Device is moving at riding speed (≥ 5 mph / 8 km/h).
    /// Continuous motion with speed greater than or equal to 5 mph.
    /// </summary>
    Riding = 3,

    /// <summary>
    /// Device is moving but speed cannot be determined yet.
    /// Legacy state for backward compatibility.
    /// </summary>
    Moving = 4
}

/// <summary>
/// Event args for movement state transitions.
/// </summary>
public class MovementStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Previous movement state.
    /// </summary>
    public MovementState PreviousState { get; }

    /// <summary>
    /// Current movement state.
    /// </summary>
    public MovementState CurrentState { get; }

    /// <summary>
    /// Timestamp when the state transition occurred.
    /// </summary>
    public DateTimeOffset TransitionTime { get; }

    /// <summary>
    /// The distance that triggered the transition (if moving).
    /// </summary>
    public double? TriggerDistanceMeters { get; }

    /// <summary>
    /// Duration in the previous state before transitioning.
    /// </summary>
    public TimeSpan DurationInPreviousState { get; }

    /// <summary>
    /// Current speed in meters per second (if available).
    /// </summary>
    public double? CurrentSpeedMetersPerSecond { get; }

    /// <summary>
    /// Current speed in miles per hour (if available).
    /// </summary>
    public double? CurrentSpeedMph => CurrentSpeedMetersPerSecond.HasValue
        ? CurrentSpeedMetersPerSecond.Value * 2.23694
        : null;

    /// <summary>
    /// Current speed in kilometers per hour (if available).
    /// </summary>
    public double? CurrentSpeedKmh => CurrentSpeedMetersPerSecond.HasValue
        ? CurrentSpeedMetersPerSecond.Value * 3.6
        : null;

    public MovementStateChangedEventArgs(
        MovementState previousState,
        MovementState currentState,
        DateTimeOffset transitionTime,
        double? triggerDistanceMeters,
        TimeSpan durationInPreviousState,
        double? currentSpeedMetersPerSecond = null)
    {
        PreviousState = previousState;
        CurrentState = currentState;
        TransitionTime = transitionTime;
        TriggerDistanceMeters = triggerDistanceMeters;
        DurationInPreviousState = durationInPreviousState;
        CurrentSpeedMetersPerSecond = currentSpeedMetersPerSecond;
    }

    public override string ToString()
    {
        var result = $"{PreviousState} → {CurrentState} at {TransitionTime:HH:mm:ss} " +
                     $"(duration: {DurationInPreviousState.TotalSeconds:F0}s";

        if (TriggerDistanceMeters.HasValue)
        {
            result += $", distance: {TriggerDistanceMeters:F1}m";
        }

        if (CurrentSpeedMph.HasValue)
        {
            result += $", speed: {CurrentSpeedMph:F1} mph";
        }

        result += ")";
        return result;
    }
}
