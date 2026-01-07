using Microsoft.Extensions.DependencyInjection;
using FWH.Common.Location.Services;
using FWH.Common.Location.Configuration;

namespace FWH.Common.Location.Extensions;

/// <summary>
/// Extension methods for registering location services with dependency injection.
/// Single Responsibility: Configure DI for location components.
/// </summary>
public static class LocationServiceCollectionExtensions
{
    /// <summary>
    /// Adds location services using OpenStreetMap's Overpass API with configuration from database.
    /// This is a free service with no API key required.
    /// Configuration is persisted to SQLite and loaded at startup.
    /// Default radius: 30 meters.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLocationServices(this IServiceCollection services)
    {
        // Register configuration service
        services.AddSingleton<LocationConfigurationService>();

        // Configure options from database
        services.AddOptions<LocationServiceOptions>()
            .Configure<LocationConfigurationService>(async (options, configService) =>
            {
                var loadedOptions = await configService.LoadOptionsAsync();
                options.DefaultRadiusMeters = loadedOptions.DefaultRadiusMeters;
                options.MaxRadiusMeters = loadedOptions.MaxRadiusMeters;
                options.MinRadiusMeters = loadedOptions.MinRadiusMeters;
                options.TimeoutSeconds = loadedOptions.TimeoutSeconds;
                options.UserAgent = loadedOptions.UserAgent;
                options.OverpassApiUrl = loadedOptions.OverpassApiUrl;
            });

        // Register HttpClient with factory
        services.AddHttpClient<ILocationService, OverpassLocationService>((serviceProvider, client) =>
        {
            // Note: Options will be loaded from database via the configuration above
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<LocationServiceOptions>>().Value;
            
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("User-Agent", options.UserAgent);
        });

        return services;
    }

    /// <summary>
    /// Adds location services with custom in-memory configuration (for testing or override).
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureOptions">Action to configure the location service options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLocationServicesWithInMemoryConfig(
        this IServiceCollection services,
        Action<LocationServiceOptions> configureOptions)
    {
        // Configure options in memory (not persisted)
        services.Configure(configureOptions);

        // Register HttpClient with factory
        services.AddHttpClient<ILocationService, OverpassLocationService>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<LocationServiceOptions>>().Value;
            
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("User-Agent", options.UserAgent);
        });

        return services;
    }
}
