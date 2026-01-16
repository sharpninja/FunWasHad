using FWH.Common.Location;
using FWH.Common.Location.Models;
using Orchestrix.Contracts.Location;
using FWH.Mobile.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Orchestrix.Contracts.Mediator;
using Xunit;

namespace FWH.Mobile.Services.Tests;

/// <summary>
/// Tests for LocationTrackingService with MediatR integration.
/// </summary>
public class LocationTrackingServiceTests
{
    private readonly IGpsService _gpsService;
    private readonly IMediatorSender _mediator;
    private readonly ILocationService _locationService;
    private readonly ILogger<LocationTrackingService> _logger;
    private readonly LocationTrackingService _service;

    public LocationTrackingServiceTests()
    {
        _gpsService = Substitute.For<IGpsService>();
        _mediator = Substitute.For<IMediatorSender>();
        _locationService = Substitute.For<ILocationService>();
        _logger = Substitute.For<ILogger<LocationTrackingService>>();

        // Setup GPS service to be available by default
        _gpsService.IsLocationAvailable.Returns(true);

        _service = new LocationTrackingService(
            _gpsService,
            _mediator,
            _locationService,
            _logger);
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

        _mediator
            .SendAsync<UpdateDeviceLocationResponse>(Arg.Any<UpdateDeviceLocationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new UpdateDeviceLocationResponse { Success = true, LocationId = 123 });

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

        _mediator
            .SendAsync<UpdateDeviceLocationResponse>(Arg.Any<UpdateDeviceLocationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new UpdateDeviceLocationResponse { Success = true });

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
    public async Task SendLocationUpdate_ShouldUseMediatorWithCorrectRequest()
    {
        // Arrange
        var testCoordinates = new GpsCoordinates(37.7749, -122.4194)
        {
            AccuracyMeters = 10.0,
            Timestamp = DateTimeOffset.UtcNow
        };

        UpdateDeviceLocationRequest? capturedRequest = null;
        _mediator
            .SendAsync<UpdateDeviceLocationResponse>(
                Arg.Do<UpdateDeviceLocationRequest>(r => capturedRequest = r),
                Arg.Any<CancellationToken>())
            .Returns(new UpdateDeviceLocationResponse { Success = true, LocationId = 456 });

        _gpsService
            .GetCurrentLocationAsync(Arg.Any<CancellationToken>())
            .Returns(testCoordinates);

        // Act
        await _service.StartTrackingAsync();
        await Task.Delay(100); // Allow tracking loop to execute

        // Assert
        await _mediator.ReceivedWithAnyArgs().SendAsync(default(IMediatorRequest<UpdateDeviceLocationResponse>)!, default);

        Assert.NotNull(capturedRequest);
        Assert.Equal(testCoordinates.Latitude, capturedRequest!.Latitude);
        Assert.Equal(testCoordinates.Longitude, capturedRequest.Longitude);
        Assert.Equal(testCoordinates.AccuracyMeters, capturedRequest.Accuracy);
        Assert.Equal(testCoordinates.Timestamp, capturedRequest.Timestamp);

        // Cleanup
        await _service.StopTrackingAsync();
    }

    [Fact]
    public async Task SendLocationUpdate_WhenMediatorReturnsSuccess_ShouldRaiseLocationUpdatedEvent()
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

        _mediator
            .SendAsync<UpdateDeviceLocationResponse>(Arg.Any<UpdateDeviceLocationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new UpdateDeviceLocationResponse { Success = true, LocationId = 789 });

        _service.LocationUpdated += (sender, coords) => eventCoordinates = coords;

        // Act
        await _service.StartTrackingAsync();
        await Task.Delay(100);

        // Assert
        Assert.NotNull(eventCoordinates);
        Assert.Equal(testCoordinates.Latitude, eventCoordinates!.Latitude);
        Assert.Equal(testCoordinates.Longitude, eventCoordinates.Longitude);

        // Cleanup
        await _service.StopTrackingAsync();
    }

    [Fact]
    public async Task SendLocationUpdate_WhenMediatorReturnsFail_ShouldLogWarning()
    {
        // Arrange
        var testCoordinates = new GpsCoordinates(37.7749, -122.4194)
        {
            Timestamp = DateTimeOffset.UtcNow
        };

        _gpsService
            .GetCurrentLocationAsync(Arg.Any<CancellationToken>())
            .Returns(testCoordinates);

        _mediator
            .SendAsync<UpdateDeviceLocationResponse>(Arg.Any<UpdateDeviceLocationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new UpdateDeviceLocationResponse
            {
                Success = false,
                ErrorMessage = "API unavailable"
            });

        // Act
        await _service.StartTrackingAsync();
        await Task.Delay(100);

        // Assert
        _logger.ReceivedWithAnyArgs().Log(
            LogLevel.Warning,
            default,
            default!,
            default,
            default!);

        // Cleanup
        await _service.StopTrackingAsync();
    }

    [Fact]
    public async Task SendLocationUpdate_WhenMediatorThrows_ShouldRaiseLocationUpdateFailedEvent()
    {
        // Arrange
        var testCoordinates = new GpsCoordinates(37.7749, -122.4194)
        {
            Timestamp = DateTimeOffset.UtcNow
        };
        Exception? eventException = null;

        _gpsService
            .GetCurrentLocationAsync(Arg.Any<CancellationToken>())
            .Returns(testCoordinates);

        var expectedException = new HttpRequestException("Network error");
        _mediator
            .SendAsync<UpdateDeviceLocationResponse>(Arg.Any<UpdateDeviceLocationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<UpdateDeviceLocationResponse>(expectedException));

        _service.LocationUpdateFailed += (sender, ex) => eventException = ex;

        // Act
        await _service.StartTrackingAsync();
        await Task.Delay(100);

        // Assert
        Assert.NotNull(eventException);
        Assert.IsType<HttpRequestException>(eventException);

        // Cleanup
        await _service.StopTrackingAsync();
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

        _mediator
            .SendAsync<UpdateDeviceLocationResponse>(Arg.Any<UpdateDeviceLocationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new UpdateDeviceLocationResponse { Success = true });

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

        _mediator
            .SendAsync<UpdateDeviceLocationResponse>(Arg.Any<UpdateDeviceLocationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new UpdateDeviceLocationResponse { Success = true });

        // Act
        await _service.StartTrackingAsync();
        await Task.Delay(500); // Wait for multiple polling cycles

        // Assert
        Assert.NotNull(eventArgs);

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
}
