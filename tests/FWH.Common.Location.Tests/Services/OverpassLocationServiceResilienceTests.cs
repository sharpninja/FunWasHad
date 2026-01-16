using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FWH.Common.Location.Configuration;
using FWH.Common.Location.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace FWH.Common.Location.Tests.Services;

/// <summary>
/// Tests for OverpassLocationService resilience behavior with Polly retry policies.
/// </summary>
public sealed class OverpassLocationServiceResilienceTests
{
    [Fact]
    public async Task GetNearbyBusinessesAsync_WithTransientFailure_RetriesAndSucceeds()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        
        var options = Options.Create(new LocationServiceOptions
        {
            OverpassApiUrl = "https://overpass-api.de/api/interpreter",
            DefaultRadiusMeters = 1000,
            MaxRadiusMeters = 5000,
            MinRadiusMeters = 100
        });
        services.AddSingleton(options);

        // Create a mock HTTP handler that fails twice then succeeds
        var callCount = 0;
        var mockHandler = new MockHttpMessageHandler((request, ct) =>
        {
            callCount++;
            
            if (callCount <= 2)
            {
                // Simulate transient failure (e.g., 503 Service Unavailable)
                return Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.ServiceUnavailable,
                    Content = new StringContent("Service temporarily unavailable")
                });
            }

            // Third attempt succeeds
            var successResponse = @"{
                ""elements"": [
                    {
                        ""type"": ""node"",
                        ""id"": 1,
                        ""lat"": 47.6062,
                        ""lon"": -122.3321,
                        ""tags"": {
                            ""name"": ""Test Business"",
                            ""amenity"": ""restaurant""
                        }
                    }
                ]
            }";

            return Task.FromResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(successResponse)
            });
        });

        services.AddHttpClient<OverpassLocationService>()
            .ConfigurePrimaryHttpMessageHandler(() => mockHandler)
            .AddStandardResilienceHandler(options =>
            {
                // Configure retry to handle transient failures
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
                options.Retry.UseJitter = false; // Disable jitter for predictable test
                options.Retry.Delay = TimeSpan.FromMilliseconds(10); // Short delay for testing
                
                // Disable circuit breaker for this test
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromDays(1);
                
                // Increase timeouts
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);
            });

        var serviceProvider = services.BuildServiceProvider();
        var service = serviceProvider.GetRequiredService<OverpassLocationService>();

        // Act
        var businesses = await service.GetNearbyBusinessesAsync(
            latitude: 47.6062,
            longitude: -122.3321,
            radiusMeters: 1000);

        // Assert
        var businessList = businesses as System.Collections.Generic.List<Models.BusinessLocation> 
            ?? businesses.ToList();
        
        Assert.True(callCount >= 3, $"Expected at least 3 calls (with retries), but got {callCount}");
        Assert.NotEmpty(businessList);
        Assert.Equal("Test Business", businessList[0].Name);
    }

    [Fact]
    public async Task GetNearbyBusinessesAsync_WithValidCoordinates_ReturnsBusinesses()
    {
        // Arrange
        var services = new ServiceCollection();
        
        var logger = Substitute.For<ILogger<OverpassLocationService>>();
        services.AddSingleton(logger);
        
        var options = Options.Create(new LocationServiceOptions
        {
            OverpassApiUrl = "https://overpass-api.de/api/interpreter",
            DefaultRadiusMeters = 1000,
            MaxRadiusMeters = 5000,
            MinRadiusMeters = 100
        });
        services.AddSingleton(options);

        var mockHandler = new MockHttpMessageHandler((request, ct) =>
        {
            var successResponse = @"{
                ""elements"": [
                    {
                        ""type"": ""node"",
                        ""id"": 1,
                        ""lat"": 47.6062,
                        ""lon"": -122.3321,
                        ""tags"": {
                            ""name"": ""Pike Place Market"",
                            ""amenity"": ""marketplace"",
                            ""addr:street"": ""Pike Street"",
                            ""addr:city"": ""Seattle""
                        }
                    }
                ]
            }";

            return Task.FromResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(successResponse)
            });
        });

        services.AddHttpClient<OverpassLocationService>()
            .ConfigurePrimaryHttpMessageHandler(() => mockHandler);

        var serviceProvider = services.BuildServiceProvider();
        var service = serviceProvider.GetRequiredService<OverpassLocationService>();

        // Act
        var businesses = await service.GetNearbyBusinessesAsync(
            latitude: 47.6062,
            longitude: -122.3321,
            radiusMeters: 1000);

        // Assert
        var businessList = businesses as System.Collections.Generic.List<Models.BusinessLocation> 
            ?? businesses.ToList();
        Assert.Single(businessList);
        Assert.Equal("Pike Place Market", businessList[0].Name);
        Assert.Equal("marketplace", businessList[0].Category);
    }

    [Fact]
    public async Task GetNearbyBusinessesAsync_WithInvalidLatitude_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var services = new ServiceCollection();
        var logger = Substitute.For<ILogger<OverpassLocationService>>();
        services.AddSingleton(logger);
        
        var options = Options.Create(new LocationServiceOptions
        {
            OverpassApiUrl = "https://overpass-api.de/api/interpreter",
            DefaultRadiusMeters = 1000,
            MaxRadiusMeters = 5000,
            MinRadiusMeters = 100
        });
        services.AddSingleton(options);

        var mockHandler = new MockHttpMessageHandler((request, ct) =>
            Task.FromResult(new HttpResponseMessage { StatusCode = HttpStatusCode.OK }));

        services.AddHttpClient<OverpassLocationService>()
            .ConfigurePrimaryHttpMessageHandler(() => mockHandler);

        var serviceProvider = services.BuildServiceProvider();
        var service = serviceProvider.GetRequiredService<OverpassLocationService>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await service.GetNearbyBusinessesAsync(
                latitude: 91.0, // Invalid latitude
                longitude: -122.3321,
                radiusMeters: 1000));
    }

    [Fact]
    public async Task GetNearbyBusinessesAsync_WithInvalidLongitude_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var services = new ServiceCollection();
        var logger = Substitute.For<ILogger<OverpassLocationService>>();
        services.AddSingleton(logger);
        
        var options = Options.Create(new LocationServiceOptions
        {
            OverpassApiUrl = "https://overpass-api.de/api/interpreter",
            DefaultRadiusMeters = 1000,
            MaxRadiusMeters = 5000,
            MinRadiusMeters = 100
        });
        services.AddSingleton(options);

        var mockHandler = new MockHttpMessageHandler((request, ct) =>
            Task.FromResult(new HttpResponseMessage { StatusCode = HttpStatusCode.OK }));

        services.AddHttpClient<OverpassLocationService>()
            .ConfigurePrimaryHttpMessageHandler(() => mockHandler);

        var serviceProvider = services.BuildServiceProvider();
        var service = serviceProvider.GetRequiredService<OverpassLocationService>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await service.GetNearbyBusinessesAsync(
                latitude: 47.6062,
                longitude: 181.0, // Invalid longitude
                radiusMeters: 1000));
    }

    /// <summary>
    /// Mock HTTP message handler for testing
    /// </summary>
    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _sendAsync;

        public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync)
        {
            _sendAsync = sendAsync;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _sendAsync(request, cancellationToken);
        }
    }
}
