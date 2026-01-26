using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FWH.Common.Location;
using FWH.Common.Location.Models;
using FWH.Mobile.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FWH.Mobile.Services;

/// <summary>
/// Typed HttpClient that proxies requests to the Location Web API.
/// </summary>
public sealed class LocationApiClient : ILocationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LocationApiClient> _logger;
    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

    public LocationApiClient(HttpClient httpClient, IOptions<LocationApiClientOptions> options, ILogger<LocationApiClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentNullException.ThrowIfNull(options);

        var resolvedOptions = options.Value ?? throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(resolvedOptions.BaseAddress))
        {
            throw new InvalidOperationException("The Location API base address has not been configured.");
        }

        // Only set BaseAddress if not already configured by HttpClient factory
        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = EnsureTrailingSlash(resolvedOptions.BaseAddress);
        }

        // Update timeout if specified and different from default
        if (resolvedOptions.Timeout > TimeSpan.Zero && _httpClient.Timeout != resolvedOptions.Timeout)
        {
            _httpClient.Timeout = resolvedOptions.Timeout;
        }
    }

    public async Task<IEnumerable<BusinessLocation>> GetNearbyBusinessesAsync(
        double latitude,
        double longitude,
        int radiusMeters,
        IEnumerable<string>? categories = null,
        CancellationToken cancellationToken = default)
    {
        var requestUri = BuildNearbyUri(latitude, longitude, radiusMeters, categories);
        return await SendCollectionRequestAsync(requestUri, cancellationToken).ConfigureAwait(false);
    }

    public async Task<BusinessLocation?> GetClosestBusinessAsync(
        double latitude,
        double longitude,
        int maxDistanceMeters = 1000,
        CancellationToken cancellationToken = default)
    {
        var requestUri = BuildClosestUri(latitude, longitude, maxDistanceMeters);

        try
        {
            using var response = await _httpClient.GetAsync(new Uri(requestUri, UriKind.Relative), cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<BusinessLocation>(_serializerOptions, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Failed to fetch closest business from Location API.");
            return null;
        }
    }

    public async Task<string?> GetAddressAsync(
        double latitude,
        double longitude,
        int maxDistanceMeters = 500,
        CancellationToken cancellationToken = default)
    {
        // Try to get address from closest business first
        var closestBusiness = await GetClosestBusinessAsync(
            latitude,
            longitude,
            maxDistanceMeters,
            cancellationToken).ConfigureAwait(false);

        if (closestBusiness != null && !string.IsNullOrEmpty(closestBusiness.Address))
        {
            return closestBusiness.Address;
        }

        // Location API doesn't have a dedicated reverse geocoding endpoint yet
        // Return null to indicate address could not be determined
        // The OverpassLocationService implementation handles actual reverse geocoding
        return null;
    }

    /// <summary>
    /// Updates the device location on the server.
    /// </summary>
    /// <param name="deviceId">Unique device identifier</param>
    /// <param name="latitude">Device latitude</param>
    /// <param name="longitude">Device longitude</param>
    /// <param name="accuracyMeters">GPS accuracy in meters</param>
    /// <param name="timestamp">Timestamp when location was captured</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if update was successful</returns>
    public async Task<bool> UpdateDeviceLocationAsync(
        string deviceId,
        double latitude,
        double longitude,
        double? accuracyMeters = null,
        DateTimeOffset? timestamp = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new
            {
                DeviceId = deviceId,
                Latitude = latitude,
                Longitude = longitude,
                AccuracyMeters = accuracyMeters,
                Timestamp = timestamp ?? DateTimeOffset.UtcNow
            };

            using var response = await _httpClient.PostAsJsonAsync(
                "api/locations/device",
                request,
                _serializerOptions,
                cancellationToken).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Failed to update device location");
            return false;
        }
    }

    private async Task<IEnumerable<BusinessLocation>> SendCollectionRequestAsync(string requestUri, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(new Uri(requestUri, UriKind.Relative), cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var locations = await response.Content.ReadFromJsonAsync<List<BusinessLocation>>(_serializerOptions, cancellationToken).ConfigureAwait(false);
            return locations ?? Enumerable.Empty<BusinessLocation>();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Failed to fetch nearby businesses from Location API.");
            return Enumerable.Empty<BusinessLocation>();
        }
    }

    private static string BuildNearbyUri(double latitude, double longitude, int radiusMeters, IEnumerable<string>? categories)
    {
        var builder = new StringBuilder("api/locations/nearby?");
        AppendCoordinateParameters(builder, latitude, longitude);
        builder.Append("&radiusMeters=");
        builder.Append(radiusMeters.ToString(CultureInfo.InvariantCulture));

        if (categories != null)
        {
            foreach (var category in categories.Where(c => !string.IsNullOrWhiteSpace(c)))
            {
                builder.Append("&categories=");
                builder.Append(Uri.EscapeDataString(category));
            }
        }

        return builder.ToString();
    }

    private static string BuildClosestUri(double latitude, double longitude, int maxDistanceMeters)
    {
        var builder = new StringBuilder("api/locations/closest?");
        AppendCoordinateParameters(builder, latitude, longitude);
        builder.Append("&maxDistanceMeters=");
        builder.Append(maxDistanceMeters.ToString(CultureInfo.InvariantCulture));
        return builder.ToString();
    }

    private static void AppendCoordinateParameters(StringBuilder builder, double latitude, double longitude)
    {
        builder.Append("latitude=");
        builder.Append(latitude.ToString(CultureInfo.InvariantCulture));
        builder.Append("&longitude=");
        builder.Append(longitude.ToString(CultureInfo.InvariantCulture));
    }

    private static Uri EnsureTrailingSlash(string baseAddress)
    {
        var formatted = baseAddress.EndsWith('/') ? baseAddress : baseAddress + "/";
        return new Uri(formatted, UriKind.Absolute);
    }
}
