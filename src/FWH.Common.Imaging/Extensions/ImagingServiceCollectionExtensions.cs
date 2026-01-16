using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;

namespace FWH.Common.Imaging.Extensions;

/// <summary>
/// Extension methods for registering imaging services with dependency injection.
/// </summary>
public static class ImagingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the imaging service implementations used by the application.
    /// </summary>
    /// <param name="services">Service collection to modify.</param>
    /// <returns>The original service collection for chaining.</returns>
    public static IServiceCollection AddImagingServices(this IServiceCollection services)
    {
        services.AddSingleton<FWH.Common.Imaging.IImagingService, FWH.Common.Imaging.ImagingService>();
        return services;
    }

    /// <summary>
    /// Registers a single imaging service implementation.
    /// </summary>
    public static IServiceCollection AddImagingService(this IServiceCollection services)
    {
        services.AddSingleton<FWH.Common.Imaging.IImagingService, FWH.Common.Imaging.ImagingService>();
        return services;
    }
}
