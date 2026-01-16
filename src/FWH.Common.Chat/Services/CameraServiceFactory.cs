using System;
using Microsoft.Extensions.DependencyInjection;

namespace FWH.Common.Chat.Services;

/// <summary>
/// Factory for creating platform-specific camera services
/// </summary>
public class CameraServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IPlatformService _platformService;

    public CameraServiceFactory(IServiceProvider serviceProvider, IPlatformService platformService)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _platformService = platformService ?? throw new ArgumentNullException(nameof(platformService));
    }

    /// <summary>
    /// Creates the appropriate camera service for the current platform
    /// </summary>
    public ICameraService CreateCameraService()
    {
        // Try to get platform-specific implementations from DI
        // The platform-specific projects will register their implementations with a key
        
        if (_platformService.IsAndroid)
        {
            // Try to get Android-specific service
            var androidService = _serviceProvider.GetKeyedService<ICameraService>("Android");
            if (androidService != null)
                return androidService;
        }
        else if (_platformService.IsIOS)
        {
            // Try to get iOS-specific service
            var iosService = _serviceProvider.GetKeyedService<ICameraService>("iOS");
            if (iosService != null)
                return iosService;
        }

        // Fallback to NoCameraService for desktop/browser/unknown platforms
        return new NoCameraService();
    }
}
