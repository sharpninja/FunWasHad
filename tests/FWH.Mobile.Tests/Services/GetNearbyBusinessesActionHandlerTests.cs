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
    public void ConstructorWithNullGpsServiceThrowsArgumentNullException()
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

    /// <summary>
    /// Tests that the GetNearbyBusinessesActionHandler returns the correct action name.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The IWorkflowActionHandler.Name property's return value for GetNearbyBusinessesActionHandler.</para>
    /// <para><strong>Data involved:</strong> The handler instance created in the test constructor. The expected action name is "get_nearby_businesses", which is used to register and identify this handler in the workflow action registry.</para>
    /// <para><strong>Why the data matters:</strong> The action name is the identifier used in workflow definitions to reference this handler. Workflows specify actions like {"action": "get_nearby_businesses", "params": {...}}, and the registry uses the name to find the corresponding handler. If the name is incorrect, workflows won't be able to find and execute this handler.</para>
    /// <para><strong>Expected outcome:</strong> The Name property should return exactly "get_nearby_businesses".</para>
    /// <para><strong>Reason for expectation:</strong> The action name must match exactly what workflows use in their JSON action definitions. The underscore-separated lowercase format ("get_nearby_businesses") is a common convention for action names. The exact match ensures workflows can successfully resolve and execute this handler.</para>
    /// </remarks>
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

        _mockNotificationService.Received().ShowError(
            Arg.Any<string>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task HandleAsync_WithGpsAvailableButNullCoordinates_ReturnsLocationUnavailableStatus()
    {
        // Arrange
        _mockGpsService.IsLocationAvailable.Returns(true);
        _mockGpsService.GetCurrentLocationAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<GpsCoordinates?>((GpsCoordinates?)null));

        var context = CreateTestContext();
        var parameters = new Dictionary<string, string>();

        // Act
        var result = await _handler.HandleAsync(context, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("location_unavailable", result["status"]);
        Assert.Contains("GPS", result["error"]);

        _mockNotificationService.Received().ShowError(
            Arg.Any<string>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task HandleAsync_WithInvalidCoordinates_ReturnsLocationUnavailableStatus()
    {
        // Arrange
        _mockGpsService.IsLocationAvailable.Returns(true);
        _mockGpsService.GetCurrentLocationAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<GpsCoordinates?>(new GpsCoordinates(91, 0))); // Invalid latitude

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
            .Returns(Task.FromResult<GpsCoordinates?>(coordinates));
        _mockLocationService.GetNearbyBusinessesAsync(
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<int>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<BusinessLocation>>(businesses));

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

        _mockNotificationService.Received().ShowSuccess(
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
            .Returns(Task.FromResult<GpsCoordinates?>(coordinates));
        _mockLocationService.GetNearbyBusinessesAsync(
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<int>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<BusinessLocation>>(businesses));

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
            .Returns(Task.FromResult<GpsCoordinates?>(coordinates));
        _mockLocationService.GetNearbyBusinessesAsync(
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<int>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<BusinessLocation>>(new List<BusinessLocation>()));

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

        await _mockLocationService.Received().GetNearbyBusinessesAsync(
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
            .Returns(Task.FromResult<GpsCoordinates?>(coordinates));
        _mockLocationService.GetNearbyBusinessesAsync(
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<int>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<BusinessLocation>>(new List<BusinessLocation>()));

        var context = CreateTestContext();
        var parameters = new Dictionary<string, string>
        {
            ["categories"] = "restaurant,cafe,shop"
        };

        // Act
        var result = await _handler.HandleAsync(context, parameters);

        // Assert
        await _mockLocationService.Received().GetNearbyBusinessesAsync(
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

        _mockNotificationService.Received().ShowWarning(
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

        _mockNotificationService.Received().ShowError(
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

        _mockNotificationService.Received().ShowError(
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
            .Returns(Task.FromResult<GpsCoordinates?>(coordinates));
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
            .Returns(Task.FromResult<GpsCoordinates?>(coordinates));
        _mockLocationService.GetNearbyBusinessesAsync(
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<int>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<BusinessLocation>>(businesses));

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
    public async Task HandleAsyncWithPermissionGrantedOnSecondAttemptSucceeds()
    {
        // Arrange
        var coordinates = new GpsCoordinates(37.7749, -122.4194);

        _mockGpsService.IsLocationAvailable.Returns(false, true);  // First check - not available, then after permission granted

        _mockGpsService.RequestLocationPermissionAsync()
            .Returns(Task.FromResult(true));

        _mockGpsService.GetCurrentLocationAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<GpsCoordinates?>(coordinates));

        _mockLocationService.GetNearbyBusinessesAsync(
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<int>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<BusinessLocation>>(new List<BusinessLocation>()));

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
