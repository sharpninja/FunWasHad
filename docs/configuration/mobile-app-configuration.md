# Mobile App Configuration with Build-Time IP Detection

## Summary

Implemented configuration support for the Mobile app using `appsettings.json` files with build-time IP address detection. The host machine's IP address is automatically detected during DEBUG builds and populated into the configuration file, eliminating the need for runtime IP detection.

## Changes Made

### 1. Created Configuration Files

**appsettings.json** (Production/Default):
```json
{
  "ApiSettings": {
    "HostIpAddress": "10.0.2.2",
    "LocationApiPort": 4748,
    "MarketingApiPort": 4749,
    "UseHttps": false
  }
}
```

**appsettings.Development.json** (Development):
```json
{
  "ApiSettings": {
    "HostIpAddress": "HOST_IP_PLACEHOLDER",
    "LocationApiPort": 4748,
    "MarketingApiPort": 4749,
    "UseHttps": false
  }
}
```

### 2. Created ApiSettings Class

`src/FWH.Mobile/FWH.Mobile/Configuration/ApiSettings.cs`:
- Strongly-typed configuration class
- Helper methods `GetLocationApiBaseUrl()` and `GetMarketingApiBaseUrl()`
- Supports both HTTP and HTTPS protocols

### 3. Updated Project File

Added MSBuild target to detect host IP at build time:

```xml
<Target Name="DetectHostIpAddress" BeforeTargets="PreBuildEvent" Condition="'$(Configuration)' == 'Debug'">
  <!-- PowerShell script that:
       1. Detects IPv4 address (excludes loopback and link-local)
       2. Updates appsettings.Development.json with detected IP
       3. Falls back to 10.0.2.2 if detection fails
  -->
</Target>
```

### 4. Added NuGet Packages

- `Microsoft.Extensions.Configuration.Json` (v10.0.2)
- `Microsoft.Extensions.Configuration.EnvironmentVariables` (v10.0.1)

### 5. Updated App.axaml.cs

- Added `BuildConfiguration()` method to load appsettings files
- Replaced runtime IP detection with configuration-based approach
- Environment variables still take precedence for overrides
- Configuration supports logging levels from appsettings

## How It Works

### Build-Time IP Detection

```
DEBUG Build Starts
    ↓
MSBuild Target: DetectHostIpAddress
    ↓
PowerShell Script Executes
    ├─ Get-NetIPAddress (IPv4 only)
    ├─ Filter: Exclude 127.* and 169.254.*
    ├─ Select first valid IP (DHCP or Manual)
    └─ Default to 10.0.2.2 if none found
    ↓
Update appsettings.Development.json
Replace "HOST_IP_PLACEHOLDER" with detected IP
    ↓
Continue Build
```

### Runtime Configuration Loading

```
App Static Constructor
    ↓
BuildConfiguration()
    ├─ Load appsettings.json
    ├─ Load appsettings.Development.json (overrides)
    └─ Load Environment Variables (overrides all)
    ↓
Read ApiSettings:HostIpAddress from configuration
    ↓
Build API base URLs
    ├─ Check Environment Variables first
    ├─ If Android: Use configured IP
    └─ If Desktop/iOS: Use localhost
    ↓
Register HttpClients with URLs
```

## Configuration Priority

1. **Environment Variables** (Highest Priority)
   - `LOCATION_API_BASE_URL`
   - `MARKETING_API_BASE_URL`

2. **appsettings.Development.json** (DEBUG builds only)
   - Auto-populated with detected IP during build

3. **appsettings.json** (Fallback)
   - Default value: `10.0.2.2` (Android emulator)

## Platform Behavior

| Platform | Configuration | IP Source | Example URL |
|----------|--------------|-----------|-------------|
| Android Emulator | DEBUG | Build-time detected or 10.0.2.2 | `http://192.168.1.100:4748/` |
| Android Device | DEBUG | Build-time detected | `http://192.168.1.100:4748/` |
| Android | RELEASE | appsettings.json (10.0.2.2) | `http://10.0.2.2:4748/` |
| Desktop | Any | localhost | `https://localhost:4747/` |
| iOS | Any | localhost | `https://localhost:4747/` |

## Benefits

1. **Build-Time Detection**: IP detected once during build, not every app start
2. **Configuration-Based**: Standard .NET configuration patterns
3. **Environment Override**: Easy to override via environment variables
4. **Logging Configuration**: Supports setting log levels via appsettings
5. **Platform-Specific**: Different behavior for Android vs Desktop/iOS
6. **Production-Safe**: RELEASE builds use stable emulator alias

## Testing

### DEBUG Build on Physical Android Device

1. Ensure device and dev machine on same network
2. Build the app: `dotnet build -c Debug`
3. Check build output for: "Detected Host IP: 192.168.x.x"
4. Deploy to device
5. App should connect to detected IP automatically

### Verify Configuration

Check the generated `appsettings.Development.json`:
```bash
cat src/FWH.Mobile/FWH.Mobile/bin/Debug/net9.0/appsettings.Development.json
```

Should show detected IP instead of `HOST_IP_PLACEHOLDER`.

### Override with Environment Variable

```bash
# Set custom API URL
export LOCATION_API_BASE_URL="http://192.168.50.100:4748/"
export MARKETING_API_BASE_URL="http://192.168.50.100:4749/"

# Run app - should use custom URLs
dotnet run
```

## Troubleshooting

### IP Not Detected

**Symptom**: App uses `10.0.2.2` even on physical device

**Solutions**:
1. Check build output for PowerShell errors
2. Verify network adapter has IPv4 address
3. Manually set environment variable as workaround
4. Check `appsettings.Development.json` for placeholder value

### Build Target Fails

**Symptom**: PowerShell script fails during build

**Solutions**:
1. MSBuild target uses `ContinueOnError="true"` - build will succeed
2. Check PowerShell execution policy
3. Manually edit `appsettings.Development.json` with correct IP

### Wrong IP Detected

**Symptom**: Multiple network adapters, wrong one selected

**Solutions**:
1. PowerShell script selects first DHCP/Manual IPv4
2. Override with environment variable
3. Modify PowerShell script filter in `.csproj` if needed

## Configuration File Structure

```
src/FWH.Mobile/FWH.Mobile/
├── appsettings.json                 # Production settings
├── appsettings.Development.json     # Development settings (auto-updated)
├── Configuration/
│   └── ApiSettings.cs              # Strongly-typed configuration
└── FWH.Mobile.csproj               # Build-time IP detection target
```

## Related Files

- `src/FWH.Mobile/FWH.Mobile/App.axaml.cs` - Configuration loading
- `src/FWH.Mobile/FWH.Mobile/FWH.Mobile.csproj` - Build target
- `src/FWH.Mobile/FWH.Mobile/Configuration/ApiSettings.cs` - Settings class
- `src/FWH.Mobile/Directory.Packages.props` - Package versions

## Migration from Runtime Detection

### Before (Runtime Detection)
```csharp
#if DEBUG
    var hostIp = GetHostIpAddress(); // Detected at runtime
    if (!string.IsNullOrEmpty(hostIp))
    {
        locationApiBaseAddress = $"http://{hostIp}:4748/";
    }
#endif
```

### After (Build-Time Configuration)
```csharp
// IP already in configuration from build-time detection
locationApiBaseAddress = apiSettings.GetLocationApiBaseUrl();
```

## Future Enhancements

1. **Certificate Configuration**: Add HTTPS certificate paths for dev
2. **Per-Environment Settings**: Support multiple environments beyond Development
3. **Secret Management**: Integrate with Azure Key Vault or Secret Manager
4. **Configuration Validation**: Add validation attributes to ApiSettings
5. **Hot Reload**: Enable configuration reload without app restart

## References

- [.NET Configuration](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration)
- [MSBuild Targets](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-targets)
- [Android Networking](https://developer.android.com/studio/run/emulator-networking)
