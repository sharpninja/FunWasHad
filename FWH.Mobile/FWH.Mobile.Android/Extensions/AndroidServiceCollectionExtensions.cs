using Android.App;
using Microsoft.Extensions.DependencyInjection;
using FWH.Common.Chat.Services;
using FWH.Mobile.Droid.Services;

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
}
