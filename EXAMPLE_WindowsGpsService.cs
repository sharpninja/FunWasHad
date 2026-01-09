# Platform Library Comparison for Camera and GPS

**Date:** 2026-01-08  
**Context:** Avalonia UI Application (.NET 9)

---

## Current Implementation Status ✅

Your application uses **native platform APIs** with a clean abstraction layer:

| Feature | Android | iOS | Desktop | Browser |
|---------|---------|-----|---------|---------|
| **Camera** | ✅ MediaStore | ✅ UIImagePicker | ❌ Fallback | ❌ Fallback |
| **GPS** | ✅ LocationManager | ✅ CLLocationManager | ❌ Fallback | ❌ Fallback |

**Pros:**
- ✅ Production-ready and tested
- ✅ Best performance (native APIs)
- ✅ Full control over behavior
- ✅ Clean architecture with factory pattern
- ✅ Proper DI integration

---

## Alternative: Microsoft Libraries

### 1. MAUI Libraries (Not Compatible)

**Packages:**
- `Microsoft.Maui.Media` - Camera
- `Microsoft.Maui.Devices.Sensors` - GPS

**Status:** ❌ **Not Recommended**

**Reason:** MAUI is a separate framework. These libraries are tightly coupled to MAUI's lifecycle and won't work in an Avalonia app without significant workarounds.

**If you were using MAUI instead:**
```csharp
// MAUI example (doesn't work in Avalonia)
using Microsoft.Maui.Media;
var photo = await MediaPicker.CapturePhotoAsync();

using Microsoft.Maui.Devices.Sensors;
var location = await Geolocation.GetLocationAsync();
```

---

### 2. Xamarin.Essentials (Legacy)

**Package:**
- `Xamarin.Essentials` v1.8.0

**Status:** ⚠️ **Not Recommended**

**Pros:**
- Unified API for camera and GPS
- Works on Android and iOS

**Cons:**
- ❌ Being phased out (replaced by MAUI)
- ❌ May have .NET 9 compatibility issues
- ❌ Requires platform-specific initialization
- ❌ Less control than native APIs

**Example (if you wanted to try):**
```csharp
// Install: Xamarin.Essentials
using Xamarin.Essentials;

// Camera
var photo = await MediaPicker.CapturePhotoAsync();

// GPS
var location = await Geolocation.GetLocationAsync(new GeolocationRequest
{
    DesiredAccuracy = GeolocationAccuracy.Best,
    Timeout = TimeSpan.FromSeconds(30)
});
```

**Setup Required:**
```csharp
// Android MainActivity
Xamarin.Essentials.Platform.Init(this, savedInstanceState);

// iOS AppDelegate
Xamarin.Essentials.Platform.Init();
```

---

### 3. Windows SDK (Desktop Only)

**Package:**
- `Microsoft.Windows.SDK.Contracts`

**Status:** ⚠️ **Windows Only**

**Use Case:** If you want to add GPS support for Windows desktop

**Example Implementation:**
```csharp
using Windows.Devices.Geolocation;

public class WindowsGpsService : IGpsService
{
    private readonly Geolocator _geolocator;

    public WindowsGpsService()
    {
        _geolocator = new Geolocator
        {
            DesiredAccuracy = PositionAccuracy.High
        };
    }

    public bool IsLocationAvailable => 
        _geolocator.LocationStatus == PositionStatus.Ready;

    public async Task<bool> RequestLocationPermissionAsync()
    {
        var status = await Geolocator.RequestAccessAsync();
        return status == GeolocationAccessStatus.Allowed;
    }

    public async Task<GpsCoordinates?> GetCurrentLocationAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var position = await _geolocator.GetGeopositionAsync();
            
            return new GpsCoordinates(
                position.Coordinate.Point.Position.Latitude,
                position.Coordinate.Point.Position.Longitude,
                position.Coordinate.Accuracy)
            {
                AltitudeMeters = position.Coordinate.Point.Position.Altitude,
                Timestamp = position.Coordinate.Timestamp
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Windows GPS error: {ex}");
            return null;
        }
    }
}
```

**Registration:**
```csharp
// In App.axaml.cs for Desktop
public static class DesktopServiceCollectionExtensions
{
    public static IServiceCollection AddDesktopGpsService(
        this IServiceCollection services)
    {
        services.AddKeyedSingleton<IGpsService, WindowsGpsService>("Desktop");
        return services;
    }
}
```

**Pros:**
- ✅ Official Microsoft API
- ✅ Works well on Windows 10/11

**Cons:**
- ❌ Windows-only (not Linux/macOS)
- ❌ Requires UWP capabilities in manifest

---

### 4. Community Packages

**GPS Libraries:**
- `GeolocatorPlugin` - Cross-platform geolocation
- `Xam.Plugin.Geolocator` - Xamarin plugin (legacy)

**Camera Libraries:**
- `MediaPlugin` - Cross-platform media picker
- `Xam.Plugin.Media` - Xamarin plugin (legacy)

**Status:** ⚠️ **Variable Quality**

**Issues:**
- May not support .NET 9
- May not work with Avalonia
- Varying levels of maintenance

---

## Recommended Approach: Enhance Current Implementation

Instead of replacing your working code, **extend it** with additional platform support:

### Add Windows Desktop GPS Support

<function_calls>
<invoke name="create_file">
<parameter name="content">using Windows.Devices.Geolocation;
using FWH.Common.Location;
using FWH.Common.Location.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FWH.Mobile.Desktop.Services;

/// <summary>
/// Windows implementation of GPS service using Windows.Devices.Geolocation.
/// Requires Windows 10/11 and appropriate capabilities.
/// </summary>
public class WindowsGpsService : IGpsService
{
    private readonly Geolocator _geolocator;
    private const int LocationTimeoutSeconds = 30;

    public WindowsGpsService()
    {
        _geolocator = new Geolocator
        {
            DesiredAccuracy = PositionAccuracy.High,
            MovementThreshold = 0 // Report all changes
        };
    }

    public bool IsLocationAvailable
    {
        get
        {
            try
            {
                var status = _geolocator.LocationStatus;
                return status == PositionStatus.Ready || 
                       status == PositionStatus.Initializing;
            }
            catch
            {
                return false;
            }
        }
    }

    public async Task<bool> RequestLocationPermissionAsync()
    {
        try
        {
            var accessStatus = await Geolocator.RequestAccessAsync();
            return accessStatus == GeolocationAccessStatus.Allowed;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error requesting location permission: {ex}");
            return false;
        }
    }

    public async Task<GpsCoordinates?> GetCurrentLocationAsync(
        CancellationToken cancellationToken = default)
    {
        if (!IsLocationAvailable)
        {
            var granted = await RequestLocationPermissionAsync();
            if (!granted)
                return null;
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                new CancellationTokenSource(TimeSpan.FromSeconds(LocationTimeoutSeconds)).Token);

            var position = await _geolocator.GetGeopositionAsync()
                .AsTask(cts.Token);

            if (position?.Coordinate == null)
                return null;

            return new GpsCoordinates(
                position.Coordinate.Point.Position.Latitude,
                position.Coordinate.Point.Position.Longitude,
                position.Coordinate.Accuracy)
            {
                AltitudeMeters = position.Coordinate.Point.Position.Altitude,
                Timestamp = position.Coordinate.Timestamp
            };
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine("GPS location request cancelled");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting GPS location: {ex}");
            return null;
        }
    }
}
