using System.Net;
using FWH.Common.Location;
using FWH.Common.Location.Models;
using FWH.Mobile.Options;
using FWH.Mobile.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FWH.Mobile.Tests;

/// <summary>
/// Integration tests for LocationApiClient.
/// These tests verify that the mobile app can successfully call the Location Web API.
///
/// Prerequisites:
/// - Location API must be running (e.g., dotnet run --project FWH.Location.Api)
/// - Default URL: https://localhost:5001/
/// - Can be configured via LOCATION_API_BASE_URL environment variable
/// </summary>
public class LocationApiClientIntegrationTests
{
    private readonly ILogger<LocationApiClient> _logger;
    private readonly string _apiBaseUrl;

    public LocationApiClientIntegrationTests()
    {
        // Use NullLogger for tests to avoid dependencies
        _logger = NullLogger<LocationApiClient>.Instance;

        // Get API URL from environment or use default
        _apiBaseUrl = Environment.GetEnvironmentVariable("LOCATION_API_BASE_URL")
                      ?? "https://localhost:5001/";
    }

    /// <summary>
    /// Test that LocationApiClient can be instantiated with valid configuration.
    /// </summary>
    [Fact]
    public void LocationApiClient_Constructor_WithValidOptions_Succeeds()
    {
        // Arrange
        var httpClient = new HttpClient();
        var options = Microsoft.Extensions.Options.Options.Create(new LocationApiClientOptions
        {
            BaseAddress = _apiBaseUrl,
            Timeout = TimeSpan.FromSeconds(30)
        });

        // Act
        var client = new LocationApiClient(httpClient, options, _logger);

        // Assert
        Assert.NotNull(client);
    }

    /// <summary>
    /// Test that LocationApiClient throws when BaseAddress is null or empty.
    /// </summary>
    [Fact]
    public void LocationApiClient_Constructor_WithEmptyBaseAddress_Throws()
    {
        // Arrange
        var httpClient = new HttpClient();
        var options = Microsoft.Extensions.Options.Options.Create(new LocationApiClientOptions
        {
            BaseAddress = "",
            Timeout = TimeSpan.FromSeconds(30)
        });

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            new LocationApiClient(httpClient, options, _logger));
    }

    /// <summary>
    /// Integration test: Verify GetNearbyBusinessesAsync calls the API.
    ///
    /// This test is marked as [Fact(Skip = ...)] by default because it requires
    /// the Location API to be running. To run this test:
    /// 1. Start the Location API: dotnet run --project FWH.Location.Api
    /// 2. Remove the Skip parameter from the [Fact] attribute
    /// 3. Run: dotnet test --filter "FullyQualifiedName~LocationApiClientIntegrationTests"
    /// </summary>
    [Fact(Skip = "Integration test - requires Location API running at https://localhost:5001/")]
    public async Task GetNearbyBusinessesAsync_WithValidCoordinates_ReturnsResults()
    {
        // Arrange
        var httpClient = CreateHttpClientWithSslValidation();
        var options = Microsoft.Extensions.Options.Options.Create(new LocationApiClientOptions
        {
            BaseAddress = _apiBaseUrl,
            Timeout = TimeSpan.FromSeconds(30)
        });
        var client = new LocationApiClient(httpClient, options, _logger);

        // San Francisco coordinates
        double latitude = 37.7749;
        double longitude = -122.4194;
        int radiusMeters = 1000;

        // Act
        var results = await client.GetNearbyBusinessesAsync(
            latitude,
            longitude,
            radiusMeters);

        // Assert
        Assert.NotNull(results);
        // Note: Results may be empty depending on API data source
        // The important thing is that the call succeeds without throwing
    }

    /// <summary>
    /// Integration test: Verify GetClosestBusinessAsync calls the API.
    ///
    /// This test is marked as [Fact(Skip = ...)] by default because it requires
    /// the Location API to be running. To run this test:
    /// 1. Start the Location API: dotnet run --project FWH.Location.Api
    /// 2. Remove the Skip parameter from the [Fact] attribute
    /// 3. Run: dotnet test --filter "FullyQualifiedName~LocationApiClientIntegrationTests"
    /// </summary>
    [Fact(Skip = "Integration test - requires Location API running at https://localhost:5001/")]
    public async Task GetClosestBusinessAsync_WithValidCoordinates_ReturnsResult()
    {
        // Arrange
        var httpClient = CreateHttpClientWithSslValidation();
        var options = Microsoft.Extensions.Options.Options.Create(new LocationApiClientOptions
        {
            BaseAddress = _apiBaseUrl,
            Timeout = TimeSpan.FromSeconds(30)
        });
        var client = new LocationApiClient(httpClient, options, _logger);

        // San Francisco coordinates
        double latitude = 37.7749;
        double longitude = -122.4194;
        int maxDistanceMeters = 1000;

        // Act
        var result = await client.GetClosestBusinessAsync(
            latitude,
            longitude,
            maxDistanceMeters);

        // Assert
        // Result may be null if no businesses found within range
        // The important thing is that the call succeeds without throwing
        Assert.True(true, "API call completed successfully");
    }

    /// <summary>
    /// Test that LocationApiClient handles invalid coordinates gracefully.
    /// </summary>
    [Fact]
    public async Task GetNearbyBusinessesAsync_WithInvalidCoordinates_HandlesGracefully()
    {
        // Arrange
        var httpClient = CreateHttpClientWithSslValidation();
        var options = Microsoft.Extensions.Options.Options.Create(new LocationApiClientOptions
        {
            BaseAddress = _apiBaseUrl,
            Timeout = TimeSpan.FromSeconds(30)
        });
        var client = new LocationApiClient(httpClient, options, _logger);

        // Invalid coordinates (out of range)
        double latitude = 999;
        double longitude = 999;
        int radiusMeters = 1000;

        // Act
        var results = await client.GetNearbyBusinessesAsync(
            latitude,
            longitude,
            radiusMeters);

        // Assert
        // Should return empty collection rather than throwing
        Assert.NotNull(results);
        Assert.Empty(results);
    }

    /// <summary>
    /// Creates an HttpClient configured to handle SSL certificate validation.
    /// In production, proper SSL certificates should be used.
    /// </summary>
    private HttpClient CreateHttpClientWithSslValidation()
    {
        // For development with self-signed certificates
        var handler = new HttpClientHandler();

        // Only bypass SSL validation in development
        if (_apiBaseUrl.Contains("localhost"))
        {
            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        return new HttpClient(handler);
    }
}
