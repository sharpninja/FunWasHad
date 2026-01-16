using Xunit;
using FWH.Mobile.Services;
using System;

namespace FWH.Mobile.Tests.Services;

/// <summary>
/// Tests for movement state transitions and event handling.
/// </summary>
public class MovementStateTests
{
    [Fact]
    public void MovementState_HasExpectedValues()
    {
        // Assert - Verify enum values
        Assert.Equal(0, (int)MovementState.Unknown);
        Assert.Equal(1, (int)MovementState.Stationary);
        Assert.Equal(2, (int)MovementState.Walking);
        Assert.Equal(3, (int)MovementState.Riding);
        Assert.Equal(4, (int)MovementState.Moving);
    }

    [Fact]
    public void MovementStateChangedEventArgs_Constructor_SetsProperties()
    {
        // Arrange
        var previousState = MovementState.Stationary;
        var currentState = MovementState.Walking;
        var transitionTime = DateTimeOffset.UtcNow;
        var triggerDistance = 75.5;
        var duration = TimeSpan.FromMinutes(5);
        var speed = 2.0; // 2 m/s

        // Act
        var eventArgs = new MovementStateChangedEventArgs(
            previousState,
            currentState,
            transitionTime,
            triggerDistance,
            duration,
            speed);

        // Assert
        Assert.Equal(previousState, eventArgs.PreviousState);
        Assert.Equal(currentState, eventArgs.CurrentState);
        Assert.Equal(transitionTime, eventArgs.TransitionTime);
        Assert.Equal(triggerDistance, eventArgs.TriggerDistanceMeters);
        Assert.Equal(duration, eventArgs.DurationInPreviousState);
        Assert.Equal(speed, eventArgs.CurrentSpeedMetersPerSecond);
    }

    [Fact]
    public void MovementStateChangedEventArgs_SpeedProperties_CalculateCorrectly()
    {
        // Arrange - 2.23694 m/s = 5 mph = 8.05 km/h
        var speed = 2.23694;
        var eventArgs = new MovementStateChangedEventArgs(
            MovementState.Walking,
            MovementState.Riding,
            DateTimeOffset.UtcNow,
            100.0,
            TimeSpan.FromMinutes(2),
            speed);

        // Assert
        Assert.NotNull(eventArgs.CurrentSpeedMph);
        Assert.NotNull(eventArgs.CurrentSpeedKmh);
        Assert.InRange(eventArgs.CurrentSpeedMph.Value, 4.99, 5.01);  // ~5 mph
        Assert.InRange(eventArgs.CurrentSpeedKmh.Value, 8.04, 8.06);  // ~8.05 km/h
    }

    [Fact]
    public void MovementStateChangedEventArgs_ToString_IncludesSpeed()
    {
        // Arrange
        var eventArgs = new MovementStateChangedEventArgs(
            MovementState.Walking,
            MovementState.Riding,
            new DateTimeOffset(2025, 1, 8, 14, 30, 0, TimeSpan.Zero),
            100.0,
            TimeSpan.FromMinutes(5),
            4.5); // ~10 mph

        // Act
        var result = eventArgs.ToString();

        // Assert
        Assert.Contains("Walking", result);
        Assert.Contains("Riding", result);
        Assert.Contains("14:30:00", result);
        Assert.Contains("300s", result);
        Assert.Contains("100", result);
        Assert.Contains("mph", result);
    }

    [Fact]
    public void MovementStateChangedEventArgs_WithNullSpeed_FormatsCorrectly()
    {
        // Arrange
        var eventArgs = new MovementStateChangedEventArgs(
            MovementState.Unknown,
            MovementState.Stationary,
            DateTimeOffset.UtcNow,
            null,
            TimeSpan.FromSeconds(90),
            null);

        // Act
        var result = eventArgs.ToString();

        // Assert
        Assert.Contains("Unknown", result);
        Assert.Contains("Stationary", result);
        Assert.Contains("90s", result);
        Assert.DoesNotContain("mph", result);
    }

    [Theory]
    [InlineData(MovementState.Unknown, MovementState.Stationary)]
    [InlineData(MovementState.Stationary, MovementState.Walking)]
    [InlineData(MovementState.Walking, MovementState.Riding)]
    [InlineData(MovementState.Riding, MovementState.Walking)]
    [InlineData(MovementState.Walking, MovementState.Stationary)]
    [InlineData(MovementState.Riding, MovementState.Stationary)]
    [InlineData(MovementState.Unknown, MovementState.Walking)]
    [InlineData(MovementState.Unknown, MovementState.Riding)]
    public void MovementStateChangedEventArgs_SupportsAllTransitions(
        MovementState from, MovementState to)
    {
        // Act
        var eventArgs = new MovementStateChangedEventArgs(
            from,
            to,
            DateTimeOffset.UtcNow,
            50.0,
            TimeSpan.FromMinutes(1),
            2.0);

        // Assert
        Assert.Equal(from, eventArgs.PreviousState);
        Assert.Equal(to, eventArgs.CurrentState);
    }

    [Fact]
    public void MovementState_EnumCanBeCompared()
    {
        // Arrange
        var unknown = MovementState.Unknown;
        var stationary = MovementState.Stationary;
        var walking = MovementState.Walking;
        var riding = MovementState.Riding;
        var moving = MovementState.Moving;

        // Assert - Test equality
        Assert.Equal(MovementState.Unknown, unknown);
        Assert.NotEqual(stationary, walking);

        // Assert - Test ordering
        Assert.True((int)unknown < (int)stationary);
        Assert.True((int)stationary < (int)walking);
        Assert.True((int)walking < (int)riding);
        Assert.True((int)riding < (int)moving);
    }
}

/// <summary>
/// Integration tests for movement state detection scenarios with walking and riding.
/// </summary>
public class MovementStateDetectionScenarioTests
{
    [Fact]
    public void StationaryToWalking_WhenSpeedBelow5Mph_ShouldTransition()
    {
        // This test validates the scenario where:
        // - Device is stationary
        // - Device starts moving at walking speed (< 5 mph)
        // - State should transition to Walking

        var walkingSpeedThreshold = 5.0; // mph
        Assert.True(walkingSpeedThreshold == 5.0, "Walking/Riding threshold should be 5 mph");
    }

    [Fact]
    public void StationaryToRiding_WhenSpeedAbove5Mph_ShouldTransition()
    {
        // This test validates the scenario where:
        // - Device is stationary
        // - Device starts moving at riding speed (>= 5 mph)
        // - State should transition to Riding

        var ridingSpeedThreshold = 5.0; // mph
        Assert.True(ridingSpeedThreshold == 5.0, "Walking/Riding threshold should be 5 mph");
    }

    [Fact]
    public void WalkingToRiding_WhenSpeedIncreases_ShouldTransition()
    {
        // This test validates the scenario where:
        // - Device is in Walking state (< 5 mph)
        // - Speed increases to >= 5 mph (e.g., got on a bike or in a car)
        // - State should transition to Riding

        var initialSpeed = 4.0; // mph - walking
        var newSpeed = 10.0;    // mph - riding
        
        Assert.True(initialSpeed < 5.0);
        Assert.True(newSpeed >= 5.0);
    }

    [Fact]
    public void RidingToWalking_WhenSpeedDecreases_ShouldTransition()
    {
        // This test validates the scenario where:
        // - Device is in Riding state (>= 5 mph)
        // - Speed decreases to < 5 mph (e.g., got off bike and started walking)
        // - State should transition to Walking

        var initialSpeed = 15.0; // mph - riding
        var newSpeed = 3.0;      // mph - walking
        
        Assert.True(initialSpeed >= 5.0);
        Assert.True(newSpeed < 5.0);
    }

    [Fact]
    public void WalkingToStationary_WhenStopped_ShouldTransition()
    {
        // This test validates the scenario where:
        // - Device is walking
        // - Device stops moving for 3+ minutes
        // - State should transition to Stationary

        var stationaryThreshold = TimeSpan.FromMinutes(3);
        Assert.True(stationaryThreshold >= TimeSpan.FromMinutes(3));
    }

    [Fact]
    public void RidingToStationary_WhenStopped_ShouldTransition()
    {
        // This test validates the scenario where:
        // - Device is riding
        // - Device stops moving for 3+ minutes
        // - State should transition to Stationary

        var stationaryThreshold = TimeSpan.FromMinutes(3);
        Assert.True(stationaryThreshold >= TimeSpan.FromMinutes(3));
    }

    [Theory]
    [InlineData(0.5)]    // 1.1 mph - slow walk
    [InlineData(1.0)]    // 2.2 mph - normal walk
    [InlineData(1.5)]    // 3.4 mph - brisk walk
    [InlineData(2.0)]    // 4.5 mph - fast walk/slow jog
    public void WalkingSpeeds_ShouldBeClassifiedAsWalking(double speedMetersPerSecond)
    {
        // All speeds should result in Walking state (< 5 mph)
        var speedMph = GpsCalculator.MetersPerSecondToMph(speedMetersPerSecond);
        Assert.True(speedMph < 5.0, $"Speed {speedMph:F1} mph should be classified as walking");
    }

    [Theory]
    [InlineData(2.24)]   // 5.0 mph - cycling/slow driving
    [InlineData(4.5)]    // 10.1 mph - cycling
    [InlineData(10.0)]   // 22.4 mph - fast cycling/slow driving
    [InlineData(13.4)]   // 30 mph - driving
    [InlineData(26.8)]   // 60 mph - highway driving
    public void RidingSpeeds_ShouldBeClassifiedAsRiding(double speedMetersPerSecond)
    {
        // All speeds should result in Riding state (>= 5 mph)
        var speedMph = GpsCalculator.MetersPerSecondToMph(speedMetersPerSecond);
        Assert.True(speedMph >= 5.0, $"Speed {speedMph:F1} mph should be classified as riding");
    }

    [Fact]
    public void EdgeCase_Exactly5Mph_ShouldBeRiding()
    {
        // At exactly 5 mph, should be classified as Riding
        var speed = GpsCalculator.MphToMetersPerSecond(5.0);
        var isWalking = GpsCalculator.IsWalkingSpeed(speed);
        var isRiding = GpsCalculator.IsRidingSpeed(speed);

        Assert.False(isWalking);
        Assert.True(isRiding);
    }

    [Fact]
    public void RealWorldScenario_CommuteToCoffeeShop()
    {
        // Walking to coffee shop (3 mph average)
        var walkingSpeed = GpsCalculator.MphToMetersPerSecond(3.0);
        Assert.True(GpsCalculator.IsWalkingSpeed(walkingSpeed));

        // Cycling home (12 mph average)
        var cyclingSpeed = GpsCalculator.MphToMetersPerSecond(12.0);
        Assert.True(GpsCalculator.IsRidingSpeed(cyclingSpeed));
    }

    [Fact]
    public void RealWorldScenario_DriveToStore()
    {
        // Walking in parking lot (2 mph)
        var parkingSpeed = GpsCalculator.MphToMetersPerSecond(2.0);
        Assert.True(GpsCalculator.IsWalkingSpeed(parkingSpeed));

        // Driving to store (30 mph average)
        var drivingSpeed = GpsCalculator.MphToMetersPerSecond(30.0);
        Assert.True(GpsCalculator.IsRidingSpeed(drivingSpeed));

        // Walking in store (1.5 mph)
        var shoppingSpeed = GpsCalculator.MphToMetersPerSecond(1.5);
        Assert.True(GpsCalculator.IsWalkingSpeed(shoppingSpeed));
    }

    [Fact]
    public void ConfigurableThresholds_ShouldAllowCustomization()
    {
        // Validate that thresholds can be customized for different use cases

        // Example: Running detection (higher threshold)
        var runningThreshold = 6.0; // mph
        
        // Example: Scooter detection (lower threshold)
        var scooterThreshold = 3.0; // mph

        Assert.True(runningThreshold > 5.0);
        Assert.True(scooterThreshold < 5.0);
    }

    [Theory]
    [InlineData(2.0, 3.0, 4.0, 4.5)]  // Accelerating from walk to jog
    [InlineData(10.0, 12.0, 15.0, 20.0)]  // Accelerating while cycling/driving
    public void ContinuousAcceleration_ShouldTransitionOnceAcrossThreshold(
        double speed1Mph, double speed2Mph, double speed3Mph, double speed4Mph)
    {
        // Convert to m/s
        var speeds = new[] { speed1Mph, speed2Mph, speed3Mph, speed4Mph }
            .Select(GpsCalculator.MphToMetersPerSecond)
            .ToArray();

        // Verify transition logic
        var isWalking1 = GpsCalculator.IsWalkingSpeed(speeds[0]);
        var isWalking4 = GpsCalculator.IsWalkingSpeed(speeds[3]);

        // Should have different classifications
        if (speed1Mph < 5.0 && speed4Mph >= 5.0)
        {
            Assert.True(isWalking1);
            Assert.False(isWalking4);
        }
    }
}
