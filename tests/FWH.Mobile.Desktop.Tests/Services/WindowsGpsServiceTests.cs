using Xunit;
using FWH.Mobile.Desktop.Services;
using FWH.Common.Location.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FWH.Mobile.Desktop.Tests.Services;

/// <summary>
/// Unit tests for WindowsGpsService.
/// Note: These tests require Windows 10/11 with location services enabled.
/// </summary>
public class WindowsGpsServiceTests
{
    [Fact]
    public void Constructor_ShouldNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => new WindowsGpsService());
        Assert.Null(exception);
    }

    [Fact]
    public void IsLocationAvailable_ShouldReturnBool()
    {
        // Arrange
        var service = new WindowsGpsService();

        // Act
        var isAvailable = service.IsLocationAvailable;

        // Assert
        Assert.IsType<bool>(isAvailable);
    }

    [Fact]
    public async Task RequestLocationPermissionAsync_ShouldReturnBool()
    {
        // Arrange
        var service = new WindowsGpsService();

        // Act
        var result = await service.RequestLocationPermissionAsync();

        // Assert
        Assert.IsType<bool>(result);
    }

    [Fact(Skip = "Requires Windows location services to be enabled and permission granted")]
    public async Task GetCurrentLocationAsync_WithPermission_ReturnsValidCoordinates()
    {
        // Arrange
        var service = new WindowsGpsService();
        
        // Ensure permission
        if (!service.IsLocationAvailable)
        {
            var granted = await service.RequestLocationPermissionAsync();
            if (!granted)
            {
                // Skip test if permission not granted
                return;
            }
        }

        // Act
        var coordinates = await service.GetCurrentLocationAsync();

        // Assert
        Assert.NotNull(coordinates);
        Assert.True(coordinates.IsValid());
        Assert.InRange(coordinates.Latitude, -90, 90);
        Assert.InRange(coordinates.Longitude, -180, 180);
        Assert.True(coordinates.AccuracyMeters > 0);
        Assert.True(coordinates.Timestamp <= DateTimeOffset.UtcNow);
        Assert.True(coordinates.Timestamp >= DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    [Fact(Skip = "Requires Windows location services to be enabled")]
    public async Task GetCurrentLocationAsync_WithTimeout_ShouldComplete()
    {
        // Arrange
        var service = new WindowsGpsService();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act
        var coordinates = await service.GetCurrentLocationAsync(cts.Token);

        // Assert - Should complete within timeout, result may be null if no location available
        Assert.True(true); // Test completed without hanging
    }

    [Fact]
    public async Task GetCurrentLocationAsync_WithCancellation_ShouldCancel()
    {
        // Arrange
        var service = new WindowsGpsService();
        using var cts = new CancellationTokenSource();
        
        // Act
        var task = service.GetCurrentLocationAsync(cts.Token);
        cts.Cancel(); // Cancel immediately
        
        var result = await task;

        // Assert - Should return null when cancelled
        Assert.Null(result);
    }

    [Fact(Skip = "Requires Windows location services and recent location")]
    public async Task GetLastKnownLocationAsync_ShouldReturnCachedLocation()
    {
        // Arrange
        var service = new WindowsGpsService();

        // Act
        var coordinates = await service.GetLastKnownLocationAsync();

        // Assert
        if (coordinates != null)
        {
            Assert.True(coordinates.IsValid());
            Assert.InRange(coordinates.Latitude, -90, 90);
            Assert.InRange(coordinates.Longitude, -180, 180);
        }
    }
}

/// <summary>
/// Integration tests for WindowsGpsService.
/// These tests require Windows location services to be enabled.
/// </summary>
public class WindowsGpsServiceIntegrationTests
{
    [Fact(Skip = "Integration test - requires Windows location services")]
    public async Task FullLocationFlow_ShouldWork()
    {
        // Arrange
        var service = new WindowsGpsService();

        // Act & Assert
        // Step 1: Check availability
        var isAvailable = service.IsLocationAvailable;
        if (!isAvailable)
        {
            // Step 2: Request permission
            var granted = await service.RequestLocationPermissionAsync();
            Assert.True(granted, "Location permission should be granted for this test");
        }

        // Step 3: Get current location
        var coordinates = await service.GetCurrentLocationAsync();
        Assert.NotNull(coordinates);
        Assert.True(coordinates.IsValid());

        // Step 4: Verify coordinate properties
        Assert.InRange(coordinates.Latitude, -90, 90);
        Assert.InRange(coordinates.Longitude, -180, 180);
        Assert.True(coordinates.AccuracyMeters > 0);
        Assert.NotNull(coordinates.Timestamp);
    }

    [Fact(Skip = "Integration test - requires Windows location services")]
    public async Task PerformanceTest_ShouldCompleteWithin30Seconds()
    {
        // Arrange
        var service = new WindowsGpsService();
        var startTime = DateTimeOffset.UtcNow;

        // Act
        var coordinates = await service.GetCurrentLocationAsync();
        var elapsed = DateTimeOffset.UtcNow - startTime;

        // Assert
        Assert.True(elapsed.TotalSeconds <= 30, 
            $"Location retrieval took {elapsed.TotalSeconds:F1}s, should complete within 30s");
    }

    [Fact(Skip = "Integration test - requires multiple location requests")]
    public async Task MultipleRequests_ShouldAllSucceed()
    {
        // Arrange
        var service = new WindowsGpsService();
        var results = new GpsCoordinates?[3];

        // Act
        for (int i = 0; i < 3; i++)
        {
            results[i] = await service.GetCurrentLocationAsync();
            await Task.Delay(1000); // Wait 1 second between requests
        }

        // Assert
        foreach (var result in results)
        {
            if (result != null) // May be null if location unavailable
            {
                Assert.True(result.IsValid());
            }
        }
    }
}

/// <summary>
/// Tests for GpsCoordinates validation.
/// </summary>
public class GpsCoordinatesValidationTests
{
    [Theory]
    [InlineData(37.7749, -122.4194, true)]  // San Francisco
    [InlineData(51.5074, -0.1278, true)]    // London
    [InlineData(35.6762, 139.6503, true)]   // Tokyo
    [InlineData(0, 0, true)]                // Null Island
    [InlineData(90, 180, true)]             // Valid extremes
    [InlineData(-90, -180, true)]           // Valid extremes
    [InlineData(91, 0, false)]              // Invalid latitude
    [InlineData(-91, 0, false)]             // Invalid latitude
    [InlineData(0, 181, false)]             // Invalid longitude
    [InlineData(0, -181, false)]            // Invalid longitude
    public void GpsCoordinates_IsValid_ShouldValidateCorrectly(
        double latitude, double longitude, bool expectedValid)
    {
        // Arrange
        var coordinates = new GpsCoordinates(latitude, longitude);

        // Act
        var isValid = coordinates.IsValid();

        // Assert
        Assert.Equal(expectedValid, isValid);
    }

    [Fact]
    public void GpsCoordinates_ToString_ShouldFormatCorrectly()
    {
        // Arrange
        var coordinates = new GpsCoordinates(37.774929, -122.419418);

        // Act
        var result = coordinates.ToString();

        // Assert
        Assert.Contains("37.774929", result);
        Assert.Contains("-122.419418", result);
    }

    [Fact]
    public void GpsCoordinates_Constructor_ShouldSetProperties()
    {
        // Arrange & Act
        var coordinates = new GpsCoordinates(37.7749, -122.4194, 50.5);

        // Assert
        Assert.Equal(37.7749, coordinates.Latitude);
        Assert.Equal(-122.4194, coordinates.Longitude);
        Assert.Equal(50.5, coordinates.AccuracyMeters);
        Assert.True(coordinates.Timestamp <= DateTimeOffset.UtcNow);
        Assert.True(coordinates.Timestamp >= DateTimeOffset.UtcNow.AddSeconds(-1));
    }

    [Fact]
    public void GpsCoordinates_OptionalProperties_CanBeNull()
    {
        // Arrange & Act
        var coordinates = new GpsCoordinates(37.7749, -122.4194);

        // Assert
        Assert.Null(coordinates.AccuracyMeters);
        Assert.Null(coordinates.AltitudeMeters);
    }

    [Fact]
    public void GpsCoordinates_OptionalProperties_CanBeSet()
    {
        // Arrange & Act
        var coordinates = new GpsCoordinates
        {
            Latitude = 37.7749,
            Longitude = -122.4194,
            AccuracyMeters = 50.0,
            AltitudeMeters = 100.0,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Assert
        Assert.Equal(37.7749, coordinates.Latitude);
        Assert.Equal(-122.4194, coordinates.Longitude);
        Assert.Equal(50.0, coordinates.AccuracyMeters);
        Assert.Equal(100.0, coordinates.AltitudeMeters);
        Assert.NotEqual(default, coordinates.Timestamp);
    }
}
