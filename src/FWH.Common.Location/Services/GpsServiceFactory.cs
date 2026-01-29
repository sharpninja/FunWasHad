using FWH.Common.Chat.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FWH.Common.Location.Services;

/// <summary>
/// Factory for creating platform-specific GPS services.
/// Uses the same pattern as CameraServiceFactory.
/// </summary>
public class GpsServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IPlatformService _platformService;

    public GpsServiceFactory(IServiceProvider serviceProvider, IPlatformService platformService)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _platformService = platformService ?? throw new ArgumentNullException(nameof(platformService));
    }

    /// <summary>
    /// Creates the appropriate GPS service for the current platform.
    /// </summary>
    public IGpsService CreateGpsService()
    {
        // Try to get platform-specific implementations from DI
        // The platform-specific projects will register their implementations with a key

        if (_platformService.IsAndroid)
        {
            // Try to get Android-specific service
            var androidService = _serviceProvider.GetKeyedService<IGpsService>("Android");
            if (androidService != null)
                return androidService;
        }
        else if (_platformService.IsIOS)
        {
            // Try to get iOS-specific service
            var iosService = _serviceProvider.GetKeyedService<IGpsService>("iOS");
            if (iosService != null)
                return iosService;
        }
        else if (_platformService.IsDesktop)
        {
            // Try to get Desktop-specific service (Windows)
            var desktopService = _serviceProvider.GetKeyedService<IGpsService>("Desktop");
            if (desktopService != null)
                return desktopService;
        }

        // Fallback to NoGpsService for browser/unknown platforms
        return new NoGpsService();
    }
}
