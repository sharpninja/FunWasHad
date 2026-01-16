# Android Development Setup - Complete Configuration

## Overview

This document provides a complete overview of the Android development setup for FWH Mobile, covering IP detection, API network configuration, and the end-to-end flow.

## Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│                     Development Machine                          │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │ Location API                                               │  │
│  │ - HTTP:  0.0.0.0:4748                                      │  │
│  │ - HTTPS: 0.0.0.0:4747                                      │  │
│  └────────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │ Marketing API                                              │  │
│  │ - HTTP:  0.0.0.0:4750                                      │  │
│  │ - HTTPS: 0.0.0.0:4749                                      │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                                                   │
│  Machine IP: 192.168.1.77 (auto-detected)                        │
└───────────────────────────┬───────────────────────────────────────┘
                            │
                    Local Network (Wi-Fi/Ethernet)
                            │
        ┌───────────────────┴───────────────────┐
        │                                       │
┌───────▼────────┐                    ┌────────▼─────────┐
│ Android Device │                    │ Android Emulator │
│                │                    │                  │
│ Config:        │                    │ Config:          │
│ 192.168.1.77   │                    │ 10.0.2.2         │
│ :4748 / :4750  │                    │ :4748 / :4750    │
└────────────────┘                    └──────────────────┘
```

## Components

### 1. MSBuild IP Detection (`DetectHostIp.ps1`)

**Location:** `src/FWH.Mobile/FWH.Mobile.Android/DetectHostIp.ps1`

**Purpose:** Automatically detects the host machine's IP address at build time and injects it into the Android app configuration.

**Trigger:** Runs during Android DEBUG builds via MSBuild target in `FWH.Mobile.Android.csproj`

**Process:**
1. Detects IP using multiple methods (Get-NetIPAddress, DNS)
2. Falls back to `10.0.2.2` (Android emulator alias) if detection fails
3. Reads `appsettings.Development.json` with `HOST_IP_PLACEHOLDER`
4. Replaces placeholder with detected IP
5. Writes `obj/appsettings.Development.processed.json`
6. Processed file is packaged as Android asset

**Output Example:**
```json
{
  "ApiSettings": {
    "HostIpAddress": "192.168.1.77",
    "LocationApiPort": 4748,
    "MarketingApiPort": 4750
  }
}
```

### 2. API Network Configuration

**Location:** 
- `src/FWH.Location.Api/Program.cs`
- `src/FWH.MarketingApi/Program.cs`

**Purpose:** Configure APIs to listen on all network interfaces (`0.0.0.0`) in Development mode, allowing connections from external devices.

**Implementation:**
```csharp
if (builder.Environment.IsDevelopment())
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(port);
        options.ListenAnyIP(httpsPort, listenOptions =>
        {
            listenOptions.UseHttps();
        });
    });
}
```

**Ports:**
- Location API: HTTP 4748, HTTPS 4747
- Marketing API: HTTP 4750, HTTPS 4749

### 3. Aspire AppHost Orchestration

**Location:** `src/FWH.AppHost/Program.cs`

**Purpose:** Orchestrates all services (APIs, databases) with fixed port mappings for consistent Android connectivity.

**Configuration:**
```csharp
var locationApi = builder.AddProject<Projects.FWH_Location_Api>("locationapi")
    .WithHttpEndpoint(port: 4748, name: "asp-http")
    .WithHttpsEndpoint(port: 4747, name: "asp-https")
    .WithExternalHttpEndpoints();

var marketingApi = builder.AddProject<Projects.FWH_MarketingApi>("marketingapi")
    .WithHttpEndpoint(port: 4750, name: "asp-http")
    .WithHttpsEndpoint(port: 4749, name: "asp-https")
    .WithExternalHttpEndpoints();
```

### 4. Mobile App Configuration Loading

**Location:** `src/FWH.Mobile/FWH.Mobile/App.axaml.cs`

**Purpose:** Loads configuration from Android assets and sets up API clients with correct base URLs.

**Process:**
1. Static constructor runs during app initialization
2. Loads configuration from Android assets (processed file with real IP)
3. Reads `ApiSettings:HostIpAddress` from config
4. Constructs API base URLs
5. Registers HttpClient instances with dependency injection

**Fallback Logic:**
```csharp
// Priority:
// 1. Environment variables (LOCATION_API_BASE_URL, MARKETING_API_BASE_URL)
// 2. Configuration file (HostIpAddress from appsettings.json)
// 3. Platform defaults (10.0.2.2 for Android, localhost for Desktop)
```

## Build & Run Workflow

### Step 1: Start APIs (Aspire AppHost)

```powershell
# Start all services via Aspire
dotnet run --project src/FWH.AppHost/FWH.AppHost.csproj
```

**What Happens:**
1. PostgreSQL container starts
2. Location API starts, listening on `0.0.0.0:4748` and `0.0.0.0:4747`
3. Marketing API starts, listening on `0.0.0.0:4750` and `0.0.0.0:4749`
4. Database migrations run automatically
5. APIs are accessible from localhost and local network

**Verify:**
```powershell
# Check services are listening
netstat -an | Select-String "4748|4749|4750|4747"

# Should show:
# TCP  0.0.0.0:4748     0.0.0.0:0      LISTENING
# TCP  0.0.0.0:4747     0.0.0.0:0      LISTENING
# TCP  0.0.0.0:4750     0.0.0.0:0      LISTENING
# TCP  0.0.0.0:4749     0.0.0.0:0      LISTENING

# Test API health
curl http://localhost:4748/health
curl http://192.168.1.77:4748/health  # Your detected IP
```

### Step 2: Build Android App

```powershell
# Clean previous builds
dotnet clean src/FWH.Mobile/FWH.Mobile.Android/FWH.Mobile.Android.csproj

# Build for Android DEBUG
dotnet build src/FWH.Mobile/FWH.Mobile.Android/FWH.Mobile.Android.csproj -c Debug
```

**What Happens:**
1. MSBuild target `DetectHostIpAddress` runs
2. PowerShell script detects IP: `192.168.1.77`
3. Reads `appsettings.Development.json` with placeholder
4. Generates `obj/appsettings.Development.processed.json` with real IP
5. Processed file is packaged as Android asset
6. APK/AAB built with configuration embedded

**Build Output:**
```
=====================================
Android Host IP Detection
=====================================
Detected Host IP: 192.168.1.77
Source: src\FWH.Mobile\FWH.Mobile\appsettings.Development.json
Target: src\FWH.Mobile\FWH.Mobile.Android\obj\appsettings.Development.processed.json
✓ Generated processed config
✓ HostIpAddress set to: 192.168.1.77
=====================================
```

### Step 3: Deploy to Device/Emulator

```powershell
# Deploy and run
dotnet build -t:Run src/FWH.Mobile/FWH.Mobile.Android/FWH.Mobile.Android.csproj -c Debug

# Or via Visual Studio: F5 (Start Debugging)
```

**What Happens:**
1. APK deployed to device/emulator
2. App starts and runs static constructor
3. Loads configuration from assets
4. Reads `HostIpAddress: "192.168.1.77"`
5. Constructs URLs: `http://192.168.1.77:4748`, `http://192.168.1.77:4750`
6. Registers HttpClient instances with these base URLs
7. App makes API requests to host machine

### Step 4: Test Connectivity

From the running Android app, test API connectivity:

```csharp
// Example: Call Location API from app
var locationService = App.ServiceProvider.GetRequiredService<ILocationService>();
var businesses = await locationService.GetNearbyBusinessesAsync(37.7749, -122.4194, 1000);

// This will make HTTP request to:
// http://192.168.1.77:4748/api/locations/nearby?latitude=37.7749&longitude=-122.4194&radiusMeters=1000
```

## Connection Matrix

| Scenario | Host IP Used | API Endpoint | Works? |
|----------|-------------|--------------|--------|
| **Android Emulator on Dev Machine** | `10.0.2.2` | `http://10.0.2.2:4748` | ✅ |
| **Physical Android Device (Same Network)** | `192.168.1.77` | `http://192.168.1.77:4748` | ✅ |
| **Desktop App (Windows/Mac)** | `localhost` | `https://localhost:4747` | ✅ |
| **iOS App (Simulator)** | `localhost` | `https://localhost:4747` | ✅ |
| **Physical Android Device (Different Network)** | N/A | N/A | ❌ (Use VPN or public endpoint) |

## Troubleshooting Guide

### Problem: "Failed to load Android asset 'appsettings.json'"

**Cause:** Configuration file null check issue (now fixed)

**Solution:** Already fixed in `App.axaml.cs`:
```csharp
var appSettingsStream = LoadAndroidAsset("appsettings.json");
if (appSettingsStream != null)
{
    builder.AddJsonStream(appSettingsStream);
}
```

### Problem: "Source.Stream cannot be null"

**Cause:** Same as above - passing null stream to AddJsonStream

**Status:** ✅ Fixed with null checks

### Problem: Android app can't connect to APIs

**Diagnostic:**

1. **Verify APIs are running:**
   ```powershell
   netstat -an | Select-String "4748"
   # Should show: 0.0.0.0:4748  LISTENING
   ```

2. **Check detected IP:**
   ```powershell
   cat src\FWH.Mobile\FWH.Mobile.Android\obj\appsettings.Development.processed.json
   # Verify HostIpAddress matches your machine
   
   ipconfig  # Compare with actual IP
   ```

3. **Test from host machine:**
   ```powershell
   curl http://192.168.1.77:4748/health
   # Should return 200 OK
   ```

4. **Test from Android (if adb connected):**
   ```powershell
   adb shell curl http://192.168.1.77:4748/health
   ```

5. **Check firewall:**
   - Windows Firewall might block ports 4748, 4749, 4750, 4747
   - Add inbound rules or temporarily disable for testing

### Problem: Wrong IP detected

**Cause:** Multiple network adapters (Wi-Fi, Ethernet, VPN)

**Solutions:**

1. **Identify correct adapter:**
   ```powershell
   Get-NetIPAddress -AddressFamily IPv4 | Where-Object {
       $_.IPAddress -notlike '127.*' -and
       $_.IPAddress -notlike '169.254.*'
   }
   ```

2. **Override with environment variables:**
   ```powershell
   $env:LOCATION_API_BASE_URL = "http://192.168.1.77:4748/"
   $env:MARKETING_API_BASE_URL = "http://192.168.1.77:4750/"
   ```

3. **Disconnect VPN during development**

4. **Clean and rebuild:**
   ```powershell
   dotnet clean
   dotnet build -c Debug
   ```

## Security Considerations

### Development Mode Only

All `0.0.0.0` listening is **Development mode only**:
- ✅ Enabled: `ASPNETCORE_ENVIRONMENT=Development`
- ❌ Disabled: Production/Staging

### Network Exposure

When listening on `0.0.0.0`:
- ✅ Localhost can connect
- ✅ LAN devices can connect (same Wi-Fi/Ethernet)
- ❌ Internet cannot connect (blocked by router NAT/firewall)

**Best Practices:**
1. Use trusted networks (home/office Wi-Fi)
2. Avoid public Wi-Fi
3. Enable firewall if concerned about LAN exposure
4. Never deploy with `0.0.0.0` to production

## Related Documentation

- [MSBuild IP Detection](./msbuild-ip-detection.md) - Technical details of IP detection
- [API Network Configuration](./api-network-configuration.md) - Kestrel configuration details
- [Debug IP Detection](./debug-ip-detection.md) - Overview and behavior matrix
- [Configuration Assets](./configuration-assets.md) - Android asset management
- [Mobile App Configuration](../configuration/mobile-app-configuration.md) - Complete config guide

## Quick Reference

### Ports

| Service | HTTP | HTTPS |
|---------|------|-------|
| Location API | 4748 | 4747 |
| Marketing API | 4750 | 4749 |

### Key Files

| File | Purpose |
|------|---------|
| `src/FWH.Mobile/FWH.Mobile.Android/DetectHostIp.ps1` | IP detection script |
| `src/FWH.Mobile/FWH.Mobile.Android/FWH.Mobile.Android.csproj` | MSBuild target |
| `src/FWH.Location.Api/Program.cs` | Location API Kestrel config |
| `src/FWH.MarketingApi/Program.cs` | Marketing API Kestrel config |
| `src/FWH.AppHost/Program.cs` | Aspire orchestration |
| `src/FWH.Mobile/FWH.Mobile/App.axaml.cs` | Mobile app config loading |

### Commands

```powershell
# Start APIs
dotnet run --project src/FWH.AppHost/FWH.AppHost.csproj

# Build Android app
dotnet build src/FWH.Mobile/FWH.Mobile.Android/FWH.Mobile.Android.csproj -c Debug

# Deploy to device
dotnet build -t:Run src/FWH.Mobile/FWH.Mobile.Android/FWH.Mobile.Android.csproj -c Debug

# Check listening ports
netstat -an | Select-String "4748|4749|4750|4747"

# Test API health
curl http://localhost:4748/health
curl http://192.168.1.77:4748/health
```
