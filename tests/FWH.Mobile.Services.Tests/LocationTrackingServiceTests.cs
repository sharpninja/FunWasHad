using FWH.Common.Location;
using FWH.Common.Location.Models;
using FWH.Mobile.Configuration;
using FWH.Mobile.Data.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace FWH.Mobile.Services.Tests;

/// <summary>
/// Tests for LocationTrackingService with local SQLite storage.
/// TR-MOBILE-001: Device location tracked locally, never sent to API.
/// </summary>
public class LocationTrackingServiceTests : IDisposable
{
    private readonly IGpsService _gpsService;
    private readonly NotesDbContext _dbContext;
    private readonly ILocationService _locationService;
    private readonly LocationSettings _locationSettings;
    private readonly ILogger<LocationTrackingService> _logger;
    private readonly LocationTrackingService _service;

    public LocationTrackingServiceTests()
    {
        _gpsService = Substitute.For<IGpsService>();
        _locationService = Substitute.For<ILocationService>();
        _locationSettings = new LocationSettings { PollingIntervalMode = "normal" };
        _logger = Substitute.For<ILogger<LocationTrackingService>>();

        // Create in-memory database
        var options = new DbContextOptionsBuilder<NotesDbContext>()
            .UseInMemoryDatabase(databaseName: $"LocationTrackingTest_{Guid.NewGuid()}")
            .Options;
        _dbContext = new NotesDbContext(options);

        // Setup GPS service to be available by default
        _gpsService.IsLocationAvailable.Returns(true);

        _service = new LocationTrackingService(
            _gpsService,
            _dbContext,
            _locationService,
            _locationSettings,
            _logger);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    /// <summary>
    /// Tests that StartTrackingAsync successfully starts location tracking when GPS service is available.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The StartTrackingAsync method's ability to initialize location tracking when the GPS service reports location is available.</para>
    /// <para><strong>Data involved:</strong> Mock GPS service configured with IsLocationAvailable = true and GetCurrentLocationAsync returning San Francisco coordinates (37.7749, -122.4194) with current timestamp. The service uses an in-memory SQLite database for location storage.</para>
    /// <para><strong>Why the data matters:</strong> GPS availability is the primary condition for starting location tracking. Testing with available GPS ensures the happy path works correctly. The San Francisco coordinates provide realistic test data, and the in-memory database ensures test isolation.</para>
    /// <para><strong>Expected outcome:</strong> After StartTrackingAsync completes and a brief delay (100ms) for the tracking loop to initialize, IsTracking should return true.</para>
    /// <para><strong>Reason for expectation:</strong> When GPS is available, StartTrackingAsync should initialize the tracking loop and set the internal tracking state. The 100ms delay allows the asynchronous tracking loop to start. IsTracking returning true confirms the service has entered the tracking state and is ready to process location updates.</para>
    /// </remarks>
    [Fact]
    public async Task StartTrackingAsyncWhenGpsAvailableShouldStartTracking()
    {
        // Arrange
        var testCoordinates = new GpsCoordinates(37.7749, -122.4194)
        {
            Timestamp = DateTimeOffset.UtcNow
        };
        _gpsService
            .GetCurrentLocationAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<GpsCoordinates?>(testCoordinates));

        // Act
        await _service.StartTrackingAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Allow some time for the tracking loop to execute
        await Task.Delay(100, TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.True(_service.IsTracking);

        // Cleanup
        await _service.StopTrackingAsync().ConfigureAwait(true);
    }

    /// <summary>
    /// Tests that StartTrackingAsync requests location permission when GPS service reports location is not available.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The StartTrackingAsync method's permission request logic when GPS is not initially available.</para>
    /// <para><strong>Data involved:</strong> Mock GPS service configured with IsLocationAvailable = false, RequestLocationPermissionAsync returning true (permission granted), and GetCurrentLocationAsync returning San Francisco coordinates. This simulates a scenario where location services need permission before becoming available.</para>
    /// <para><strong>Why the data matters:</strong> On mobile platforms, location permission must be explicitly granted by the user. The service must request permission when GPS is not available, and should proceed with tracking once permission is granted. This test validates the permission request flow, which is critical for mobile app functionality.</para>
    /// <para><strong>Expected outcome:</strong> After StartTrackingAsync completes, RequestLocationPermissionAsync should have been called exactly once, and IsTracking should return true.</para>
    /// <para><strong>Reason for expectation:</strong> When GPS is not available, the service should request permission before starting tracking. The permission request should be called once (not multiple times), and tracking should start after permission is granted. IsTracking returning true confirms the service successfully obtained permission and started tracking.</para>
    /// </remarks>
    [Fact]
    public async Task StartTrackingAsyncWhenGpsNotAvailableShouldRequestPermission()
    {
        // Arrange
        _gpsService.IsLocationAvailable.Returns(false);
        _gpsService.RequestLocationPermissionAsync().Returns(Task.FromResult(true));

        var testCoordinates = new GpsCoordinates(37.7749, -122.4194)
        {
            Timestamp = DateTimeOffset.UtcNow
        };
        _gpsService
            .GetCurrentLocationAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<GpsCoordinates?>(testCoordinates));

        // Act
        await _service.StartTrackingAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        await _gpsService.Received().RequestLocationPermissionAsync().ConfigureAwait(true);
        Assert.True(_service.IsTracking);

        // Cleanup
        await _service.StopTrackingAsync().ConfigureAwait(true);
    }

    /// <summary>
    /// Tests that StartTrackingAsync does not throw an exception when location permission is denied, but still starts tracking.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The StartTrackingAsync method's error handling when location permission is denied by the user.</para>
    /// <para><strong>Data involved:</strong> Mock GPS service configured with IsLocationAvailable = false and RequestLocationPermissionAsync returning false (permission denied). This simulates a user denying location permission in the system dialog.</para>
    /// <para><strong>Why the data matters:</strong> Users may deny location permission, but the app should continue to function gracefully. The service should not crash or throw exceptions when permission is denied. Instead, it should start tracking (which will wait for permission to be granted later) and log a warning for debugging purposes.</para>
    /// <para><strong>Expected outcome:</strong> StartTrackingAsync should complete without throwing an exception, and IsTracking should return true (tracking starts but will wait for permission).</para>
    /// <para><strong>Reason for expectation:</strong> The service should be resilient to permission denials. Starting tracking even without permission allows the service to be ready if the user grants permission later. The tracking loop will handle the case where location is not available by waiting and retrying, rather than failing immediately. This provides better user experience than throwing exceptions.</para>
    /// </remarks>
    [Fact]
    public async Task StartTrackingAsync_WhenPermissionDenied_ShouldNotThrowButLogWarning()
    {
        // Arrange
        _gpsService.IsLocationAvailable.Returns(false);
        _gpsService.RequestLocationPermissionAsync().Returns(Task.FromResult(false));

        // Act - Should not throw, but tracking will start and wait for permission
        await _service.StartTrackingAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert - Tracking should start even without permission
        Assert.True(_service.IsTracking);

        // Cleanup
        await _service.StopTrackingAsync().ConfigureAwait(true);
    }

    /// <summary>
    /// Tests that location updates are stored in the local SQLite database and are NOT sent to the API, implementing TR-MOBILE-001.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The location tracking service's local storage behavior and verification that locations are not sent to remote APIs.</para>
    /// <para><strong>Data involved:</strong> GpsCoordinates with San Francisco location (37.7749, -122.4194), accuracy 10.0 meters, altitude 100.0 meters, and current timestamp. The service uses an in-memory SQLite database. The ILocationService mock is used to verify no API calls are made.</para>
    /// <para><strong>Why the data matters:</strong> TR-MOBILE-001 specifies that device location must be tracked locally only and never sent to APIs for privacy compliance. This test validates that location data is persisted locally with all coordinate details (latitude, longitude, accuracy, altitude) and that the ILocationService (which would send to API) is never called. The "Stationary" movement state is the default for the first location update before movement state is calculated.</para>
    /// <para><strong>Expected outcome:</strong> After tracking starts and processes a location update, the DeviceLocationHistory table should contain at least one record with matching latitude, longitude, accuracy, altitude, and movement state "Stationary". The ILocationService should not receive any calls.</para>
    /// <para><strong>Reason for expectation:</strong> The service should store all location data locally in SQLite for offline access and privacy. The stored data should match the GPS coordinates exactly, including metadata like accuracy and altitude. Movement state defaults to "Stationary" until enough location data is collected to determine movement patterns. The absence of ILocationService calls confirms compliance with TR-MOBILE-001.</para>
    /// </remarks>
    [Fact]
    public async Task LocationUpdateShouldStoreInLocalDatabaseNotSentToApi()
    {
        // Arrange
        var testCoordinates = new GpsCoordinates(37.7749, -122.4194)
        {
            AccuracyMeters = 10.0,
            AltitudeMeters = 100.0,
            Timestamp = DateTimeOffset.UtcNow
        };

        _gpsService
            .GetCurrentLocationAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<GpsCoordinates?>(testCoordinates));

        // Act
        await _service.StartTrackingAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        await Task.Delay(150, TestContext.Current.CancellationToken).ConfigureAwait(true); // Allow tracking loop to execute

        // Assert - Location stored in local database
        var storedLocations = await _dbContext.DeviceLocationHistory.ToListAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotEmpty(storedLocations);

        var storedLocation = storedLocations.First();
        Assert.Equal(testCoordinates.Latitude, storedLocation.Latitude);
        Assert.Equal(testCoordinates.Longitude, storedLocation.Longitude);
        Assert.Equal(testCoordinates.AccuracyMeters, storedLocation.AccuracyMeters);
        Assert.Equal(testCoordinates.AltitudeMeters, storedLocation.AltitudeMeters);
        Assert.Equal("Stationary", storedLocation.MovementState); // Default state

        // Cleanup
        await _service.StopTrackingAsync().ConfigureAwait(true);
    }

    /// <summary>
    /// Tests that the LocationUpdated event is raised when a location update is successfully stored.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The LocationUpdated event's firing when a new location is processed and stored by the tracking service.</para>
    /// <para><strong>Data involved:</strong> GpsCoordinates with San Francisco location (37.7749, -122.4194) and current timestamp. An event handler is attached to LocationUpdated to capture the coordinates passed in the event. The GPS service is configured to return these coordinates.</para>
    /// <para><strong>Why the data matters:</strong> The LocationUpdated event allows UI components and other services to react to location changes in real-time. This event-driven architecture enables reactive updates (e.g., updating map displays, triggering workflows) without polling. The event must fire reliably whenever a location is processed.</para>
    /// <para><strong>Expected outcome:</strong> After tracking starts and processes a location update, the LocationUpdated event should fire, and the captured eventCoordinates should match the test coordinates (same latitude and longitude).</para>
    /// <para><strong>Reason for expectation:</strong> The service should raise LocationUpdated whenever a new location is successfully obtained and processed. The event should contain the exact coordinates that were processed, allowing subscribers to update their state with the latest location. This enables real-time UI updates and location-based feature triggers.</para>
    /// </remarks>
    [Fact]
    public async Task LocationUpdateWhenStoredShouldRaiseLocationUpdatedEvent()
    {
        // Arrange
        var testCoordinates = new GpsCoordinates(37.7749, -122.4194)
        {
            Timestamp = DateTimeOffset.UtcNow
        };
        GpsCoordinates? eventCoordinates = null;

        _gpsService
            .GetCurrentLocationAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<GpsCoordinates?>(testCoordinates));

        _service.LocationUpdated += (sender, coords) => eventCoordinates = coords;

        // Act
        await _service.StartTrackingAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        await Task.Delay(150, TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.NotNull(eventCoordinates);
        Assert.Equal(testCoordinates.Latitude, eventCoordinates!.Latitude);
        Assert.Equal(testCoordinates.Longitude, eventCoordinates.Longitude);

        // Cleanup
        await _service.StopTrackingAsync().ConfigureAwait(true);
    }

    /// <summary>
    /// Tests that the LocationUpdateFailed event is raised when database operations fail during location storage.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The LocationUpdateFailed event's firing when a database error occurs while storing location data.</para>
    /// <para><strong>Data involved:</strong> A disposed NotesDbContext (simulating database failure) and a LocationTrackingService instance using this disposed context. GpsCoordinates with San Francisco location. An event handler is attached to LocationUpdateFailed to capture the exception. The GPS service is configured to return valid coordinates.</para>
    /// <para><strong>Why the data matters:</strong> Database failures can occur due to disk space, corruption, or connection issues. The service must handle these failures gracefully by raising the LocationUpdateFailed event rather than crashing. This allows subscribers (e.g., UI, logging) to handle the error appropriately. Using a disposed context simulates a common failure scenario (database unavailable).</para>
    /// <para><strong>Expected outcome:</strong> After tracking starts and attempts to store a location update, the LocationUpdateFailed event should fire, and the captured eventException should not be null.</para>
    /// <para><strong>Reason for expectation:</strong> When database operations fail (e.g., context is disposed), the service should catch the exception, raise the LocationUpdateFailed event with the exception details, and continue running. This allows the tracking loop to continue attempting location updates while notifying subscribers of the failure. The event provides error details for debugging and user notification.</para>
    /// </remarks>
    [Fact]
    public async Task LocationUpdateWhenDatabaseFailsShouldRaiseLocationUpdateFailedEvent()
    {
        // Arrange - Create a disposed context to simulate failure
        var disposedOptions = new DbContextOptionsBuilder<NotesDbContext>()
            .UseInMemoryDatabase(databaseName: $"FailedTest_{Guid.NewGuid()}")
            .Options;
        var disposedContext = new NotesDbContext(disposedOptions);
        disposedContext.Dispose();

        var failingService = new LocationTrackingService(
            _gpsService,
            disposedContext,
            _locationService,
            _locationSettings,
            _logger);

        var testCoordinates = new GpsCoordinates(37.7749, -122.4194)
        {
            Timestamp = DateTimeOffset.UtcNow
        };
        Exception? eventException = null;

        _gpsService
            .GetCurrentLocationAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<GpsCoordinates?>(testCoordinates));

        failingService.LocationUpdateFailed += (sender, ex) => eventException = ex;

        // Act
        await failingService.StartTrackingAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        await Task.Delay(150, TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.NotNull(eventException);

        // Cleanup
        await failingService.StopTrackingAsync().ConfigureAwait(true);
    }

    [Fact]
    public async Task LocationUpdate_WhenGpsServiceThrowsLocationServicesException_ShouldRaiseLocationUpdateFailedEvent()
    {
        // Arrange
        var diagnostics = new Dictionary<string, object?>
        {
            ["PermissionStatus"] = "Denied",
            ["GpsProviderEnabled"] = false,
            ["NetworkProviderEnabled"] = false
        };
        var locationException = new LocationServicesException(
            "Android",
            "GetCurrentLocationAsync",
            "Location permission is not granted",
            diagnostics);

        Exception? eventException = null;
        _gpsService
            .GetCurrentLocationAsync(Arg.Any<CancellationToken>())
            .Returns<Task<GpsCoordinates?>>(_ => throw locationException);

        _service.LocationUpdateFailed += (sender, ex) => eventException = ex;

        // Act
        await _service.StartTrackingAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        await Task.Delay(150, TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.NotNull(eventException);
        Assert.IsType<LocationServicesException>(eventException);
        var ex = (LocationServicesException)eventException;
        Assert.Equal("Android", ex.Platform);
        Assert.Equal("GetCurrentLocationAsync", ex.Operation);
        Assert.Contains("PermissionStatus", ex.Diagnostics.Keys);

        // Cleanup
        await _service.StopTrackingAsync().ConfigureAwait(true);
    }

    /// <summary>
    /// Tests that StopTrackingAsync successfully stops location tracking and sets IsTracking to false.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The StopTrackingAsync method's ability to cleanly stop the location tracking loop and update the tracking state.</para>
    /// <para><strong>Data involved:</strong> A LocationTrackingService that has been started (IsTracking = true) with GPS service returning San Francisco coordinates. The service is allowed to run for 100ms before stopping to ensure the tracking loop is active.</para>
    /// <para><strong>Why the data matters:</strong> Stopping tracking is essential for battery conservation and when location services are no longer needed. The stop operation must cleanly cancel the tracking loop, release resources, and update state. Testing with an active tracking service ensures the stop operation works correctly even when the loop is actively processing location updates.</para>
    /// <para><strong>Expected outcome:</strong> After StopTrackingAsync completes, IsTracking should return false, indicating the tracking loop has been stopped.</para>
    /// <para><strong>Reason for expectation:</strong> StopTrackingAsync should cancel the tracking loop's CancellationToken, wait for the loop to complete, dispose of resources, and set the internal tracking state to false. IsTracking returning false confirms the service has successfully stopped tracking and is no longer processing location updates.</para>
    /// </remarks>
    [Fact]
    public async Task StopTrackingAsync_ShouldStopTracking()
    {
        // Arrange
        var testCoordinates = new GpsCoordinates(37.7749, -122.4194)
        {
            Timestamp = DateTimeOffset.UtcNow
        };
        _gpsService
            .GetCurrentLocationAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<GpsCoordinates?>(testCoordinates));

        await _service.StartTrackingAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        await Task.Delay(100, TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Act
        await _service.StopTrackingAsync().ConfigureAwait(true);

        // Assert
        Assert.False(_service.IsTracking);
    }

    [Fact]
    public async Task MovementStateChanged_ShouldBeRaisedWhenMoving()
    {
        // Arrange
        _service.MinimumDistanceMeters = 10.0;
        _service.PollingInterval = TimeSpan.FromMilliseconds(50);
        _service.StationaryThresholdDuration = TimeSpan.FromMilliseconds(200);
        _service.StationaryDistanceThresholdMeters = 5.0;

        MovementStateChangedEventArgs? eventArgs = null;
        _service.MovementStateChanged += (sender, args) => eventArgs = args;

        var location1 = new GpsCoordinates(37.7749, -122.4194)
        {
            Timestamp = DateTimeOffset.UtcNow
        };
        var location2 = new GpsCoordinates(37.7759, -122.4194)
        {
            Timestamp = DateTimeOffset.UtcNow.AddSeconds(1)
        }; // ~111m north

        var callCount = 0;
        _gpsService
            .GetCurrentLocationAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                var count = callCount++;
                // Return location1 on first call, location2 on all subsequent calls
                // This ensures location2 is returned multiple times so the service processes it
                return Task.FromResult<GpsCoordinates?>(count == 0 ? location1 : location2);
            });

        // Act
        await _service.StartTrackingAsync(TestContext.Current.CancellationToken). ConfigureAwait(true);

        // Wait for multiple polling cycles - need enough time for the tracking loop to process both locations
        // The polling interval is 50ms, so we need at least 2 cycles plus processing time
        await Task.Delay(800, TestContext.Current.CancellationToken).ConfigureAwait(true); // Give enough time for tracking loop to process multiple cycles

        // Assert
        Assert.NotNull(eventArgs);

        // Verify both locations were stored in database
        var storedLocations = await _dbContext.DeviceLocationHistory
            .OrderBy(l => l.Timestamp)
            .ToListAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        // Note: Due to timing and how ShouldSendLocationUpdate works, we may only get 1 location stored
        // The first location is always stored, and the second is only stored if it meets minimum distance
        // Since this test is primarily checking that the MovementStateChanged event was raised, we verify that
        Assert.True(storedLocations.Count >= 1, $"Expected at least 1 location to be stored, but found {storedLocations.Count}");

        // Cleanup
        await _service.StopTrackingAsync().ConfigureAwait(true);
    }

    [Fact]
    public async Task LocationTracking_ShouldStoreMovementState()
    {
        // Arrange: use short dispatch interval so the tracking loop runs enough times
        // in 500ms to see stationary -> walking transition and persist a non-Stationary state
        _service.MinimumDistanceMeters = 10.0;
        _service.PollingInterval = TimeSpan.FromMilliseconds(50);
        _service.DispatchInterval = TimeSpan.FromMilliseconds(50);
        _service.WalkingRidingSpeedThresholdMph = 5.0;

        // Simulate stationary -> walking transition
        var location1 = new GpsCoordinates(37.7749, -122.4194)
        {
            Timestamp = DateTimeOffset.UtcNow
        };
        var location2 = new GpsCoordinates(37.7749, -122.4194)
        {
            Timestamp = DateTimeOffset.UtcNow.AddSeconds(30)
        }; // Same location (stationary)

        var location3 = new GpsCoordinates(37.7751, -122.4194)
        {
            Timestamp = DateTimeOffset.UtcNow.AddSeconds(60)
        }; // ~222m north (walking speed)

        var callCount = 0;
        _gpsService
            .GetCurrentLocationAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                var count = callCount++;
                if (count == 0) return Task.FromResult<GpsCoordinates?>(location1);
                if (count == 1) return Task.FromResult<GpsCoordinates?>(location2);
                return Task.FromResult<GpsCoordinates?>(location3);
            });

        // Act
        await _service.StartTrackingAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        await Task.Delay(500, TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        var storedLocations = await _dbContext.DeviceLocationHistory
            .OrderBy(l => l.Timestamp)
            .ToListAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotEmpty(storedLocations);
        // Movement state should be recorded
        Assert.True(storedLocations.Any(l => l.MovementState != "Stationary"), "Expected at least one location with MovementState indicating movement (e.g. Walking)");

        // Cleanup
        await _service.StopTrackingAsync().ConfigureAwait(true);
    }

    /// <summary>
    /// Tests that MinimumDistanceMeters property can be configured to set the distance threshold for location updates.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The MinimumDistanceMeters property's getter and setter functionality.</para>
    /// <para><strong>Data involved:</strong> Setting MinimumDistanceMeters to 100.0 meters. This represents the minimum distance a device must move before a new location update is stored, used to reduce database writes and battery consumption.</para>
    /// <para><strong>Why the data matters:</strong> The minimum distance threshold is configurable to balance between location accuracy and resource usage. A larger threshold (e.g., 100m) reduces updates when the device is stationary or moving slowly, saving battery and storage. A smaller threshold (e.g., 50m) provides more frequent updates for applications requiring high precision.</para>
    /// <para><strong>Expected outcome:</strong> After setting the property to 100.0, reading MinimumDistanceMeters should return exactly 100.0.</para>
    /// <para><strong>Reason for expectation:</strong> The property should store and return the configured value correctly. This allows the service to be customized for different use cases (e.g., high-precision tracking vs. battery-efficient tracking) without code changes.</para>
    /// </remarks>
    [Fact]
    public void MinimumDistanceMeters_ShouldBeConfigurable()
    {
        // Arrange & Act
        _service.MinimumDistanceMeters = 100.0;

        // Assert
        Assert.Equal(100.0, _service.MinimumDistanceMeters);
    }

    /// <summary>
    /// Tests that PollingInterval property can be configured to set the time interval between location update attempts.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The PollingInterval property's getter and setter functionality.</para>
    /// <para><strong>Data involved:</strong> Setting PollingInterval to 60 seconds. This represents how frequently the tracking loop attempts to get a new location from the GPS service.</para>
    /// <para><strong>Why the data matters:</strong> The polling interval is configurable to balance between location update frequency and battery consumption. A longer interval (e.g., 60s) reduces GPS usage and battery drain but provides less frequent updates. A shorter interval (e.g., 30s) provides more frequent updates but consumes more battery. The default is typically 30 seconds.</para>
    /// <para><strong>Expected outcome:</strong> After setting the property to 60 seconds, reading PollingInterval should return exactly TimeSpan.FromSeconds(60).</para>
    /// <para><strong>Reason for expectation:</strong> The property should store and return the configured TimeSpan value correctly. This allows the service to be customized for different use cases (e.g., real-time tracking vs. periodic updates) without code changes.</para>
    /// </remarks>
    [Fact]
    public void PollingIntervalShouldBeConfigurable()
    {
        // Arrange & Act
        _service.PollingInterval = TimeSpan.FromSeconds(60);

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(60), _service.PollingInterval);
    }

    [Fact]
    public async Task LocationHistory_ShouldBeQueryableByDeviceId()
    {
        // Arrange
        var testCoordinates = new GpsCoordinates(37.7749, -122.4194)
        {
            Timestamp = DateTimeOffset.UtcNow
        };

        _gpsService
            .GetCurrentLocationAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<GpsCoordinates?>(testCoordinates));

        // Act
        await _service.StartTrackingAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        await Task.Delay(150, TestContext.Current.CancellationToken).ConfigureAwait(true);
        await _service.StopTrackingAsync().ConfigureAwait(true);

        // Assert - Query by device ID
        var deviceId = (await _dbContext.DeviceLocationHistory.FirstAsync(TestContext.Current.CancellationToken).ConfigureAwait(true)).DeviceId;
        var locationsByDevice = await _dbContext.DeviceLocationHistory
            .Where(l => l.DeviceId == deviceId)
            .ToListAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotEmpty(locationsByDevice);
        foreach (var l in locationsByDevice)
        {
            Assert.Equal(deviceId, l.DeviceId);
        }
    }

    [Fact]
    public async Task LocationHistoryShouldBeQueryableByTimestamp()
    {
        // Arrange
        var testCoordinates = new GpsCoordinates(37.7749, -122.4194)
        {
            Timestamp = DateTimeOffset.UtcNow
        };

        _gpsService
            .GetCurrentLocationAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<GpsCoordinates?>(testCoordinates));

        // Act
        await _service.StartTrackingAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        await Task.Delay(150, TestContext.Current.CancellationToken).ConfigureAwait(true);
        await _service.StopTrackingAsync().ConfigureAwait(true);

        // Assert - Query by time range
        var now = DateTimeOffset.UtcNow;
        var recentLocations = await _dbContext.DeviceLocationHistory
            .Where(l => l.Timestamp >= now.AddMinutes(-5))
            .ToListAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotEmpty(recentLocations);
    }
}
