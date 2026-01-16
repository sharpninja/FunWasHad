# Android DEBUG Mode - Host IP Detection

## Summary

Added automatic host IP address detection when building the Android app in DEBUG mode. This enables testing on physical Android devices by automatically detecting and injecting the host machine's actual IP address into the configuration at **build time**.

## Problem

When developing Android apps:
- **Emulator**: Uses `10.0.2.2` as a special alias for the host machine's `localhost`
- **Physical Device**: Cannot use `10.0.2.2` - needs the actual IP address of the host machine
- Previously, developers had to manually set environment variables or hardcode IP addresses

## Solution

### Automatic IP Detection via MSBuild Target

When the app is built in DEBUG configuration for Android:
1. **Build-Time Detection**: An MSBuild target runs before compilation
2. **IP Detection**: Multiple methods detect the host machine's IP address with fallbacks
3. **Config Generation**: Creates a processed `appsettings.Development.json` in the `obj` folder
4. **Asset Injection**: The processed file (with real IP) is packaged as an Android asset
5. **Original Preserved**: The source `appsettings.Development.json` keeps the placeholder

### MSBuild Target Implementation

Located in `src/FWH.Mobile/FWH.Mobile/FWH.Mobile.csproj`:

```xml
<Target Name="DetectHostIpAddress" 
        BeforeTargets="BeforeBuild" 
        Condition="'$(Configuration)' == 'Debug' and '$(TargetPlatformIdentifier)' == 'android'">
```

**IP Detection Methods (with fallbacks):**
1. **Primary**: `Get-NetIPAddress` with DHCP/Manual filtering
2. **Secondary**: DNS-based detection via `[System.Net.Dns]`
3. **Fallback**: Android emulator alias `10.0.2.2`

**File Processing:**
- Reads: `appsettings.Development.json` (contains `HOST_IP_PLACEHOLDER`)
- Replaces: `"HOST_IP_PLACEHOLDER"` → `"192.168.1.100"` (detected IP)
- Writes: `obj/appsettings.Development.processed.json`
- Packages: Processed file as Android asset `Assets/appsettings.Development.json`

### Configuration Files

**Source file** (`appsettings.Development.json`):
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

**Generated file** (`obj/appsettings.Development.processed.json`):
```json
{
  "ApiSettings": {
    "HostIpAddress": "192.168.1.100",
    "LocationApiPort": 4748,
    "MarketingApiPort": 4749,
    "UseHttps": false
  }
}
```

## Build Output

During Android DEBUG builds, you'll see:

```
Detected Host IP: 192.168.1.100
Generated processed config at: E:\github\FunWasHad\src\FWH.Mobile\FWH.Mobile\obj\appsettings.Development.processed.json
HostIpAddress set to: 192.168.1.100
```

Or if detection fails:

```
Using Android emulator default: 10.0.2.2
```

## Behavior Matrix

| Configuration | Platform | IP Used | When Detected | Example |
|--------------|----------|---------|---------------|---------|
| DEBUG | Android Emulator | Auto-detected or `10.0.2.2` | Build time | `192.168.1.100` or `10.0.2.2` |
| DEBUG | Android Device | Auto-detected or `10.0.2.2` | Build time | `192.168.1.100` |
| RELEASE | Android | `10.0.2.2` (emulator) | N/A | `10.0.2.2` |
| Any | Desktop/iOS | `localhost` | N/A | `https://localhost:4747` |

## Benefits

1. **Build-Time Detection**: IP resolved once during build, not every app launch
2. **No Runtime Overhead**: Configuration is pre-processed
3. **Source Control Safe**: Original files keep placeholder, never committed with IPs
4. **Works on Physical Devices**: Can test on real Android devices without manual setup
5. **Emulator Compatible**: Falls back to `10.0.2.2` if detection fails
6. **Cross-Platform**: Only affects Android DEBUG builds
7. **Clean Builds**: `Clean` target removes processed files

## Environment Variable Override

You can still manually override the API addresses at runtime using environment variables:

```bash
export LOCATION_API_BASE_URL="http://192.168.1.50:4748/"
export MARKETING_API_BASE_URL="http://192.168.1.50:4749/"
```

Environment variables take precedence over configuration files.

## Troubleshooting

### IP Detection Fails

If the MSBuild target shows:
```
Using Android emulator default: 10.0.2.2
```

**Causes:**
- No active network adapter with a valid IP
- Running on a machine without network connectivity
- PowerShell execution policy blocks the script

**Solutions:**
1. Check network connectivity: `ipconfig` (Windows) or `ifconfig` (Linux/Mac)
2. Verify PowerShell can run: `powershell -NoProfile -Command "Get-NetIPAddress"`
3. Manually set environment variables (see above)

### Processed File Not Created

Check build output for errors. Common issues:
- PowerShell not available
- Insufficient file permissions in `obj` folder
- Source `appsettings.Development.json` missing or malformed

### Wrong IP Detected

If the wrong network adapter is selected:
1. Check `ipconfig /all` to see all adapters
2. The script prioritizes DHCP > Manual > first available
3. Override with environment variables for specific IP

## Implementation Details

### Clean Target

The project includes a clean target that removes processed files:

```xml
<Target Name="CleanProcessedConfigs" AfterTargets="Clean">
  <Delete Files="$(ProjectDir)obj\appsettings.Development.processed.json" 
          ContinueOnError="true" />
</Target>
```

### Asset Replacement

For Android DEBUG builds, the original `appsettings.Development.json` is replaced with the processed version:

```xml
<ItemGroup Condition="'$(Configuration)' == 'Debug' and '$(TargetPlatformIdentifier)' == 'android'">
  <AndroidAsset Remove="appsettings.Development.json" />
  <AndroidAsset Include="obj\appsettings.Development.processed.json">
    <Link>Assets\appsettings.Development.json</Link>
  </AndroidAsset>
</ItemGroup>
```

## Testing

1. **Build the Android project in DEBUG**:
   ```bash
   dotnet build -c Debug -f net9.0-android
   ```

2. **Check build output** for detected IP:
   ```
   Detected Host IP: 192.168.1.100
   ```

3. **Deploy to Android device/emulator**:
   ```bash
   dotnet build -t:Run -c Debug -f net9.0-android
   ```

4. **Verify connectivity** in app logs
# Set custom API endpoints
adb shell setprop debug.LOCATION_API_BASE_URL "http://192.168.1.50:4748/"
adb shell setprop debug.MARKETING_API_BASE_URL "http://192.168.1.50:4749/"
```

Environment variables take precedence over auto-detection.

## IP Detection Logic

The method detects the host IP by:
1. Getting the host name via `Dns.GetHostName()`
2. Resolving all IP addresses for that host
3. Filtering for IPv4 addresses (AddressFamily.InterNetwork)
4. Excluding loopback addresses (`127.*`)
5. Excluding link-local addresses (`169.254.*`)
6. Returning the first valid address found

## Debug Output

When IP detection runs, you'll see diagnostic messages in the debug console:

```
Detected host IP address: 192.168.1.100
```

or

```
Failed to detect host IP address: No network adapters found
```

## Testing

### On Android Emulator
1. Build in DEBUG mode
2. Run the app
3. App should connect to either auto-detected IP or `10.0.2.2`
4. Check debug output for detected IP

### On Physical Android Device
1. Ensure device and development machine are on the same network
2. Build in DEBUG mode
3. Deploy to device
4. App should connect to host machine's IP automatically
5. Verify API calls work in the log viewer

### In RELEASE Mode
1. Build in RELEASE mode
2. App should always use `10.0.2.2` (emulator alias)
3. Production behavior is not affected

## Troubleshooting

### IP Detection Fails
- Check that the development machine has a network connection
- Verify the machine has a non-loopback IPv4 address
- Check firewall settings (ports 4747-4749 should be open)

### Can't Connect to APIs
- Ensure API services are running on the host machine
- Verify ports 4747-4749 are accessible from the Android device
- Check that device and host are on the same network
- Try setting environment variables manually to override

### Wrong IP Detected
- If multiple network adapters are present, the first valid IP is used
- Use environment variables to explicitly set the correct IP

## Related Files

- `src/FWH.Mobile/FWH.Mobile/App.axaml.cs` - Main implementation
- `src/FWH.Mobile/FWH.Mobile/Options/LocationApiClientOptions.cs` - Configuration options
- `src/FWH.Orchestrix.Mediator.Remote/Extensions/MediatorServiceCollectionExtensions.cs` - API client registration

## References

- [Android Emulator Networking](https://developer.android.com/studio/run/emulator-networking)
- [.NET DNS Class](https://learn.microsoft.com/en-us/dotnet/api/system.net.dns)
- [Android Debug Bridge (ADB)](https://developer.android.com/tools/adb)
