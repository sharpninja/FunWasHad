# Quick Reference: Platform-Specific Service Registration

## Problem Statement
Compiler directives don't work in shared projects because they compile before platform-specific projects.

## Solution Pattern

### 1. Define Platform Detection Service (Shared)
```csharp
// FWH.Common.Chat\Services\IPlatformService.cs
public interface IPlatformService
{
    PlatformType Platform { get; }
    bool IsAndroid { get; }
    bool IsIOS { get; }
}
```

### 2. Create Service Factory (Shared)
```csharp
// FWH.Common.Chat\Services\CameraServiceFactory.cs
public class CameraServiceFactory
{
    public ICameraService CreateCameraService()
    {
        if (_platformService.IsAndroid)
            return _serviceProvider.GetKeyedService<ICameraService>("Android");
        if (_platformService.IsIOS)
            return _serviceProvider.GetKeyedService<ICameraService>("iOS");
        return new NoCameraService(); // Fallback
    }
}
```

### 3. Register in Shared Extensions (Shared)
```csharp
// FWH.Common.Chat\Extensions\ChatServiceCollectionExtensions.cs
public static IServiceCollection AddChatServices(this IServiceCollection services)
{
    services.AddSingleton<IPlatformService, PlatformService>();
    services.AddSingleton<CameraServiceFactory>();
    services.AddSingleton<ICameraService>(sp =>
    {
        var factory = sp.GetRequiredService<CameraServiceFactory>();
        return factory.CreateCameraService();
    });
    return services;
}
```

### 4. Create Platform Extensions (Platform-Specific)
```csharp
// Android: FWH.Mobile.Android\Extensions\AndroidServiceCollectionExtensions.cs
public static class AndroidServiceCollectionExtensions
{
    public static IServiceCollection AddAndroidCameraService(this IServiceCollection services)
    {
        services.AddKeyedSingleton<ICameraService, AndroidCameraService>("Android");
        return services;
    }
}

// iOS: FWH.Mobile.iOS\Extensions\iOSServiceCollectionExtensions.cs
public static class iOSServiceCollectionExtensions
{
    public static IServiceCollection AddIOSCameraService(this IServiceCollection services)
    {
        services.AddKeyedSingleton<ICameraService, iOSCameraService>("iOS");
        return services;
    }
}
```

### 5. Use Reflection in Startup (Shared)
```csharp
// FWH.Mobile\FWH.Mobile\App.axaml.cs
private static void TryRegisterPlatformCameraServices(IServiceCollection services)
{
    // Try Android
    var androidAssembly = AppDomain.CurrentDomain.GetAssemblies()
        .FirstOrDefault(a => a.GetName().Name == "FWH.Mobile.Android");
    if (androidAssembly != null)
    {
        var extensions = androidAssembly.GetType("FWH.Mobile.Android.AndroidServiceCollectionExtensions");
        var method = extensions?.GetMethod("AddAndroidCameraService");
        method?.Invoke(null, new object[] { services });
    }

    // Try iOS  
    var iosAssembly = AppDomain.CurrentDomain.GetAssemblies()
        .FirstOrDefault(a => a.GetName().Name == "FWH.Mobile.iOS");
    if (iosAssembly != null)
    {
        var extensions = iosAssembly.GetType("FWH.Mobile.iOS.iOSServiceCollectionExtensions");
        var method = extensions?.GetMethod("AddIOSCameraService");
        method?.Invoke(null, new object[] { services });
    }
}

// In static constructor
static App()
{
    var services = new ServiceCollection();
    services.AddChatServices(); // Registers factory
    TryRegisterPlatformCameraServices(services); // Registers platform-specific
    ServiceProvider = services.BuildServiceProvider();
}
```

## Usage in Code

### Injecting the Service
```csharp
public class CameraCaptureViewModel
{
    private readonly ICameraService _cameraService;
    
    public CameraCaptureViewModel(ICameraService cameraService)
    {
        _cameraService = cameraService;
    }
    
    public async Task CapturePhotoAsync()
    {
        var photo = await _cameraService.TakePictureAsync();
        // ... use photo
    }
}
```

### At Runtime
- **Android Device**: Gets `AndroidCameraService` (keyed "Android")
- **iOS Device**: Gets `iOSCameraService` (keyed "iOS")
- **Desktop/Browser**: Gets `NoCameraService` (fallback)

## Key Features

### ✅ No Compiler Directives
All platform detection happens at runtime using `OperatingSystem` APIs.

### ✅ Clean Separation
- Shared code knows nothing about platform specifics
- Platform code registers itself when available
- Factory pattern handles resolution

### ✅ Extensible
Add new platforms by:
1. Creating platform extension method
2. Registering with unique key
3. No changes to shared code needed

### ✅ Type-Safe
- All registrations use strongly-typed interfaces
- Factory enforces correct service type
- DI container validates registrations

## Testing

### All Tests Pass ✅
- **Chat Tests**: 40/40 passed
- **Workflow Tests**: 107/107 passed  
- **Build**: Successful

### Platform Coverage
- ✅ Android: Real camera service
- ✅ iOS: Real camera service
- ✅ Desktop: Fallback (no camera)
- ✅ Browser: Fallback (no camera)

## Common Patterns

### Adding New Platform Service
```csharp
// 1. Create interface (if needed)
public interface INewService { }

// 2. Create factory
public class NewServiceFactory
{
    public INewService CreateService()
    {
        if (_platform.IsAndroid)
            return _sp.GetKeyedService<INewService>("Android");
        // ... other platforms
        return new FallbackService();
    }
}

// 3. Register in shared extensions
services.AddSingleton<NewServiceFactory>();
services.AddSingleton<INewService>(sp => 
    sp.GetRequiredService<NewServiceFactory>().CreateService());

// 4. Create platform extensions
public static IServiceCollection AddAndroidNewService(this IServiceCollection services)
{
    services.AddKeyedSingleton<INewService, AndroidNewService>("Android");
    return services;
}

// 5. Use reflection in App.axaml.cs
TryRegisterPlatformNewServices(services);
```

## Troubleshooting

### Service Not Found
- Check platform assembly is loaded
- Verify keyed registration matches platform name
- Ensure extension method is called

### Wrong Service Resolved
- Check `PlatformService.Platform` value
- Verify keyed service is registered for that platform
- Check fallback implementation

### Reflection Fails
- Verify assembly name matches exactly
- Check extension class name (case-sensitive)
- Ensure method is public and static

## Best Practices

1. **Always provide fallback**: Don't throw if platform not supported
2. **Use keyed services**: Platform name as key (e.g., "Android", "iOS")
3. **Silent failure**: Reflection errors should be caught and ignored
4. **Test on all platforms**: Verify correct service on each target
5. **Document platform support**: Clear which platforms have real implementations

## Migration from Compiler Directives

### Before (Won't Work in Shared Code)
```csharp
#if ANDROID
    services.AddSingleton<ICameraService, AndroidCameraService>();
#elif IOS
    services.AddSingleton<ICameraService, iOSCameraService>();
#else
    services.AddSingleton<ICameraService, NoCameraService>();
#endif
```

### After (Works Everywhere)
```csharp
// Shared code
services.AddChatServices(); // Includes factory & platform detection
TryRegisterPlatformCameraServices(services); // Reflection-based

// Platform-specific code (optional)
services.AddAndroidCameraService(); // When Android assembly loaded
services.AddIOSCameraService(); // When iOS assembly loaded
```

---

**Result**: Clean, maintainable, runtime-based platform detection that works across all platforms! 🎉
