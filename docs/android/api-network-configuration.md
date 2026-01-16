# API Network Configuration for Android Development

## Overview

The FWH Location API and Marketing API are configured to listen on all network interfaces (`0.0.0.0`) during Development mode, allowing Android devices and emulators to connect via the host machine's IP address.

## Configuration

### Location API (FWH.Location.Api)

**Ports:**
- HTTP: `4748` (all interfaces in Development)
- HTTPS: `4747` (all interfaces in Development)

**Endpoints:**
- Android Emulator: `http://10.0.2.2:4748`
- Physical Android Device: `http://192.168.1.77:4748` (example, auto-detected)
- Desktop/iOS: `https://localhost:4747` or `http://localhost:4748`

### Marketing API (FWH.MarketingApi)

**Ports:**
- HTTP: `4750` (all interfaces in Development)
- HTTPS: `4749` (all interfaces in Development)

**Endpoints:**
- Android Emulator: `http://10.0.2.2:4750`
- Physical Android Device: `http://192.168.1.77:4750` (example, auto-detected)
- Desktop/iOS: `https://localhost:4749` or `http://localhost:4750`

## Implementation

### Kestrel Configuration

Both APIs use the following Kestrel configuration in `Program.cs`:

```csharp
if (builder.Environment.IsDevelopment())
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        // Listen on all interfaces for HTTP
        options.ListenAnyIP(port);
        
        // Listen on all interfaces for HTTPS
        options.ListenAnyIP(httpsPort, listenOptions =>
        {
            listenOptions.UseHttps();
        });
    });
}
```

### Why `0.0.0.0`?

**localhost (127.0.0.1):**
- Only accepts connections from the same machine
- Android devices/emulators cannot reach it from external network

**0.0.0.0 (all interfaces):**
- Accepts connections from any network interface
- Allows Android emulators via `10.0.2.2`
- Allows physical Android devices via LAN IP (e.g., `192.168.1.77`)
- Still accepts `localhost` connections from the host machine

## Security Considerations

### Development Mode Only

The `0.0.0.0` configuration is **only active in Development mode**:

```csharp
if (builder.Environment.IsDevelopment())
{
    // Listen on 0.0.0.0
}
```

In **Production**, APIs should:
- Listen on specific interfaces or use reverse proxy
- Enforce HTTPS with valid certificates
- Implement proper authentication and authorization
- Use API gateways and rate limiting

### Local Network Exposure

When listening on `0.0.0.0`, the APIs are accessible to:
- ✅ Your development machine (localhost)
- ✅ Devices on your local network (same Wi-Fi/Ethernet)
- ❌ Internet (blocked by router/firewall)

**Recommendations:**
1. Use trusted networks only (home/office Wi-Fi)
2. Avoid public Wi-Fi while developing
3. Enable firewall rules if concerned about LAN exposure
4. Use VPN for remote development if needed

## Integration with Android IP Detection

The Kestrel `0.0.0.0` configuration works in tandem with the Android MSBuild IP detection:

### Build-Time IP Detection

1. **MSBuild Target** (in `FWH.Mobile.Android.csproj`):
   - Detects host IP at build time
   - Injects IP into `appsettings.Development.json`
   - Example: `"HostIpAddress": "192.168.1.77"`

2. **API Configuration** (in `Program.cs`):
   - APIs listen on `0.0.0.0:4748` and `0.0.0.0:4750`
   - Accept connections from any local network IP

3. **Mobile App Connection**:
   - Loads config with detected IP
   - Connects to `http://192.168.1.77:4748`
   - API accepts connection because it listens on all interfaces

### Connection Flow

```
┌─────────────────────────────────────────────────────────────┐
│ 1. Developer builds Android app (DEBUG)                     │
│    MSBuild detects host IP: 192.168.1.77                    │
│    Injects into appsettings.Development.json                │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│ 2. APIs start via Aspire AppHost                            │
│    Location API listens on 0.0.0.0:4748                     │
│    Marketing API listens on 0.0.0.0:4750                    │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│ 3. Android app reads configuration                          │
│    LocationApiBaseUrl: http://192.168.1.77:4748             │
│    MarketingApiBaseUrl: http://192.168.1.77:4750            │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│ 4. App makes HTTP request to 192.168.1.77:4748              │
│    Request reaches host via local network                   │
│    API accepts on 0.0.0.0:4748                              │
│    Response returned to app                                 │
└─────────────────────────────────────────────────────────────┘
```

## Troubleshooting

### Android App Cannot Connect

**Symptom:**
- App shows connection errors
- Timeout when accessing APIs

**Diagnostic Steps:**

1. **Check API is running:**
   ```powershell
   # Check if process is listening on port
   netstat -an | Select-String "4748"
   
   # Should show: 0.0.0.0:4748  LISTENING
   ```

2. **Verify IP detection:**
   ```powershell
   # Check what IP was detected
   cat src\FWH.Mobile\FWH.Mobile.Android\obj\appsettings.Development.processed.json
   
   # Confirm HostIpAddress matches your machine IP
   ipconfig
   ```

3. **Test API accessibility:**
   ```powershell
   # From your development machine
   curl http://localhost:4748/health
   
   # From Android device (if adb connected)
   adb shell curl http://192.168.1.77:4748/health
   ```

4. **Check firewall:**
   ```powershell
   # Windows Firewall might block ports
   # Add inbound rule for ports 4748, 4749, 4750
   
   # Or temporarily disable for testing
   # (Not recommended for production)
   ```

### API Shows "Address Already in Use"

**Symptom:**
```
System.IO.IOException: Failed to bind to address http://0.0.0.0:4748: address already in use.
```

**Solutions:**

1. **Another process using the port:**
   ```powershell
   # Find process using port 4748
   netstat -ano | Select-String "4748"
   
   # Kill the process (find PID from netstat output)
   taskkill /PID <pid> /F
   ```

2. **Previous API instance still running:**
   - Stop all running instances
   - Rebuild and restart via AppHost

3. **Aspire orchestration conflict:**
   - Stop AppHost completely
   - Clean solution: `dotnet clean`
   - Rebuild: `dotnet build`
   - Start AppHost again

### Wrong IP Detected

**Symptom:**
- IP in config doesn't match your network
- App tries to connect to wrong address

**Solutions:**

1. **Multiple network adapters:**
   - Check `ipconfig` output
   - Identify correct adapter (Wi-Fi vs Ethernet vs VPN)
   - Use environment variables to override:
     ```powershell
     $env:LOCATION_API_BASE_URL = "http://192.168.1.77:4748/"
     dotnet build
     ```

2. **VPN interfering:**
   - Disconnect VPN during development
   - Or use VPN IP if APIs need to be accessed over VPN

3. **Clean and rebuild:**
   ```powershell
   dotnet clean src/FWH.Mobile/FWH.Mobile.Android/FWH.Mobile.Android.csproj
   dotnet build src/FWH.Mobile/FWH.Mobile.Android/FWH.Mobile.Android.csproj -c Debug
   ```

## Testing

### Verify API is listening on 0.0.0.0

```powershell
# Start the AppHost
dotnet run --project src/FWH.AppHost/FWH.AppHost.csproj

# In another terminal, check listening addresses
netstat -an | Select-String "4748|4749|4750"

# Expected output (Windows):
# TCP  0.0.0.0:4748     0.0.0.0:0      LISTENING
# TCP  0.0.0.0:4747     0.0.0.0:0      LISTENING
# TCP  0.0.0.0:4750     0.0.0.0:0      LISTENING
# TCP  0.0.0.0:4749     0.0.0.0:0      LISTENING
```

### Test from Different Sources

```powershell
# From localhost
curl http://localhost:4748/health

# From LAN IP
curl http://192.168.1.77:4748/health

# From Android emulator (using adb)
adb shell curl http://10.0.2.2:4748/health
```

All should return successful health check responses.

## Environment-Specific Behavior

| Environment | Listening Address | Accessible From |
|------------|-------------------|-----------------|
| **Development** | `0.0.0.0` | Localhost, LAN devices, Android emulator |
| **Staging** | Configured via deployment | Typically behind load balancer/gateway |
| **Production** | Configured via deployment | Public internet via reverse proxy/CDN |

## Related Configuration Files

- `src/FWH.Location.Api/Program.cs` - Location API Kestrel config
- `src/FWH.MarketingApi/Program.cs` - Marketing API Kestrel config
- `src/FWH.AppHost/Program.cs` - Aspire orchestration with port mappings
- `src/FWH.Mobile/FWH.Mobile.Android/DetectHostIp.ps1` - IP detection script
- `src/FWH.Mobile/FWH.Mobile.Android/FWH.Mobile.Android.csproj` - MSBuild target
- `src/FWH.Mobile/FWH.Mobile/Configuration/ApiSettings.cs` - Mobile app config model

## References

- [Kestrel web server in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel)
- [Configure endpoints for Kestrel](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints)
- [Android Emulator Networking](https://developer.android.com/studio/run/emulator-networking)
- [.NET Aspire overview](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview)
