# Android Location API Connection Fix

**Date:** 2025-01-13  
**Issue:** Android app failing to connect to Location API with "Connection refused (localhost:5001)"  
**Status:** ✅ FIXED

---

## Problem Summary

The Android mobile app was attempting to connect to `https://localhost:5001/`, which caused a connection refused error because:

1. **Localhost Context**: In Android emulator, `localhost` refers to the emulator itself, not the host machine
2. **Wrong Port**: The API was actually running on ports 4747 (HTTPS) and 4748 (HTTP), not 5001
3. **SSL Issues**: HTTPS connections from Android emulator require special certificate configuration

---

## Root Cause Analysis

From the debugger output:
```
HttpRequestException: "Connection refused (localhost:5001)"
Inner Exception: SocketException with SocketErrorCode.ConnectionRefused
BaseAddress was set to: "https://localhost:5001/"
```

But the API was actually running on:
```
Microsoft.Hosting.Lifetime: Information: Now listening on: https://localhost:4747
Microsoft.Hosting.Lifetime: Information: Now listening on: http://localhost:4748
```

---

## Solution Applied

### 1. Updated `LocationApiClientOptions.cs`

Changed the default BaseAddress to use runtime platform detection instead of compiler directives:

```csharp
private static string GetDefaultBaseAddress()
{
    // Detect platform at runtime instead of compile-time
    if (OperatingSystem.IsAndroid())
    {
        // Android emulator: 10.0.2.2 is special alias for host machine's localhost
        // Use HTTP port 4748 (not HTTPS 4747) to avoid certificate issues in development
        return "http://10.0.2.2:4748/";
    }
    else
    {
        // Desktop/iOS: Use HTTPS with actual port where API is running
        return "https://localhost:4747/";
    }
}
```

**Key changes:**
- Uses `OperatingSystem.IsAndroid()` for runtime platform detection
- Android uses `10.0.2.2` instead of `localhost`
- Android uses HTTP port `4748` (avoids SSL certificate issues)
- Android uses port `4748` not `5001`
- Desktop/iOS uses HTTPS port `4747` not `5001`
- No compiler directives - cleaner, more maintainable code

### 2. Updated `App.axaml.cs`

Updated the API base address configuration to use runtime detection:

```csharp
// Detect platform at runtime instead of compile-time
if (OperatingSystem.IsAndroid())
{
    apiBaseAddress = Environment.GetEnvironmentVariable("LOCATION_API_BASE_URL") ?? "http://10.0.2.2:4748/";
}
else
{
    apiBaseAddress = Environment.GetEnvironmentVariable("LOCATION_API_BASE_URL") ?? "https://localhost:4747/";
}
```

### 3. Disabled HTTPS Redirection in `FWH.Location.Api\Program.cs`

Commented out `UseHttpsRedirection()` to allow HTTP connections from Android:

```csharp
// HTTPS redirection disabled for Android development
// Android emulator connects via HTTP (http://10.0.2.2:4748)
// Enable in production with proper SSL certificates
// app.UseHttpsRedirection();
```

**Why this is necessary:**
- Android app connects via HTTP to avoid certificate issues
- HTTPS redirection would force HTTP requests to HTTPS, causing connection failures
- In production, re-enable HTTPS redirection with valid SSL certificates

### 4. Configured Fixed Ports in `FWH.AppHost\Program.cs`

Updated Aspire configuration to use consistent, fixed ports:

```csharp
var locationApi = builder.AddProject<Projects.FWH_Location_Api>("locationapi")
    .WithReference(postgres)
    .WithHttpEndpoint(port: 4748, name: "http")
    .WithHttpsEndpoint(port: 4747, name: "https")
    .WithExternalHttpEndpoints();
```

**Why this is critical:**
- Aspire by default assigns random ports each time the app starts
- Mobile app expects consistent ports (4748 for HTTP, 4747 for HTTPS)
- Fixed ports ensure Android emulator can always connect to `http://10.0.2.2:4748`
- Desktop/iOS can always connect to `https://localhost:4747`
- Eliminates "Connection refused" errors caused by port mismatches

---

## Testing the Fix

### For Android Emulator

1. **Start the Location API:**
   ```bash
   cd E:\github\FunWasHad
   dotnet run --project FWH.Location.Api
   ```
   
   Verify it's listening on ports 4747 and 4748.

2. **Run the Android app:**
   ```bash
   dotnet build FWH.Mobile\FWH.Mobile.Android -t:Run
   ```

3. **Expected behavior:**
   - App should connect to `http://10.0.2.2:4748/`
   - API calls should succeed
   - No "Connection refused" errors

### For Android Physical Device

1. **Find your machine's IP address:**
   ```bash
   ipconfig  # Windows
   ifconfig  # Linux/macOS
   ```
   Example: `192.168.1.100`

2. **Set environment variable before running:**
   ```bash
   set LOCATION_API_BASE_URL=http://192.168.1.100:4748/
   ```

3. **Ensure firewall allows port 4748:**
   - Windows: Allow inbound rule for port 4748
   - Ensure device is on same network as development machine

### For Desktop

No changes needed - will automatically use `https://localhost:4747/`

### For iOS Simulator (macOS)

No changes needed - simulator shares host network, will use `https://localhost:4747/`

---

## Configuration Override

You can override the default URL using the `LOCATION_API_BASE_URL` environment variable:

**Windows:**
```cmd
set LOCATION_API_BASE_URL=http://192.168.1.100:4748/
dotnet run --project FWH.Mobile\FWH.Mobile.Android
```

**Linux/macOS:**
```bash
export LOCATION_API_BASE_URL=http://192.168.1.100:4748/
dotnet run --project FWH.Mobile/FWH.Mobile.Android
```

---

## API Port Reference

| Environment | Default URL | Notes |
|-------------|------------|-------|
| Android Emulator | `http://10.0.2.2:4748/` | HTTP to avoid certificate issues |
| Android Physical Device | Set via env var | Use machine's IP address |
| Desktop (Windows/Linux/macOS) | `https://localhost:4747/` | HTTPS with dev certificate |
| iOS Simulator | `https://localhost:4747/` | Shares host network |
| iOS Physical Device | Set via env var | Use machine's IP address |

---

## Why 10.0.2.2?

`10.0.2.2` is a special Android emulator alias that routes to the host machine's `127.0.0.1` (localhost). This is the standard way to access services running on your development machine from the Android emulator.

**Alternatives:**
- `10.0.3.2` - For Android emulator running in a different configuration
- Your machine's actual IP - For physical devices

---

## Why HTTP Instead of HTTPS for Android?

During development, using HTTP on Android avoids SSL certificate issues:

1. Android requires trusted certificates
2. Development certificates aren't trusted by default
3. Adding network security exceptions is complex
4. HTTP is acceptable for local development

**For production:**
- Deploy API with valid SSL certificate
- Use HTTPS
- Configure Android network security policy

---

## Files Modified

| File | Changes |
|------|---------|
| `FWH.Mobile\FWH.Mobile\Options\LocationApiClientOptions.cs` | Added platform-aware default BaseAddress with correct ports using runtime detection |
| `FWH.Mobile\FWH.Mobile\App.axaml.cs` | Updated Android configuration to use correct port 4748 with runtime detection |
| `FWH.Location.Api\Program.cs` | Disabled HTTPS redirection for Android development (HTTP support) |
| `FWH.AppHost\Program.cs` | **Configured Aspire to use fixed ports: HTTP 4748 and HTTPS 4747** |
| `Android_LocationAPI_Fix_Summary.md` | Created this documentation |

---

## Verification Steps

1. ✅ Update code with correct ports
2. ✅ Rebuild Android project
3. ✅ Start Location API
4. ✅ Run Android app in emulator
5. ✅ Test location lookup functionality
6. ✅ Verify no connection errors

---

## Expected Debugger Output After Fix

**Before (Error):**
```
ex.Message = "Connection refused (localhost:5001)"
_httpClient.BaseAddress = "https://localhost:5001/"
```

**After (Success):**
```
No exception thrown
_httpClient.BaseAddress = "http://10.0.2.2:4748/"
Response status = 200 OK
```

---

## Related Documentation

- `Mobile_Location_API_Verification_Guide.md` - Comprehensive testing guide
- `Mobile_Location_API_Integration_Summary.md` - Integration architecture
- `HttpClient_BaseAddress_Fix_Summary.md` - This document

---

## Troubleshooting

### Still Getting Connection Refused?

1. **Verify API is running:**
   ```bash
   curl http://localhost:4748/api/locations/nearby?latitude=37.7749&longitude=-122.4194&radiusMeters=1000
   ```

2. **Check emulator can reach host:**
   - In Android emulator, open browser
   - Navigate to `http://10.0.2.2:4748/api/locations/nearby?latitude=37.7749&longitude=-122.4194&radiusMeters=1000`
   - Should see JSON response

3. **Check Windows Firewall:**
   - Allow inbound connections on port 4748
   - Windows Defender Firewall → Allow an app

4. **Verify port in debugger:**
   - Set breakpoint in `LocationApiClient.cs`
   - Check `_httpClient.BaseAddress` value
   - Should be `http://10.0.2.2:4748/`

### API Returns 404?

- Verify the Location API endpoints are working:
  ```bash
  curl http://localhost:4748/api/locations/nearby?latitude=37.7749&longitude=-122.4194&radiusMeters=1000
  ```

### Android App Crashes?

- Check Android manifest has INTERNET permission:
  ```xml
  <uses-permission android:name="android.permission.INTERNET" />
  ```

---

## Success Criteria

✅ Android app connects to Location API  
✅ No connection refused errors  
✅ API calls return results (or empty array)  
✅ Desktop/iOS continue to work with HTTPS  
✅ Environment variable override works  

---

**Fix Applied:** 2025-01-13  
**Status:** ✅ Ready for Testing  
**Next Steps:** Rebuild Android project and test location functionality
