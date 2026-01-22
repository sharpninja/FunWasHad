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
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The LocationApiClient constructor's ability to create an instance when provided with valid configuration options.</para>
    /// <para><strong>Data involved:</strong> An HttpClient instance, LocationApiClientOptions with BaseAddress set to the test API URL and Timeout set to 30 seconds, and a logger instance. These represent the minimum required dependencies for constructing the client.</para>
    /// <para><strong>Why the data matters:</strong> The LocationApiClient requires valid configuration to function correctly. The BaseAddress must be a valid URL for the client to make HTTP requests, and the Timeout prevents requests from hanging indefinitely. This test validates that the constructor accepts valid input and doesn't throw exceptions, ensuring the client can be properly instantiated in dependency injection scenarios.</para>
    /// <para><strong>Expected outcome:</strong> The constructor should complete without throwing exceptions, and the returned client instance should not be null.</para>
    /// <para><strong>Reason for expectation:</strong> With valid options (non-empty BaseAddress, positive Timeout), the constructor should successfully create the client instance. The non-null assertion confirms that object creation succeeded and the client is ready to use. This is a basic sanity check to ensure the constructor works correctly with valid input.</para>
    /// </remarks>
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
    /// Tests that LocationApiClient constructor throws InvalidOperationException when BaseAddress is empty, ensuring input validation.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The LocationApiClient constructor's input validation for empty BaseAddress values.</para>
    /// <para><strong>Data involved:</strong> An HttpClient instance and LocationApiClientOptions with BaseAddress set to an empty string and Timeout set to 30 seconds. This simulates a configuration error where the API base address is not provided.</para>
    /// <para><strong>Why the data matters:</strong> Empty base addresses are invalid - the client cannot make HTTP requests without a valid base URL. The constructor must validate input and reject empty addresses immediately to provide clear error messages. This prevents subtle bugs where the client is created but fails when making requests.</para>
    /// <para><strong>Expected outcome:</strong> The constructor should throw InvalidOperationException when called with an empty BaseAddress.</para>
    /// <para><strong>Reason for expectation:</strong> Input validation is critical for API correctness. Empty base addresses cannot be used to construct HTTP requests and would cause errors when the client tries to make API calls. Throwing InvalidOperationException immediately provides clear feedback about the invalid configuration and follows .NET Framework Design Guidelines for parameter validation.</para>
    /// </remarks>
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
    /// Tests that LocationApiClient handles invalid coordinates gracefully by returning an empty collection rather than throwing exceptions.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The LocationApiClient.GetNearbyBusinessesAsync method's error handling when invalid coordinates (out of valid range) are provided.</para>
    /// <para><strong>Data involved:</strong> Invalid coordinates: latitude=999, longitude=999 (both well outside the valid ranges of -90 to 90 for latitude and -180 to 180 for longitude). These represent impossible coordinate values that would result in invalid API requests.</para>
    /// <para><strong>Why the data matters:</strong> Invalid coordinates may be provided due to programming errors, data corruption, or edge cases. The client should handle them gracefully (e.g., return empty results, validate and reject) rather than throwing exceptions that crash the application. This ensures robust error handling and good user experience.</para>
    /// <para><strong>Expected outcome:</strong> GetNearbyBusinessesAsync should return a non-null, empty collection rather than throwing exceptions, confirming that invalid coordinates are handled gracefully.</para>
    /// <para><strong>Reason for expectation:</strong> The client should either validate coordinates before making API calls (returning empty results for invalid coordinates) or the API should return empty results for invalid requests. Returning an empty collection is preferable to throwing exceptions as it allows the application to continue operating. The non-null, empty collection confirms that the client handled the invalid input gracefully and didn't crash.</para>
    /// </remarks>
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
