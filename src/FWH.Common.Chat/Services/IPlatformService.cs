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

    /// <summary>
    /// Gets the platform-specific database directory path.
    /// Returns the appropriate directory for persistent storage on each platform.
    /// </summary>
    /// <param name="databaseName">The name of the database file (e.g., "notes.db")</param>
    /// <returns>Full path to the database file</returns>
    string GetDatabasePath(string databaseName);
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
