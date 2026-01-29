using Xunit;
using FWH.Common.Location;
using FWH.Common.Location.Services;
using FWH.Common.Location.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FWH.Mobile.Desktop.Tests.Services;

/// <summary>
/// Tests for GPS service factory and fallback behavior.
/// These tests don't require actual location hardware.
/// </summary>
public class GpsServiceFactoryTests
{
    /// <summary>
    /// Tests that NoGpsService.IsLocationAvailable returns false, indicating location services are not available.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The NoGpsService.IsLocationAvailable property's return value when location services are not available (fallback implementation).</para>
    /// <para><strong>Data involved:</strong> A NoGpsService instance, which is a fallback implementation used when no platform-specific GPS service is available (e.g., on platforms without location support or in test environments).</para>
    /// <para><strong>Why the data matters:</strong> NoGpsService provides a safe fallback when location services cannot be used. It must correctly report that location is not available so the application can handle this gracefully (e.g., show appropriate UI, skip location-dependent features). This prevents null reference exceptions and allows the app to function even without GPS capabilities.</para>
    /// <para><strong>Expected outcome:</strong> IsLocationAvailable should return false.</para>
    /// <para><strong>Reason for expectation:</strong> NoGpsService represents the absence of location capabilities, so it should always return false for IsLocationAvailable. This allows calling code to check availability before attempting to get location, preventing errors and enabling graceful degradation of location-dependent features.</para>
    /// </remarks>
    [Fact]
    public void NoGpsServiceIsLocationAvailableReturnsFalse()
    {
        // Arrange
        var service = new NoGpsService();

        // Act
        var result = service.IsLocationAvailable;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task NoGpsServiceGetCurrentLocationAsyncReturnsNull()
    {
        // Arrange
        var service = new NoGpsService();

        // Act
        var result = await service.GetCurrentLocationAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task NoGpsService_RequestLocationPermissionAsync_ReturnsFalse()
    {
        // Arrange
        var service = new NoGpsService();

        // Act
        var result = await service.RequestLocationPermissionAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task NoGpsServiceWithCancellationTokenCompletesImmediately()
    {
        // Arrange
        var service = new NoGpsService();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        // Act
        var startTime = DateTimeOffset.UtcNow;
        var result = await service.GetCurrentLocationAsync(cts.Token);
        var elapsed = DateTimeOffset.UtcNow - startTime;

        // Assert
        Assert.Null(result);
        Assert.True(elapsed.TotalMilliseconds < 50, "Should complete immediately");
    }
}

/// <summary>
/// Tests for GpsCoordinates model.
/// </summary>
public class GpsCoordinatesModelTests
{
    [Fact]
    public void DefaultConstructor_ShouldSetTimestamp()
    {
        // Act
        var coords = new GpsCoordinates();

        // Assert
        Assert.NotEqual(default, coords.Timestamp);
        Assert.True(coords.Timestamp <= DateTimeOffset.UtcNow);
        Assert.True(coords.Timestamp >= DateTimeOffset.UtcNow.AddSeconds(-1));
    }

    [Fact]
    public void ParameterizedConstructor_ShouldSetLatLon()
    {
        // Act
        var coords = new GpsCoordinates(40.7128, -74.0060);

        // Assert
        Assert.Equal(40.7128, coords.Latitude);
        Assert.Equal(-74.0060, coords.Longitude);
    }

    [Fact]
    public void ParameterizedConstructorWithAccuracy_ShouldSetAllProperties()
    {
        // Act
        var coords = new GpsCoordinates(40.7128, -74.0060, 25.5);

        // Assert
        Assert.Equal(40.7128, coords.Latitude);
        Assert.Equal(-74.0060, coords.Longitude);
        Assert.Equal(25.5, coords.AccuracyMeters);
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(90, 180, true)]
    [InlineData(-90, -180, true)]
    [InlineData(89.9999, 179.9999, true)]
    [InlineData(-89.9999, -179.9999, true)]
    public void IsValid_WithValidCoordinates_ReturnsTrue(double lat, double lon, bool expected)
    {
        // Arrange
        var coords = new GpsCoordinates(lat, lon);

        // Act
        var result = coords.IsValid();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(90.0001, 0)]
    [InlineData(-90.0001, 0)]
    [InlineData(0, 180.0001)]
    [InlineData(0, -180.0001)]
    [InlineData(91, 0)]
    [InlineData(-91, 0)]
    [InlineData(0, 181)]
    [InlineData(0, -181)]
    [InlineData(100, 200)]
    public void IsValid_WithInvalidCoordinates_ReturnsFalse(double lat, double lon)
    {
        // Arrange
        var coords = new GpsCoordinates(lat, lon);

        // Act
        var result = coords.IsValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ToString_ShouldFormatAsExpected()
    {
        // Arrange
        var coords = new GpsCoordinates(37.774929, -122.419418);

        // Act
        var result = coords.ToString();

        // Assert
        Assert.Contains("37.774929", result);
        Assert.Contains("-122.419418", result);
        Assert.Contains("(", result);
        Assert.Contains(")", result);
        Assert.Contains(",", result);
    }

    [Fact]
    public void AltitudeMeters_CanBeSetAndRetrieved()
    {
        // Arrange
        var coords = new GpsCoordinates(40.7128, -74.0060)
        {
            AltitudeMeters = 123.45
        };

        // Act & Assert
        Assert.Equal(123.45, coords.AltitudeMeters);
    }

    [Fact]
    public void AccuracyMeters_CanBeSetAndRetrieved()
    {
        // Arrange
        var coords = new GpsCoordinates(40.7128, -74.0060)
        {
            AccuracyMeters = 67.89
        };

        // Act & Assert
        Assert.Equal(67.89, coords.AccuracyMeters);
    }

    [Fact]
    public void Timestamp_CanBeSetAndRetrieved()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var coords = new GpsCoordinates(40.7128, -74.0060)
        {
            Timestamp = timestamp
        };

        // Act & Assert
        Assert.Equal(timestamp, coords.Timestamp);
    }
}

/// <summary>
/// Tests for edge cases and error handling.
/// </summary>
public class GpsServiceEdgeCaseTests
{
    [Fact]
    public void GpsCoordinatesExtremeLatitudesValidatesCorrectly()
    {
        // Arrange & Act
        var northPole = new GpsCoordinates(90, 0);
        var southPole = new GpsCoordinates(-90, 0);

        // Assert
        Assert.True(northPole.IsValid());
        Assert.True(southPole.IsValid());
    }

    [Fact]
    public void GpsCoordinates_ExtremeLongitudes_ValidatesCorrectly()
    {
        // Arrange & Act
        var dateLine = new GpsCoordinates(0, 180);
        var primeMeridian = new GpsCoordinates(0, 0);

        // Assert
        Assert.True(dateLine.IsValid());
        Assert.True(primeMeridian.IsValid());
    }

    [Fact]
    public void GpsCoordinatesNegativeAccuracyIsAllowed()
    {
        // Arrange & Act
        var coords = new GpsCoordinates(0, 0, -1);

        // Assert
        Assert.Equal(-1, coords.AccuracyMeters);
        // Note: Validation of accuracy is not part of IsValid()
    }

    [Fact]
    public void GpsCoordinates_VeryLargeAccuracy_IsAllowed()
    {
        // Arrange & Act
        var coords = new GpsCoordinates(0, 0, double.MaxValue);

        // Assert
        Assert.Equal(double.MaxValue, coords.AccuracyMeters);
    }

    [Fact]
    public void GpsCoordinates_FutureTimestamp_IsAllowed()
    {
        // Arrange & Act
        var futureTime = DateTimeOffset.UtcNow.AddDays(1);
        var coords = new GpsCoordinates(0, 0)
        {
            Timestamp = futureTime
        };

        // Assert
        Assert.Equal(futureTime, coords.Timestamp);
    }

    [Fact]
    public void GpsCoordinates_PastTimestamp_IsAllowed()
    {
        // Arrange & Act
        var pastTime = DateTimeOffset.UtcNow.AddYears(-10);
        var coords = new GpsCoordinates(0, 0)
        {
            Timestamp = pastTime
        };

        // Assert
        Assert.Equal(pastTime, coords.Timestamp);
    }
}

/// <summary>
/// Tests for common location scenarios.
/// </summary>
public class CommonLocationScenariosTests
{
    [Theory]
    [InlineData(37.7749, -122.4194, "San Francisco")]
    [InlineData(40.7128, -74.0060, "New York")]
    [InlineData(51.5074, -0.1278, "London")]
    [InlineData(35.6762, 139.6503, "Tokyo")]
    [InlineData(-33.8688, 151.2093, "Sydney")]
    [InlineData(48.8566, 2.3522, "Paris")]
    public void GpsCoordinates_MajorCities_AreValid(double lat, double lon, string city)
    {
        // Arrange & Act
        var coords = new GpsCoordinates(lat, lon);

        // Assert
        Assert.True(coords.IsValid(), $"{city} coordinates should be valid");
    }

    [Fact]
    public void GpsCoordinates_EquatorAndPrimeMeridian_IsValid()
    {
        // Arrange - Null Island (0, 0)
        var coords = new GpsCoordinates(0, 0);

        // Act
        var result = coords.IsValid();

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(10.0)]      // High accuracy
    [InlineData(50.0)]      // Medium accuracy
    [InlineData(100.0)]     // Low accuracy
    [InlineData(500.0)]     // Very low accuracy
    [InlineData(1000.0)]    // Poor accuracy
    public void GpsCoordinatesVariousAccuracyLevelsArePreserved(double accuracy)
    {
        // Arrange & Act
        var coords = new GpsCoordinates(0, 0, accuracy);

        // Assert
        Assert.Equal(accuracy, coords.AccuracyMeters);
    }
}
