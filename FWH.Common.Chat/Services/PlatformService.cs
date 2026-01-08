using System;
using System.Runtime.InteropServices;

namespace FWH.Common.Chat.Services;

/// <summary>
/// Default platform detection service using runtime information
/// </summary>
public class PlatformService : IPlatformService
{
    private readonly PlatformType _platform;

    public PlatformService()
    {
        _platform = DetectPlatform();
    }

    public PlatformType Platform => _platform;
    
    public bool IsAndroid => _platform == PlatformType.Android;
    
    public bool IsIOS => _platform == PlatformType.iOS;
    
    public bool IsDesktop => _platform == PlatformType.Desktop;
    
    public bool IsBrowser => _platform == PlatformType.Browser;

    private static PlatformType DetectPlatform()
    {
        // Check for browser/WASM first
        if (RuntimeInformation.OSDescription.Contains("Browser", StringComparison.OrdinalIgnoreCase) ||
            OperatingSystem.IsBrowser())
        {
            return PlatformType.Browser;
        }

        // Check for Android
        if (OperatingSystem.IsAndroid())
        {
            return PlatformType.Android;
        }

        // Check for iOS/tvOS/watchOS/macCatalyst
        if (OperatingSystem.IsIOS() || 
            OperatingSystem.IsTvOS() || 
            OperatingSystem.IsWatchOS() ||
            OperatingSystem.IsMacCatalyst())
        {
            return PlatformType.iOS;
        }

        // Check for desktop platforms
        if (OperatingSystem.IsWindows() || 
            OperatingSystem.IsLinux() || 
            OperatingSystem.IsMacOS())
        {
            return PlatformType.Desktop;
        }

        return PlatformType.Unknown;
    }
}
