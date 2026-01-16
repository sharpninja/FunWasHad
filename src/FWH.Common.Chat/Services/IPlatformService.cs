namespace FWH.Common.Chat.Services;

/// <summary>
/// Service for detecting the current runtime platform
/// </summary>
public interface IPlatformService
{
    /// <summary>
    /// Gets the current platform type
    /// </summary>
    PlatformType Platform { get; }
    
    /// <summary>
    /// Checks if the current platform is Android
    /// </summary>
    bool IsAndroid { get; }
    
    /// <summary>
    /// Checks if the current platform is iOS
    /// </summary>
    bool IsIOS { get; }
    
    /// <summary>
    /// Checks if the current platform is Desktop
    /// </summary>
    bool IsDesktop { get; }
    
    /// <summary>
    /// Checks if the current platform is Browser/WASM
    /// </summary>
    bool IsBrowser { get; }
}

/// <summary>
/// Platform types supported by the application
/// </summary>
public enum PlatformType
{
    Unknown,
    Android,
    iOS,
    Desktop,
    Browser
}
