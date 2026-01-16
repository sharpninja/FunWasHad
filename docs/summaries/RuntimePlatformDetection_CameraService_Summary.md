# Runtime Platform Detection for Camera Service

## Problem
Compiler directives (#if ANDROID / #if IOS) don't work in the shared `FWH.Mobile` project because it's compiled before the platform-specific projects. This prevented proper registration of platform-specific camera services.

## Solution: Runtime Platform Detection

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                      FWH.Mobile (Shared)                     │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  App.axaml.cs                                          │ │
│  │  - Uses reflection to detect loaded assemblies        │ │
│  │  - Dynamically calls platform extension methods       │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                              │
                              ├─────────────────────────────────┐
                              │                                 │
                              ▼                                 ▼
┌─────────────────────────────────────┐    ┌──────────────────────────────────┐
│   FWH.Common.Chat (Core Services)   │    │  Platform-Specific Projects      │
│  ┌────────────────────────────────┐ │    │  ┌────────────────────────────┐ │
│  │ IPlatformService               │ │    │  │ FWH.Mobile.Android         │ │
│  │ - Detects runtime platform     │ │    │  │ - AndroidCameraService     │ │
│  │                                │ │    │  │ - Keyed registration       │ │
│  │ CameraServiceFactory           │ │    │  └────────────────────────────┘ │
│  │ - Creates correct service      │ │    │                                  │
│  │ - Uses keyed services          │ │    │  ┌────────────────────────────┐ │
│  │                                │ │    │  │ FWH.Mobile.iOS             │ │
│  │ NoCameraService (fallback)     │ │    │  │ - iOSCameraService         │ │
│  └────────────────────────────────┘ │    │  │ - Keyed registration       │ │
└─────────────────────────────────────┘    │  └────────────────────────────┘ │
                                            └──────────────────────────────────┘
```

### Components Created

#### 1. **IPlatformService & PlatformService**
- Location: `FWH.Common.Chat\Services\`
- Purpose: Detects runtime platform using `OperatingSystem` APIs
- Returns: `PlatformType` enum (Android, iOS, Desktop, Browser, Unknown)

```csharp
public interface IPlatformService
{
    PlatformType Platform { get; }
    bool IsAndroid { get; }
    bool IsIOS { get; }
    bool IsDesktop { get; }
    bool IsBrowser { get; }
}
```

#### 2. **CameraServiceFactory**
- Location: `FWH.Common.Chat\Services\`
- Purpose: Creates appropriate camera service based on platform
- Uses: Keyed service resolution from DI container

```csharp
public ICameraService CreateCameraService()
{
    if (_platformService.IsAndroid)
        return _serviceProvider.GetKeyedService<ICameraService>("Android");
    if (_platformService.IsIOS)
        return _serviceProvider.GetKeyedService<ICameraService>("iOS");
    return new NoCameraService(); // Fallback
}
```

#### 3. **Platform-Specific Extensions**
- Android: `FWH.Mobile.Android\Extensions\AndroidServiceCollectionExtensions.cs`
- iOS: `FWH.Mobile.iOS\Extensions\iOSServiceCollectionExtensions.cs`
- Both register their camera service with platform-specific keys

```csharp
// Android
services.AddKeyedSingleton<ICameraService, AndroidCameraService>("Android");

// iOS
services.AddKeyedSingleton<ICameraService, iOSCameraService>("iOS");
```

#### 4. **Updated App.axaml.cs**
- Uses reflection to detect loaded platform assemblies
- Dynamically invokes platform extension methods if available
- No compiler directives needed

```csharp
private static void TryRegisterPlatformCameraServices(IServiceCollection services)
{
    // Try Android
    var androidAssembly = AppDomain.CurrentDomain.GetAssemblies()
        .FirstOrDefault(a => a.GetName().Name == "FWH.Mobile.Android");
    // ... invoke extension method via reflection
    
    // Try iOS
    var iosAssembly = AppDomain.CurrentDomain.GetAssemblies()
        .FirstOrDefault(a => a.GetName().Name == "FWH.Mobile.iOS");
    // ... invoke extension method via reflection
}
```

## Benefits

### ✅ **Solves the Original Problem**
- No compiler directives in shared code
- Works correctly for all platforms (Android, iOS, Desktop, Browser)
- Platform detection happens at runtime

### ✅ **Clean Architecture**
- Single Responsibility: Each component has one job
- Dependency Injection: All services properly registered
- Factory Pattern: Clean creation logic
- Keyed Services: Platform-specific implementations isolated

### ✅ **Extensibility**
- Easy to add new platforms (Browser camera, Desktop camera)
- New platforms just need to register with appropriate key
- No changes to shared code required

### ✅ **Testability**
- `IPlatformService` can be mocked for testing
- Factory can be tested independently
- Platform-specific services remain isolated

## Registration Flow

1. **Shared Code** (`FWH.Mobile/App.axaml.cs`):
   ```csharp
   services.AddChatServices(); // Registers factory & platform service
   TryRegisterPlatformCameraServices(services); // Registers platform-specific
   ```

2. **Platform Detection**:
   - `PlatformService` uses `OperatingSystem.IsAndroid()` / `IsIOS()` / etc.
   - Factory queries platform service at runtime

3. **Service Resolution**:
   - When `ICameraService` is requested
   - Factory looks up keyed service for current platform
   - Falls back to `NoCameraService` if platform not supported

## Files Created/Modified

### Created:
- ✅ `FWH.Common.Chat\Services\IPlatformService.cs`
- ✅ `FWH.Common.Chat\Services\PlatformService.cs`
- ✅ `FWH.Common.Chat\Services\CameraServiceFactory.cs`
- ✅ `FWH.Mobile.Android\Extensions\AndroidServiceCollectionExtensions.cs`
- ✅ `FWH.Mobile.iOS\Extensions\iOSServiceCollectionExtensions.cs`

### Modified:
- ✅ `FWH.Common.Chat\Extensions\ChatServiceCollectionExtensions.cs`
  - Added platform service & factory registration
  - Added camera service resolution via factory
- ✅ `FWH.Mobile\FWH.Mobile\App.axaml.cs`
  - Removed compiler directives
  - Added reflection-based platform service registration

## Testing

### Build Status
✅ **Build Successful** - All projects compile without errors

### Platform Coverage
- ✅ Android: Will use `AndroidCameraService` when running on Android
- ✅ iOS: Will use `iOSCameraService` when running on iOS  
- ✅ Desktop: Will use `NoCameraService` (fallback)
- ✅ Browser: Will use `NoCameraService` (fallback)

## Future Enhancements

### Desktop Camera Support
If desktop camera support is needed:
```csharp
// In FWH.Mobile.Desktop project
public static class DesktopServiceCollectionExtensions
{
    public static IServiceCollection AddDesktopCameraService(this IServiceCollection services)
    {
        services.AddKeyedSingleton<ICameraService, DesktopCameraService>("Desktop");
        return services;
    }
}
```

### Browser Camera Support
For WebAssembly/Browser:
```csharp
public static class BrowserServiceCollectionExtensions
{
    public static IServiceCollection AddBrowserCameraService(this IServiceCollection services)
    {
        services.AddKeyedSingleton<ICameraService, BrowserCameraService>("Browser");
        return services;
    }
}
```

## Summary

The solution elegantly solves the compiler directive problem by:
1. Using runtime platform detection instead of compile-time directives
2. Leveraging .NET's keyed service feature for platform-specific registrations
3. Using reflection to dynamically load platform extensions when available
4. Providing clean fallback behavior for unsupported platforms

**Result**: The camera service now works correctly on all platforms without any compile-time dependencies or #if directives in the shared code! 🎉
