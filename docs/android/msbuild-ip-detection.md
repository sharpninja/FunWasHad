# MSBuild-Based Host IP Detection for Android

## Overview

The FWH Mobile app uses an MSBuild target to automatically detect and inject the host machine's IP address into the Android app configuration at build time. This eliminates the need for manual configuration when developing with physical Android devices.

## How It Works

### Build Pipeline

```
┌─────────────────────────────────────────────────────────────┐
│ 1. Developer triggers Android DEBUG build                   │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│ 2. MSBuild Target: DetectHostIpAddress (BeforeBuild)       │
│    - Runs PowerShell script                                 │
│    - Detects host machine IP address                        │
│    - Falls back to 10.0.2.2 if detection fails             │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│ 3. Process appsettings.Development.json                     │
│    - Read source file with HOST_IP_PLACEHOLDER              │
│    - Replace placeholder with detected IP                   │
│    - Write to obj/appsettings.Development.processed.json   │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│ 4. Package as Android Asset                                 │
│    - Remove original appsettings.Development.json           │
│    - Include processed version as Assets/appsettings.json   │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│ 5. Continue normal build process                            │
│    - App loads config from Android assets at runtime        │
│    - Config already contains correct IP address             │
└─────────────────────────────────────────────────────────────┘
```

## MSBuild Target Details

### Location

File: `src/FWH.Mobile/FWH.Mobile/FWH.Mobile.csproj`

### Trigger Conditions

The target only runs when **all** conditions are met:

1. **Configuration**: `Debug`
2. **Platform**: `TargetPlatformIdentifier == 'android'`
3. **Timing**: Before `BeforeBuild` phase

```xml
<Target Name="DetectHostIpAddress" 
        BeforeTargets="BeforeBuild" 
        Condition="'$(Configuration)' == 'Debug' and '$(TargetPlatformIdentifier)' == 'android'">
```

### IP Detection Logic

The PowerShell script uses multiple detection methods with fallbacks:

#### Method 1: Get-NetIPAddress (Primary)

```powershell
$hostIp = Get-NetIPAddress -AddressFamily IPv4 -PrefixOrigin Dhcp, Manual |
    Where-Object { 
        $_.IPAddress -notlike '127.*' -and 
        $_.IPAddress -notlike '169.254.*' -and
        $_.IPAddress -match '^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$'
    } |
    Sort-Object -Property PrefixOrigin -Descending |
    Select-Object -First 1 -ExpandProperty IPAddress
```

**Filters:**
- IPv4 addresses only
- DHCP or manually assigned addresses
- Excludes loopback (`127.*`)
- Excludes link-local (`169.254.*`)
- Validates IP format with regex
- Prioritizes DHCP over Manual

#### Method 2: DNS-Based Detection (Fallback)

```powershell
if ([string]::IsNullOrEmpty($hostIp)) {
    $hostName = [System.Net.Dns]::GetHostName()
    $hostIp = [System.Net.Dns]::GetHostEntry($hostName).AddressList |
        Where-Object { 
            $_.AddressFamily -eq 'InterNetwork' -and
            $_.ToString() -notlike '127.*' -and
            $_.ToString() -notlike '169.254.*'
        } |
        Select-Object -First 1 -ExpandProperty IPAddressToString
}
```

#### Method 3: Android Emulator Default (Final Fallback)

```powershell
if ([string]::IsNullOrEmpty($hostIp)) {
    $hostIp = '10.0.2.2'
    Write-Host "Using Android emulator default: $hostIp" -ForegroundColor Yellow
}
```

### File Processing

#### Input File

`src/FWH.Mobile/FWH.Mobile/appsettings.Development.json`:

```json
{
  "ApiSettings": {
    "HostIpAddress": "HOST_IP_PLACEHOLDER",
    "LocationApiPort": 4748,
    "MarketingApiPort": 4749
  }
}
```

#### Processing

```powershell
$content = Get-Content $appsettingsPath -Raw
$updatedContent = $content -replace '"HOST_IP_PLACEHOLDER"', "\"$hostIp\""
Set-Content -Path $processedPath -Value $updatedContent -NoNewline -Force
```

#### Output File

`src/FWH.Mobile/FWH.Mobile/obj/appsettings.Development.processed.json`:

```json
{
  "ApiSettings": {
    "HostIpAddress": "192.168.1.100",
    "LocationApiPort": 4748,
    "MarketingApiPort": 4749
  }
}
```

### Asset Packaging

The processed file replaces the original in Android assets:

```xml
<ItemGroup Condition="'$(Configuration)' == 'Debug' and '$(TargetPlatformIdentifier)' == 'android'">
  <AndroidAsset Remove="appsettings.Development.json" />
  <AndroidAsset Include="obj\appsettings.Development.processed.json" 
                Condition="Exists('obj\appsettings.Development.processed.json')">
    <Link>Assets\appsettings.Development.json</Link>
  </AndroidAsset>
</ItemGroup>
```

**Result:**
- Original file: Excluded from APK
- Processed file: Packaged as `Assets/appsettings.Development.json`
- App sees: Config with real IP address

## Build Output Examples

### Successful Detection

```
Detected Host IP: 192.168.1.100
Generated processed config at: E:\github\FunWasHad\src\FWH.Mobile\FWH.Mobile\obj\appsettings.Development.processed.json
HostIpAddress set to: 192.168.1.100
```

### Fallback to Emulator Default

```
Using Android emulator default: 10.0.2.2
Generated processed config at: E:\github\FunWasHad\src\FWH.Mobile\FWH.Mobile\obj\appsettings.Development.processed.json
HostIpAddress set to: 10.0.2.2
```

### Detection Failure with Warning

```
Warning: IP detection failed: <error details>
Using Android emulator default: 10.0.2.2
```

## Clean Target

Removes processed files during `Clean` operation:

```xml
<Target Name="CleanProcessedConfigs" AfterTargets="Clean">
  <Delete Files="$(ProjectDir)obj\appsettings.Development.processed.json" 
          ContinueOnError="true" />
</Target>
```

**Usage:**

```bash
dotnet clean
# Removes: obj/appsettings.Development.processed.json
```

## Testing the Detection

### Verify Detection Manually

Run the PowerShell script directly:

```powershell
# Test Method 1: Get-NetIPAddress
Get-NetIPAddress -AddressFamily IPv4 -PrefixOrigin Dhcp, Manual |
    Where-Object { 
        $_.IPAddress -notlike '127.*' -and 
        $_.IPAddress -notlike '169.254.*'
    } |
    Select-Object -First 1 -ExpandProperty IPAddress

# Test Method 2: DNS-Based
$hostName = [System.Net.Dns]::GetHostName()
[System.Net.Dns]::GetHostEntry($hostName).AddressList |
    Where-Object { 
        $_.AddressFamily -eq 'InterNetwork' -and
        $_.ToString() -notlike '127.*'
    } |
    Select-Object -First 1
```

### Build and Check

```bash
# Clean previous builds
dotnet clean

# Build for Android in Debug mode
dotnet build -c Debug -f net9.0-android

# Check the processed file
cat src/FWH.Mobile/FWH.Mobile/obj/appsettings.Development.processed.json
```

### Verify in APK

```bash
# Extract APK (after build)
unzip -l bin/Debug/net9.0-android/*.apk | grep appsettings

# Should show: assets/appsettings.Development.json
```

## Troubleshooting

### Problem: No IP Detected

**Symptoms:**
```
Using Android emulator default: 10.0.2.2
```

**Causes & Solutions:**

1. **No Network Connection**
   - Check: `ipconfig` (Windows) or `ifconfig` (Linux/Mac)
   - Solution: Connect to a network (Wi-Fi or Ethernet)

2. **Virtual Network Adapters Only**
   - Check: `Get-NetIPAddress` output
   - Solution: Connect physical adapter or use environment variables

3. **PowerShell Execution Policy**
   - Check: `Get-ExecutionPolicy`
   - Solution: The build uses `-ExecutionPolicy Bypass`, so this shouldn't block

### Problem: Wrong IP Detected

**Symptoms:**
App can't connect to APIs, wrong IP in logs

**Causes & Solutions:**

1. **Multiple Network Adapters**
   - Check: `Get-NetIPAddress -AddressFamily IPv4`
   - Solution: Script prioritizes DHCP, might pick wrong adapter
   - Workaround: Use environment variables to override

2. **VPN or Virtual Adapters**
   - Check: `ipconfig /all` - look for VPN adapters
   - Solution: Disconnect VPN during development or use env vars

### Problem: Processed File Not Created

**Symptoms:**
Build succeeds but original placeholder remains

**Diagnostic Steps:**

1. Check build output for MSBuild target execution
2. Verify source file exists: `appsettings.Development.json`
3. Check obj folder permissions
4. Look for PowerShell errors in build output

**Solution:**
- Ensure PowerShell is available in PATH
- Check file permissions on project directory
- Run `dotnet clean` and rebuild

### Problem: Changes Not Applied

**Symptoms:**
Modified appsettings.json but old IP still used

**Cause:**
Processed file cached in obj folder

**Solution:**
```bash
dotnet clean
dotnet build -c Debug -f net9.0-android
```

## Environment Variable Override

For complete control, set environment variables before building:

### Windows (PowerShell)

```powershell
$env:LOCATION_API_BASE_URL = "http://192.168.1.50:4748/"
$env:MARKETING_API_BASE_URL = "http://192.168.1.50:4749/"
dotnet build -c Debug -f net9.0-android
```

### Linux/Mac

```bash
export LOCATION_API_BASE_URL="http://192.168.1.50:4748/"
export MARKETING_API_BASE_URL="http://192.168.1.50:4749/"
dotnet build -c Debug -f net9.0-android
```

Environment variables are read at **runtime** and override the configuration.

## Best Practices

1. **Never commit processed files**
   - `obj/` folder is in `.gitignore`
   - Source `appsettings.Development.json` always has placeholder

2. **Clean builds when switching networks**
   ```bash
   dotnet clean && dotnet build
   ```

3. **Verify IP after build**
   - Check build output for "Detected Host IP"
   - Confirm IP matches your network adapter

4. **Use environment variables for CI/CD**
   - Build servers may not have network adapters
   - Set explicit IPs in build pipeline

5. **Document team network setup**
   - Share typical IP ranges (e.g., `192.168.1.x`)
   - Document any VPN requirements

## Security Considerations

1. **DEBUG Only**: Target never runs in Release builds
2. **Local Development**: Detects local network IPs only
3. **No External Calls**: No data sent outside the build machine
4. **Temporary Files**: Processed configs in `obj/` (excluded from source control)

## Platform Behavior

| Platform | DEBUG Build | RELEASE Build |
|----------|-------------|---------------|
| **Android** | ✅ IP detection runs | ❌ Uses default config |
| **iOS** | ❌ Not needed (uses localhost) | ❌ Uses default config |
| **Desktop** | ❌ Not needed (uses localhost) | ❌ Uses default config |
| **Browser** | ❌ Not applicable | ❌ Not applicable |

Only Android DEBUG builds trigger IP detection.

## Related Files

- `src/FWH.Mobile/FWH.Mobile/FWH.Mobile.csproj` - MSBuild target definition
- `src/FWH.Mobile/FWH.Mobile/appsettings.json` - Default config (emulator)
- `src/FWH.Mobile/FWH.Mobile/appsettings.Development.json` - DEBUG config (placeholder)
- `src/FWH.Mobile/FWH.Mobile/Configuration/ApiSettings.cs` - Configuration model
- `src/FWH.Mobile/FWH.Mobile/App.axaml.cs` - Configuration loading
- `docs/android/debug-ip-detection.md` - High-level overview
- `docs/configuration/mobile-app-configuration.md` - Complete configuration guide

## Version History

- **v1.0**: Initial MSBuild-based IP detection implementation
- Replaces previous runtime detection approach
- Improves build-time vs. runtime separation of concerns
