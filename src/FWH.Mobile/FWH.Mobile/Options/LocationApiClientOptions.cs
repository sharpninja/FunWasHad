namespace FWH.Mobile.Options;

/// <summary>
/// Options for connecting to the Location Web API.
/// </summary>
public sealed class LocationApiClientOptions
{
    /// <summary>
    /// Base address for the Location API.
    /// Default is platform-specific:
    /// - Android: http://10.0.2.2:4748/ (emulator's host machine alias + HTTP port)
    /// - Other platforms: https://localhost:4747/ (HTTPS port)
    /// 
    /// For Android physical devices, set LOCATION_API_BASE_URL environment variable
    /// to your machine's IP address (e.g., http://192.168.1.100:4748/)
    /// </summary>
    public string BaseAddress { get; set; } = GetDefaultBaseAddress();

    /// <summary>
    /// HTTP timeout for API calls.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    private static string GetDefaultBaseAddress()
    {
        // Detect platform at runtime instead of compile-time
        if (OperatingSystem.IsAndroid())
        {
            // Android emulator: 10.0.2.2 is special alias for host machine's localhost
            // Use HTTP port 4748 (not HTTPS 4747) to avoid certificate issues in development
            return "http://10.0.2.2:4748/";
        }
        else
        {
            // Desktop/iOS: Use HTTPS with actual port where API is running
            return "https://localhost:4747/";
        }
    }
}
