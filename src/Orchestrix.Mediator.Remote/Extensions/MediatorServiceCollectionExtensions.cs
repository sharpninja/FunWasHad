using Microsoft.Extensions.DependencyInjection;
using Orchestrix.Contracts.Mediator;
using Orchestrix.Mediator.Remote.Location;
using Orchestrix.Mediator.Remote.Marketing;
using Orchestrix.Mediator.Remote.Mediator;

namespace Orchestrix.Mediator.Remote.Extensions;

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
        services.AddTransient<IMediatorHandler<Orchestrix.Contracts.Location.UpdateDeviceLocationRequest, Orchestrix.Contracts.Location.UpdateDeviceLocationResponse>, UpdateDeviceLocationHandler>();
        services.AddTransient<IMediatorHandler<Orchestrix.Contracts.Location.GetDeviceLocationHistoryRequest, Orchestrix.Contracts.Location.GetDeviceLocationHistoryResponse>, GetDeviceLocationHistoryHandler>();
        services.AddTransient<IMediatorHandler<Orchestrix.Contracts.Location.GetNearbyBusinessesRequest, Orchestrix.Contracts.Location.GetNearbyBusinessesResponse>, GetNearbyBusinessesHandler>();

        // Marketing
        services.AddTransient<IMediatorHandler<Orchestrix.Contracts.Marketing.GetBusinessMarketingRequest, Orchestrix.Contracts.Marketing.GetBusinessMarketingResponse>, GetBusinessMarketingHandler>();
        services.AddTransient<IMediatorHandler<Orchestrix.Contracts.Marketing.GetBusinessThemeRequest, Orchestrix.Contracts.Marketing.GetBusinessThemeResponse>, GetBusinessThemeHandler>();
        services.AddTransient<IMediatorHandler<Orchestrix.Contracts.Marketing.GetBusinessCouponsRequest, Orchestrix.Contracts.Marketing.GetBusinessCouponsResponse>, GetBusinessCouponsHandler>();
        // Note: additional Marketing handlers (menu/news/nearby) should be registered here once implemented.
        services.AddTransient<IMediatorHandler<Orchestrix.Contracts.Marketing.SubmitFeedbackRequest, Orchestrix.Contracts.Marketing.SubmitFeedbackResponse>, SubmitFeedbackHandler>();
        services.AddTransient<IMediatorHandler<Orchestrix.Contracts.Marketing.UploadFeedbackAttachmentRequest, Orchestrix.Contracts.Marketing.UploadFeedbackAttachmentResponse>, UploadFeedbackAttachmentHandler>();

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

