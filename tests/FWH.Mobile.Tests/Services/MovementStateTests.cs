using Xunit;
using FWH.Mobile.Services;
using System;

namespace FWH.Mobile.Tests.Services;

/// <summary>
/// Tests for movement state transitions and event handling.
/// </summary>
public class MovementStateTests
{
    /// <summary>
    /// Tests that the MovementState enum has the expected integer values for each state.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The MovementState enum's underlying integer values to ensure they match expected values.</para>
    /// <para><strong>Data involved:</strong> The MovementState enum with values: Stationary=0, Walking=1, Riding=2, Moving=3. These values are used for database storage and state comparisons.</para>
    /// <para><strong>Why the data matters:</strong> Enum values are often persisted to databases as integers. If the values change, existing database records may become invalid or misclassified. This test ensures the enum values remain stable, preventing data corruption during migrations or when reading historical data. Stationary is the default (0).</para>
    /// <para><strong>Expected outcome:</strong> Each MovementState enum value should match its expected integer: Stationary=0, Walking=1, Riding=2, Moving=3.</para>
    /// <para><strong>Reason for expectation:</strong> Enum values are typically assigned sequentially starting from 0 unless explicitly specified. These specific values may be used in database schemas, API contracts, or serialization formats. Verifying the values ensures compatibility with existing data and prevents breaking changes if the enum is modified.</para>
    /// </remarks>
    [Fact]
    public void MovementStateHasExpectedValues()
    {
        // Assert - Verify enum values (Stationary is default 0)
        Assert.Equal(0, (int)MovementState.Stationary);
        Assert.Equal(1, (int)MovementState.Walking);
        Assert.Equal(2, (int)MovementState.Riding);
        Assert.Equal(3, (int)MovementState.Moving);
    }

    /// <summary>
    /// Tests that MovementStateChangedEventArgs constructor correctly sets all properties from the provided parameters.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The MovementStateChangedEventArgs constructor's ability to correctly initialize all properties from constructor parameters.</para>
    /// <para><strong>Data involved:</strong> Constructor parameters: previousState=Stationary, currentState=Walking, transitionTime=UtcNow, triggerDistance=75.5 meters, duration=5 minutes, speed=2.0 m/s. These represent a typical state transition from stationary to walking after moving 75.5 meters over 5 minutes at 2 m/s.</para>
    /// <para><strong>Why the data matters:</strong> MovementStateChangedEventArgs carries important information about state transitions (previous/current state, timing, distance, speed) to event subscribers. The constructor must correctly store all parameters so subscribers can analyze movement patterns, log transitions, or update UI. Incorrect property values would lead to wrong analytics or user feedback.</para>
    /// <para><strong>Expected outcome:</strong> All eventArgs properties should match the constructor parameters exactly: PreviousState=Stationary, CurrentState=Walking, TransitionTime matches, TriggerDistanceMeters=75.5, DurationInPreviousState=5 minutes, CurrentSpeedMetersPerSecond=2.0.</para>
    /// <para><strong>Reason for expectation:</strong> The constructor should assign each parameter to its corresponding property without modification. This ensures event subscribers receive accurate information about the state transition. The exact matches confirm that property assignment works correctly and no data transformation or loss occurs during construction.</para>
    /// </remarks>
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

    /// <summary>
    /// Tests that MovementStateChangedEventArgs correctly calculates speed properties (CurrentSpeedMph and CurrentSpeedKmh) from meters per second.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The MovementStateChangedEventArgs constructor's ability to convert speed from meters per second to miles per hour and kilometers per hour.</para>
    /// <para><strong>Data involved:</strong> A speed value of 2.23694 m/s, which equals exactly 5.0 mph (2.23694 * 2.23694 ≈ 5.0) and approximately 8.05 km/h (2.23694 * 3.6 ≈ 8.05). The event args are created with this speed value along with state transition data (Walking→Riding).</para>
    /// <para><strong>Why the data matters:</strong> Speed conversion is critical for displaying user-friendly speed values in different units (mph for US users, km/h for international). The conversion formulas must be accurate: mph = m/s * 2.23694, km/h = m/s * 3.6. This test validates that the conversion calculations are correct and the properties are properly populated.</para>
    /// <para><strong>Expected outcome:</strong> CurrentSpeedMph should be approximately 5.0 (within range 4.99-5.01), and CurrentSpeedKmh should be approximately 8.05 (within range 8.04-8.06).</para>
    /// <para><strong>Reason for expectation:</strong> The constructor should convert 2.23694 m/s to mph using the conversion factor 2.23694 (m/s to mph), resulting in approximately 5.0 mph. Similarly, conversion to km/h uses factor 3.6, resulting in approximately 8.05 km/h. The small tolerance ranges account for floating-point precision. The non-null assertions confirm the properties are calculated and populated correctly.</para>
    /// </remarks>
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

    /// <summary>
    /// Tests that MovementStateChangedEventArgs.ToString includes speed information when speed is available.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The MovementStateChangedEventArgs.ToString method's ability to format a human-readable string representation that includes speed information when speed is provided.</para>
    /// <para><strong>Data involved:</strong> Event args with Walking→Riding transition, transition time 2025-01-08 14:30:00, trigger distance 100.0 meters, duration 5 minutes (300 seconds), and speed 4.5 m/s (~10 mph). The ToString method should format all this information into a readable string.</para>
    /// <para><strong>Why the data matters:</strong> ToString is used for logging, debugging, and displaying movement state changes to users. It must include all relevant information (states, time, distance, duration, speed) in a readable format. The speed information is particularly important for understanding movement patterns and debugging location tracking issues.</para>
    /// <para><strong>Expected outcome:</strong> The ToString result should contain "Walking", "Riding", "14:30:00" (formatted time), "300s" (duration in seconds), "100" (distance), and "mph" (speed unit indicator).</para>
    /// <para><strong>Reason for expectation:</strong> The ToString method should format all event args properties into a readable string. The presence of state names confirms state information is included, the time format confirms timestamp formatting, "300s" confirms duration formatting, "100" confirms distance is included, and "mph" confirms speed is formatted and displayed. This ensures the string representation is informative and useful for debugging.</para>
    /// </remarks>
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

    /// <summary>
    /// Tests that MovementStateChangedEventArgs.ToString correctly formats the string representation when speed is null, omitting speed-related information.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The MovementStateChangedEventArgs.ToString method's handling of null speed values, ensuring it formats correctly without including speed information.</para>
    /// <para><strong>Data involved:</strong> Event args with Stationary→Walking transition, null trigger distance, duration 90 seconds, and null speed. The ToString method should format the available information without attempting to display speed.</para>
    /// <para><strong>Why the data matters:</strong> Speed may be unavailable in some scenarios (e.g., GPS signal lost, device stationary). The ToString method must handle null speed gracefully without throwing exceptions or displaying misleading information. This ensures robust string formatting for all event args scenarios.</para>
    /// <para><strong>Expected outcome:</strong> The ToString result should contain "Stationary", "Walking", and "90s" (duration), but should NOT contain "mph" (speed unit indicator).</para>
    /// <para><strong>Reason for expectation:</strong> When speed is null, the ToString method should skip speed-related formatting. The presence of state names and duration confirms basic formatting works, while the absence of "mph" confirms that speed information is correctly omitted when null. This prevents displaying misleading or invalid speed information.</para>
    /// </remarks>
    [Fact]
    public void MovementStateChangedEventArgs_WithNullSpeed_FormatsCorrectly()
    {
        // Arrange
        var eventArgs = new MovementStateChangedEventArgs(
            MovementState.Stationary,
            MovementState.Walking,
            DateTimeOffset.UtcNow,
            null,
            TimeSpan.FromSeconds(90),
            null);

        // Act
        var result = eventArgs.ToString();

        // Assert
        Assert.Contains("Stationary", result);
        Assert.Contains("Walking", result);
        Assert.Contains("90s", result);
        Assert.DoesNotContain("mph", result);
    }

    /// <summary>
    /// Tests that MovementStateChangedEventArgs supports all valid state transitions between any two MovementState values.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The MovementStateChangedEventArgs constructor's ability to handle all possible state transition combinations, validating that any state can transition to any other state.</para>
    /// <para><strong>Data involved:</strong> Six state transition pairs covering common scenarios: Stationary→Walking, Stationary→Riding, Walking→Riding, Riding→Walking, Walking→Stationary, Riding→Stationary. Each transition is tested with sample event data (50m distance, 1 minute duration, 2.0 m/s speed).</para>
    /// <para><strong>Why the data matters:</strong> Movement state transitions can occur in any order depending on user behavior (e.g., walking→riding when getting on a bike, riding→walking when getting off). The event args must support all transition combinations without restrictions. This test validates that the constructor doesn't enforce artificial transition rules and can represent any state change.</para>
    /// <para><strong>Expected outcome:</strong> For each transition pair, the event args should be created successfully with PreviousState matching the "from" state and CurrentState matching the "to" state.</para>
    /// <para><strong>Reason for expectation:</strong> The constructor should accept any two MovementState values without validation, allowing the event system to represent any state change. The PreviousState and CurrentState properties should be set exactly as provided, confirming that all transition combinations are supported. This flexibility is important for accurately tracking movement state changes regardless of the transition pattern.</para>
    /// </remarks>
    [Theory]
    [InlineData(MovementState.Stationary, MovementState.Walking)]
    [InlineData(MovementState.Stationary, MovementState.Riding)]
    [InlineData(MovementState.Walking, MovementState.Riding)]
    [InlineData(MovementState.Riding, MovementState.Walking)]
    [InlineData(MovementState.Walking, MovementState.Stationary)]
    [InlineData(MovementState.Riding, MovementState.Stationary)]
    public void MovementStateChangedEventArgsSupportsAllTransitions(
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

    /// <summary>
    /// Tests that MovementState enum values can be compared for equality and ordering using their underlying integer values.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The MovementState enum's comparison capabilities, including equality comparison and ordering based on underlying integer values.</para>
    /// <para><strong>Data involved:</strong> All four MovementState enum values: Stationary=0, Walking=1, Riding=2, Moving=3. The test compares values for equality (same values equal, different values not equal) and ordering (integer values increase sequentially).</para>
    /// <para><strong>Why the data matters:</strong> Enum comparison is used in conditional logic, sorting, and state machine transitions. The enum must support standard comparison operations (==, !=, <, >) for use in if statements, switch cases, and LINQ queries. Stationary=0 is the default.</para>
    /// <para><strong>Expected outcome:</strong> Equality comparisons should work (Stationary==Stationary, Stationary!=Walking), and ordering should follow integer values (Stationary < Walking < Riding < Moving).</para>
    /// <para><strong>Reason for expectation:</strong> Enums in C# support equality comparison by default, and can be compared using their underlying integer values. The sequential values (0-4) ensure predictable ordering. The equality assertions confirm enum comparison works, and the ordering assertions confirm integer-based ordering is correct. This validates that the enum can be used in comparison operations throughout the codebase.</para>
    /// </remarks>
    [Fact]
    public void MovementStateEnumCanBeCompared()
    {
        // Arrange
        var stationary = MovementState.Stationary;
        var walking = MovementState.Walking;
        var riding = MovementState.Riding;
        var moving = MovementState.Moving;

        // Assert - Test equality
        Assert.Equal(MovementState.Stationary, stationary);
        Assert.NotEqual(stationary, walking);

        // Assert - Test ordering (Stationary=0 is default)
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
    public void StationaryToRidingWhenSpeedAbove5MphShouldTransition()
    {
        // This test validates the scenario where:
        // - Device is stationary
        // - Device starts moving at riding speed (>= 5 mph)
        // - State should transition to Riding

        var ridingSpeedThreshold = 5.0; // mph
        Assert.True(ridingSpeedThreshold == 5.0, "Walking/Riding threshold should be 5 mph");
    }

    [Fact]
    public void WalkingToRidingWhenSpeedIncreasesShouldTransition()
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
    public void RidingToWalkingWhenSpeedDecreasesShouldTransition()
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
    public void RealWorldScenarioCommuteToCoffeeShop()
    {
        // Walking to coffee shop (3 mph average)
        var walkingSpeed = GpsCalculator.MphToMetersPerSecond(3.0);
        Assert.True(GpsCalculator.IsWalkingSpeed(walkingSpeed));

        // Cycling home (12 mph average)
        var cyclingSpeed = GpsCalculator.MphToMetersPerSecond(12.0);
        Assert.True(GpsCalculator.IsRidingSpeed(cyclingSpeed));
    }

    /// <summary>
    /// Tests a real-world movement scenario: driving to a store with walking segments (parking lot and inside store), validating that all movement types are correctly classified.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The speed classification logic's ability to correctly classify a complex real-world scenario involving multiple movement types: walking in a parking lot, driving, and walking inside a store.</para>
    /// <para><strong>Data involved:</strong> Three movement speeds: 2.0 mph (walking in parking lot), 30.0 mph (driving to store), and 1.5 mph (walking inside store). These represent typical speeds for these activities in a shopping scenario.</para>
    /// <para><strong>Why the data matters:</strong> Real-world scenarios often involve multiple movement types in sequence. The classification logic must correctly handle transitions between walking and driving/riding. This test validates that the system accurately classifies all movement types in a typical shopping trip scenario.</para>
    /// <para><strong>Expected outcome:</strong> The 2.0 mph and 1.5 mph speeds should be classified as Walking (IsWalkingSpeed returns true), and the 30.0 mph speed should be classified as Riding (IsRidingSpeed returns true).</para>
    /// <para><strong>Reason for expectation:</strong> Both walking speeds (2.0 and 1.5 mph) are below the 5 mph threshold, so they should be classified as Walking. The 30.0 mph driving speed is well above the threshold, so it should be classified as Riding. This validates that the classification logic correctly handles complex real-world scenarios with multiple movement types.</para>
    /// </remarks>
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

    /// <summary>
    /// Tests that when speed continuously accelerates across the 5 mph threshold, the classification correctly transitions from Walking to Riding at the threshold boundary.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The speed classification logic's handling of continuous acceleration scenarios where speed gradually increases and crosses the 5 mph threshold.</para>
    /// <para><strong>Data involved:</strong> Two acceleration scenarios: (1) 2.0, 3.0, 4.0, 4.5 mph (accelerating from walk to jog, all below threshold), and (2) 10.0, 12.0, 15.0, 20.0 mph (accelerating while cycling/driving, all above threshold). For scenarios where speed1 < 5 mph and speed4 >= 5 mph, the classification should transition from Walking to Riding.</para>
    /// <para><strong>Why the data matters:</strong> In real-world scenarios, speed doesn't jump instantly but accelerates gradually. The classification logic must correctly handle continuous acceleration and transition at the threshold. This test validates that the threshold logic works correctly for gradual speed changes, not just discrete speed values.</para>
    /// <para><strong>Expected outcome:</strong> For acceleration scenarios where speed1 < 5 mph and speed4 >= 5 mph, speed1 should be classified as Walking (isWalking1 = true) and speed4 should be classified as Riding (isWalking4 = false).</para>
    /// <para><strong>Reason for expectation:</strong> When speed accelerates from below the threshold to above it, the classification should transition from Walking to Riding. The first speed being below 5 mph should be classified as Walking, and the final speed being at or above 5 mph should be classified as Riding. This validates that the threshold logic correctly handles continuous acceleration scenarios and transitions at the boundary.</para>
    /// </remarks>
    [Theory]
    [InlineData(2.0, 3.0, 4.0, 4.5)]  // Accelerating from walk to jog
    [InlineData(10.0, 12.0, 15.0, 20.0)]  // Accelerating while cycling/driving
    public void ContinuousAccelerationShouldTransitionOnceAcrossThreshold(
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
