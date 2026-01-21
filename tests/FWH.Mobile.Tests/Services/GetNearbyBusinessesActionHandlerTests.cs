using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FWH.Common.Location;
using FWH.Common.Location.Models;
using FWH.Common.Workflow.Actions;
using FWH.Common.Workflow.Instance;
using FWH.Common.Workflow.Models;
using FWH.Mobile.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace FWH.Mobile.Tests.Services;

/// <summary>
/// Tests for GetNearbyBusinessesActionHandler workflow action.
/// </summary>
public class GetNearbyBusinessesActionHandlerTests
{
    private readonly IGpsService _mockGpsService;
    private readonly ILocationService _mockLocationService;
    private readonly INotificationService _mockNotificationService;
    private readonly ILogger<GetNearbyBusinessesActionHandler> _mockLogger;
    private readonly GetNearbyBusinessesActionHandler _handler;

    public GetNearbyBusinessesActionHandlerTests()
    {
        _mockGpsService = Substitute.For<IGpsService>();
        _mockLocationService = Substitute.For<ILocationService>();
        _mockNotificationService = Substitute.For<INotificationService>();
        _mockLogger = Substitute.For<ILogger<GetNearbyBusinessesActionHandler>>();

        _handler = new GetNearbyBusinessesActionHandler(
            _mockGpsService,
            _mockLocationService,
            _mockNotificationService,
            _mockLogger);
    }

    [Fact]
    public void Constructor_WithNullGpsService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new GetNearbyBusinessesActionHandler(
                null!,
                _mockLocationService,
                _mockNotificationService,
                _mockLogger));
    }

    [Fact]
    public void Constructor_WithNullLocationService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new GetNearbyBusinessesActionHandler(
                _mockGpsService,
                null!,
                _mockNotificationService,
                _mockLogger));
    }

    [Fact]
    public void Constructor_WithNullNotificationService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new GetNearbyBusinessesActionHandler(
                _mockGpsService,
                _mockLocationService,
                null!,
                _mockLogger));
    }

    [Fact]
    public void Name_ReturnsCorrectActionName()
    {
        // Act
        var name = _handler.Name;

        // Assert
        Assert.Equal("get_nearby_businesses", name);
    }

    [Fact]
    public async Task HandleAsync_WithGpsUnavailableAndPermissionDenied_ReturnsPermissionDeniedStatus()
    {
        // Arrange
        _mockGpsService.IsLocationAvailable.Returns(false);
        _mockGpsService.RequestLocationPermissionAsync()
            .Returns(Task.FromResult(false));

        var context = CreateTestContext();
        var parameters = new Dictionary<string, string>();

        // Act
        var result = await _handler.HandleAsync(context, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("permission_denied", result["status"]);
        Assert.Contains("permission", result["error"].ToLower());

        _mockNotificationService.Received(1).ShowError(
            Arg.Any<string>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task HandleAsync_WithGpsAvailableButNullCoordinates_ReturnsLocationUnavailableStatus()
    {
        // Arrange
        _mockGpsService.IsLocationAvailable.Returns(true);
        _mockGpsService.GetCurrentLocationAsync(Arg.Any<CancellationToken>())
            .Returns((GpsCoordinates?)null);

        var context = CreateTestContext();
        var parameters = new Dictionary<string, string>();

        // Act
        var result = await _handler.HandleAsync(context, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("location_unavailable", result["status"]);
        Assert.Contains("GPS", result["error"]);

        _mockNotificationService.Received(1).ShowError(
            Arg.Any<string>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task HandleAsync_WithInvalidCoordinates_ReturnsLocationUnavailableStatus()
    {
        // Arrange
        _mockGpsService.IsLocationAvailable.Returns(true);
        _mockGpsService.GetCurrentLocationAsync(Arg.Any<CancellationToken>())
            .Returns(new GpsCoordinates(91, 0)); // Invalid latitude

        var context = CreateTestContext();
        var parameters = new Dictionary<string, string>();

        // Act
        var result = await _handler.HandleAsync(context, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("location_unavailable", result["status"]);
    }

    [Fact]
    public async Task HandleAsync_WithValidLocationAndBusinesses_ReturnsSuccessWithDetails()
    {
        // Arrange
        var coordinates = new GpsCoordinates(37.7749, -122.4194, 25.0);
        var businesses = new List<BusinessLocation>
        {
            new BusinessLocation { Name = "Business A", DistanceMeters = 100 },
            new BusinessLocation { Name = "Business B", DistanceMeters = 200 },
            new BusinessLocation { Name = "Business C", DistanceMeters = 300 }
        };

        _mockGpsService.IsLocationAvailable.Returns(true);
        _mockGpsService.GetCurrentLocationAsync(Arg.Any<CancellationToken>())
            .Returns(coordinates);
        _mockLocationService.GetNearbyBusinessesAsync(
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<int>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(businesses);

        var context = CreateTestContext();
        var parameters = new Dictionary<string, string>();

        // Act
        var result = await _handler.HandleAsync(context, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("success", result["status"]);
        Assert.Equal("37.774900", result["latitude"]);
        Assert.Equal("-122.419400", result["longitude"]);
        Assert.Equal("25", result["accuracy"]);
        Assert.Equal("3", result["count"]);
        Assert.Equal("Business A", result["closest_business"]);
        Assert.Equal("100", result["closest_distance"]);
        Assert.Contains("Business A", result["businesses"]);

        _mockNotificationService.Received(1).ShowSuccess(
            Arg.Any<string>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task HandleAsync_WithNoBusinessesFound_ReturnsSuccessWithZeroCount()
    {
        // Arrange
        var coordinates = new GpsCoordinates(37.7749, -122.4194);
        var businesses = new List<BusinessLocation>();

        _mockGpsService.IsLocationAvailable.Returns(true);
        _mockGpsService.GetCurrentLocationAsync(Arg.Any<CancellationToken>())
            .Returns(coordinates);
        _mockLocationService.GetNearbyBusinessesAsync(
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<int>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(businesses);

        var context = CreateTestContext();
        var parameters = new Dictionary<string, string>();

        // Act
        var result = await _handler.HandleAsync(context, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("success", result["status"]);
        Assert.Equal("0", result["count"]);
        Assert.Equal(string.Empty, result["businesses"]);

        _mockNotificationService.Received().ShowInfo(
            Arg.Any<string>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task HandleAsync_WithCustomRadius_UsesProvidedRadius()
    {
        // Arrange
        var coordinates = new GpsCoordinates(37.7749, -122.4194);
        _mockGpsService.IsLocationAvailable.Returns(true);
        _mockGpsService.GetCurrentLocationAsync(Arg.Any<CancellationToken>())
            .Returns(coordinates);
        _mockLocationService.GetNearbyBusinessesAsync(
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<int>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<BusinessLocation>());

        var context = CreateTestContext();
        var parameters = new Dictionary<string, string>
        {
            ["radius"] = "2000"
        };

        // Act
        var result = await _handler.HandleAsync(context, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("2000", result["radius"]);

        await _mockLocationService.Received(1).GetNearbyBusinessesAsync(
            Arg.Any<double>(),
            Arg.Any<double>(),
            2000,
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithCategories_PassesCategoriesToLocationService()
    {
        // Arrange
        var coordinates = new GpsCoordinates(37.7749, -122.4194);
        _mockGpsService.IsLocationAvailable.Returns(true);
        _mockGpsService.GetCurrentLocationAsync(Arg.Any<CancellationToken>())
            .Returns(coordinates);
        _mockLocationService.GetNearbyBusinessesAsync(
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<int>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<BusinessLocation>());

        var context = CreateTestContext();
        var parameters = new Dictionary<string, string>
        {
            ["categories"] = "restaurant,cafe,shop"
        };

        // Act
        var result = await _handler.HandleAsync(context, parameters);

        // Assert
        await _mockLocationService.Received(1).GetNearbyBusinessesAsync(
            Arg.Any<double>(),
            Arg.Any<double>(),
            Arg.Any<int>(),
            Arg.Is<IEnumerable<string>>(c => c.Contains("restaurant") && c.Contains("cafe") && c.Contains("shop")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithCancellation_ReturnsCancelledStatus()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockGpsService.IsLocationAvailable.Returns(true);
        _mockGpsService.GetCurrentLocationAsync(Arg.Any<CancellationToken>())
            .Returns<Task<GpsCoordinates?>>(_ => throw new OperationCanceledException());

        var context = CreateTestContext();
        var parameters = new Dictionary<string, string>();

        // Act
        var result = await _handler.HandleAsync(context, parameters, cts.Token);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("cancelled", result["status"]);

        _mockNotificationService.Received(1).ShowWarning(
            Arg.Any<string>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task HandleAsync_WhenGpsServiceThrowsLocationServicesException_ReturnsErrorStatusWithDiagnostics()
    {
        // Arrange
        var diagnostics = new Dictionary<string, object?>
        {
            ["PermissionStatus"] = "Denied",
            ["GpsProviderEnabled"] = false,
            ["NetworkProviderEnabled"] = false,
            ["Error"] = "No location providers are enabled"
        };
        var locationException = new LocationServicesException(
            "Android",
            "GetCurrentLocationAsync",
            "No location providers (GPS or Network) are enabled on the device",
            diagnostics);

        _mockGpsService.IsLocationAvailable.Returns(true);
        _mockGpsService.GetCurrentLocationAsync(Arg.Any<CancellationToken>())
            .Returns<Task<GpsCoordinates?>>(_ => throw locationException);

        var context = CreateTestContext();
        var parameters = new Dictionary<string, string>();

        // Act
        var result = await _handler.HandleAsync(context, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("error", result["status"]);
        Assert.Contains("No location providers", result["error"]);
        Assert.Equal("Android", result["platform"]);
        Assert.Equal("GetCurrentLocationAsync", result["operation"]);
        Assert.Contains("PermissionStatus", result["diagnostics"]);

        _mockNotificationService.Received(1).ShowError(
            Arg.Is<string>(msg => msg.Contains("Location service error")),
            Arg.Any<string>());
    }

    [Fact]
    public async Task HandleAsync_WhenGpsServiceThrowsGenericException_ReturnsErrorStatus()
    {
        // Arrange
        _mockGpsService.IsLocationAvailable.Returns(true);
        _mockGpsService.GetCurrentLocationAsync(Arg.Any<CancellationToken>())
            .Returns<Task<GpsCoordinates?>>(_ => throw new InvalidOperationException("GPS hardware failure"));

        var context = CreateTestContext();
        var parameters = new Dictionary<string, string>();

        // Act
        var result = await _handler.HandleAsync(context, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("error", result["status"]);
        Assert.Contains("GPS hardware failure", result["error"]);

        _mockNotificationService.Received(1).ShowError(
            Arg.Any<string>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task HandleAsync_WhenLocationServiceThrowsException_ReturnsErrorStatus()
    {
        // Arrange
        var coordinates = new GpsCoordinates(37.7749, -122.4194);
        _mockGpsService.IsLocationAvailable.Returns(true);
        _mockGpsService.GetCurrentLocationAsync(Arg.Any<CancellationToken>())
            .Returns(coordinates);
        _mockLocationService.GetNearbyBusinessesAsync(
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<int>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<IEnumerable<BusinessLocation>>>(_ => throw new Exception("API error"));

        var context = CreateTestContext();
        var parameters = new Dictionary<string, string>();

        // Act
        var result = await _handler.HandleAsync(context, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("error", result["status"]);
        Assert.Contains("API error", result["error"]);
    }

    [Fact]
    public async Task HandleAsync_WithManyBusinesses_ReturnsTop5InBusinessesField()
    {
        // Arrange
        var coordinates = new GpsCoordinates(37.7749, -122.4194);
        var businesses = Enumerable.Range(1, 10)
            .Select(i => new BusinessLocation
            {
                Name = $"Business {i}",
                DistanceMeters = i * 100
            })
            .ToList();

        _mockGpsService.IsLocationAvailable.Returns(true);
        _mockGpsService.GetCurrentLocationAsync(Arg.Any<CancellationToken>())
            .Returns(coordinates);
        _mockLocationService.GetNearbyBusinessesAsync(
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<int>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(businesses);

        var context = CreateTestContext();
        var parameters = new Dictionary<string, string>();

        // Act
        var result = await _handler.HandleAsync(context, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("10", result["count"]);

        // Should only include top 5
        var businessNames = result["businesses"].Split(',');
        Assert.Equal(5, businessNames.Length);
        Assert.Contains("Business 1", result["businesses"]);
        Assert.Contains("Business 5", result["businesses"]);
        Assert.DoesNotContain("Business 6", result["businesses"]);
    }

    [Fact]
    public async Task HandleAsync_WithPermissionGrantedOnSecondAttempt_Succeeds()
    {
        // Arrange
        var coordinates = new GpsCoordinates(37.7749, -122.4194);

        _mockGpsService.IsLocationAvailable.Returns(false, true);  // First check - not available, then after permission granted

        _mockGpsService.RequestLocationPermissionAsync()
            .Returns(Task.FromResult(true));

        _mockGpsService.GetCurrentLocationAsync(Arg.Any<CancellationToken>())
            .Returns(coordinates);

        _mockLocationService.GetNearbyBusinessesAsync(
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<int>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<BusinessLocation>());

        var context = CreateTestContext();
        var parameters = new Dictionary<string, string>();

        // Act
        var result = await _handler.HandleAsync(context, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("success", result["status"]);
    }

    private ActionHandlerContext CreateTestContext()
    {
        var mockInstanceManager = Substitute.For<IWorkflowInstanceManager>();

        // WorkflowNode constructor: (string Id, string Label, string? JsonMetadata = null, string? NoteMarkdown = null)
        var node = new WorkflowNode(
            Id: "test-node",
            Label: "Test Node",
            JsonMetadata: null,
            NoteMarkdown: null);

        var nodes = new List<WorkflowNode> { node };
        var transitions = new List<Transition>();
        var startPoints = new List<StartPoint>();

        // WorkflowDefinition constructor: (string Id, string Name, IReadOnlyList<WorkflowNode> Nodes, IReadOnlyList<Transition> Transitions, IReadOnlyList<StartPoint> StartPoints)
        var definition = new WorkflowDefinition(
            Id: "test-workflow",
            Name: "Test Workflow",
            Nodes: nodes,
            Transitions: transitions,
            StartPoints: startPoints);

        return new ActionHandlerContext("test-workflow-id", node, definition, mockInstanceManager);
    }
}
