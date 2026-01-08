using System;

namespace FWH.Mobile.Options;

/// <summary>
/// Options for connecting to the Location Web API.
/// </summary>
public sealed class LocationApiClientOptions
{
    /// <summary>
    /// Base address for the Location API (e.g., https://localhost:5001/).
    /// </summary>
    public string BaseAddress { get; set; } = "https://localhost:5001/";

    /// <summary>
    /// HTTP timeout for API calls.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
