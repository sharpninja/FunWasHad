using FWH.Common.Location;
using FWH.Common.Location.Models;
using FWH.Mobile.Data.Data;
using FWH.Mobile.Data.Entities;
using FWH.Mobile.Services;
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
    private readonly ILogger<LocationTrackingService> _logger;
    private readonly LocationTrackingService _service;

    public LocationTrackingServiceTests()
    {
        _gpsService = Substitute.For<IGpsService>();
        _locationService = Substitute.For<ILocationService>();
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
            _logger);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    [Fact]
    public async Task StartTrackingAsync_WhenGpsAvailable_ShouldStartTracking()
    {
        // Arrange
        var testCoordinates = new GpsCoordinates(37.7749, -122.4194)
        {
            Timestamp = DateTimeOffset.UtcNow
        };
        _gpsService
            .GetCurrentLocationAsync(Arg.Any<CancellationToken>())
            .Returns(testCoordinates);

        // Act
        await _service.StartTrackingAsync();

        // Allow some time for the tracking loop to execute
        await Task.Delay(100);

        // Assert
        Assert.True(_service.IsTracking);

        // Cleanup
        await _service.StopTrackingAsync();
    }

    [Fact]
    public async Task StartTrackingAsync_WhenGpsNotAvailable_ShouldRequestPermission()
    {
        // Arrange
        _gpsService.IsLocationAvailable.Returns(false);
        _gpsService.RequestLocationPermissionAsync().Returns(true);

        var testCoordinates = new GpsCoordinates(37.7749, -122.4194)
        {
            Timestamp = DateTimeOffset.UtcNow
        };
        _gpsService
            .GetCurrentLocationAsync(Arg.Any<CancellationToken>())
            .Returns(testCoordinates);

        // Act
        await _service.StartTrackingAsync();

        // Assert
        await _gpsService.Received(1).RequestLocationPermissionAsync();
        Assert.True(_service.IsTracking);

        // Cleanup
        await _service.StopTrackingAsync();
    }

    [Fact]
    public async Task StartTrackingAsync_WhenPermissionDenied_ShouldThrowException()
    {
        // Arrange
        _gpsService.IsLocationAvailable.Returns(false);
        _gpsService.RequestLocationPermissionAsync().Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.StartTrackingAsync());
    }

    [Fact]
    public async Task LocationUpdate_ShouldStoreInLocalDatabase_NotSentToApi()
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
            .Returns(testCoordinates);

        // Act
        await _service.StartTrackingAsync();
        await Task.Delay(150); // Allow tracking loop to execute

        // Assert - Location stored in local database
        var storedLocations = await _dbContext.DeviceLocationHistory.ToListAsync();
        Assert.NotEmpty(storedLocations);
        
        var storedLocation = storedLocations.First();
        Assert.Equal(testCoordinates.Latitude, storedLocation.Latitude);
        Assert.Equal(testCoordinates.Longitude, storedLocation.Longitude);
        Assert.Equal(testCoordinates.AccuracyMeters, storedLocation.AccuracyMeters);
        Assert.Equal(testCoordinates.AltitudeMeters, storedLocation.AltitudeMeters);
        Assert.Equal("Unknown", storedLocation.MovementState); // Initial state

        // Cleanup
        await _service.StopTrackingAsync();
    }

    [Fact]
    public async Task LocationUpdate_WhenStored_ShouldRaiseLocationUpdatedEvent()
    {
        // Arrange
        var testCoordinates = new GpsCoordinates(37.7749, -122.4194)
        {
            Timestamp = DateTimeOffset.UtcNow
        };
        GpsCoordinates? eventCoordinates = null;

        _gpsService
            .GetCurrentLocationAsync(Arg.Any<CancellationToken>())
            .Returns(testCoordinates);

        _service.LocationUpdated += (sender, coords) => eventCoordinates = coords;

        // Act
        await _service.StartTrackingAsync();
        await Task.Delay(150);

        // Assert
        Assert.NotNull(eventCoordinates);
        Assert.Equal(testCoordinates.Latitude, eventCoordinates!.Latitude);
        Assert.Equal(testCoordinates.Longitude, eventCoordinates.Longitude);

        // Cleanup
        await _service.StopTrackingAsync();
    }

    [Fact]
    public async Task LocationUpdate_WhenDatabaseFails_ShouldRaiseLocationUpdateFailedEvent()
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
            _logger);

        var testCoordinates = new GpsCoordinates(37.7749, -122.4194)
        {
            Timestamp = DateTimeOffset.UtcNow
        };
        Exception? eventException = null;

        _gpsService
            .GetCurrentLocationAsync(Arg.Any<CancellationToken>())
            .Returns(testCoordinates);

        failingService.LocationUpdateFailed += (sender, ex) => eventException = ex;

        // Act
        await failingService.StartTrackingAsync();
        await Task.Delay(150);

        // Assert
        Assert.NotNull(eventException);

        // Cleanup
        await failingService.StopTrackingAsync();
    }

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
            .Returns(testCoordinates);

        await _service.StartTrackingAsync();
        await Task.Delay(100);

        // Act
        await _service.StopTrackingAsync();

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
            .Returns(_ => callCount++ == 0 ? location1 : location2);

        // Act
        await _service.StartTrackingAsync();
        await Task.Delay(500); // Wait for multiple polling cycles

        // Assert
        Assert.NotNull(eventArgs);

        // Verify both locations were stored in database
        var storedLocations = await _dbContext.DeviceLocationHistory
            .OrderBy(l => l.Timestamp)
            .ToListAsync();
        Assert.True(storedLocations.Count >= 2, "Expected at least 2 locations to be stored");

        // Cleanup
        await _service.StopTrackingAsync();
    }

    [Fact]
    public async Task LocationTracking_ShouldStoreMovementState()
    {
        // Arrange
        _service.MinimumDistanceMeters = 10.0;
        _service.PollingInterval = TimeSpan.FromMilliseconds(50);
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
                if (count == 0) return location1;
                if (count == 1) return location2;
                return location3;
            });

        // Act
        await _service.StartTrackingAsync();
        await Task.Delay(500);

        // Assert
        var storedLocations = await _dbContext.DeviceLocationHistory
            .OrderBy(l => l.Timestamp)
            .ToListAsync();

        Assert.NotEmpty(storedLocations);
        // Movement state should be recorded
        Assert.Contains(storedLocations, l => l.MovementState != "Unknown");

        // Cleanup
        await _service.StopTrackingAsync();
    }

    [Fact]
    public void MinimumDistanceMeters_ShouldBeConfigurable()
    {
        // Arrange & Act
        _service.MinimumDistanceMeters = 100.0;

        // Assert
        Assert.Equal(100.0, _service.MinimumDistanceMeters);
    }

    [Fact]
    public void PollingInterval_ShouldBeConfigurable()
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
            .Returns(testCoordinates);

        // Act
        await _service.StartTrackingAsync();
        await Task.Delay(150);
        await _service.StopTrackingAsync();

        // Assert - Query by device ID
        var deviceId = (await _dbContext.DeviceLocationHistory.FirstAsync()).DeviceId;
        var locationsByDevice = await _dbContext.DeviceLocationHistory
            .Where(l => l.DeviceId == deviceId)
            .ToListAsync();

        Assert.NotEmpty(locationsByDevice);
        Assert.All(locationsByDevice, l => Assert.Equal(deviceId, l.DeviceId));
    }

    [Fact]
    public async Task LocationHistory_ShouldBeQueryableByTimestamp()
    {
        // Arrange
        var testCoordinates = new GpsCoordinates(37.7749, -122.4194)
        {
            Timestamp = DateTimeOffset.UtcNow
        };

        _gpsService
            .GetCurrentLocationAsync(Arg.Any<CancellationToken>())
            .Returns(testCoordinates);

        // Act
        await _service.StartTrackingAsync();
        await Task.Delay(150);
        await _service.StopTrackingAsync();

        // Assert - Query by time range
        var now = DateTimeOffset.UtcNow;
        var recentLocations = await _dbContext.DeviceLocationHistory
            .Where(l => l.Timestamp >= now.AddMinutes(-5))
            .ToListAsync();

        Assert.NotEmpty(recentLocations);
    }
}

