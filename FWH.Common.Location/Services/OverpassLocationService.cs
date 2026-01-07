using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FWH.Common.Location.Models;
using FWH.Common.Location.Configuration;

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

            var query = BuildOverpassQuery(latitude, longitude, validatedRadius, categories);
            _logger.LogDebug("Querying Overpass API: lat={Lat}, lon={Lon}, radius={Radius}m",
                latitude, longitude, validatedRadius);

            var content = new StringContent(query);
            var response = await _httpClient.PostAsync(_overpassApiUrl, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Overpass API returned status code: {StatusCode}", response.StatusCode);
                return Enumerable.Empty<BusinessLocation>();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<OverpassResponse>(json);

            if (result?.Elements == null)
            {
                return Enumerable.Empty<BusinessLocation>();
            }

            var businesses = result.Elements
                .Where(e => e.Tags != null && !string.IsNullOrEmpty(e.Tags.GetValueOrDefault("name")))
                .Select(e => ConvertToBusinessLocation(e, latitude, longitude))
                .OrderBy(b => b.DistanceMeters)
                .ToList();

            _logger.LogInformation("Found {Count} businesses near ({Lat}, {Lon}) within {Radius}m",
                businesses.Count, latitude, longitude, validatedRadius);

            return businesses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching nearby businesses from Overpass API");
            return Enumerable.Empty<BusinessLocation>();
        }
    }

    public async Task<BusinessLocation?> GetClosestBusinessAsync(
        double latitude,
        double longitude,
        int maxDistanceMeters = 1000,
        CancellationToken cancellationToken = default)
    {
        var businesses = await GetNearbyBusinessesAsync(
            latitude,
            longitude,
            maxDistanceMeters,
            null,
            cancellationToken);

        return businesses.FirstOrDefault();
    }

    private static string BuildOverpassQuery(
        double latitude,
        double longitude,
        int radiusMeters,
        IEnumerable<string>? categories)
    {
        var categoryFilters = categories?.ToList() ?? new List<string>();

        // Build Overpass QL query
        var filters = new List<string>();

        if (categoryFilters.Any())
        {
            foreach (var category in categoryFilters)
            {
                filters.Add($"node[\"amenity\"=\"{category}\"](around:{radiusMeters},{latitude},{longitude});");
                filters.Add($"way[\"amenity\"=\"{category}\"](around:{radiusMeters},{latitude},{longitude});");
                filters.Add($"node[\"shop\"=\"{category}\"](around:{radiusMeters},{latitude},{longitude});");
                filters.Add($"way[\"shop\"=\"{category}\"](around:{radiusMeters},{latitude},{longitude});");
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
