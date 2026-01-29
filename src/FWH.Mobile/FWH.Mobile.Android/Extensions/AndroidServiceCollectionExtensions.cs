using FWH.Common.Chat.Services;
using FWH.Common.Location;
using FWH.Mobile.Android.Services;
using FWH.Mobile.Droid.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FWH.Mobile.Android;

/// <summary>
/// Extension methods for registering Android-specific services
/// </summary>
public static class AndroidServiceCollectionExtensions
{
    /// <summary>
    /// Registers Android-specific camera service
    /// </summary>
    public static IServiceCollection AddAndroidCameraService(this IServiceCollection services)
    {
        // Register with "Android" key for platform-specific resolution
        services.AddKeyedSingleton<ICameraService, AndroidCameraService>("Android");
        return services;
    }

    /// <summary>
    /// Registers Android-specific GPS service
    /// </summary>
    public static IServiceCollection AddAndroidGpsService(this IServiceCollection services)
    {
        // Register with "Android" key for platform-specific resolution
        services.AddKeyedSingleton<IGpsService, AndroidGpsService>("Android");
        return services;
    }
}
