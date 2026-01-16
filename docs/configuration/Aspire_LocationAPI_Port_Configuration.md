# Aspire Location API Port Configuration

**Date:** 2025-01-13  
**Purpose:** Document Aspire port configuration for Location API  
**Status:** ✅ Configured

---

## Overview

This document explains how the Location API is configured in Aspire with fixed ports to ensure consistent connectivity from the mobile app.

---

## Problem: Dynamic Port Allocation

By default, .NET Aspire assigns random ports to services each time they start. This causes problems for mobile apps that need to know the exact port number to connect to.

**Example of the problem:**
- First run: API starts on `http://localhost:5123`
- Second run: API starts on `http://localhost:6234`
- Mobile app configured for port 4748: **Connection fails** ❌

---

## Solution: Fixed Port Configuration

### Configuration in `FWH.AppHost\Program.cs`

```csharp
var locationApi = builder.AddProject<Projects.FWH_Location_Api>("locationapi")
    .WithReference(postgres)
    .WithHttpEndpoint(port: 4748, name: "http")      // Fixed HTTP port
    .WithHttpsEndpoint(port: 4747, name: "https")    // Fixed HTTPS port
    .WithExternalHttpEndpoints();
```

### Why These Ports?

| Port | Protocol | Used By | Purpose |
|------|----------|---------|---------|
| 4748 | HTTP | Android emulator | Avoid SSL certificate issues |
| 4747 | HTTPS | Desktop, iOS | Secure connections with dev certificates |

---

## How It Works

### 1. Aspire AppHost Startup

When you run `FWH.AppHost`, Aspire:
1. Reads the port configuration
2. Starts Location API with **exactly** these ports:
   - `http://localhost:4748`
   - `https://localhost:4747`
3. Makes them available via service discovery

### 2. Mobile App Connection

The mobile app uses platform-aware configuration:

**Android:**
```csharp
if (OperatingSystem.IsAndroid())
{
    apiBaseAddress = "http://10.0.2.2:4748/";  // Maps to host's port 4748
}
```

**Desktop/iOS:**
```csharp
else
{
    apiBaseAddress = "https://localhost:4747/";  // Direct HTTPS connection
}
```

### 3. Request Flow

```
┌─────────────────────────────────────────────────────────────┐
│                    Android Emulator                         │
│                                                             │
│  App makes request to: http://10.0.2.2:4748/api/...       │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           │ (10.0.2.2 is emulator's alias 
                           │  for host machine's localhost)
                           │
┌──────────────────────────▼──────────────────────────────────┐
│                    Host Machine (Windows)                    │
│                                                             │
│  ┌───────────────────────────────────────────────────────┐ │
│  │  Location API listening on:                           │ │
│  │  - http://localhost:4748  ◄── Android connects here   │ │
│  │  - https://localhost:4747 ◄── Desktop connects here   │ │
│  └───────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

---

## Verification

### Check Ports Are Configured

1. **Start Aspire AppHost:**
   ```bash
   dotnet run --project FWH.AppHost
   ```

2. **Check the console output:**
   ```
   Microsoft.Hosting.Lifetime: Information: Now listening on: https://localhost:4747
   Microsoft.Hosting.Lifetime: Information: Now listening on: http://localhost:4748
   ```

3. **Verify in Aspire Dashboard:**
   - Open browser to Aspire dashboard (URL shown in console)
   - Navigate to Resources → locationapi
   - Check endpoints show 4747 and 4748

### Test Connection from Android

1. **Start Aspire AppHost** (if not already running)

2. **Test from Android emulator:**
   ```bash
   # In Android emulator's browser or terminal
   curl http://10.0.2.2:4748/api/locations/nearby?latitude=37.7749&longitude=-122.4194&radiusMeters=1000
   ```

3. **Expected result:**
   - JSON response with business locations (or empty array)
   - No connection errors

### Test Connection from Desktop

```bash
curl https://localhost:4747/api/locations/nearby?latitude=37.7749&longitude=-122.4194&radiusMeters=1000 -k
```

---

## Alternative: Service Discovery

For services that can use service discovery, Aspire provides automatic endpoint resolution:

```csharp
// Instead of hard-coded URL:
services.AddHttpClient("locationapi", client => {
    client.BaseAddress = new Uri("http://locationapi");  // Service name
});
```

**However, this doesn't work for Android emulator because:**
- Service discovery requires the client to be part of the Aspire app model
- Mobile apps run independently and can't access Aspire's internal service registry
- Fixed ports are the best solution for mobile scenarios

---

## Aspire Endpoint Configuration Methods

### Method 1: WithHttpEndpoint (Used in this project)

```csharp
.WithHttpEndpoint(port: 4748, name: "http")
```

**Pros:**
- Explicit port numbers
- Easy to understand
- Works across all platforms

**Cons:**
- Port conflicts if another service uses the same port
- Must manage port numbers manually

### Method 2: WithEndpoint

```csharp
.WithEndpoint(port: 4748, scheme: "http", name: "http")
```

More verbose but gives more control over endpoint configuration.

### Method 3: Environment Variables

```csharp
.WithEnvironment("ASPNETCORE_URLS", "http://localhost:4748;https://localhost:4747")
```

**Not recommended** because:
- Bypasses Aspire's endpoint management
- Harder to maintain
- Less integration with Aspire dashboard

---

## Port Conflict Resolution

If ports 4747 or 4748 are already in use:

### Option 1: Change Ports (Recommended)

1. **Update `FWH.AppHost\Program.cs`:**
   ```csharp
   .WithHttpEndpoint(port: 5748, name: "http")
   .WithHttpsEndpoint(port: 5747, name: "https")
   ```

2. **Update mobile app configuration:**
   ```csharp
   // In LocationApiClientOptions.cs and App.axaml.cs
   return "http://10.0.2.2:5748/";  // Android
   return "https://localhost:5747/"; // Desktop
   ```

### Option 2: Find and Stop Conflicting Service

```bash
# Windows
netstat -ano | findstr :4748
taskkill /PID <pid> /F

# Linux/macOS
lsof -i :4748
kill -9 <pid>
```

---

## Production Deployment

For production, consider:

1. **Use Standard Ports**
   - HTTP: 80
   - HTTPS: 443

2. **Remove Fixed Port Configuration**
   ```csharp
   .WithHttpEndpoint(name: "http")     // Let cloud provider assign
   .WithHttpsEndpoint(name: "https")
   ```

3. **Configure Load Balancer**
   - Load balancer provides stable public endpoint
   - Backend services can use any ports

4. **Update Mobile App**
   - Use production domain: `https://api.funwashad.com`
   - Remove `10.0.2.2` logic (only for development)

---

## Troubleshooting

### Issue: "Address already in use" Error

**Symptoms:**
```
System.IO.IOException: Failed to bind to address http://127.0.0.1:4748
```

**Solutions:**
1. Check if another process is using the port
2. Change to different port numbers
3. Stop conflicting service

### Issue: Android Still Can't Connect

**Checklist:**
- ✅ Aspire AppHost is running
- ✅ Console shows "Now listening on: http://localhost:4748"
- ✅ Android app configured with `http://10.0.2.2:4748/`
- ✅ HTTPS redirection is disabled in Location API
- ✅ Windows Firewall allows port 4748
- ✅ No proxy blocking connections

### Issue: Desktop Works but Android Doesn't

This is typically an Android emulator networking issue:

1. **Verify emulator can reach host:**
   ```bash
   # In Android emulator terminal (adb shell)
   ping 10.0.2.2
   curl http://10.0.2.2:4748/api/locations/nearby?latitude=37&longitude=-122&radiusMeters=1000
   ```

2. **Check Android manifest has INTERNET permission:**
   ```xml
   <uses-permission android:name="android.permission.INTERNET" />
   ```

---

## Key Takeaways

✅ **Fixed ports ensure consistent connectivity**  
✅ **Port 4748 (HTTP) for Android emulator**  
✅ **Port 4747 (HTTPS) for desktop/iOS**  
✅ **Configured in Aspire AppHost, not in API project**  
✅ **Matches mobile app expectations**  
✅ **Eliminates random port assignment issues**  

---

## Related Documentation

- `Android_LocationAPI_Fix_Summary.md` - Complete fix documentation
- `Aspire_QuickReference.md` - General Aspire guidance
- `Mobile_Location_API_Verification_Guide.md` - Testing procedures

---

**Document Version:** 1.0  
**Date:** 2025-01-13  
**Status:** ✅ Configuration Complete
