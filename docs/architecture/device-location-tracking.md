# Device Location Tracking Architecture (TR-MOBILE-001)

## Overview

Device location tracking in the FWH Mobile app follows a **local-only architecture**. Device locations are tracked and stored in the local SQLite database and are **NEVER sent to the API**.

## Architecture Principle

**TR-MOBILE-001: Local-Only Device Location Tracking**

> Device location data is considered private and sensitive. The mobile app tracks location locally for features like movement state detection and address-based workflows, but this data remains on the device. Only aggregated, anonymized queries (like "find businesses near this location") are sent to the API.

## Components

### 1. DeviceLocationEntity (SQLite)

**Location:** `src/FWH.Mobile.Data/Entities/DeviceLocationEntity.cs`

Entity for storing location history in local SQLite database:

```csharp
public class DeviceLocationEntity
{
    public long Id { get; set; }
    public string DeviceId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? AccuracyMeters { get; set; }
    public double? AltitudeMeters { get; set; }
    public double? SpeedMetersPerSecond { get; set; }
    public double? HeadingDegrees { get; set; }
    public string MovementState { get; set; }  // Unknown, Stationary, Walking, Riding
    public DateTimeOffset Timestamp { get; set; }
    public string? Address { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
```

### 2. NotesDbContext (Database Access)

**Location:** `src/FWH.Mobile.Data/Data/NotesDbContext.cs`

Includes `DeviceLocationHistory` DbSet:

```csharp
public DbSet<DeviceLocationEntity> DeviceLocationHistory { get; set; }
```

**Indexes for efficient querying:**
- `DeviceId`
- `Timestamp`
- `(DeviceId, Timestamp)` composite index

### 3. LocationTrackingService (Background Tracking)

**Location:** `src/FWH.Mobile/FWH.Mobile/Services/LocationTrackingService.cs`

Monitors GPS and stores location updates locally:

```csharp
public class LocationTrackingService : ILocationTrackingService
{
    private readonly IGpsService _gpsService;
    private readonly NotesDbContext _dbContext;  // ✅ Local database
    // private readonly IMediatorSender _mediator;  // ❌ REMOVED - no API calls
    
    private async Task SendLocationUpdateAsync(GpsCoordinates location, ...)
    {
        // Store in local SQLite database
        var locationEntity = new DeviceLocationEntity { ... };
        await _dbContext.DeviceLocationHistory.AddAsync(locationEntity);
        await _dbContext.SaveChangesAsync();
    }
}
```

## What Gets Sent to the API?

### ✅ Allowed: Query Parameters

The mobile app **MAY send coordinates as query parameters** to request location-based data:

```http
GET /api/locations/nearby?latitude=37.7749&longitude=-122.4194&radiusMeters=1000
```

**Purpose:** Get nearby businesses for the current location
**Privacy:** One-time query, not tracked/stored by API

### ❌ Prohibited: Device Location Tracking

The mobile app **MUST NOT send device location for tracking purposes**:

```http
POST /api/locations
{
  "deviceId": "abc123",
  "latitude": 37.7749,
  "longitude": -122.4194,
  "timestamp": "2025-01-16T10:00:00Z"
}
```

**Why prohibited:**
- Privacy concerns
- Battery/bandwidth waste
- No business requirement for server-side device tracking
- Local tracking is sufficient for app features

## Data Flow

```
┌─────────────────────────────────────────────────────────────┐
│ GPS Hardware                                                │
└────────────────┬────────────────────────────────────────────┘
                 │
                 │ Raw coordinates
                 │
┌────────────────▼────────────────────────────────────────────┐
│ IGpsService (Platform-specific)                             │
│ - AndroidGpsService / iOSGpsService / DesktopGpsService     │
└────────────────┬────────────────────────────────────────────┘
                 │
                 │ GpsCoordinates
                 │
┌────────────────▼────────────────────────────────────────────┐
│ LocationTrackingService                                     │
│ - Polls GPS every 30 seconds                                │
│ - Detects movement state (stationary/walking/riding)       │
│ - Stores in SQLite when moved > 50m threshold              │
└────────────────┬────────────────────────────────────────────┘
                 │
                 │ DeviceLocationEntity
                 │
┌────────────────▼────────────────────────────────────────────┐
│ NotesDbContext (SQLite)                                     │
│ - DeviceLocationHistory table                               │
│ - Indexed by DeviceId, Timestamp                            │
│ - Stored locally on device                                  │
└─────────────────────────────────────────────────────────────┘
                 │
                 │ Query for features
                 │
┌────────────────▼────────────────────────────────────────────┐
│ App Features                                                │
│ - Movement state detection                                  │
│ - Address change notifications                              │
│ - Location history visualization                            │
└─────────────────────────────────────────────────────────────┘


┌─────────────────────────────────────────────────────────────┐
│ Nearby Business Query (Separate flow)                       │
└─────────────────────────────────────────────────────────────┘
                 │
                 │ Current lat/long (query parameter)
                 │
┌────────────────▼────────────────────────────────────────────┐
│ LocationApiClient                                           │
│ GET /api/locations/nearby?lat=...&lon=...                  │
└────────────────┬────────────────────────────────────────────┘
                 │
                 │ HTTP request (one-time query)
                 │
┌────────────────▼────────────────────────────────────────────┐
│ Location API (Server)                                       │
│ - Queries Overpass API for businesses                       │
│ - Returns business list                                     │
│ - Does NOT store device location                            │
└─────────────────────────────────────────────────────────────┘
```

## Tracking Configuration

Default configuration in `LocationTrackingService`:

| Setting | Default | Purpose |
|---------|---------|---------|
| `MinimumDistanceMeters` | 50m | Minimum distance before storing new location |
| `PollingInterval` | 30s | How often to poll GPS |
| `StationaryThresholdDuration` | 3 min | Duration to detect stationary state |
| `StationaryDistanceThresholdMeters` | 10m | Max movement to remain stationary |
| `WalkingRidingSpeedThresholdMph` | 5 mph | Speed threshold to differentiate walking/riding |
| `StationaryAddressCheckDelay` | 1 min | Delay before checking address when stationary |

## Movement States

The tracking service detects and records movement states:

| State | Description | Typical Speed |
|-------|-------------|---------------|
| `Unknown` | Initial state, not enough data | N/A |
| `Stationary` | Device not moving | < 2 mph |
| `Walking` | Slow movement | 2-5 mph |
| `Riding` | Fast movement (vehicle, bike) | > 5 mph |

## Local Queries

Example queries on local SQLite database:

### Get Recent Location History

```csharp
var recentLocations = await _dbContext.DeviceLocationHistory
    .Where(l => l.DeviceId == deviceId)
    .Where(l => l.Timestamp >= startTime)
    .OrderByDescending(l => l.Timestamp)
    .Take(100)
    .ToListAsync();
```

### Get Locations by Movement State

```csharp
var stationaryLocations = await _dbContext.DeviceLocationHistory
    .Where(l => l.DeviceId == deviceId)
    .Where(l => l.MovementState == "Stationary")
    .OrderByDescending(l => l.Timestamp)
    .ToListAsync();
```

### Get Location History for Date Range

```csharp
var historyForDay = await _dbContext.DeviceLocationHistory
    .Where(l => l.DeviceId == deviceId)
    .Where(l => l.Timestamp >= startOfDay && l.Timestamp < endOfDay)
    .OrderBy(l => l.Timestamp)
    .ToListAsync();
```

## Privacy & Security

### Data Retention

**Recommendation:** Implement automatic cleanup of old location data:

```csharp
// Delete location data older than 30 days
var cutoffDate = DateTimeOffset.UtcNow.AddDays(-30);
var oldLocations = _dbContext.DeviceLocationHistory
    .Where(l => l.Timestamp < cutoffDate);
_dbContext.DeviceLocationHistory.RemoveRange(oldLocations);
await _dbContext.SaveChangesAsync();
```

### User Control

**Future enhancement:** Allow users to:
- View their location history
- Delete location history
- Pause/resume location tracking
- Adjust tracking frequency/accuracy

### Data Export

If needed, users should be able to export their location data:

```json
{
  "deviceId": "abc123",
  "exportDate": "2025-01-16T10:00:00Z",
  "locations": [
    {
      "timestamp": "2025-01-16T09:00:00Z",
      "latitude": 37.7749,
      "longitude": -122.4194,
      "movementState": "Walking",
      "address": "123 Main St, San Francisco, CA"
    }
  ]
}
```

## Deprecated Components

### ⚠️ UpdateDeviceLocationHandler (DO NOT USE)

**Location:** `src/FWH.Orchestrix.Mediator.Remote/Location/LocationHandlers.cs`

This remote handler **should NOT be registered or used** in the mobile app:

```csharp
// ❌ This handler sends location to API - DO NOT USE in mobile app
public class UpdateDeviceLocationHandler : IMediatorHandler<...>
{
    // This exists only for potential future server-to-server scenarios
}
```

**Migration:** If you find code using `UpdateDeviceLocationRequest`:

```csharp
// ❌ OLD: Sending to API
await _mediator.SendAsync(new UpdateDeviceLocationRequest { ... });

// ✅ NEW: Store locally
var entity = new DeviceLocationEntity { ... };
await _dbContext.DeviceLocationHistory.AddAsync(entity);
await _dbContext.SaveChangesAsync();
```

## Testing

### Unit Tests

Test local storage in `LocationTrackingServiceTests`:

```csharp
[Fact]
public async Task LocationUpdate_StoresInLocalDatabase_NotSentToApi()
{
    // Arrange
    var mockGps = new Mock<IGpsService>();
    var dbContext = CreateInMemoryDbContext();
    var service = new LocationTrackingService(mockGps.Object, dbContext, ...);
    
    // Act
    await service.StartTrackingAsync();
    await Task.Delay(100); // Wait for first poll
    
    // Assert
    var storedLocations = await dbContext.DeviceLocationHistory.ToListAsync();
    Assert.NotEmpty(storedLocations);
    // Verify no HTTP calls made
}
```

### Integration Tests

Test that location queries work:

```csharp
[Fact]
public async Task GetNearbyBusinesses_UsesQueryParameter_DoesNotStoreLocation()
{
    // Arrange
    var client = _factory.CreateClient();
    
    // Act
    var response = await client.GetAsync(
        "/api/locations/nearby?latitude=37.7749&longitude=-122.4194&radiusMeters=1000");
    
    // Assert
    response.EnsureSuccessStatusCode();
    // Verify location API DB does NOT contain device location entry
}
```

## Migration Checklist

If updating existing code:

- [ ] Remove `IMediatorSender` dependency from `LocationTrackingService`
- [ ] Inject `NotesDbContext` instead
- [ ] Replace `UpdateDeviceLocationRequest` with `DeviceLocationEntity`
- [ ] Update `SendLocationUpdateAsync` to use `DbContext.SaveChangesAsync()`
- [ ] Remove registration of `UpdateDeviceLocationHandler` from mobile app DI
- [ ] Update tests to verify local storage instead of API calls
- [ ] Add data retention/cleanup logic
- [ ] Document user privacy controls

## Related Files

| File | Purpose |
|------|---------|
| `src/FWH.Mobile.Data/Entities/DeviceLocationEntity.cs` | SQLite entity |
| `src/FWH.Mobile.Data/Data/NotesDbContext.cs` | Database context |
| `src/FWH.Mobile/FWH.Mobile/Services/LocationTrackingService.cs` | Background tracking |
| `src/FWH.Mobile/FWH.Mobile/Services/ILocationTrackingService.cs` | Service interface |
| `src/FWH.Mobile/FWH.Mobile/Services/LocationApiClient.cs` | API queries only |
| `src/FWH.Orchestrix.Mediator.Remote/Location/LocationHandlers.cs` | ⚠️ Deprecated handler |

## References

- [LocationTrackingService Implementation](../../src/FWH.Mobile/FWH.Mobile/Services/LocationTrackingService.cs)
- [GPS Service Architecture](../architecture/gps-service-architecture.md)
- [Mobile App Configuration](../configuration/mobile-app-configuration.md)
- [Privacy Guidelines](../security/privacy-guidelines.md)

## Summary

**Key Takeaway:** Device location is tracked locally in SQLite for app features. The API is used only for querying location-based data (like nearby businesses), never for storing or tracking device locations.

This architecture ensures:
- ✅ User privacy protected
- ✅ Reduced server load and bandwidth
- ✅ Offline capability for location history
- ✅ Fast local queries
- ✅ Compliance with mobile app best practices
