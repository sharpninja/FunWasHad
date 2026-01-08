using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;
using FWH.Common.Location.Services;
using FWH.Common.Location.Configuration;
using System.Linq;

namespace FWH.Common.Location.Tests;

/// <summary>
/// Tests for input validation and error handling in location services
/// </summary>
public class LocationServiceValidationTests
{
    private readonly Mock<ILogger<OverpassLocationService>> _mockLogger;

    public LocationServiceValidationTests()
    {
        _mockLogger = new Mock<ILogger<OverpassLocationService>>();
    }

    private LocationServiceOptions CreateDefaultOptions()
    {
        return new LocationServiceOptions
        {
            DefaultRadiusMeters = 30,
            MaxRadiusMeters = 5000,
            MinRadiusMeters = 50,
            TimeoutSeconds = 30,
            UserAgent = "FunWasHad/1.0",
            OverpassApiUrl = "https://overpass-api.de/api/interpreter"
        };
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

    [Theory]
    [InlineData(91.0, 0.0, 1000)]   // Latitude too high
    [InlineData(-91.0, 0.0, 1000)]  // Latitude too low
    [InlineData(0.0, 181.0, 1000)]  // Longitude too high
    [InlineData(0.0, -181.0, 1000)] // Longitude too low
    public async Task GetNearbyBusinessesAsync_InvalidCoordinates_ThrowsArgumentOutOfRangeException(
        double latitude, double longitude, int radius)
    {
        // Arrange
        var httpClient = CreateMockHttpClient(@"{""elements"": []}");
        var options = Options.Create(CreateDefaultOptions());
        var service = new OverpassLocationService(httpClient, _mockLogger.Object, options);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
        {
            await service.GetNearbyBusinessesAsync(latitude, longitude, radius);
        });
    }

    [Theory]
    [InlineData(90.0, 0.0, 1000)]   // North Pole
    [InlineData(-90.0, 0.0, 1000)]  // South Pole
    public async Task GetNearbyBusinessesAsync_ExtremeCoordinates_ReturnsEmptyGracefully(
        double latitude, double longitude, int radius)
    {
        // Arrange
        var httpClient = CreateMockHttpClient(@"{""elements"": []}");
        var options = Options.Create(CreateDefaultOptions());
        var service = new OverpassLocationService(httpClient, _mockLogger.Object, options);

        // Act
        var results = await service.GetNearbyBusinessesAsync(latitude, longitude, radius);

        // Assert - Should handle gracefully (likely no businesses at poles)
        Assert.NotNull(results);
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetNearbyBusinessesAsync_NegativeRadius_ThrowsOrClampsToMin()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(@"{""elements"": []}");
        var options = Options.Create(CreateDefaultOptions());
        var service = new OverpassLocationService(httpClient, _mockLogger.Object, options);

        // Act
        var exception = await Record.ExceptionAsync(async () =>
        {
            await service.GetNearbyBusinessesAsync(0.0, 0.0, -100); // Negative radius
        });

        // Assert - Should either throw or clamp to minimum
        if (exception != null)
        {
            Assert.IsType<ArgumentOutOfRangeException>(exception);
        }
    }

    [Fact]
    public async Task GetNearbyBusinessesAsync_ZeroRadius_ClampsToMinimum()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(@"{""elements"": []}");
        var options = Options.Create(CreateDefaultOptions());
        var service = new OverpassLocationService(httpClient, _mockLogger.Object, options);

        // Act - Zero radius should be clamped to minimum
        var results = await service.GetNearbyBusinessesAsync(0.0, 0.0, 0);

        // Assert - Should complete without error (clamped to min)
        Assert.NotNull(results);
    }

    [Theory]
    [InlineData("{ invalid json }")]                    // Malformed JSON
    [InlineData(@"{""version"": ""0.6""}")]           // Missing elements field
    public async Task GetNearbyBusinessesAsync_InvalidResponse_ReturnsEmpty(string responseContent)
    {
        // Arrange
        var httpClient = CreateMockHttpClient(responseContent);
        var options = Options.Create(CreateDefaultOptions());
        var service = new OverpassLocationService(httpClient, _mockLogger.Object, options);

        // Act
        var results = await service.GetNearbyBusinessesAsync(37.7749, -122.4194, 1000);

        // Assert - Should return empty list instead of throwing
        Assert.NotNull(results);
        Assert.Empty(results);
    }

    [Theory]
    [InlineData(true, false)]  // Missing latitude
    [InlineData(false, true)]  // Missing longitude
    public async Task GetNearbyBusinessesAsync_ElementMissingCoordinates_SkipsInvalidEntry(
        bool includeLat, bool includeLon)
    {
        // Arrange
        var latField = includeLat ? @"""lat"": 37.7749," : "";
        var lonField = includeLon ? @"""lon"": -122.4194," : "";
        
        var responseWithMissingFields = $@"{{
            ""elements"": [
                {{
                    ""type"": ""node"",
                    ""id"": 123,
                    {latField}
                    {lonField}
                    ""tags"": {{""name"": ""Invalid Business""}}
                }},
                {{
                    ""type"": ""node"",
                    ""id"": 456,
                    ""lat"": 37.7749,
                    ""lon"": -122.4194,
                    ""tags"": {{""name"": ""Valid Business""}}
                }}
            ]
        }}";
        
        var httpClient = CreateMockHttpClient(responseWithMissingFields);
        var options = Options.Create(CreateDefaultOptions());
        var service = new OverpassLocationService(httpClient, _mockLogger.Object, options);

        // Act
        var results = await service.GetNearbyBusinessesAsync(37.7749, -122.4194, 1000);
        var businesses = results.ToList();

        // Assert - Should only return the valid entry
        Assert.Single(businesses);
        Assert.Equal("Valid Business", businesses[0].Name);
    }

    [Fact]
    public async Task GetNearbyBusinessesAsync_VeryLargeResponse_HandlesEfficiently()
    {
        // Arrange - Create response with 2000 businesses
        var elementsJson = string.Join(",\n", Enumerable.Range(0, 2000).Select(i => $@"
                {{
                    ""type"": ""node"",
                    ""id"": {i},
                    ""lat"": {37.7749 + (i * 0.0001)},
                    ""lon"": {-122.4194 + (i * 0.0001)},
                    ""tags"": {{""name"": ""Business {i}"", ""amenity"": ""restaurant""}}
                }}"));
        
        var largeResponse = $@"{{""elements"": [{elementsJson}]}}";
        var httpClient = CreateMockHttpClient(largeResponse);
        var options = Options.Create(CreateDefaultOptions());
        var service = new OverpassLocationService(httpClient, _mockLogger.Object, options);

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var results = await service.GetNearbyBusinessesAsync(37.7749, -122.4194, 5000);
        sw.Stop();
        var businesses = results.ToList();

        // Assert
        Assert.Equal(2000, businesses.Count);
        Assert.True(sw.Elapsed.TotalSeconds < 5, $"Parsing took {sw.Elapsed.TotalSeconds} seconds (expected < 5s)");
    }

    [Fact]
    public async Task GetClosestBusinessAsync_InvalidCoordinates_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(@"{""elements"": []}");
        var options = Options.Create(CreateDefaultOptions());
        var service = new OverpassLocationService(httpClient, _mockLogger.Object, options);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
        {
            await service.GetClosestBusinessAsync(91.0, 0.0, 1000);
        });
    }

    [Fact]
    public async Task GetClosestBusinessAsync_NoBusinessesFound_ReturnsNull()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(@"{""elements"": []}");
        var options = Options.Create(CreateDefaultOptions());
        var service = new OverpassLocationService(httpClient, _mockLogger.Object, options);

        // Act
        var result = await service.GetClosestBusinessAsync(37.7749, -122.4194, 1000);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetNearbyBusinessesAsync_RequestTimeout_HandlesGracefully()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout"));

        var httpClient = new HttpClient(handlerMock.Object);
        var options = Options.Create(CreateDefaultOptions());
        var service = new OverpassLocationService(httpClient, _mockLogger.Object, options);

        // Act
        var results = await service.GetNearbyBusinessesAsync(37.7749, -122.4194, 1000);

        // Assert - Should return empty list instead of throwing
        Assert.NotNull(results);
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetNearbyBusinessesAsync_NetworkError_ReturnsEmpty()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(handlerMock.Object);
        var options = Options.Create(CreateDefaultOptions());
        var service = new OverpassLocationService(httpClient, _mockLogger.Object, options);

        // Act
        var results = await service.GetNearbyBusinessesAsync(37.7749, -122.4194, 1000);

        // Assert
        Assert.NotNull(results);
        Assert.Empty(results);
    }

    [Theory]
    [InlineData(-100, 50)]  // Negative radius returns minimum
    [InlineData(0, 50)]     // Zero radius returns minimum
    public void LocationServiceOptions_ValidateAndClampRadius_ReturnsExpectedValue(
        int inputRadius, int expectedRadius)
    {
        // Arrange
        var options = new LocationServiceOptions
        {
            MinRadiusMeters = 50,
            MaxRadiusMeters = 5000
        };

        // Act
        var result = options.ValidateAndClampRadius(inputRadius);

        // Assert
        Assert.Equal(expectedRadius, result);
    }
}
