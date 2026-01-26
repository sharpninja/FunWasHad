using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using FWH.Common.Location.Models;
using FWH.Location.Api.Data;
using Microsoft.Extensions.DependencyInjection;
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

    /// <summary>
    /// Tests that GET /api/locations/nearby returns OK status with business results when businesses are found within the radius.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The GET /api/locations/nearby endpoint's ability to query for nearby businesses and return results.</para>
    /// <para><strong>Data involved:</strong> Query parameters: latitude=1, longitude=2, radiusMeters=50. The ILocationService mock is configured to return a single BusinessLocation with Name="Test Place", Latitude=1, Longitude=2, Category="cafe". The mock allows any coordinate/radius values to be passed.</para>
    /// <para><strong>Why the data matters:</strong> Nearby business discovery is a core feature of the location API. The endpoint must correctly pass query parameters to the location service and return the results. Using a mock allows testing the controller logic without external API dependencies. The test data represents a typical nearby business query scenario.</para>
    /// <para><strong>Expected outcome:</strong> HTTP 200 OK status code with a JSON array containing exactly one BusinessLocation object with Name="Test Place".</para>
    /// <para><strong>Reason for expectation:</strong> When the location service returns results, the endpoint should return them as a JSON array with a 200 OK status. The response should match the service results exactly, allowing clients to display nearby businesses to users.</para>
    /// </remarks>
    [Fact]
    public async Task NearbyReturnsOkWithResults()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        var substitute = _factory.LocationServiceSubstitute;
        substitute
            .GetNearbyBusinessesAsync(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<int>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<BusinessLocation>>(new[]
            {
                new BusinessLocation { Name = "Test Place", Latitude = 1, Longitude = 2, Category = "cafe" }
            }));

        var response = await client.GetAsync(new Uri("/api/locations/nearby?latitude=1&longitude=2&radiusMeters=50", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<List<BusinessLocation>>();
        Assert.NotNull(payload);
        Assert.Single(payload!);
        Assert.Equal("Test Place", payload![0].Name);
    }

    /// <summary>
    /// Tests that GET /api/locations/nearby returns Bad Request when latitude is outside the valid range (-90 to 90 degrees).
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The GET /api/locations/nearby endpoint's input validation for latitude coordinate values.</para>
    /// <para><strong>Data involved:</strong> Query parameter latitude=999, which is outside the valid range of -90 to 90 degrees. Longitude=2 and radiusMeters=50 are valid. Invalid latitude values can cause calculation errors or security issues if not validated.</para>
    /// <para><strong>Why the data matters:</strong> Input validation is critical for API security and data integrity. Invalid coordinates can cause exceptions in distance calculations, database queries, or lead to unexpected behavior. The endpoint must validate coordinate ranges before processing to prevent errors and potential security issues.</para>
    /// <para><strong>Expected outcome:</strong> HTTP 400 Bad Request status code.</para>
    /// <para><strong>Reason for expectation:</strong> REST API best practices require validation of input parameters. Latitude must be between -90 and 90 degrees (valid range for Earth coordinates). Returning 400 Bad Request allows clients to correct invalid input, distinguishing it from server errors (500) or not found (404).</para>
    /// </remarks>
    [Fact]
    public async Task Nearby_InvalidLatitude_ReturnsBadRequest()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        var response = await client.GetAsync(new Uri("/api/locations/nearby?latitude=999&longitude=2&radiusMeters=50", UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Tests that GET /api/locations/closest returns OK status with the closest business when one is found within the maximum distance.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The GET /api/locations/closest endpoint's ability to find and return the single closest business to given coordinates.</para>
    /// <para><strong>Data involved:</strong> Query parameters: latitude=1, longitude=2, maxDistanceMeters=100. The ILocationService mock is configured to return a BusinessLocation with Name="Closest", Latitude=1, Longitude=2, Category="shop". The mock allows any coordinate/distance values.</para>
    /// <para><strong>Why the data matters:</strong> Finding the closest business is useful for features like "find nearest restaurant" or default business selection. Unlike the nearby endpoint which returns multiple results, this endpoint returns only the single closest match, which is more efficient for use cases that need just one result.</para>
    /// <para><strong>Expected outcome:</strong> HTTP 200 OK status code with a BusinessLocation JSON object with Name="Closest".</para>
    /// <para><strong>Reason for expectation:</strong> When the location service finds a closest business within the maximum distance, the endpoint should return it as a single object (not an array) with a 200 OK status. This allows clients to directly use the result without array indexing.</para>
    /// </remarks>
    [Fact]
    public async Task ClosestReturnsOkWhenFound()
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

    /// <summary>
    /// Tests that GET /api/locations/closest returns 404 Not Found when no business is found within the maximum distance.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The GET /api/locations/closest endpoint's handling of the case where no businesses are found within the specified maximum distance.</para>
    /// <para><strong>Data involved:</strong> Query parameters: latitude=1, longitude=2, maxDistanceMeters=100. The ILocationService mock is configured to return null, indicating no businesses were found within the maximum distance.</para>
    /// <para><strong>Why the data matters:</strong> In remote areas or when the maximum distance is too small, no businesses may be found. The endpoint must handle this gracefully by returning 404 Not Found rather than an empty result or error. This allows clients to distinguish between "no results" (404) and "server error" (500).</para>
    /// <para><strong>Expected outcome:</strong> HTTP 404 Not Found status code.</para>
    /// <para><strong>Reason for expectation:</strong> REST API best practices dictate that when a resource (closest business) cannot be found, the endpoint should return 404 Not Found. This is more semantically correct than returning 200 OK with null or empty data, as it clearly indicates the requested resource doesn't exist within the search criteria.</para>
    /// </remarks>
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

    /// <summary>
    /// Tests that POST /api/locations/confirmed persists location confirmation data and returns Created status.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The POST /api/locations/confirmed endpoint's ability to persist location confirmation records to the database.</para>
    /// <para><strong>Data involved:</strong> A request containing a BusinessLocation object (Name="Cafe Uno", Address="123 Main", Latitude=1.1, Longitude=2.2, Category="cafe") and user coordinates (latitude=1.2, longitude=2.3). The user coordinates differ slightly from the business coordinates, representing the user's actual location when confirming the business location.</para>
    /// <para><strong>Why the data matters:</strong> Location confirmations allow users to verify and correct business locations in the database. The user coordinates are stored separately from business coordinates to track where the user was when they made the confirmation, which is useful for data quality and validation. This test validates that both business and user location data are persisted correctly.</para>
    /// <para><strong>Expected outcome:</strong> HTTP 201 Created status code, and querying the database should reveal exactly one LocationConfirmation record with BusinessName="Cafe Uno", UserLatitude=1.2, and UserLongitude=2.3.</para>
    /// <para><strong>Reason for expectation:</strong> When a valid confirmation request is received, the endpoint should create a LocationConfirmation entity in the database with both the business information and the user's coordinates. The 201 Created status indicates successful resource creation. Querying the database after the request confirms the data was persisted correctly, validating the full request-to-database flow.</para>
    /// </remarks>
    [Fact]
    public async Task ConfirmedPersistsAndReturnsCreated()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

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

        // Query the database after the request completes to ensure we're using the same DbContext instance
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LocationDbContext>();
        var saved = db.LocationConfirmations.ToList();
        Assert.Single(saved);
        Assert.Equal("Cafe Uno", saved[0].BusinessName);
        Assert.Equal(1.2, saved[0].UserLatitude);
        Assert.Equal(2.3, saved[0].UserLongitude);
    }

}
