using System.Text.Json;
using System.Text.Json.Serialization;
using FWH.Common.Location.Configuration;
using FWH.Common.Location.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FWH.Common.Location.Services;

/// <summary>
/// Location service implementation using OpenStreetMap's Overpass API.
/// This is a free, open-source solution with no API key required.
/// Single Responsibility: Query Overpass API for nearby POIs.
/// </summary>
public class OverpassLocationService : ILocationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OverpassLocationService> _logger;
    private readonly LocationServiceOptions _options;
    private readonly string _overpassApiUrl;

    public OverpassLocationService(
        HttpClient httpClient,
        ILogger<OverpassLocationService> logger,
        IOptions<LocationServiceOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _overpassApiUrl = _options.OverpassApiUrl;
    }

    public async Task<IEnumerable<BusinessLocation>> GetNearbyBusinessesAsync(
        double latitude,
        double longitude,
        int radiusMeters,
        IEnumerable<string>? categories = null,
        CancellationToken cancellationToken = default)
    {
        // Validate coordinates
        ValidateCoordinates(latitude, longitude);

        try
        {
            // Validate and clamp radius
            var validatedRadius = _options.ValidateAndClampRadius(radiusMeters);
            if (validatedRadius != radiusMeters)
            {
                _logger.LogWarning(
                    "Requested radius {RequestedRadius}m clamped to {ValidatedRadius}m (min: {Min}m, max: {Max}m)",
                    radiusMeters, validatedRadius, _options.MinRadiusMeters, _options.MaxRadiusMeters);
            }

            var query = BuildOverpassQuery(latitude, longitude, validatedRadius, categories, _logger);
            _logger.LogDebug("Querying Overpass API: lat={Lat}, lon={Lon}, radius={Radius}m",
                latitude, longitude, validatedRadius);

            var content = new StringContent(query);

            // The HttpClient is configured with resilience policies (retry, circuit breaker, timeout)
            // in LocationServiceCollectionExtensions, so transient failures will be automatically retried
            var response = await _httpClient.PostAsync(new Uri(_overpassApiUrl), content, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Overpass API returned status code: {StatusCode} after resilience policies applied",
                    response.StatusCode);
                return Enumerable.Empty<BusinessLocation>();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);
            var result = JsonSerializer.Deserialize<OverpassResponse>(json);

            if (result?.Elements == null)
            {
                return Enumerable.Empty<BusinessLocation>();
            }

            var businesses = result.Elements
                .Where(e => e.Tags != null && !string.IsNullOrEmpty(e.Tags.GetValueOrDefault("name")))
                .Where(e => (e.Lat.HasValue || e.Center?.Lat != null) && (e.Lon.HasValue || e.Center?.Lon != null)) // Filter out entries missing coordinates
                .Select(e => ConvertToBusinessLocation(e, latitude, longitude))
                .OrderBy(b => b.DistanceMeters)
                .ThenBy(b => b.Name)
                .ToList();

            _logger.LogInformation("Found {Count} businesses near ({Lat}, {Lon}) within {Radius}m",
                businesses.Count, latitude, longitude, validatedRadius);

            return businesses;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching nearby businesses from Overpass API");
            return Enumerable.Empty<BusinessLocation>();
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Request timeout fetching nearby businesses from Overpass API");
            return Enumerable.Empty<BusinessLocation>();
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Fetching nearby businesses was cancelled");
            return Enumerable.Empty<BusinessLocation>();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error fetching nearby businesses from Overpass API");
            return Enumerable.Empty<BusinessLocation>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching nearby businesses from Overpass API");
            return Enumerable.Empty<BusinessLocation>();
        }
    }

    public async Task<BusinessLocation?> GetClosestBusinessAsync(
        double latitude,
        double longitude,
        int maxDistanceMeters = 1000,
        CancellationToken cancellationToken = default)
    {
        // Validate coordinates
        ValidateCoordinates(latitude, longitude);

        var businesses = await GetNearbyBusinessesAsync(
            latitude,
            longitude,
            maxDistanceMeters,
            null,
            cancellationToken).ConfigureAwait(false);

        return businesses.FirstOrDefault();
    }

    public async Task<string?> GetAddressAsync(
        double latitude,
        double longitude,
        int maxDistanceMeters = 500,
        CancellationToken cancellationToken = default)
    {
        // Validate coordinates
        ValidateCoordinates(latitude, longitude);

        try
        {
            // First try to get address from a nearby business
            var closestBusiness = await GetClosestBusinessAsync(
                latitude,
                longitude,
                maxDistanceMeters,
                cancellationToken).ConfigureAwait(false);

            if (closestBusiness != null && !string.IsNullOrEmpty(closestBusiness.Address))
            {
                return closestBusiness.Address;
            }

            // If no business found, query for any address data (nodes/ways with address tags)
            var validatedRadius = _options.ValidateAndClampRadius(maxDistanceMeters);
            var query = BuildAddressQuery(latitude, longitude, validatedRadius);
            _logger.LogDebug("Querying Overpass API for address: lat={Lat}, lon={Lon}, radius={Radius}m",
                latitude, longitude, validatedRadius);

            var content = new StringContent(query);
            var response = await _httpClient.PostAsync(new Uri(_overpassApiUrl), content, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Overpass API returned status code: {StatusCode} for address query",
                    response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);
            var result = JsonSerializer.Deserialize<OverpassResponse>(json);

            if (result?.Elements == null || !result.Elements.Any())
            {
                return null;
            }

            // Find the closest element with address data
            var addressElements = result.Elements
                .Where(e => e.Tags != null && HasAddressTags(e.Tags))
                .Where(e => (e.Lat.HasValue || e.Center?.Lat != null) && (e.Lon.HasValue || e.Center?.Lon != null))
                .Select(e => new
                {
                    Element = e,
                    Distance = CalculateDistance(
                        latitude,
                        longitude,
                        e.Lat ?? e.Center?.Lat ?? 0,
                        e.Lon ?? e.Center?.Lon ?? 0)
                })
                .OrderBy(x => x.Distance)
                .ToList();

            var closestElement = addressElements.FirstOrDefault();
            if (closestElement != null)
            {
                var address = BuildAddressFromTags(closestElement.Element.Tags!);
                if (!string.IsNullOrEmpty(address))
                {
                    _logger.LogDebug("Found address: {Address} at distance {Distance}m", address, closestElement.Distance);
                    return address;
                }
            }

            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching address from Overpass API");
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Request timeout fetching address from Overpass API");
            return null;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Fetching address was cancelled");
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error fetching address from Overpass API");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching address from Overpass API");
            return null;
        }
    }

    private static void ValidateCoordinates(double latitude, double longitude)
    {
        if (latitude < -90 || latitude > 90)
        {
            throw new ArgumentOutOfRangeException(
                nameof(latitude),
                latitude,
                "Latitude must be between -90 and 90 degrees");
        }

        if (longitude < -180 || longitude > 180)
        {
            throw new ArgumentOutOfRangeException(
                nameof(longitude),
                longitude,
                "Longitude must be between -180 and 180 degrees");
        }
    }

    private static string BuildOverpassQuery(
        double latitude,
        double longitude,
        int radiusMeters,
        IEnumerable<string>? categories,
        ILogger<OverpassLocationService>? logger = null)
    {
        var categoryFilters = categories?.ToList() ?? new List<string>();

        // Build Overpass QL query
        var filters = new List<string>();

        if (categoryFilters.Any())
        {
            foreach (var category in categoryFilters)
            {
                // Sanitize category to prevent injection: only allow alphanumeric, hyphens, underscores, and colons
                // (colons are used in OSM tag keys like "addr:street")
                var sanitizedCategory = SanitizeCategory(category);
                if (string.IsNullOrEmpty(sanitizedCategory))
                {
                    logger?.LogWarning("Invalid category '{Category}' filtered out due to unsafe characters", category);
                    continue;
                }

                filters.Add($"node[\"amenity\"=\"{sanitizedCategory}\"](around:{radiusMeters},{latitude},{longitude});");
                filters.Add($"way[\"amenity\"=\"{sanitizedCategory}\"](around:{radiusMeters},{latitude},{longitude});");
                filters.Add($"node[\"shop\"=\"{sanitizedCategory}\"](around:{radiusMeters},{latitude},{longitude});");
                filters.Add($"way[\"shop\"=\"{sanitizedCategory}\"](around:{radiusMeters},{latitude},{longitude});");
            }
        }
        else
        {
            filters.Add($"node[\"amenity\"](around:{radiusMeters},{latitude},{longitude});");
            filters.Add($"way[\"amenity\"](around:{radiusMeters},{latitude},{longitude});");
            filters.Add($"node[\"shop\"](around:{radiusMeters},{latitude},{longitude});");
            filters.Add($"way[\"shop\"](around:{radiusMeters},{latitude},{longitude});");
            filters.Add($"node[\"tourism\"](around:{radiusMeters},{latitude},{longitude});");
            filters.Add($"way[\"tourism\"](around:{radiusMeters},{latitude},{longitude});");
        }

        var query = $@"
[out:json][timeout:25];
(
  {string.Join("\n  ", filters)}
);
out center;
";

        return query;
    }

    /// <summary>
    /// Sanitizes a category string to prevent injection attacks in Overpass queries.
    /// Only allows alphanumeric characters, hyphens, underscores, and colons.
    /// </summary>
    /// <param name="category">The category string to sanitize</param>
    /// <returns>Sanitized category string, or empty string if invalid</returns>
    private static string SanitizeCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return string.Empty;

        // Only allow alphanumeric, hyphens, underscores, and colons (for OSM tag keys like "addr:street")
        // This prevents injection of Overpass QL syntax like quotes, brackets, semicolons, etc.
        var sanitized = new System.Text.StringBuilder(category.Length);
        foreach (var c in category)
        {
            if (char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == ':')
            {
                sanitized.Append(c);
            }
        }

        var result = sanitized.ToString();
        // Additional validation: ensure it's not empty and doesn't start/end with colon
        if (string.IsNullOrEmpty(result) || result.StartsWith(':') || result.EndsWith(':'))
        {
            return string.Empty;
        }

        return result;
    }

    private static string BuildAddressQuery(
        double latitude,
        double longitude,
        int radiusMeters)
    {
        // Query for any nodes/ways with address tags
        var query = $@"
[out:json][timeout:25];
(
  node[""addr:street""](around:{radiusMeters},{latitude},{longitude});
  way[""addr:street""](around:{radiusMeters},{latitude},{longitude});
  node[""addr:housenumber""](around:{radiusMeters},{latitude},{longitude});
  way[""addr:housenumber""](around:{radiusMeters},{latitude},{longitude});
);
out center;
";

        return query;
    }

    private static bool HasAddressTags(Dictionary<string, string> tags)
    {
        return tags.ContainsKey("addr:street") ||
               tags.ContainsKey("addr:housenumber") ||
               tags.ContainsKey("addr:city") ||
               tags.ContainsKey("addr:postcode");
    }

    private static string? BuildAddressFromTags(Dictionary<string, string> tags)
    {
        var addressParts = new List<string>();
        if (tags.TryGetValue("addr:housenumber", out var houseNumber))
            addressParts.Add(houseNumber);
        if (tags.TryGetValue("addr:street", out var street))
            addressParts.Add(street);
        if (tags.TryGetValue("addr:city", out var city))
            addressParts.Add(city);
        if (tags.TryGetValue("addr:postcode", out var postcode))
            addressParts.Add(postcode);

        return addressParts.Any() ? string.Join(", ", addressParts) : null;
    }

    private static BusinessLocation ConvertToBusinessLocation(
        OverpassElement element,
        double searchLat,
        double searchLon)
    {
        var tags = element.Tags ?? new Dictionary<string, string>();
        var name = tags.GetValueOrDefault("name", "Unnamed Location");

        // Build address
        var addressParts = new List<string>();
        if (tags.TryGetValue("addr:housenumber", out var houseNumber))
            addressParts.Add(houseNumber);
        if (tags.TryGetValue("addr:street", out var street))
            addressParts.Add(street);
        if (tags.TryGetValue("addr:city", out var city))
            addressParts.Add(city);
        if (tags.TryGetValue("addr:postcode", out var postcode))
            addressParts.Add(postcode);

        var address = addressParts.Any() ? string.Join(", ", addressParts) : null;

        // Determine category
        var category = tags.GetValueOrDefault("amenity")
                    ?? tags.GetValueOrDefault("shop")
                    ?? tags.GetValueOrDefault("tourism")
                    ?? "unknown";

        // Get coordinates
        var lat = element.Lat ?? element.Center?.Lat ?? 0;
        var lon = element.Lon ?? element.Center?.Lon ?? 0;

        // Calculate distance
        var distance = CalculateDistance(searchLat, searchLon, lat, lon);

        return new BusinessLocation
        {
            Name = name,
            Address = address,
            Latitude = lat,
            Longitude = lon,
            Category = category,
            Tags = tags,
            DistanceMeters = distance
        };
    }

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Haversine formula
        const double earthRadiusMeters = 6371000;

        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadiusMeters * c;
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }

    #region Overpass API Response Models

    private class OverpassResponse
    {
        [JsonPropertyName("elements")]
        public List<OverpassElement>? Elements { get; set; }
    }

    private class OverpassElement
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("lat")]
        public double? Lat { get; set; }

        [JsonPropertyName("lon")]
        public double? Lon { get; set; }

        [JsonPropertyName("center")]
        public OverpassCenter? Center { get; set; }

        [JsonPropertyName("tags")]
        public Dictionary<string, string>? Tags { get; set; }
    }

    private class OverpassCenter
    {
        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lon")]
        public double Lon { get; set; }
    }

    #endregion
}
