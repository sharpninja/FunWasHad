using System.Net;
using FWH.Mobile.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FWH.Mobile.Services;

/// <summary>
/// Represents the availability state of the Location API.
/// </summary>
public enum ApiAvailabilityState
{
    /// <summary>
    /// API is available and responding normally (200 OK).
    /// </summary>
    Available,

    /// <summary>
    /// API returned an error status code (404 or 5xx).
    /// </summary>
    Error,

    /// <summary>
    /// API is unreachable (timeout, network error, no HTTP response).
    /// </summary>
    Unreachable
}

/// <summary>
/// Background service that periodically checks if the Location API is available.
/// </summary>
public sealed class LocationApiHeartbeatService : BackgroundService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LocationApiHeartbeatService> _logger;
    private readonly LocationApiClientOptions _options;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(60); // Check every 60 seconds
    private readonly TimeSpan _requestTimeout = TimeSpan.FromSeconds(5); // 5 second timeout for health check

    public event EventHandler<ApiAvailabilityState>? AvailabilityChanged;

    private ApiAvailabilityState _availabilityState = ApiAvailabilityState.Available; // Assume available initially
    private readonly object _lock = new();

    public ApiAvailabilityState AvailabilityState
    {
        get
        {
            lock (_lock)
            {
                return _availabilityState;
            }
        }
        private set
        {
            ApiAvailabilityState changed;
            lock (_lock)
            {
                changed = _availabilityState;
                _availabilityState = value;
            }

            if (changed != value)
            {
                AvailabilityChanged?.Invoke(this, value);
            }
        }
    }

    /// <summary>
    /// Gets whether the API is available (for backward compatibility).
    /// </summary>
    public bool IsAvailable => AvailabilityState == ApiAvailabilityState.Available;

    public LocationApiHeartbeatService(
        IHttpClientFactory httpClientFactory,
        IOptions<LocationApiClientOptions> options,
        ILogger<LocationApiHeartbeatService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value ?? throw new ArgumentNullException(nameof(options.Value));

        _httpClient = httpClientFactory.CreateClient("LocationApi");
        _httpClient.Timeout = _requestTimeout;

        // Ensure BaseAddress is set
        if (_httpClient.BaseAddress == null && !string.IsNullOrWhiteSpace(_options.BaseAddress))
        {
            var baseAddress = _options.BaseAddress.EndsWith('/') ? _options.BaseAddress : _options.BaseAddress + "/";
            _httpClient.BaseAddress = new Uri(baseAddress, UriKind.Absolute);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Location API heartbeat service started. Checking immediately, then every {Interval} seconds.", _checkInterval.TotalSeconds);

        // Perform initial check immediately on startup
        try
        {
            await CheckApiAvailabilityAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking Location API availability on startup");
        }

        // Then check periodically
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_checkInterval, stoppingToken).ConfigureAwait(false);

            if (stoppingToken.IsCancellationRequested)
                break;

            try
            {
                await CheckApiAvailabilityAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking Location API availability");
            }
        }

        _logger.LogInformation("Location API heartbeat service stopped.");
    }

    private async Task CheckApiAvailabilityAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_requestTimeout);

            using var response = await _httpClient.GetAsync("health", cts.Token).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                AvailabilityState = ApiAvailabilityState.Available;
            }
            else
            {
                // Check if it's a 404 or 5xx error
                var statusCode = (int)response.StatusCode;
                if (statusCode == 404 || statusCode >= 500)
                {
                    _logger.LogWarning("Location API health check returned error status {StatusCode}", response.StatusCode);
                    AvailabilityState = ApiAvailabilityState.Error;
                }
                else
                {
                    // Other 4xx errors (like 401, 403) - treat as available since server is reachable
                    AvailabilityState = ApiAvailabilityState.Available;
                }
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogDebug("Location API health check timed out after {Timeout} seconds", _requestTimeout.TotalSeconds);
            AvailabilityState = ApiAvailabilityState.Unreachable;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogDebug(ex, "Location API health check failed: {Message}", ex.Message);
            AvailabilityState = ApiAvailabilityState.Unreachable;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error during Location API health check");
            AvailabilityState = ApiAvailabilityState.Unreachable;
        }
    }

}
