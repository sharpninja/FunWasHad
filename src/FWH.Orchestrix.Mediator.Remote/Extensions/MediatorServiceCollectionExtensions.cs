using Microsoft.Extensions.DependencyInjection;
using FWH.Orchestrix.Contracts.Mediator;
using FWH.Orchestrix.Mediator.Remote.Location;
using FWH.Orchestrix.Mediator.Remote.Marketing;
using FWH.Orchestrix.Mediator.Remote.Mediator;
using System.Net.Http;
using Polly;
using Polly.CircuitBreaker;
using Polly.Fallback;

namespace FWH.Orchestrix.Mediator.Remote.Extensions;

/// <summary>
/// Extension methods for registering remote mediator handlers.
/// </summary>
public static class MediatorServiceCollectionExtensions
{
    /// <summary>
    /// Adds remote mediator handlers for all APIs.
    /// </summary>
    public static IServiceCollection AddRemoteMediatorHandlers(this IServiceCollection services)
    {
        services.AddSingleton<IMediatorSender, ServiceProviderMediatorSender>();

        // Location
        services.AddTransient<IMediatorHandler<FWH.Orchestrix.Contracts.Location.UpdateDeviceLocationRequest, FWH.Orchestrix.Contracts.Location.UpdateDeviceLocationResponse>, UpdateDeviceLocationHandler>();
        services.AddTransient<IMediatorHandler<FWH.Orchestrix.Contracts.Location.GetDeviceLocationHistoryRequest, FWH.Orchestrix.Contracts.Location.GetDeviceLocationHistoryResponse>, GetDeviceLocationHistoryHandler>();
        services.AddTransient<IMediatorHandler<FWH.Orchestrix.Contracts.Location.GetNearbyBusinessesRequest, FWH.Orchestrix.Contracts.Location.GetNearbyBusinessesResponse>, GetNearbyBusinessesHandler>();

        // Marketing
        services.AddTransient<IMediatorHandler<FWH.Orchestrix.Contracts.Marketing.GetBusinessMarketingRequest, FWH.Orchestrix.Contracts.Marketing.GetBusinessMarketingResponse>, GetBusinessMarketingHandler>();
        services.AddTransient<IMediatorHandler<FWH.Orchestrix.Contracts.Marketing.GetBusinessThemeRequest, FWH.Orchestrix.Contracts.Marketing.GetBusinessThemeResponse>, GetBusinessThemeHandler>();
        services.AddTransient<IMediatorHandler<FWH.Orchestrix.Contracts.Marketing.GetBusinessCouponsRequest, FWH.Orchestrix.Contracts.Marketing.GetBusinessCouponsResponse>, GetBusinessCouponsHandler>();
        // Note: additional Marketing handlers (menu/news/nearby) should be registered here once implemented.
        services.AddTransient<IMediatorHandler<FWH.Orchestrix.Contracts.Marketing.SubmitFeedbackRequest, FWH.Orchestrix.Contracts.Marketing.SubmitFeedbackResponse>, SubmitFeedbackHandler>();
        services.AddTransient<IMediatorHandler<FWH.Orchestrix.Contracts.Marketing.UploadFeedbackAttachmentRequest, FWH.Orchestrix.Contracts.Marketing.UploadFeedbackAttachmentResponse>, UploadFeedbackAttachmentHandler>();

        return services;
    }

    /// <summary>
    /// Configures HTTP clients for API handlers.
    /// </summary>
    public static IServiceCollection AddApiHttpClients(
        this IServiceCollection services,
        Action<ApiClientOptions> configure)
    {
        var options = new ApiClientOptions();
        configure(options);

        // Register Location API client with resilience
        services.AddHttpClient("LocationApi", client =>
        {
            client.BaseAddress = new Uri(options.LocationApiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddResilienceHandler("location-api-pipeline", (ResiliencePipelineBuilder<HttpResponseMessage> builder) =>
        {
            // 1. Fallback (outer layer - executed last if all else fails)
            //builder.AddFallback(new FallbackStrategyOptions<HttpResponseMessage>
            //{
            //    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
            //        .Handle<HttpRequestException>()
            //        .Handle<TimeoutException>()
            //        .Handle<BrokenCircuitException>()
            //        .HandleResult(response => !response.IsSuccessStatusCode),
            //    FallbackAction = args =>
            //    {
            //        // Return an empty response depending on the endpoint
            //        var fallbackResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            //        {
            //            Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json")
            //        };
                    
            //        return Outcome.FromResultAsValueTask(fallbackResponse);
            //    }
            //});
            
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
            builder.AddTimeout(TimeSpan.FromSeconds(30));
        });

        // Register Marketing API client with resilience
        services.AddHttpClient("MarketingApi", client =>
        {
            client.BaseAddress = new Uri(options.MarketingApiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddResilienceHandler("marketing-api-pipeline", (ResiliencePipelineBuilder<HttpResponseMessage> builder) =>
        {
            // 1. Fallback (outer layer - executed last if all else fails)
            //builder.AddFallback(new FallbackStrategyOptions<HttpResponseMessage>
            //{
            //    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
            //        .Handle<HttpRequestException>()
            //        .Handle<TimeoutException>()
            //        .Handle<BrokenCircuitException>()
            //        .HandleResult(response => !response.IsSuccessStatusCode),
            //    FallbackAction = args =>
            //    {
            //        // Return an empty response
            //        var fallbackResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            //        {
            //            Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json")
            //        };
                    
            //        return Outcome.FromResultAsValueTask(fallbackResponse);
            //    }
            //});
            
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

        return services;
    }
}

/// <summary>
/// Options for API client configuration.
/// </summary>
public class ApiClientOptions
{
    public string LocationApiBaseUrl { get; set; } = "https://localhost:4747";
    public string MarketingApiBaseUrl { get; set; } = "https://localhost:4749";
}

