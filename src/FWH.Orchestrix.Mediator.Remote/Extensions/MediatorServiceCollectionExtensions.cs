using Microsoft.Extensions.DependencyInjection;
using FWH.Orchestrix.Contracts.Mediator;
using FWH.Orchestrix.Mediator.Remote.Location;
using FWH.Orchestrix.Mediator.Remote.Marketing;
using FWH.Orchestrix.Mediator.Remote.Mediator;

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

        // Register Location API client
        services.AddHttpClient("LocationApi", client =>
        {
            client.BaseAddress = new Uri(options.LocationApiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Register Marketing API client
        services.AddHttpClient("MarketingApi", client =>
        {
            client.BaseAddress = new Uri(options.MarketingApiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
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

