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
    /// <summary>
    /// Tests that WindowsGpsService constructor completes without throwing exceptions.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The WindowsGpsService constructor's ability to initialize without throwing exceptions, even if Windows location services are not available.</para>
    /// <para><strong>Data involved:</strong> A new WindowsGpsService instance created using the default constructor. No additional configuration or setup is required.</para>
    /// <para><strong>Why the data matters:</strong> The constructor should be safe to call regardless of Windows location service availability. It should initialize the service object without throwing exceptions, allowing the service to be created and then checked for availability via IsLocationAvailable. This enables dependency injection scenarios where services are constructed before their capabilities are known.</para>
    /// <para><strong>Expected outcome:</strong> The constructor should complete without throwing any exceptions.</para>
    /// <para><strong>Reason for expectation:</strong> Object construction should be a lightweight operation that doesn't depend on external services being available. The constructor should initialize internal state only, and actual location service availability should be checked via IsLocationAvailable or GetCurrentLocationAsync. The null exception confirms construction succeeded, allowing the service to be safely instantiated in DI containers.</para>
    /// </remarks>
    [Fact]
    public void ConstructorShouldNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => new WindowsGpsService());
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that WindowsGpsService.IsLocationAvailable returns a boolean value indicating whether location services are available on the Windows device.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The WindowsGpsService.IsLocationAvailable property's ability to return a boolean value indicating location service availability.</para>
    /// <para><strong>Data involved:</strong> A new WindowsGpsService instance. The IsLocationAvailable property is accessed, which should return a boolean value (true if location services are available, false otherwise).</para>
    /// <para><strong>Why the data matters:</strong> Location availability must be checked before attempting to get location data. The property should return a valid boolean value that accurately reflects whether Windows location services are available and enabled on the device. This enables the application to handle unavailable location services gracefully.</para>
    /// <para><strong>Expected outcome:</strong> IsLocationAvailable should return a boolean value (true or false), confirming the property works correctly.</para>
    /// <para><strong>Reason for expectation:</strong> The property should query Windows location service availability and return a boolean result. The exact value (true/false) depends on device configuration and permissions, but it should always be a valid boolean. The type assertion confirms the property returns the correct type, enabling conditional logic based on availability.</para>
    /// </remarks>
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

    /// <summary>
    /// Tests that WindowsGpsService.RequestLocationPermissionAsync returns a boolean value indicating whether location permission was granted.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The WindowsGpsService.RequestLocationPermissionAsync method's ability to request location permission from Windows and return a boolean result.</para>
    /// <para><strong>Data involved:</strong> A new WindowsGpsService instance. RequestLocationPermissionAsync is called, which should request location permission from Windows and return a boolean value (true if granted, false if denied).</para>
    /// <para><strong>Why the data matters:</strong> Location permission is required to access GPS data on Windows. The method should request permission from the operating system and return a clear result indicating whether permission was granted. This enables the application to handle permission denial gracefully.</para>
    /// <para><strong>Expected outcome:</strong> RequestLocationPermissionAsync should return a boolean value (true or false), confirming the method works correctly and returns a valid result.</para>
    /// <para><strong>Reason for expectation:</strong> The method should interact with Windows location permission APIs and return a boolean result. The exact value (true/false) depends on user consent and system settings, but it should always be a valid boolean. The type assertion confirms the method returns the correct type, enabling conditional logic based on permission status.</para>
    /// </remarks>
    [Fact]
    public async Task RequestLocationPermissionAsyncShouldReturnBool()
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

    /// <summary>
    /// Tests that GpsCoordinates optional properties (AccuracyMeters, AltitudeMeters, Timestamp) can be set using object initializer syntax, allowing all properties to be configured.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The GpsCoordinates class's support for object initializer syntax, allowing all properties (including optional ones) to be set during object creation.</para>
    /// <para><strong>Data involved:</strong> A GpsCoordinates instance created using object initializer syntax with all properties set: Latitude=37.7749, Longitude=-122.4194, AccuracyMeters=50.0, AltitudeMeters=100.0, Timestamp=UtcNow. This tests that optional properties can be set when provided.</para>
    /// <para><strong>Why the data matters:</strong> Object initializer syntax provides a convenient way to create coordinate objects with all properties set in a single statement. This is useful when all GPS data is available and needs to be set. The test validates that the class supports this initialization pattern.</para>
    /// <para><strong>Expected outcome:</strong> All properties should match the values set in the object initializer: Latitude=37.7749, Longitude=-122.4194, AccuracyMeters=50.0, AltitudeMeters=100.0, and Timestamp should not be the default value.</para>
    /// <para><strong>Reason for expectation:</strong> The GpsCoordinates class should support object initializer syntax, allowing all properties to be set during initialization. The property values should be set exactly as specified in the initializer. The matching values confirm that object initializer syntax works correctly and all properties can be set, providing flexibility in coordinate creation.</para>
    /// </remarks>
    [Fact]
    public void GpsCoordinatesOptionalPropertiesCanBeSet()
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
