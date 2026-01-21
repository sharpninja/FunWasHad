using Xunit;
using FWH.Mobile.Services;

namespace FWH.Mobile.Tests.Services;

/// <summary>
/// Tests for GPS distance and bearing calculations.
/// </summary>
public class GpsCalculatorTests
{
    [Theory]
    [InlineData(0, 0, 0, 0, 0)] // Same point
    [InlineData(0, 0, 0, 1, 111319.49)] // 1 degree longitude at equator ≈ 111.32 km
    [InlineData(0, 0, 1, 0, 111319.49)] // 1 degree latitude ≈ 111.32 km
    [InlineData(40.7128, -74.0060, 40.7589, -73.9851, 5420.0)] // NYC: Times Square to Central Park (actual distance)
    public void CalculateDistance_WithKnownCoordinates_ReturnsExpectedDistance(
        double lat1, double lon1, double lat2, double lon2, double expectedMeters)
    {
        // Act
        var distance = GpsCalculator.CalculateDistance(lat1, lon1, lat2, lon2);

        // Assert - Allow 2% tolerance for floating point precision and slight calculation variations
        Assert.InRange(distance, expectedMeters * 0.98, expectedMeters * 1.02);
    }

    [Fact]
    public void CalculateDistance_WithSamePoint_ReturnsZero()
    {
        // Arrange
        var lat = 37.7749;
        var lon = -122.4194;

        // Act
        var distance = GpsCalculator.CalculateDistance(lat, lon, lat, lon);

        // Assert
        Assert.Equal(0, distance, precision: 5);
    }

    [Theory]
    [InlineData(0, 0, 1, 0, 0)]     // North
    [InlineData(0, 0, 0, 1, 90)]    // East
    [InlineData(0, 0, -1, 0, 180)]  // South
    [InlineData(0, 0, 0, -1, 270)]  // West
    public void CalculateBearing_WithCardinalDirections_ReturnsCorrectBearing(
        double lat1, double lon1, double lat2, double lon2, double expectedBearing)
    {
        // Act
        var bearing = GpsCalculator.CalculateBearing(lat1, lon1, lat2, lon2);

        // Assert - Allow 5 degree tolerance
        Assert.InRange(bearing, expectedBearing - 5, expectedBearing + 5);
    }

    [Theory]
    [InlineData(0, 0, 0, 0, 100, true)]      // Same point
    [InlineData(0, 0, 0, 0.0009, 102, true)] // ~100m at equator (actual: ~100.2m, need 102m radius for safety)
    [InlineData(0, 0, 0, 0.002, 100, false)] // ~200m at equator
    [InlineData(40.7128, -74.0060, 40.7589, -73.9851, 10000, true)]  // Within 10km
    [InlineData(40.7128, -74.0060, 40.7589, -73.9851, 1000, false)]  // Not within 1km
    public void IsWithinRadius_WithVariousDistances_ReturnsExpectedResult(
        double centerLat, double centerLon, double pointLat, double pointLon,
        double radiusMeters, bool expectedResult)
    {
        // Act
        var result = GpsCalculator.IsWithinRadius(
            centerLat, centerLon, pointLat, pointLon, radiusMeters);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void RadiansToDegrees_WithPi_Returns180()
    {
        // Act
        var degrees = GpsCalculator.RadiansToDegrees(Math.PI);

        // Assert
        Assert.Equal(180.0, degrees, precision: 10);
    }

    [Fact]
    public void CalculateDistance_WithAntipodes_ReturnsHalfEarthCircumference()
    {
        // Arrange - Points on opposite sides of Earth
        var lat1 = 0.0;
        var lon1 = 0.0;
        var lat2 = 0.0;
        var lon2 = 180.0;
        var expectedDistance = 20015086.0; // Half of Earth's circumference at equator

        // Act
        var distance = GpsCalculator.CalculateDistance(lat1, lon1, lat2, lon2);

        // Assert - Allow 1% tolerance
        Assert.InRange(distance, expectedDistance * 0.99, expectedDistance * 1.01);
    }

    [Theory]
    [InlineData(37.7749, -122.4194, 37.7849, -122.4194, 1113)] // ~1.11km north in SF
    [InlineData(51.5074, -0.1278, 51.5174, -0.1278, 1112)]     // ~1.11km north in London
    [InlineData(-33.8688, 151.2093, -33.8588, 151.2093, 1112)] // ~1.11km south in Sydney
    public void CalculateDistance_WithVariousLatitudes_HandlesAllRegions(
        double lat1, double lon1, double lat2, double lon2, double expectedMeters)
    {
        // Act
        var distance = GpsCalculator.CalculateDistance(lat1, lon1, lat2, lon2);

        // Assert
        Assert.InRange(distance, expectedMeters * 0.95, expectedMeters * 1.05);
    }

    [Fact]
    public void CalculateDistance_With50MetersMovement_DetectsCorrectly()
    {
        // Arrange - Movement of approximately 50 meters at equator
        var lat1 = 0.0;
        var lon1 = 0.0;
        var lat2 = 0.00045; // ~50 meters
        var lon2 = 0.0;

        // Act
        var distance = GpsCalculator.CalculateDistance(lat1, lon1, lat2, lon2);

        // Assert - Should be close to 50 meters
        Assert.InRange(distance, 45, 55);
    }

    [Theory]
    [InlineData(0, 0, 0.00045, 0, 50)]       // ~50m north
    [InlineData(0, 0, 0.0009, 0, 100)]       // ~100m north
    [InlineData(0, 0, 0.0045, 0, 500)]       // ~500m north
    [InlineData(40.7128, -74.0060, 40.7133, -74.0060, 55)] // ~55m north in NYC
    public void CalculateDistance_WithSmallDistances_IsAccurate(
        double lat1, double lon1, double lat2, double lon2, double expectedMeters)
    {
        // Act
        var distance = GpsCalculator.CalculateDistance(lat1, lon1, lat2, lon2);

        // Assert - Allow 10% tolerance for small distances
        Assert.InRange(distance, expectedMeters * 0.9, expectedMeters * 1.1);
    }
}

/// <summary>
/// Tests for speed calculations.
/// </summary>
public class SpeedCalculationTests
{
    [Theory]
    [InlineData(100, 10, 10.0)]      // 100m in 10s = 10 m/s
    [InlineData(1000, 100, 10.0)]    // 1000m in 100s = 10 m/s
    [InlineData(50, 5, 10.0)]        // 50m in 5s = 10 m/s
    [InlineData(1609.34, 3600, 0.447)] // 1 mile in 1 hour ≈ 0.447 m/s (1 mph)
    public void CalculateSpeed_WithDistanceAndTime_ReturnsCorrectSpeed(
        double distanceMeters, double timeSeconds, double expectedMetersPerSecond)
    {
        // Arrange
        var timeElapsed = TimeSpan.FromSeconds(timeSeconds);

        // Act
        var speed = GpsCalculator.CalculateSpeed(distanceMeters, timeElapsed);

        // Assert
        Assert.NotNull(speed);
        Assert.InRange(speed.Value, expectedMetersPerSecond * 0.99, expectedMetersPerSecond * 1.01);
    }

    [Fact]
    public void CalculateSpeed_WithZeroTime_ReturnsNull()
    {
        // Act
        var speed = GpsCalculator.CalculateSpeed(100, TimeSpan.Zero);

        // Assert
        Assert.Null(speed);
    }

    [Fact]
    public void CalculateSpeed_WithNegativeTime_ReturnsNull()
    {
        // Act
        var speed = GpsCalculator.CalculateSpeed(100, TimeSpan.FromSeconds(-10));

        // Assert
        Assert.Null(speed);
    }

    [Fact]
    public void CalculateSpeed_WithGpsCoordinates_ReturnsCorrectSpeed()
    {
        // Arrange - 100m movement in 10 seconds
        var lat1 = 0.0;
        var lon1 = 0.0;
        var time1 = DateTimeOffset.UtcNow;

        var lat2 = 0.0009; // ~100m
        var lon2 = 0.0;
        var time2 = time1.AddSeconds(10);

        // Act
        var speed = GpsCalculator.CalculateSpeed(lat1, lon1, time1, lat2, lon2, time2);

        // Assert
        Assert.NotNull(speed);
        Assert.InRange(speed.Value, 9.0, 11.0); // ~10 m/s with tolerance
    }

    [Theory]
    [InlineData(2.23694, 5.0)]   // 2.23694 m/s = 5 mph
    [InlineData(4.4739, 10.0)]   // 4.4739 m/s = 10 mph
    [InlineData(13.4112, 30.0)]  // 13.4112 m/s = 30 mph
    [InlineData(26.8224, 60.0)]  // 26.8224 m/s = 60 mph
    public void MetersPerSecondToMph_ConvertsCorrectly(double metersPerSecond, double expectedMph)
    {
        // Act
        var mph = GpsCalculator.MetersPerSecondToMph(metersPerSecond);

        // Assert
        Assert.InRange(mph, expectedMph * 0.99, expectedMph * 1.01);
    }

    [Theory]
    [InlineData(3.6, 13.0)]      // 3.6 m/s = 13 km/h
    [InlineData(10.0, 36.0)]     // 10 m/s = 36 km/h
    [InlineData(27.778, 100.0)]  // 27.778 m/s = 100 km/h
    public void MetersPerSecondToKmh_ConvertsCorrectly(double metersPerSecond, double expectedKmh)
    {
        // Act
        var kmh = GpsCalculator.MetersPerSecondToKmh(metersPerSecond);

        // Assert
        Assert.InRange(kmh, expectedKmh * 0.99, expectedKmh * 1.01);
    }

    [Theory]
    [InlineData(5.0, 2.23694)]   // 5 mph = 2.23694 m/s
    [InlineData(10.0, 4.4739)]   // 10 mph = 4.4739 m/s
    [InlineData(60.0, 26.8224)]  // 60 mph = 26.8224 m/s
    public void MphToMetersPerSecond_ConvertsCorrectly(double mph, double expectedMetersPerSecond)
    {
        // Act
        var metersPerSecond = GpsCalculator.MphToMetersPerSecond(mph);

        // Assert
        Assert.InRange(metersPerSecond, expectedMetersPerSecond * 0.99, expectedMetersPerSecond * 1.01);
    }

    [Theory]
    [InlineData(13.0, 3.6)]      // 13 km/h = 3.6 m/s
    [InlineData(36.0, 10.0)]     // 36 km/h = 10 m/s
    [InlineData(100.0, 27.778)]  // 100 km/h = 27.778 m/s
    public void KmhToMetersPerSecond_ConvertsCorrectly(double kmh, double expectedMetersPerSecond)
    {
        // Act
        var metersPerSecond = GpsCalculator.KmhToMetersPerSecond(kmh);

        // Assert
        Assert.InRange(metersPerSecond, expectedMetersPerSecond * 0.99, expectedMetersPerSecond * 1.01);
    }

    [Theory]
    [InlineData(0.5, true)]      // 1.1 mph - walking
    [InlineData(1.0, true)]      // 2.2 mph - walking
    [InlineData(1.5, true)]      // 3.4 mph - walking
    [InlineData(2.0, true)]      // 4.5 mph - walking
    [InlineData(2.23, true)]     // 4.99 mph - walking (just under threshold)
    public void IsWalkingSpeed_WithSpeedsBelowThreshold_ReturnsTrue(double metersPerSecond, bool expected)
    {
        // Act
        var result = GpsCalculator.IsWalkingSpeed(metersPerSecond);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(2.24, false)]    // 5.01 mph - riding (just above threshold)
    [InlineData(4.5, false)]     // 10.1 mph - riding
    [InlineData(10.0, false)]    // 22.4 mph - riding
    [InlineData(20.0, false)]    // 44.7 mph - riding
    [InlineData(26.8, false)]    // 60 mph - riding
    public void IsWalkingSpeed_WithSpeedsAboveThreshold_ReturnsFalse(double metersPerSecond, bool expected)
    {
        // Act
        var result = GpsCalculator.IsWalkingSpeed(metersPerSecond);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(2.24, true)]     // 5.01 mph - riding
    [InlineData(4.5, true)]      // 10.1 mph - riding
    [InlineData(10.0, true)]     // 22.4 mph - riding
    [InlineData(20.0, true)]     // 44.7 mph - riding
    public void IsRidingSpeed_WithSpeedsAboveThreshold_ReturnsTrue(double metersPerSecond, bool expected)
    {
        // Act
        var result = GpsCalculator.IsRidingSpeed(metersPerSecond);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0.5, false)]     // 1.1 mph - walking
    [InlineData(1.0, false)]     // 2.2 mph - walking
    [InlineData(2.0, false)]     // 4.5 mph - walking
    [InlineData(2.23, false)]    // 4.99 mph - walking
    public void IsRidingSpeed_WithSpeedsBelowThreshold_ReturnsFalse(double metersPerSecond, bool expected)
    {
        // Act
        var result = GpsCalculator.IsRidingSpeed(metersPerSecond);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsWalkingSpeed_AtExactly5Mph_ReturnsFalse()
    {
        // Arrange - Exactly 5 mph = 2.23694 m/s
        var speed = GpsCalculator.MphToMetersPerSecond(5.0);

        // Act
        var result = GpsCalculator.IsWalkingSpeed(speed);

        // Assert - At exactly 5 mph, should be riding, not walking
        Assert.False(result);
    }

    [Fact]
    public void IsRidingSpeed_AtExactly5Mph_ReturnsTrue()
    {
        // Arrange - Exactly 5 mph = 2.23694 m/s
        var speed = GpsCalculator.MphToMetersPerSecond(5.0);

        // Act
        var result = GpsCalculator.IsRidingSpeed(speed);

        // Assert - At exactly 5 mph, should be riding
        Assert.True(result);
    }
}
