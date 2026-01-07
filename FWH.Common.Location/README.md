# FWH.Common.Location

Location services for the Fun Was Had application using OpenStreetMap's Overpass API.

## Overview

This library provides a **free, no-API-key-required** solution for finding businesses and points of interest near GPS coordinates using OpenStreetMap data via the Overpass API.

## Features

- ✅ **Free & Open Source** - No API keys required
- ✅ **Global Coverage** - OpenStreetMap data worldwide
- ✅ **Rich Data** - Business names, addresses, categories, and custom tags
- ✅ **Distance Calculation** - Haversine formula for accurate distances
- ✅ **.NET 9 Compatible** - Modern C# with nullable reference types
- ✅ **Dependency Injection** - First-class DI support
- ✅ **Configurable** - Customizable search radius, timeouts, and limits
- ✅ **Fully Tested** - Comprehensive unit test coverage

## Quick Start

### 1. Register Services (Default Configuration)

In your `App.axaml.cs` or startup configuration:

```csharp
using FWH.Common.Location.Extensions;

// In your service registration
services.AddLocationServices();
```

### 2. Register Services (Custom Configuration)

```csharp
services.AddLocationServices(options =>
{
    options.DefaultRadiusMeters = 500;    // Default search radius
    options.MaxRadiusMeters = 10000;      // Max allowed radius (10km)
    options.MinRadiusMeters = 100;        // Min allowed radius
    options.TimeoutSeconds = 60;          // API timeout
    options.UserAgent = "MyApp/2.0";      // Custom user agent
});
```

### 3. Inject and Use

```csharp
public class MyWorkflowService
{
    private readonly ILocationService _locationService;

    public MyWorkflowService(ILocationService locationService)
    {
        _locationService = locationService;
    }

    public async Task<IEnumerable<BusinessLocation>> FindNearbyAsync()
    {
        var businesses = await _locationService.GetNearbyBusinessesAsync(
            latitude: 37.7749,
            longitude: -122.4194,
            radiusMeters: 1000); // Will be validated against config limits

        return businesses;
    }
}
```

## Configuration

### LocationServiceOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `DefaultRadiusMeters` | int | 1000 | Default search radius if not specified |
| `MaxRadiusMeters` | int | 5000 | Maximum allowed search radius |
| `MinRadiusMeters` | int | 50 | Minimum allowed search radius |
| `TimeoutSeconds` | int | 30 | HTTP timeout for API requests |
| `UserAgent` | string | "FunWasHad/1.0" | User agent for HTTP requests |
| `OverpassApiUrl` | string | (Overpass URL) | Base URL for Overpass API |

### Configuration Examples

#### Production Settings (Conservative)
```csharp
services.AddLocationServices(options =>
{
    options.MaxRadiusMeters = 3000;      // Limit to 3km
    options.MinRadiusMeters = 100;       // At least 100m
    options.TimeoutSeconds = 45;         // Longer timeout
});
```

#### Development Settings (Generous)
```csharp
services.AddLocationServices(options =>
{
    options.MaxRadiusMeters = 10000;     // Allow up to 10km
    options.MinRadiusMeters = 10;        // Very small searches
    options.TimeoutSeconds = 120;        // Extended timeout
});
```

#### Custom Overpass Instance
```csharp
services.AddLocationServices(options =>
{
    options.OverpassApiUrl = "https://my-overpass.example.com/api/interpreter";
    options.TimeoutSeconds = 60;
});
```

## Radius Validation

The service automatically validates and clamps radius values:

```csharp
// If configured with MaxRadiusMeters = 5000
await _locationService.GetNearbyBusinessesAsync(
    37.7749, -122.4194, 
    radiusMeters: 10000);  // Will be clamped to 5000m

// If configured with MinRadiusMeters = 50
await _locationService.GetNearbyBusinessesAsync(
    37.7749, -122.4194,
    radiusMeters: 10);     // Will be clamped to 50m
```

A warning is logged when clamping occurs.

## API Reference

### ILocationService.GetNearbyBusinessesAsync

Finds businesses within a specified radius.

**Parameters:**
- `latitude` - Center point latitude
- `longitude` - Center point longitude
- `radiusMeters` - Search radius in meters (validated against config)
- `categories` - Optional filter (e.g., `["restaurant", "cafe"]`)
- `cancellationToken` - Cancellation token

**Returns:** Collection of `BusinessLocation` objects sorted by distance

### ILocationService.GetClosestBusinessAsync

Finds the single closest business to a point.

**Parameters:**
- `latitude` - Search point latitude
- `longitude` - Search point longitude
- `maxDistanceMeters` - Maximum search radius (default 1000m, validated)
- `cancellationToken` - Cancellation token

**Returns:** Closest `BusinessLocation` or null

## Business Location Model

```csharp
public record BusinessLocation
{
    string Name              // "Starbucks Coffee"
    string? Address          // "123 Market St, San Francisco"
    double Latitude          // 37.7749
    double Longitude         // -122.4194
    string? Category         // "cafe"
    Dictionary<string, string> Tags  // All OSM tags
    double? DistanceMeters   // 245.7
}
```

## Common Categories

### Food & Drink
- `restaurant`, `cafe`, `fast_food`, `bar`, `pub`

### Shopping
- `supermarket`, `convenience`, `clothes`, `electronics`, `bookshop`

### Services
- `bank`, `pharmacy`, `hospital`, `dentist`

### Entertainment
- `cinema`, `theatre`, `nightclub`, `casino`

### Tourism
- `hotel`, `motel`, `museum`, `attraction`

## Examples

### Find Restaurants Near User

```csharp
var restaurants = await _locationService.GetNearbyBusinessesAsync(
    37.7749, -122.4194, 1000,
    new[] { "restaurant", "cafe", "fast_food" });
```

### Find Closest Coffee Shop

```csharp
var coffeeShop = await _locationService.GetClosestBusinessAsync(
    37.7749, -122.4194, 500);
```

### Integration with Fun Was Had Workflow

```csharp
public async Task<string> HandleAddressInput(double lat, double lon)
{
    // Find nearby businesses (radius auto-validated)
    var businesses = await _locationService.GetNearbyBusinessesAsync(
        lat, lon, 500);

    if (businesses.Any())
    {
        var businessList = string.Join(", ", 
            businesses.Select(b => b.Name).Take(5));
        return $"Found businesses nearby: {businessList}";
    }

    return "No businesses found nearby.";
}
```

## Performance Considerations

- **Radius Limits**: Configure `MaxRadiusMeters` appropriately for your use case
- **Rate Limiting**: Overpass API has rate limits (~2 requests/second)
- **Timeout**: Adjust `TimeoutSeconds` based on your network conditions
- **Caching**: Consider caching results for frequently queried locations

## Troubleshooting

### No Results Returned

1. Check if coordinates are valid
2. Try increasing search radius (within configured max)
3. Verify internet connectivity
4. Check [Overpass API status](https://overpass-api.de/api/status)

### Timeout Errors

- Increase `TimeoutSeconds` in configuration
- Reduce search radius
- Reduce number of categories

### Radius Warnings in Logs

If you see warnings about radius clamping, adjust either:
- The requested radius in your code
- The `MaxRadiusMeters`/`MinRadiusMeters` in configuration

## Links

- [Overpass API Documentation](https://wiki.openstreetmap.org/wiki/Overpass_API)
- [Overpass Turbo (Query Builder)](https://overpass-turbo.eu/)
- [OpenStreetMap Wiki](https://wiki.openstreetmap.org/)
