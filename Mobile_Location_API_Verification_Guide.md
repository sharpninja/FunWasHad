# Mobile App Location API Integration - Verification Guide

**Date:** 2026-01-07  
**Purpose:** Verify that the FWH Mobile App can successfully call the FWH Location Web API  
**Status:** ✅ Ready for Verification

---

## Overview

The FWH Mobile App includes a **LocationApiClient** that communicates with the **FWH Location Web API** to retrieve nearby businesses and points of interest. This guide provides step-by-step instructions to verify the integration.

### Architecture

```
┌─────────────────────────────────────┐
│      FWH.Mobile App                 │
│                                     │
│  ┌───────────────────────────────┐ │
│  │   LocationApiClient           │ │
│  │   (ILocationService impl)     │ │
│  └─────────────┬─────────────────┘ │
│                │ HTTP/HTTPS         │
└────────────────┼─────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────┐
│   FWH.Location.Api (Web API)        │
│                                     │
│  ┌───────────────────────────────┐ │
│  │   LocationsController         │ │
│  └─────────────┬─────────────────┘ │
│                │                    │
│  ┌─────────────▼─────────────────┐ │
│  │   OverpassLocationService     │ │
│  │   (OpenStreetMap API)         │ │
│  └───────────────────────────────┘ │
└─────────────────────────────────────┘
```

---

## Prerequisites

### Software Requirements
- ✅ .NET 9 SDK installed
- ✅ Visual Studio 2022 (recommended) or VS Code
- ✅ Android SDK (for Android testing)
- ✅ iOS development tools (for iOS testing, macOS only)

### Configuration Check
1. **API Configuration** - Default: `https://localhost:5001/`
2. **Environment Variable** (optional): `LOCATION_API_BASE_URL`
3. **Mobile App** - Configured in `App.axaml.cs`

---

## Verification Methods

### Method 1: Automated Integration Tests ⚡

Run the integration tests to verify API connectivity.

#### Step 1: Start the Location API

```bash
cd E:\github\FunWasHad
dotnet run --project FWH.Location.Api
```

**Expected Output:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

#### Step 2: Run Integration Tests

In a **new terminal**:

```bash
cd E:\github\FunWasHad
dotnet test FWH.Mobile.Tests --filter "FullyQualifiedName~LocationApiClientIntegrationTests"
```

**Note:** The integration tests are skipped by default. To enable them:

1. Open `FWH.Mobile.Tests\LocationApiClientIntegrationTests.cs`
2. Remove the `Skip = "..."` parameter from the `[Fact]` attributes
3. Run the tests again

**Expected Results:**
- ✅ Constructor tests pass
- ✅ `GetNearbyBusinessesAsync` test passes (when un-skipped)
- ✅ `GetClosestBusinessAsync` test passes (when un-skipped)

---

### Method 2: Manual API Testing 🧪

Test the API endpoints directly using curl or a browser.

#### Test 1: Health Check

```bash
curl https://localhost:5001/api/locations/nearby?latitude=37.7749&longitude=-122.4194&radiusMeters=1000 -k
```

**Expected Response:**
```json
[
  {
    "name": "Sample Business",
    "address": "123 Market St, San Francisco",
    "latitude": 37.7749,
    "longitude": -122.4194,
    "category": "restaurant",
    "tags": {},
    "distanceMeters": 245.7
  },
  ...
]
```

#### Test 2: Closest Business

```bash
curl https://localhost:5001/api/locations/closest?latitude=37.7749&longitude=-122.4194&maxDistanceMeters=1000 -k
```

**Expected Response:**
```json
{
  "name": "Closest Business",
  "address": "456 Main St, San Francisco",
  "latitude": 37.7750,
  "longitude": -122.4195,
  "category": "cafe",
  "tags": {},
  "distanceMeters": 50.2
}
```

#### Test 3: No Results (404)

```bash
curl -i https://localhost:5001/api/locations/closest?latitude=0&longitude=0&maxDistanceMeters=10 -k
```

**Expected Response:**
```
HTTP/1.1 404 Not Found
```

---

### Method 3: Mobile App End-to-End Testing 📱

Test the integration within the running mobile app.

#### Prerequisites
1. Location API must be running (see Method 1, Step 1)
2. Mobile app must be configured with correct API URL

#### For Desktop Testing

**Step 1:** Verify API URL in `App.axaml.cs`:
```csharp
var apiBaseAddress = Environment.GetEnvironmentVariable("LOCATION_API_BASE_URL") 
                     ?? "https://localhost:5001/";
```

**Step 2:** Run the Desktop app:
```bash
cd E:\github\FunWasHad
dotnet run --project FWH.Mobile\FWH.Mobile.Desktop
```

**Step 3:** Add test code to trigger location lookup (see Method 4)

#### For Android Testing

**Step 1:** Configure API URL for network access:
- Android emulator: `http://10.0.2.2:5000/` (maps to host's localhost)
- Physical device: `http://<YOUR_IP>:5000/` (get your IP via `ipconfig`)

**Step 2:** Set environment variable or update code:
```csharp
// In App.axaml.cs, change default for Android testing:
var apiBaseAddress = "http://10.0.2.2:5000/"; // Emulator
// OR
var apiBaseAddress = "http://192.168.1.100:5000/"; // Physical device
```

**Step 3:** Run on Android:
```bash
cd E:\github\FunWasHad
dotnet build FWH.Mobile\FWH.Mobile.Android -t:Run
```

#### For iOS Testing (macOS only)

**Step 1:** Configure API URL:
```csharp
var apiBaseAddress = "https://<YOUR_MAC_IP>:5001/";
```

**Step 2:** Run on iOS Simulator:
```bash
cd E:\github\FunWasHad
dotnet build FWH.Mobile\FWH.Mobile.iOS -t:Run
```

---

### Method 4: Add Test Usage to Mobile App 🔧

Add a test button or command to trigger location API calls.

#### Option A: Add to ChatViewModel

Edit `FWH.Common.Chat\ViewModels\ChatViewModel.cs`:

```csharp
using FWH.Common.Location;

public class ChatViewModel : ViewModelBase
{
    private readonly ILocationService _locationService;
    
    public ChatViewModel(IServiceProvider serviceProvider, ILocationService locationService)
    {
        _locationService = locationService;
        // ...existing code...
    }
    
    [RelayCommand]
    private async Task TestLocationApiAsync()
    {
        try
        {
            var businesses = await _locationService.GetNearbyBusinessesAsync(
                37.7749, -122.4194, 1000);
            
            var count = businesses.Count();
            // Add chat message with result
            ChatList.AddEntry(new ChatEntry<TextPayload>(
                new TextPayload($"Found {count} nearby businesses!")));
        }
        catch (Exception ex)
        {
            ChatList.AddEntry(new ChatEntry<TextPayload>(
                new TextPayload($"Error: {ex.Message}")));
        }
    }
}
```

#### Option B: Add Test Page

Create `FWH.Mobile\FWH.Mobile\Views\LocationTestPage.axaml`:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <StackPanel Margin="20">
        <TextBlock Text="Location API Test" FontSize="24" Margin="0,0,0,20"/>
        
        <Button Content="Test GetNearbyBusinesses" 
                Command="{Binding TestNearbyCommand}"
                Margin="0,0,0,10"/>
        
        <Button Content="Test GetClosestBusiness" 
                Command="{Binding TestClosestCommand}"
                Margin="0,0,0,10"/>
        
        <TextBlock Text="{Binding ResultMessage}" 
                   TextWrapping="Wrap"
                   Margin="0,20,0,0"/>
    </StackPanel>
</UserControl>
```

And corresponding ViewModel:

```csharp
public class LocationTestViewModel : ObservableObject
{
    private readonly ILocationService _locationService;
    
    [ObservableProperty]
    private string _resultMessage = "Click a button to test";
    
    public LocationTestViewModel(ILocationService locationService)
    {
        _locationService = locationService;
    }
    
    [RelayCommand]
    private async Task TestNearbyAsync()
    {
        try
        {
            ResultMessage = "Calling API...";
            var results = await _locationService.GetNearbyBusinessesAsync(
                37.7749, -122.4194, 1000);
            
            ResultMessage = $"✅ Success! Found {results.Count()} businesses\n" +
                          string.Join("\n", results.Take(5).Select(b => $"- {b.Name}"));
        }
        catch (Exception ex)
        {
            ResultMessage = $"❌ Error: {ex.Message}";
        }
    }
    
    [RelayCommand]
    private async Task TestClosestAsync()
    {
        try
        {
            ResultMessage = "Calling API...";
            var result = await _locationService.GetClosestBusinessAsync(
                37.7749, -122.4194, 1000);
            
            if (result != null)
            {
                ResultMessage = $"✅ Success!\n" +
                              $"Name: {result.Name}\n" +
                              $"Distance: {result.DistanceMeters:F1}m";
            }
            else
            {
                ResultMessage = "✅ API call succeeded, but no businesses found nearby";
            }
        }
        catch (Exception ex)
        {
            ResultMessage = $"❌ Error: {ex.Message}";
        }
    }
}
```

---

## Troubleshooting

### Issue: Connection Refused

**Symptoms:**
- `HttpRequestException: Connection refused`
- Tests fail with network errors

**Solutions:**
1. ✅ Verify Location API is running: `dotnet run --project FWH.Location.Api`
2. ✅ Check API URL configuration
3. ✅ For Android emulator, use `http://10.0.2.2:5000/`
4. ✅ For physical device, use your computer's IP address
5. ✅ Disable firewall or allow port 5000/5001

### Issue: SSL Certificate Errors

**Symptoms:**
- `HttpRequestException: SSL certificate problem`
- Certificate validation errors

**Solutions:**
1. ✅ Use HTTP instead of HTTPS for development
2. ✅ Trust the development certificate: `dotnet dev-certs https --trust`
3. ✅ For Android, configure network security (see below)

#### Android Network Security Configuration

Create `FWH.Mobile.Android\Resources\xml\network_security_config.xml`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<network-security-config>
    <base-config cleartextTrafficPermitted="true">
        <trust-anchors>
            <certificates src="system" />
            <certificates src="user" />
        </trust-anchors>
    </base-config>
    <domain-config cleartextTrafficPermitted="true">
        <domain includeSubdomains="true">10.0.2.2</domain>
        <domain includeSubdomains="true">localhost</domain>
    </domain-config>
</network-security-config>
```

Add to `AndroidManifest.xml`:

```xml
<application
    android:networkSecurityConfig="@xml/network_security_config"
    ...>
```

### Issue: Timeout Errors

**Symptoms:**
- `TaskCanceledException: The request was canceled`
- Operations timeout

**Solutions:**
1. ✅ Increase timeout in `LocationApiClientOptions`
2. ✅ Check network connectivity
3. ✅ Verify API is responsive (test with curl)
4. ✅ Check if firewall is blocking requests

### Issue: Empty Results

**Symptoms:**
- API returns empty array `[]`
- No businesses found

**Solutions:**
1. ✅ This is expected behavior if no businesses exist at coordinates
2. ✅ Try different coordinates (e.g., major city centers)
3. ✅ Verify Overpass API is accessible from the API server
4. ✅ Check API logs for Overpass API errors

---

## Configuration Reference

### LocationApiClientOptions

**File:** `FWH.Mobile\FWH.Mobile\Options\LocationApiClientOptions.cs`

```csharp
public sealed class LocationApiClientOptions
{
    public string BaseAddress { get; set; } = "https://localhost:5001/";
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
```

### Current Registration

**File:** `FWH.Mobile\FWH.Mobile\App.axaml.cs`

```csharp
var apiBaseAddress = Environment.GetEnvironmentVariable("LOCATION_API_BASE_URL") 
                     ?? "https://localhost:5001/";

services.Configure<LocationApiClientOptions>(options =>
{
    options.BaseAddress = apiBaseAddress;
    options.Timeout = TimeSpan.FromSeconds(30);
});

services.AddHttpClient<ILocationService, LocationApiClient>();
```

### Environment Variable (Optional)

Set the environment variable to override the default API URL:

**Windows:**
```cmd
set LOCATION_API_BASE_URL=http://192.168.1.100:5000/
dotnet run --project FWH.Mobile\FWH.Mobile.Desktop
```

**Linux/macOS:**
```bash
export LOCATION_API_BASE_URL=http://192.168.1.100:5000/
dotnet run --project FWH.Mobile/FWH.Mobile.Desktop
```

---

## API Endpoints Reference

### GET /api/locations/nearby

**Description:** Returns nearby businesses within a radius

**Query Parameters:**
- `latitude` (required): Latitude coordinate
- `longitude` (required): Longitude coordinate  
- `radiusMeters` (optional, default=30): Search radius in meters
- `categories` (optional): Array of category filters

**Example:**
```
GET /api/locations/nearby?latitude=37.7749&longitude=-122.4194&radiusMeters=1000&categories=restaurant&categories=cafe
```

**Response:** `200 OK` with array of `BusinessLocation`

### GET /api/locations/closest

**Description:** Returns the closest business to coordinates

**Query Parameters:**
- `latitude` (required): Latitude coordinate
- `longitude` (required): Longitude coordinate
- `maxDistanceMeters` (optional, default=1000): Maximum search distance

**Example:**
```
GET /api/locations/closest?latitude=37.7749&longitude=-122.4194&maxDistanceMeters=1000
```

**Response:** 
- `200 OK` with single `BusinessLocation`
- `404 Not Found` if no business within range

---

## Verification Checklist

Use this checklist to confirm the integration works:

### Unit Tests
- [ ] `LocationApiClient_Constructor_WithValidOptions_Succeeds` passes
- [ ] `LocationApiClient_Constructor_WithEmptyBaseAddress_Throws` passes
- [ ] `GetNearbyBusinessesAsync_WithInvalidCoordinates_HandlesGracefully` passes

### Integration Tests (with API running)
- [ ] `GetNearbyBusinessesAsync_WithValidCoordinates_ReturnsResults` passes
- [ ] `GetClosestBusinessAsync_WithValidCoordinates_ReturnsResult` passes

### Manual API Tests
- [ ] `/api/locations/nearby` endpoint responds
- [ ] `/api/locations/closest` endpoint responds
- [ ] Invalid coordinates return appropriate errors

### Mobile App Tests
- [ ] Desktop app can call API
- [ ] Android emulator can call API
- [ ] Android device can call API (optional)
- [ ] iOS simulator can call API (optional)
- [ ] Error handling works correctly

---

## Success Criteria

✅ **The mobile app successfully calls the Location Web API when:**

1. ✅ `LocationApiClient` is registered in DI container
2. ✅ `ILocationService` can be injected into ViewModels
3. ✅ API calls complete without exceptions
4. ✅ Results (even if empty) are returned correctly
5. ✅ Error handling works gracefully
6. ✅ Configuration is flexible (environment variable support)
7. ✅ Integration tests pass when API is running

---

## Next Steps

1. **Run the verification tests** using Method 1
2. **Test on Android/iOS** using Method 3
3. **Integrate into workflow** - Add location lookup to the chat workflow
4. **Add UI** - Create interface for displaying nearby businesses
5. **Production configuration** - Deploy API and configure production URLs

---

## Related Files

- `FWH.Mobile\FWH.Mobile\Services\LocationApiClient.cs` - HTTP client implementation
- `FWH.Mobile\FWH.Mobile\Options\LocationApiClientOptions.cs` - Configuration options
- `FWH.Mobile\FWH.Mobile\App.axaml.cs` - Service registration
- `FWH.Location.Api\Controllers\LocationsController.cs` - API endpoints
- `FWH.Location.Api\Program.cs` - API startup configuration
- `FWH.Mobile.Tests\LocationApiClientIntegrationTests.cs` - Integration tests

---

**Document Version:** 1.0  
**Last Updated:** 2026-01-07  
**Status:** ✅ Ready for Verification
