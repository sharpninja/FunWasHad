using Microsoft.Extensions.DependencyInjection;
using FWH.Common.Chat.Services;
using FWH.Mobile.iOS.Services;

namespace FWH.Mobile.iOS;

/// <summary>
/// Extension methods for registering iOS-specific services
/// </summary>
public static class iOSServiceCollectionExtensions
{
    /// <summary>
    /// Registers iOS-specific camera service
    /// </summary>
    public static IServiceCollection AddIOSCameraService(this IServiceCollection services)
    {
        // Register with "iOS" key for platform-specific resolution
        services.AddKeyedSingleton<ICameraService, iOSCameraService>("iOS");
        return services;
    }
}
