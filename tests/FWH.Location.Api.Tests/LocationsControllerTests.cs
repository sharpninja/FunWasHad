using System.Net;
using System.Net.Http.Json;
using FWH.Common.Location.Models;
using FWH.Location.Api.Data;
using NSubstitute;
using Xunit;

namespace FWH.Location.Api.Tests;

public class LocationsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public LocationsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Nearby_ReturnsOk_WithResults()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        var substitute = _factory.LocationServiceSubstitute;
        substitute
            .GetNearbyBusinessesAsync(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<int>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<BusinessLocation>>(new[]
            {
                new BusinessLocation { Name = "Test Place", Latitude = 1, Longitude = 2, Category = "cafe" }
            }));

        var response = await client.GetAsync("/api/locations/nearby?latitude=1&longitude=2&radiusMeters=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<List<BusinessLocation>>();
        Assert.NotNull(payload);
        Assert.Single(payload!);
        Assert.Equal("Test Place", payload![0].Name);
    }

    [Fact]
    public async Task Nearby_InvalidLatitude_ReturnsBadRequest()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/locations/nearby?latitude=999&longitude=2&radiusMeters=50");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Closest_ReturnsOk_WhenFound()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        var substitute = _factory.LocationServiceSubstitute;
        substitute
            .GetClosestBusinessAsync(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BusinessLocation?>(new BusinessLocation { Name = "Closest", Latitude = 1, Longitude = 2, Category = "shop" }));

        var response = await client.GetAsync("/api/locations/closest?latitude=1&longitude=2&maxDistanceMeters=100");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<BusinessLocation>();
        Assert.NotNull(payload);
        Assert.Equal("Closest", payload!.Name);
    }

    [Fact]
    public async Task Closest_NotFound_Returns404()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        var substitute = _factory.LocationServiceSubstitute;
        substitute
            .GetClosestBusinessAsync(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<BusinessLocation?>(null));

        var response = await client.GetAsync("/api/locations/closest?latitude=1&longitude=2&maxDistanceMeters=100");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Confirmed_Persists_AndReturnsCreated()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LocationDbContext>();

        var request = new
        {
            business = new BusinessLocation
            {
                Name = "Cafe Uno",
                Address = "123 Main",
                Latitude = 1.1,
                Longitude = 2.2,
                Category = "cafe",
                Tags = new Dictionary<string, string>()
            },
            latitude = 1.2,
            longitude = 2.3
        };

        var response = await client.PostAsJsonAsync("/api/locations/confirmed", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var saved = db.LocationConfirmations.ToList();
        Assert.Single(saved);
        Assert.Equal("Cafe Uno", saved[0].BusinessName);
        Assert.Equal(1.2, saved[0].UserLatitude);
        Assert.Equal(2.3, saved[0].UserLongitude);
    }

    /// <summary>
    /// Tests TR-API-005: Device location update endpoint with deviceId in request body.
    /// </summary>
    [Fact]
    public async Task UpdateDeviceLocation_WithBodyDeviceId_ReturnsOk()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LocationDbContext>();

        var request = new
        {
            deviceId = "test-device-123",
            latitude = 37.7749,
            longitude = -122.4194,
            accuracyMeters = 25.5,
            timestamp = DateTimeOffset.UtcNow
        };

        var response = await client.PostAsJsonAsync("/api/locations/device", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(result);

        var saved = db.DeviceLocations.ToList();
        Assert.Single(saved);
        Assert.Equal("test-device-123", saved[0].DeviceId);
        Assert.Equal(37.7749, saved[0].Latitude);
        Assert.Equal(-122.4194, saved[0].Longitude);
        Assert.Equal(25.5, saved[0].AccuracyMeters);
    }

    /// <summary>
    /// Tests TR-API-005: Device location update endpoint with deviceId in route.
    /// </summary>
    [Fact]
    public async Task UpdateDeviceLocation_WithRouteDeviceId_ReturnsOk()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LocationDbContext>();

        var request = new
        {
            deviceId = "route-device-456",
            latitude = 40.7128,
            longitude = -74.0060,
            accuracyMeters = 30.0
        };

        var response = await client.PostAsJsonAsync("/api/locations/device/route-device-456", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var saved = db.DeviceLocations.ToList();
        Assert.Single(saved);
        Assert.Equal("route-device-456", saved[0].DeviceId);
    }

    /// <summary>
    /// Tests TR-API-005: Device location update validation - invalid latitude.
    /// Implements TR-SEC-001: Data validation.
    /// </summary>
    [Fact]
    public async Task UpdateDeviceLocation_InvalidLatitude_ReturnsBadRequest()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        var request = new
        {
            deviceId = "test-device",
            latitude = 999.0, // Invalid latitude
            longitude = -122.4194
        };

        var response = await client.PostAsJsonAsync("/api/locations/device", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Tests TR-API-005: Device location update validation - invalid longitude.
    /// Implements TR-SEC-001: Data validation.
    /// </summary>
    [Fact]
    public async Task UpdateDeviceLocation_InvalidLongitude_ReturnsBadRequest()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        var request = new
        {
            deviceId = "test-device",
            latitude = 37.7749,
            longitude = 999.0 // Invalid longitude
        };

        var response = await client.PostAsJsonAsync("/api/locations/device", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Tests TR-API-005: Device location update validation - missing device ID.
    /// </summary>
    [Fact]
    public async Task UpdateDeviceLocation_MissingDeviceId_ReturnsBadRequest()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        var request = new
        {
            latitude = 37.7749,
            longitude = -122.4194
            // deviceId is missing
        };

        var response = await client.PostAsJsonAsync("/api/locations/device", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Tests TR-API-005: Device location update validation - null request body.
    /// </summary>
    [Fact]
    public async Task UpdateDeviceLocation_NullRequest_ReturnsBadRequest()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync<object>("/api/locations/device", null!);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Tests TR-API-005: Device location update - mismatched device IDs.
    /// </summary>
    [Fact]
    public async Task UpdateDeviceLocation_MismatchedDeviceIds_ReturnsBadRequest()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        var request = new
        {
            deviceId = "body-device-id",
            latitude = 37.7749,
            longitude = -122.4194
        };

        // Route has different deviceId than body
        var response = await client.PostAsJsonAsync("/api/locations/device/route-device-id", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
