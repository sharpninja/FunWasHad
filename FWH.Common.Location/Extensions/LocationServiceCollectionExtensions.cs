using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using FWH.Common.Location.Configuration;
using FWH.Common.Location.Services;
using FWH.Common.Chat.Services;
using System;

namespace FWH.Common.Location.Extensions;

public static class LocationServiceCollectionExtensions
{
    /// <summary>
    /// Adds location services with database-backed configuration.
    /// </summary>
    public static IServiceCollection AddLocationServices(this IServiceCollection services)
    {
        // Register HttpClient for Overpass API
        services.AddHttpClient<OverpassLocationService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "FunWasHad/1.0");
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
        // Register HttpClient
        services.AddHttpClient<OverpassLocationService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "FunWasHad/1.0");
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
