using Microsoft.Extensions.DependencyInjection;
using FWH.Common.Location;
using FWH.Mobile.Desktop.Services;

namespace FWH.Mobile.Desktop;

/// <summary>
/// Extension methods for registering Desktop-specific services (Windows).
/// </summary>
public static class DesktopServiceCollectionExtensions
{
    /// <summary>
    /// Registers Windows-specific GPS service using Windows.Devices.Geolocation.
    /// Only works on Windows 10/11 with location capability enabled.
    /// </summary>
    public static IServiceCollection AddDesktopGpsService(this IServiceCollection services)
    {
        // Register with "Desktop" key for platform-specific resolution
        services.AddKeyedSingleton<IGpsService, WindowsGpsService>("Desktop");
        return services;
    }
}
