using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using FWH.Common.Location.Configuration;
using FWH.Common.Location.Services;
using FWH.Common.Chat.Services;
using System;
using System.Net.Http;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.CircuitBreaker;
using Polly.Fallback;

namespace FWH.Common.Location.Extensions;

public static class LocationServiceCollectionExtensions
{
    private const int Timeout_Seconds = 30;

    /// <summary>
    /// Adds location services with database-backed configuration.
    /// </summary>
    public static IServiceCollection AddLocationServices(this IServiceCollection services)
    {
        // Register HttpClient for Overpass API with custom resilience pipeline
        // that includes retry, circuit breaker, timeout
        services.AddHttpClient<OverpassLocationService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "FunWasHad/1.0");
        })
        .AddResilienceHandler("overpass-pipeline", (ResiliencePipelineBuilder<HttpResponseMessage> builder) =>
        {
            // 1. Circuit breaker (prevents cascading failures)
            builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                SamplingDuration = TimeSpan.FromSeconds(30),
                FailureRatio = 0.5,
                MinimumThroughput = 3,
                BreakDuration = TimeSpan.FromSeconds(15),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutException>()
                    .HandleResult(response => !response.IsSuccessStatusCode)
            });

            // 3. Retry with exponential backoff
            builder.AddRetry(new Polly.Retry.RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                BackoffType = Polly.DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromSeconds(1),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutException>()
                    .HandleResult(response => !response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
            });

            // 4. Timeout per attempt (inner layer - wraps each individual request)
            builder.AddTimeout(TimeSpan.FromSeconds(Timeout_Seconds));
        });

        // Register configuration service
        services.AddSingleton<LocationConfigurationService>();

        // Register options
        services.AddOptions<LocationServiceOptions>()
            .Configure<LocationConfigurationService>((options, configService) =>
            {
                var config = configService.LoadOptionsAsync().GetAwaiter().GetResult();
                options.DefaultRadiusMeters = config.DefaultRadiusMeters;
                options.MaxRadiusMeters = config.MaxRadiusMeters;
                options.MinRadiusMeters = config.MinRadiusMeters;
                options.TimeoutSeconds = config.TimeoutSeconds;
                options.UserAgent = config.UserAgent;
                options.OverpassApiUrl = config.OverpassApiUrl;
            });

        // Register the base service
        services.AddSingleton<OverpassLocationService>();

        // Register rate-limited decorator as the main ILocationService
        services.AddSingleton<ILocationService>(sp =>
        {
            var innerService = sp.GetRequiredService<OverpassLocationService>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RateLimitedLocationService>>();
            return new RateLimitedLocationService(innerService, logger, maxRequestsPerMinute: 10);
        });

        // Register GPS service factory and service
        // Note: Requires IPlatformService from FWH.Common.Chat to be registered first
        services.AddSingleton<GpsServiceFactory>();
        services.AddSingleton<IGpsService>(sp =>
        {
            var factory = sp.GetRequiredService<GpsServiceFactory>();
            return factory.CreateGpsService();
        });

        return services;
    }

    /// <summary>
    /// Adds location services with in-memory configuration (for testing).
    /// </summary>
    public static IServiceCollection AddLocationServicesWithInMemoryConfig(
        this IServiceCollection services,
        Action<LocationServiceOptions> configureOptions)
    {
        // Register HttpClient with custom resilience pipeline
        // that includes retry, circuit breaker, timeout, and fallback
        services.AddHttpClient<OverpassLocationService>(client =>
        {
            //client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "FunWasHad/1.0");
        })
        .AddResilienceHandler("overpass-pipeline", (ResiliencePipelineBuilder<HttpResponseMessage> builder) =>
        {
            // 1. Fallback (outer layer - executed last if all else fails)
            builder.AddFallback(new FallbackStrategyOptions<HttpResponseMessage>
            {
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutException>()
                    .Handle<BrokenCircuitException>()
                    .HandleResult(response => !response.IsSuccessStatusCode),
                FallbackAction = args =>
                {
                    // Return an empty JSON response that the service will interpret as no results
                    var fallbackResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                    {
                        Content = new StringContent(@"{""elements"":[]}", System.Text.Encoding.UTF8, "application/json")
                    };

                    return Outcome.FromResultAsValueTask(fallbackResponse);
                },
                OnFallback = args =>
                {
                    // Fallback triggered - return empty result set
                    return default;
                }
            });

            // 2. Circuit breaker (prevents cascading failures)
            builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                SamplingDuration = TimeSpan.FromSeconds(30),
                FailureRatio = 0.5,
                MinimumThroughput = 3,
                BreakDuration = TimeSpan.FromSeconds(15),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutException>()
                    .HandleResult(response => !response.IsSuccessStatusCode)
            });

            // 3. Retry with exponential backoff
            builder.AddRetry(new Polly.Retry.RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                BackoffType = Polly.DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromSeconds(1),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutException>()
                    .HandleResult(response => !response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
            });

            // 4. Timeout per attempt (inner layer - wraps each individual request)
            builder.AddTimeout(TimeSpan.FromSeconds(10));
        });

        // Configure options
        services.Configure(configureOptions);

        // Register the base service
        services.AddSingleton<OverpassLocationService>();

        // Register rate-limited decorator
        services.AddSingleton<ILocationService>(sp =>
        {
            var innerService = sp.GetRequiredService<OverpassLocationService>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RateLimitedLocationService>>();
            return new RateLimitedLocationService(innerService, logger, maxRequestsPerMinute: 10);
        });

        // Register GPS service factory and service
        services.AddSingleton<GpsServiceFactory>();
        services.AddSingleton<IGpsService>(sp =>
        {
            var factory = sp.GetRequiredService<GpsServiceFactory>();
            return factory.CreateGpsService();
        });

        return services;
    }
}
