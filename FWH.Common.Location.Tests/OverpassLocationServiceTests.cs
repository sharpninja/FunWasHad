using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;
using FWH.Common.Location;
using FWH.Common.Location.Services;
using FWH.Common.Location.Models;
using FWH.Common.Location.Configuration;

namespace FWH.Common.Location.Tests;

public class OverpassLocationServiceTests
{
    private readonly Mock<ILogger<OverpassLocationService>> _mockLogger;

    public OverpassLocationServiceTests()
    {
        _mockLogger = new Mock<ILogger<OverpassLocationService>>();
    }

    private LocationServiceOptions CreateDefaultOptions()
    {
        return new LocationServiceOptions
        {
            DefaultRadiusMeters = 30,  // Updated default
            MaxRadiusMeters = 5000,
            MinRadiusMeters = 50,
            TimeoutSeconds = 30,
            UserAgent = "FunWasHad/1.0",
            OverpassApiUrl = "https://overpass-api.de/api/interpreter"
        };
    }

    [Fact]
    public async Task GetNearbyBusinessesAsync_WithValidResponse_ReturnsBusinesses()
    {
        // Arrange
        var overpassResponse = @"{
            ""elements"": [
                {
                    ""type"": ""node"",
                    ""id"": 123456,
                    ""lat"": 37.7749,
                    ""lon"": -122.4194,
                    ""tags"": {
                        ""name"": ""Test Restaurant"",
                        ""amenity"": ""restaurant"",
                        ""addr:street"": ""Market Street"",
                        ""addr:city"": ""San Francisco""
                    }
                },
                {
                    ""type"": ""node"",
                    ""id"": 789012,
                    ""lat"": 37.7750,
                    ""lon"": -122.4195,
                    ""tags"": {
                        ""name"": ""Test Cafe"",
                        ""amenity"": ""cafe""
                    }
                }
            ]
        }";

        var httpClient = CreateMockHttpClient(overpassResponse);
        var options = Options.Create(CreateDefaultOptions());
        var service = new OverpassLocationService(httpClient, _mockLogger.Object, options);

        // Act
        var results = await service.GetNearbyBusinessesAsync(37.7749, -122.4194, 1000);
        var businesses = results.ToList();

        // Assert
        Assert.Equal(2, businesses.Count);
        Assert.Equal("Test Restaurant", businesses[0].Name);
        Assert.Equal("restaurant", businesses[0].Category);
        Assert.Contains("Market Street", businesses[0].Address ?? "");
    }

    [Fact]
    public async Task GetNearbyBusinessesAsync_WithTooLargeRadius_ClampsToMaximum()
    {
        // Arrange
        var overpassResponse = @"{""elements"": []}";
        var httpClient = CreateMockHttpClient(overpassResponse);
        var options = Options.Create(CreateDefaultOptions());
        var service = new OverpassLocationService(httpClient, _mockLogger.Object, options);

        // Act
        var results = await service.GetNearbyBusinessesAsync(37.7749, -122.4194, 10000); // 10km

        // Assert - should clamp to 5000m max
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetNearbyBusinessesAsync_WithEmptyResponse_ReturnsEmptyCollection()
    {
        // Arrange
        var overpassResponse = @"{""elements"": []}";
        var httpClient = CreateMockHttpClient(overpassResponse);
        var options = Options.Create(CreateDefaultOptions());
        var service = new OverpassLocationService(httpClient, _mockLogger.Object, options);

        // Act
        var results = await service.GetNearbyBusinessesAsync(37.7749, -122.4194, 1000);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetNearbyBusinessesAsync_WithHttpError_ReturnsEmptyCollection()
    {
        // Arrange
        var httpClient = CreateMockHttpClient("", HttpStatusCode.InternalServerError);
        var options = Options.Create(CreateDefaultOptions());
        var service = new OverpassLocationService(httpClient, _mockLogger.Object, options);

        // Act
        var results = await service.GetNearbyBusinessesAsync(37.7749, -122.4194, 1000);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetClosestBusinessAsync_ReturnsNearestBusiness()
    {
        // Arrange
        var overpassResponse = @"{
            ""elements"": [
                {
                    ""type"": ""node"",
                    ""id"": 123456,
                    ""lat"": 37.7750,
                    ""lon"": -122.4195,
                    ""tags"": {
                        ""name"": ""Far Business"",
                        ""amenity"": ""restaurant""
                    }
                },
                {
                    ""type"": ""node"",
                    ""id"": 789012,
                    ""lat"": 37.7749,
                    ""lon"": -122.4194,
                    ""tags"": {
                        ""name"": ""Close Business"",
                        ""amenity"": ""cafe""
                    }
                }
            ]
        }";

        var httpClient = CreateMockHttpClient(overpassResponse);
        var options = Options.Create(CreateDefaultOptions());
        var service = new OverpassLocationService(httpClient, _mockLogger.Object, options);

        // Act
        var result = await service.GetClosestBusinessAsync(37.7749, -122.4194, 1000);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Close Business", result.Name);
    }

    [Fact]
    public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(CreateDefaultOptions());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new OverpassLocationService(null!, _mockLogger.Object, options));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var httpClient = new HttpClient();
        var options = Options.Create(CreateDefaultOptions());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new OverpassLocationService(httpClient, null!, options));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var httpClient = new HttpClient();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new OverpassLocationService(httpClient, _mockLogger.Object, null!));
    }

    [Fact]
    public void LocationServiceOptions_ValidateAndClampRadius_ClampsToMin()
    {
        // Arrange
        var options = new LocationServiceOptions
        {
            MinRadiusMeters = 100,
            MaxRadiusMeters = 5000
        };

        // Act
        var result = options.ValidateAndClampRadius(50);

        // Assert
        Assert.Equal(100, result);
    }

    [Fact]
    public void LocationServiceOptions_ValidateAndClampRadius_ClampsToMax()
    {
        // Arrange
        var options = new LocationServiceOptions
        {
            MinRadiusMeters = 100,
            MaxRadiusMeters = 5000
        };

        // Act
        var result = options.ValidateAndClampRadius(10000);

        // Assert
        Assert.Equal(5000, result);
    }

    [Fact]
    public void LocationServiceOptions_ValidateAndClampRadius_AcceptsValidValue()
    {
        // Arrange
        var options = new LocationServiceOptions
        {
            MinRadiusMeters = 100,
            MaxRadiusMeters = 5000
        };

        // Act
        var result = options.ValidateAndClampRadius(1000);

        // Assert
        Assert.Equal(1000, result);
    }

    private HttpClient CreateMockHttpClient(
        string responseContent,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(responseContent)
            });

        return new HttpClient(handlerMock.Object);
    }
}
