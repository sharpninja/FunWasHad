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
using Moq;
using Xunit;

namespace FWH.Mobile.Tests.Services;

/// <summary>
/// Tests for GetNearbyBusinessesActionHandler workflow action.
/// </summary>
public class GetNearbyBusinessesActionHandlerTests
{
    private readonly Mock<IGpsService> _mockGpsService;
    private readonly Mock<ILocationService> _mockLocationService;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<ILogger<GetNearbyBusinessesActionHandler>> _mockLogger;
    private readonly GetNearbyBusinessesActionHandler _handler;

    public GetNearbyBusinessesActionHandlerTests()
    {
        _mockGpsService = new Mock<IGpsService>();
        _mockLocationService = new Mock<ILocationService>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockLogger = new Mock<ILogger<GetNearbyBusinessesActionHandler>>();

        _handler = new GetNearbyBusinessesActionHandler(
            _mockGpsService.Object,
            _mockLocationService.Object,
            _mockNotificationService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithNullGpsService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new GetNearbyBusinessesActionHandler(
                null!,
                _mockLocationService.Object,
                _mockNotificationService.Object,
                _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLocationService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new GetNearbyBusinessesActionHandler(
                _mockGpsService.Object,
                null!,
                _mockNotificationService.Object,
                _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullNotificationService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new GetNearbyBusinessesActionHandler(
                _mockGpsService.Object,
                _mockLocationService.Object,
                null!,
                _mockLogger.Object));
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
        _mockGpsService.Setup(x => x.IsLocationAvailable).Returns(false);
        _mockGpsService.Setup(x => x.RequestLocationPermissionAsync())
            .ReturnsAsync(false);

        var context = CreateTestContext();
        var parameters = new Dictionary<string, string>();

        // Act
        var result = await _handler.HandleAsync(context, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("permission_denied", result["status"]);
        Assert.Contains("permission", result["error"].ToLower());
        
        _mockNotificationService.Verify(x => x.ShowError(
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithGpsAvailableButNullCoordinates_ReturnsLocationUnavailableStatus()
    {
        // Arrange
        _mockGpsService.Setup(x => x.IsLocationAvailable).Returns(true);
        _mockGpsService.Setup(x => x.GetCurrentLocationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((GpsCoordinates?)null);

        var context = CreateTestContext();
        var parameters = new Dictionary<string, string>();

        // Act
        var result = await _handler.HandleAsync(context, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("location_unavailable", result["status"]);
        Assert.Contains("GPS", result["error"]);
        
        _mockNotificationService.Verify(x => x.ShowError(
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidCoordinates_ReturnsLocationUnavailableStatus()
    {
        // Arrange
        _mockGpsService.Setup(x => x.IsLocationAvailable).Returns(true);
        _mockGpsService.Setup(x => x.GetCurrentLocationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GpsCoordinates(91, 0)); // Invalid latitude

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

        _mockGpsService.Setup(x => x.IsLocationAvailable).Returns(true);
        _mockGpsService.Setup(x => x.GetCurrentLocationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(coordinates);
        _mockLocationService.Setup(x => x.GetNearbyBusinessesAsync(
                It.IsAny<double>(),
                It.IsAny<double>(),
                It.IsAny<int>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(businesses);

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
        
        _mockNotificationService.Verify(x => x.ShowSuccess(
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithNoBusinessesFound_ReturnsSuccessWithZeroCount()
    {
        // Arrange
        var coordinates = new GpsCoordinates(37.7749, -122.4194);
        var businesses = new List<BusinessLocation>();

        _mockGpsService.Setup(x => x.IsLocationAvailable).Returns(true);
        _mockGpsService.Setup(x => x.GetCurrentLocationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(coordinates);
        _mockLocationService.Setup(x => x.GetNearbyBusinessesAsync(
                It.IsAny<double>(),
                It.IsAny<double>(),
                It.IsAny<int>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(businesses);

        var context = CreateTestContext();
        var parameters = new Dictionary<string, string>();

        // Act
        var result = await _handler.HandleAsync(context, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("success", result["status"]);
        Assert.Equal("0", result["count"]);
        Assert.Equal(string.Empty, result["businesses"]);
        
        _mockNotificationService.Verify(x => x.ShowInfo(
            It.IsAny<string>(),
            It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task HandleAsync_WithCustomRadius_UsesProvidedRadius()
    {
        // Arrange
        var coordinates = new GpsCoordinates(37.7749, -122.4194);
        _mockGpsService.Setup(x => x.IsLocationAvailable).Returns(true);
        _mockGpsService.Setup(x => x.GetCurrentLocationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(coordinates);
        _mockLocationService.Setup(x => x.GetNearbyBusinessesAsync(
                It.IsAny<double>(),
                It.IsAny<double>(),
                It.IsAny<int>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BusinessLocation>());

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
        
        _mockLocationService.Verify(x => x.GetNearbyBusinessesAsync(
            It.IsAny<double>(),
            It.IsAny<double>(),
            2000,
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithCategories_PassesCategoriesToLocationService()
    {
        // Arrange
        var coordinates = new GpsCoordinates(37.7749, -122.4194);
        _mockGpsService.Setup(x => x.IsLocationAvailable).Returns(true);
        _mockGpsService.Setup(x => x.GetCurrentLocationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(coordinates);
        _mockLocationService.Setup(x => x.GetNearbyBusinessesAsync(
                It.IsAny<double>(),
                It.IsAny<double>(),
                It.IsAny<int>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BusinessLocation>());

        var context = CreateTestContext();
        var parameters = new Dictionary<string, string>
        {
            ["categories"] = "restaurant,cafe,shop"
        };

        // Act
        var result = await _handler.HandleAsync(context, parameters);

        // Assert
        _mockLocationService.Verify(x => x.GetNearbyBusinessesAsync(
            It.IsAny<double>(),
            It.IsAny<double>(),
            It.IsAny<int>(),
            It.Is<IEnumerable<string>>(c => c.Contains("restaurant") && c.Contains("cafe") && c.Contains("shop")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithCancellation_ReturnsCancelledStatus()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockGpsService.Setup(x => x.IsLocationAvailable).Returns(true);
        _mockGpsService.Setup(x => x.GetCurrentLocationAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var context = CreateTestContext();
        var parameters = new Dictionary<string, string>();

        // Act
        var result = await _handler.HandleAsync(context, parameters, cts.Token);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("cancelled", result["status"]);
        
        _mockNotificationService.Verify(x => x.ShowWarning(
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenGpsServiceThrowsException_ReturnsErrorStatus()
    {
        // Arrange
        _mockGpsService.Setup(x => x.IsLocationAvailable).Returns(true);
        _mockGpsService.Setup(x => x.GetCurrentLocationAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("GPS hardware failure"));

        var context = CreateTestContext();
        var parameters = new Dictionary<string, string>();

        // Act
        var result = await _handler.HandleAsync(context, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("error", result["status"]);
        Assert.Contains("GPS hardware failure", result["error"]);
        
        _mockNotificationService.Verify(x => x.ShowError(
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenLocationServiceThrowsException_ReturnsErrorStatus()
    {
        // Arrange
        var coordinates = new GpsCoordinates(37.7749, -122.4194);
        _mockGpsService.Setup(x => x.IsLocationAvailable).Returns(true);
        _mockGpsService.Setup(x => x.GetCurrentLocationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(coordinates);
        _mockLocationService.Setup(x => x.GetNearbyBusinessesAsync(
                It.IsAny<double>(),
                It.IsAny<double>(),
                It.IsAny<int>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("API error"));

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

        _mockGpsService.Setup(x => x.IsLocationAvailable).Returns(true);
        _mockGpsService.Setup(x => x.GetCurrentLocationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(coordinates);
        _mockLocationService.Setup(x => x.GetNearbyBusinessesAsync(
                It.IsAny<double>(),
                It.IsAny<double>(),
                It.IsAny<int>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(businesses);

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
        
        _mockGpsService.SetupSequence(x => x.IsLocationAvailable)
            .Returns(false)  // First check - not available
            .Returns(true);  // After permission granted
        
        _mockGpsService.Setup(x => x.RequestLocationPermissionAsync())
            .ReturnsAsync(true);
        
        _mockGpsService.Setup(x => x.GetCurrentLocationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(coordinates);
        
        _mockLocationService.Setup(x => x.GetNearbyBusinessesAsync(
                It.IsAny<double>(),
                It.IsAny<double>(),
                It.IsAny<int>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BusinessLocation>());

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
        var mockInstanceManager = new Mock<IWorkflowInstanceManager>();
        
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

        return new ActionHandlerContext("test-workflow-id", node, definition, mockInstanceManager.Object);
    }
}
