# Android Location API Fix - Implementation Guide

**Date:** 2026-01-08  
**Issue:** Location API not being called on Android  
**Status:** ⚠️ **REQUIRES MANUAL UPDATES**

---

## Problem Identified

The Location API is configured with `https://localhost:5001/` which **does not work on Android**:

- On Android, `localhost` refers to the Android device/emulator itself, NOT your development machine
- The app tries to connect to a non-existent API on the device
- All Location API calls fail silently or timeout

---

## Root Cause

**File:** `FWH.Mobile/FWH.Mobile/App.axaml.cs` (Line 64)

```csharp
// ❌ WRONG for Android
var apiBaseAddress = Environment.GetEnvironmentVariable("LOCATION_API_BASE_URL") 
    ?? "https://localhost:5001/";
```

---

## Solution Applied

### 1. ✅ Updated App.axaml.cs

**File:** `FWH.Mobile/FWH.Mobile/App.axaml.cs`

**Change Applied:**
```csharp
// ✅ Platform-specific URL configuration
string apiBaseAddress;

#if ANDROID
// Android emulator: 10.0.2.2 is special alias for host machine's localhost
// Android physical device: Set LOCATION_API_BASE_URL to your machine's IP
apiBaseAddress = Environment.GetEnvironmentVariable("LOCATION_API_BASE_URL") 
    ?? "http://10.0.2.2:5000/";
#else
// Desktop/iOS: Use localhost or environment variable
apiBaseAddress = Environment.GetEnvironmentVariable("LOCATION_API_BASE_URL") 
    ?? "https://localhost:5001/";
#endif

services.Configure<LocationApiClientOptions>(options =>
{
    options.BaseAddress = apiBaseAddress;
    options.Timeout = TimeSpan.FromSeconds(30);
});
```

**Key Changes:**
- ✅ Uses `#if ANDROID` conditional compilation
- ✅ Android emulator: `http://10.0.2.2:5000/` (special alias for host)
- ✅ Allows environment variable override for physical devices
- ✅ Uses HTTP instead of HTTPS (simpler for development)
- ✅ Desktop/iOS: Still uses `https://localhost:5001/`

---

### 2. ⚠️ Update AndroidManifest.xml (MANUAL REQUIRED)

**File:** `FWH.Mobile/FWH.Mobile.Android/Properties/AndroidManifest.xml`

**Current Content:**
```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android" android:installLocation="auto">
	<uses-permission android:name="android.permission.INTERNET" />
	<uses-permission android:name="android.permission.CAMERA" />
	<uses-feature android:name="android.hardware.camera" android:required="false" />
	<uses-feature android:name="android.hardware.camera.any" android:required="false" />
	<uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" />
	<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" />
	<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
	<application android:label="FWH.Mobile" android:icon="@drawable/Icon" />
</manifest>
```

**Required Changes:**
1. Add `ACCESS_COARSE_LOCATION` permission
2. Add `android:usesCleartextTraffic="true"` to `<application>` tag

**Updated Content (Copy this):**
```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android" android:installLocation="auto">
	<uses-permission android:name="android.permission.INTERNET" />
	
	<!-- Camera permissions -->
	<uses-permission android:name="android.permission.CAMERA" />
	<uses-feature android:name="android.hardware.camera" android:required="false" />
	<uses-feature android:name="android.hardware.camera.any" android:required="false" />
	
	<!-- Location permissions -->
	<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
	<uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION" />
	
	<!-- File permissions for saving photos (if needed) -->
	<uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" />
	<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" />
	
	<!-- Enable cleartext HTTP traffic for development (localhost API calls) -->
	<application android:label="FWH.Mobile" android:icon="@drawable/Icon" android:usesCleartextTraffic="true" />
</manifest>
```

**Key Additions:**
- ✅ `ACCESS_COARSE_LOCATION` permission (for network-based location)
- ✅ `android:usesCleartextTraffic="true"` (allows HTTP calls in debug builds)

⚠️ **Security Note:** `usesCleartextTraffic="true"` is for development only. For production, use HTTPS and remove this attribute.

---

## Testing Instructions

### For Android Emulator

1. **Start Location API on your development machine:**
   ```bash
   cd E:\github\FunWasHad
   dotnet run --project FWH.Location.Api
   ```
   
   Wait for:
   ```
   Now listening on: http://localhost:5000
   Now listening on: https://localhost:5001
   ```

2. **Update AndroidManifest.xml** (manual step above)

3. **Build and deploy to emulator:**
   ```bash
   dotnet build FWH.Mobile.Android -t:Run
   ```

4. **Verify in logs:**
   - Look for successful HTTP requests to `http://10.0.2.2:5000/api/locations/nearby`
   - Should see business results in app

### For Android Physical Device

1. **Find your machine's IP address:**
   ```bash
   # Windows
   ipconfig
   # Look for IPv4 Address, e.g., 192.168.1.100
   
   # Linux/Mac
   ifconfig
   # or
   ip addr
   ```

2. **Start Location API on all interfaces:**
   ```bash
   cd E:\github\FunWasHad
   dotnet run --project FWH.Location.Api --urls "http://0.0.0.0:5000"
   ```

3. **Set environment variable before running app:**
   ```bash
   # Option 1: Set in your build configuration
   # Option 2: Hardcode in App.axaml.cs for testing:
   # apiBaseAddress = "http://192.168.1.100:5000/";  // Use YOUR IP
   ```

4. **Ensure firewall allows port 5000:**
   ```bash
   # Windows: Add inbound rule for port 5000
   # Or temporarily disable firewall for testing
   ```

5. **Build and deploy:**
   ```bash
   dotnet build FWH.Mobile.Android -t:Run
   ```

---

## Verification Checklist

### ✅ Code Changes
- [x] App.axaml.cs updated with platform-specific URL
- [ ] AndroidManifest.xml updated with cleartext traffic permission (MANUAL)

### ✅ Network Configuration
- [ ] Location API running on host machine
- [ ] Firewall allows port 5000 (for physical device)
- [ ] Android device on same network (for physical device)

### ✅ Permissions
- [x] INTERNET permission in manifest
- [ ] ACCESS_FINE_LOCATION permission in manifest (existing)
- [ ] ACCESS_COARSE_LOCATION permission in manifest (NEEDS ADDING)
- [ ] usesCleartextTraffic="true" in manifest (NEEDS ADDING)

### ✅ Runtime Testing
- [ ] GPS permissions granted in app
- [ ] Location API responds to requests
- [ ] App receives nearby businesses data
- [ ] Workflow displays results

---

## Debugging Tips

### 1. Check API is accessible from Android

Use `adb` to test connectivity:

```bash
# Connect to emulator/device
adb shell

# Test API endpoint
curl http://10.0.2.2:5000/api/locations/nearby?latitude=37.7749&longitude=-122.4194&radiusMeters=1000

# Should return JSON with businesses
```

### 2. View Android Logs

```bash
adb logcat | grep -i "location\|http\|network"
```

Look for:
- HTTP request attempts
- Connection errors
- Timeout messages
- Permission denials

### 3. Verify Location API is Running

```bash
# From your development machine
curl http://localhost:5000/api/locations/nearby?latitude=37.7749&longitude=-122.4194&radiusMeters=1000
```

Should return JSON array of businesses.

### 4. Test Permissions

In app, check:
```csharp
// AndroidGpsService logs permission status
System.Diagnostics.Debug.WriteLine($"Location available: {_gpsService.IsLocationAvailable}");
```

---

## Common Issues & Solutions

### Issue 1: "Connection refused" or "Network unreachable"

**Causes:**
- Location API not running
- Wrong IP address for physical device
- Firewall blocking connection

**Solutions:**
- Verify API is running: `curl http://localhost:5000/api/locations/nearby?...`
- For emulator: Use `http://10.0.2.2:5000/`
- For device: Use your machine's LAN IP (e.g., `http://192.168.1.100:5000/`)
- Disable firewall temporarily or add rule for port 5000

### Issue 2: "Cleartext HTTP traffic not permitted"

**Cause:**
- Android 9+ blocks HTTP by default

**Solution:**
- Add `android:usesCleartextTraffic="true"` to `<application>` tag in AndroidManifest.xml

### Issue 3: Timeout after 30 seconds

**Causes:**
- Location API is slow
- Network latency
- GPS taking too long

**Solutions:**
- Increase timeout in LocationApiClientOptions
- Check Location API logs for slow queries
- Test with smaller radius (e.g., 500m instead of 1000m)

### Issue 4: Empty results but no error

**Causes:**
- Wrong coordinates
- No businesses in area
- API returned empty array

**Solutions:**
- Test with known coordinates (San Francisco: 37.7749, -122.4194)
- Check Overpass API directly
- Verify radius is reasonable (100-2000m)

---

## Platform-Specific URLs Summary

| Platform | URL | Notes |
|----------|-----|-------|
| **Android Emulator** | `http://10.0.2.2:5000/` | Special alias for host localhost |
| **Android Physical** | `http://<YOUR_IP>:5000/` | Use machine's LAN IP (e.g., 192.168.1.100) |
| **iOS Simulator** | `https://localhost:5001/` | Simulator shares host network |
| **iOS Device** | `https://<YOUR_IP>:5001/` | Use machine's IP, may need HTTPS |
| **Desktop** | `https://localhost:5001/` | Direct localhost access |
| **Browser** | `https://localhost:5001/` | Browser on same machine |

---

## Production Configuration

For production deployment:

1. **Remove cleartext traffic:**
   ```xml
   <!-- Remove or set to false -->
   <application ... android:usesCleartextTraffic="false" />
   ```

2. **Use HTTPS:**
   ```csharp
   #if ANDROID
   apiBaseAddress = Environment.GetEnvironmentVariable("LOCATION_API_BASE_URL") 
       ?? "https://api.yourproduction.com/";
   #endif
   ```

3. **Add network security config (optional):**
   Create `Resources/xml/network_security_config.xml`:
   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <network-security-config>
       <base-config cleartextTrafficPermitted="false" />
       <!-- Only if needed for specific domains -->
       <domain-config cleartextTrafficPermitted="true">
           <domain includeSubdomains="true">yourdomain.com</domain>
       </domain-config>
   </network-security-config>
   ```

   Reference in AndroidManifest.xml:
   ```xml
   <application android:networkSecurityConfig="@xml/network_security_config" ... />
   ```

---

## Next Steps

1. ✅ **Code change applied** - App.axaml.cs updated
2. ⏳ **Manual update required** - Update AndroidManifest.xml (copy XML above)
3. ⏳ **Test on emulator** - Follow testing instructions
4. ⏳ **Test on physical device** - If available
5. ⏳ **Verify workflow** - Ensure nearby businesses action works

---

## Summary of Changes

### ✅ Automatic (Applied)
- App.axaml.cs: Platform-specific URL configuration
- Uses `http://10.0.2.2:5000/` for Android emulator
- Environment variable override support

### ⚠️ Manual (Required)
- AndroidManifest.xml: Add `ACCESS_COARSE_LOCATION` permission
- AndroidManifest.xml: Add `android:usesCleartextTraffic="true"`

### Result
Once manifest is updated, Location API calls from Android should work correctly!

---

**Status:** ⚠️ **PENDING MANIFEST UPDATE**  
**ETA:** 2 minutes (manual copy/paste required)  
**Priority:** 🔴 **HIGH** - Blocks Android location features

---

*Document Version: 1.0*  
*Date: 2026-01-08*  
*Issue: Android Location API not working*  
*Solution: Platform-specific URLs + Cleartext HTTP permission*
