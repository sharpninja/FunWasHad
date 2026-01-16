# Android Configuration Files Setup

## Summary

Ensured that `appsettings.json` and `appsettings.Development.json` files are properly available to the Android app by including them as Android Assets and implementing platform-specific configuration loading.

## Problem

Configuration files (`appsettings.json`) need to be accessible on Android, but:
- Android apps use APK assets, not file system files
- Files marked as `Content` with `CopyToOutputDirectory` don't get embedded in the APK
- Different platforms require different approaches to access configuration files

## Solution

### 1. Platform-Specific Project Configuration

Updated `FWH.Mobile.csproj` to include configuration files differently based on target platform:

```xml
<!-- Configuration files - available on all platforms -->
<ItemGroup>
  <!-- For Desktop/iOS: Copy to output directory -->
  <Content Include="appsettings.json" Condition="'$(TargetPlatformIdentifier)' != 'android'">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  </Content>
  <Content Include="appsettings.Development.json" Condition="'$(TargetPlatformIdentifier)' != 'android'">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  </Content>
  
  <!-- For Android: Include as AndroidAsset -->
  <AndroidAsset Include="appsettings.json" Condition="'$(TargetPlatformIdentifier)' == 'android'">
    <Link>Assets\appsettings.json</Link>
  </AndroidAsset>
  <AndroidAsset Include="appsettings.Development.json" Condition="'$(TargetPlatformIdentifier)' == 'android'">
    <Link>Assets\appsettings.Development.json</Link>
  </AndroidAsset>
</ItemGroup>
```

**Key Points**:
- Desktop/iOS: Files are copied to output directory (file system access)
- Android: Files are included as `AndroidAsset` (embedded in APK)
- Conditional inclusion based on `TargetPlatformIdentifier`

### 2. Platform-Specific Configuration Loading

Updated `BuildConfiguration()` method in `App.axaml.cs`:

```csharp
private static IConfiguration BuildConfiguration()
{
    var builder = new ConfigurationBuilder();

    if (OperatingSystem.IsAndroid())
    {
        // On Android, read from assets
        builder.AddJsonStream(LoadAndroidAsset("appsettings.json"));
        
        var devSettingsStream = LoadAndroidAsset("appsettings.Development.json");
        if (devSettingsStream != null)
        {
            builder.AddJsonStream(devSettingsStream);
        }
    }
    else
    {
        // On Desktop/iOS, read from file system
        builder
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);
    }

    builder.AddEnvironmentVariables();
    return builder.Build();
}
```

### 3. Android Asset Loading

Added `LoadAndroidAsset()` helper method:

```csharp
private static Stream? LoadAndroidAsset(string fileName)
{
    if (!OperatingSystem.IsAndroid())
        return null;

    try
    {
        // Use reflection to access Android assets
        var contextType = Type.GetType("Android.App.Application, Mono.Android");
        var contextProperty = contextType?.GetProperty("Context");
        var context = contextProperty?.GetValue(null);

        var assetsProperty = context?.GetType().GetProperty("Assets");
        var assets = assetsProperty?.GetValue(context);

        var openMethod = assets?.GetType().GetMethod("Open", new[] { typeof(string) });
        var stream = openMethod?.Invoke(assets, new object[] { fileName }) as Stream;

        return stream;
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Failed to load Android asset '{fileName}': {ex.Message}");
        return null;
    }
}
```

**Why Reflection**:
- Avoids compile-time dependency on `Mono.Android`
- Allows the same code to run on all platforms
- Safe fallback if Android types not available

## How It Works

### Android Platform

```
APK Structure:
├── assemblies/
├── Assets/
│   ├── appsettings.json              ← Embedded here
│   └── appsettings.Development.json  ← Embedded here
└── lib/

App Startup:
    ↓
BuildConfiguration()
    ↓
OperatingSystem.IsAndroid() → TRUE
    ↓
LoadAndroidAsset("appsettings.json")
    ├─ Reflection → Android.App.Application.Context
    ├─ Get Assets manager
    ├─ Open("appsettings.json")
    └─ Return Stream
    ↓
ConfigurationBuilder.AddJsonStream(stream)
    ↓
Configuration Loaded ✓
```

### Desktop/iOS Platform

```
Output Directory:
├── FWH.Mobile.dll
├── appsettings.json              ← File system
├── appsettings.Development.json  ← File system
└── ...

App Startup:
    ↓
BuildConfiguration()
    ↓
OperatingSystem.IsAndroid() → FALSE
    ↓
SetBasePath(Directory.GetCurrentDirectory())
    ↓
AddJsonFile("appsettings.json")
    ↓
Configuration Loaded ✓
```

## Verification

### Check Android APK Contents

```bash
# Extract APK to verify assets are included
unzip app.apk -d extracted/
ls extracted/Assets/

# Should show:
# appsettings.json
# appsettings.Development.json
```

### Debug Output

When running on Android, you should see:
```
Loaded Android asset: appsettings.json
Loaded Android asset: appsettings.Development.json
```

### Test Configuration Loading

Add temporary debug code:
```csharp
var config = BuildConfiguration();
var hostIp = config["ApiSettings:HostIpAddress"];
System.Diagnostics.Debug.WriteLine($"Configured Host IP: {hostIp}");
```

## Platform Differences

| Aspect | Android | Desktop/iOS |
|--------|---------|-------------|
| **Build Action** | `AndroidAsset` | `Content` |
| **Location** | APK `Assets/` folder | Output directory |
| **Access Method** | `AssetManager.Open()` | `File.ReadAllText()` |
| **Reload Support** | No (embedded in APK) | Yes (`reloadOnChange`) |
| **Modification** | Requires rebuild | Edit file directly |

## Build-Time IP Detection on Android

The MSBuild target still updates `appsettings.Development.json` before build:

```
DEBUG Build
    ↓
DetectHostIpAddress Target
    ↓
PowerShell: Update appsettings.Development.json
Replace "HOST_IP_PLACEHOLDER" with detected IP
    ↓
Android Build
    ↓
Include appsettings.Development.json as AndroidAsset
    ↓
File embedded in APK with detected IP ✓
```

## Testing

### Test on Android Emulator

1. Build DEBUG: `dotnet build -c Debug`
2. Deploy to emulator
3. Check debug output for "Loaded Android asset"
4. Verify app connects to configured IP

### Test on Android Device

1. Ensure device and dev machine on same network
2. Build DEBUG (IP auto-detected)
3. Deploy to device
4. Verify configuration loaded from assets
5. Confirm API connections work

### Test on Desktop

1. Build: `dotnet build`
2. Run app
3. Should load from file system
4. Configuration file changes should be detected

## Troubleshooting

### Assets Not Found on Android

**Symptom**: `Failed to load Android asset 'appsettings.json'`

**Solutions**:
1. Verify `AndroidAsset` in `.csproj` for Android target
2. Clean and rebuild: `dotnet clean && dotnet build`
3. Check APK contents (extract and verify)
4. Ensure target platform is set to Android

### Configuration Not Loading

**Symptom**: App uses default values instead of configured values

**Solutions**:
1. Check debug output for "Loaded Android asset" messages
2. Verify JSON syntax in appsettings files
3. Ensure `ApiSettings` class matches JSON structure
4. Check for exceptions in `BuildConfiguration()`

### Build-Time IP Not Updated

**Symptom**: Android app still uses `HOST_IP_PLACEHOLDER`

**Solutions**:
1. Ensure DEBUG configuration
2. Check PowerShell execution policy
3. Manually edit `appsettings.Development.json`
4. Rebuild to re-embed updated file

## Related Files

- `src/FWH.Mobile/FWH.Mobile/FWH.Mobile.csproj` - Platform-specific includes
- `src/FWH.Mobile/FWH.Mobile/App.axaml.cs` - Configuration loading
- `src/FWH.Mobile/FWH.Mobile/appsettings.json` - Production config
- `src/FWH.Mobile/FWH.Mobile/appsettings.Development.json` - Development config
- `src/FWH.Mobile/FWH.Mobile/Configuration/ApiSettings.cs` - Settings class

## References

- [Android Assets](https://developer.android.com/guide/topics/resources/providing-resources#OriginalFiles)
- [MSBuild Conditional Compilation](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-conditional-constructs)
- [.NET Configuration](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration)
- [ConfigurationBuilder.AddJsonStream](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.jsonconfigurationextensions.addjsonstream)
