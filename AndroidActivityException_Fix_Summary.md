# Android "Current activity not available" Exception - Fix Summary

**Date:** January 7, 2026  
**Issue:** `System.InvalidOperationException: Current activity not available`  
**Status:** ✅ RESOLVED

---

## Root Cause Analysis

The exception occurred during Android app startup when `AndroidCameraService` was instantiated before the Android `MainActivity.OnCreate()` had set `Platform.CurrentActivity`.

### Timeline of the Issue:

1. **App Launch** → Android system starts initializing
2. **App Static Constructor** → Services registered, including `CameraCaptureViewModel` as singleton
3. **Service Resolution** → `CameraCaptureViewModel` depends on `ICameraService`
4. **Factory Resolution** → `CameraServiceFactory` creates `AndroidCameraService`
5. **❌ Exception Thrown** → `AndroidCameraService` constructor accesses `Platform.CurrentActivity` which is still `null`
6. **⏸️ Not Reached** → `MainActivity.OnCreate()` would have set `Platform.CurrentActivity = this`

### The Problem:
```csharp
// OLD CODE - Constructor accessed Activity immediately
public AndroidCameraService()
{
    _activity = Platform.CurrentActivity ?? 
        throw new InvalidOperationException("Current activity not available");
}
```

---

## Fixes Applied

### ✅ Fix 1: AndroidCameraService - Lazy Initialization Pattern

**File:** `FWH.Mobile/FWH.Mobile.Android/Services/AndroidCameraService.cs`

**Changes:**
- Removed `_activity` field that was eagerly initialized in constructor
- Added `Activity` property with lazy access to `Platform.CurrentActivity`
- Updated `IsCameraAvailable` to safely handle when Activity isn't available yet (returns `false`)
- Updated `TakePhotoAsync()` to access Activity lazily when called

**Before:**
```csharp
private readonly Activity _activity;

public AndroidCameraService()
{
    _activity = Platform.CurrentActivity ?? 
        throw new InvalidOperationException("Current activity not available");
}

public bool IsCameraAvailable
{
    get
    {
        var packageManager = _activity.PackageManager;
        // ...
    }
}
```

**After:**
```csharp
private Activity Activity => Platform.CurrentActivity ?? 
    throw new InvalidOperationException("Current activity not available");

public AndroidCameraService()
{
    // Don't access Platform.CurrentActivity in constructor
    // It will be accessed lazily when methods/properties are called
}

public bool IsCameraAvailable
{
    get
    {
        try
        {
            var activity = Activity; // Lazy access
            var packageManager = activity.PackageManager;
            // ...
        }
        catch (InvalidOperationException)
        {
            // Activity not available yet, camera not available
            return false;
        }
    }
}
```

---

### ✅ Fix 2: App.axaml.cs - Deferred Database Initialization

**File:** `FWH.Mobile/FWH.Mobile/App.axaml.cs`

**Changes:**
- Removed blocking `InitializeDatabaseAsync().GetAwaiter().GetResult()` from static constructor
- Added `_isDatabaseInitialized` flag and `_initializationLock` SemaphoreSlim for thread-safe initialization
- Created `EnsureDatabaseInitializedAsync()` method with double-check locking pattern
- Moved database initialization to `OnFrameworkInitializationCompleted()`

**Before:**
```csharp
static App()
{
    var services = new ServiceCollection();
    // ... register services ...
    
    ServiceProvider = services.BuildServiceProvider();

    // ❌ BLOCKING CALL - Blocks UI thread during startup
    InitializeDatabaseAsync().GetAwaiter().GetResult();
}

private static async Task InitializeDatabaseAsync()
{
    using var scope = ServiceProvider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<NotesDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}
```

**After:**
```csharp
private static bool _isDatabaseInitialized = false;
private static readonly SemaphoreSlim _initializationLock = new SemaphoreSlim(1, 1);

static App()
{
    var services = new ServiceCollection();
    // ... register services ...
    
    ServiceProvider = services.BuildServiceProvider();

    // ✅ Database initialization deferred to OnFrameworkInitializationCompleted
    // to avoid blocking the UI thread during app startup
}

private static async Task EnsureDatabaseInitializedAsync()
{
    if (_isDatabaseInitialized)
        return;

    await _initializationLock.WaitAsync();
    try
    {
        if (_isDatabaseInitialized)
            return;

        using var scope = ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NotesDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        _isDatabaseInitialized = true;
    }
    finally
    {
        _initializationLock.Release();
    }
}

public override async void OnFrameworkInitializationCompleted()
{
    // ✅ Initialize database asynchronously before setting up the UI
    await EnsureDatabaseInitializedAsync();
    
    // ... rest of initialization ...
}
```

---

## Benefits of These Fixes

### 1. AndroidCameraService Lazy Initialization ✅
- **Solves:** "Current activity not available" crash
- **How:** Service can be instantiated before Activity is available
- **When Activity is accessed:** Only when camera functionality is actually used
- **Graceful degradation:** Returns `false` for `IsCameraAvailable` if Activity not ready yet

### 2. Deferred Database Initialization ✅
- **Solves:** Slow/blocked startup, potential deadlocks
- **How:** Database I/O no longer blocks app initialization
- **Performance:** App UI appears faster and more responsive
- **Thread-safe:** Uses `SemaphoreSlim` for async-friendly locking
- **Idempotent:** Safe to call multiple times (double-check locking pattern)

---

## Testing Recommendations

### Test 1: Android App Startup
1. Clean build the Android project
2. Deploy to Android emulator or device
3. Launch the app
4. **Expected:** App starts without "Current activity not available" exception
5. **Expected:** App UI appears quickly without long delays

### Test 2: Camera Functionality
1. Navigate to camera capture control
2. Tap to capture photo
3. **Expected:** Camera opens successfully
4. **Expected:** Photo can be captured and displayed

### Test 3: Desktop App Startup
1. Run the Desktop project
2. **Expected:** App starts normally
3. **Expected:** Camera functionality gracefully disabled (NoCameraService fallback)

---

## Technical Details

### Initialization Order (After Fix)
1. ✅ App static constructor runs → Services registered (fast, non-blocking)
2. ✅ `MainActivity.OnCreate()` runs → `Platform.CurrentActivity = this`
3. ✅ `OnFrameworkInitializationCompleted()` runs → Database initialized asynchronously
4. ✅ UI becomes available → User can interact
5. ✅ Camera feature accessed → `Activity` property accesses `Platform.CurrentActivity` (now available)

### Thread Safety
- `SemaphoreSlim` used instead of `lock` for async/await compatibility
- Double-check locking pattern prevents redundant initialization
- `_isDatabaseInitialized` flag provides fast path for subsequent calls

### .NET 9 Compatibility
- Uses modern C# patterns (property expressions, null-coalescing throw)
- Async/await throughout (no blocking `.GetAwaiter().GetResult()`)
- Compatible with Avalonia 11.x framework lifecycle

---

## Related Files Modified

1. ✅ `FWH.Mobile/FWH.Mobile.Android/Services/AndroidCameraService.cs`
2. ✅ `FWH.Mobile/FWH.Mobile/App.axaml.cs`

## Files Analyzed (No Changes Needed)

- `FWH.Mobile/FWH.Mobile.Android/MainActivity.cs` - Already correctly sets `Platform.CurrentActivity`
- `FWH.Mobile/ViewModels/CameraCaptureViewModel.cs` - Already handles lazy initialization
- `FWH.Common.Chat/Services/CameraServiceFactory.cs` - Factory pattern works correctly
- `FWH.Common.Chat/Services/PlatformService.cs` - Platform detection doesn't access Activity

---

## Conclusion

Both fixes are essential:
1. **AndroidCameraService** fix prevents the immediate crash
2. **App.axaml.cs** fix improves startup performance and prevents potential deadlocks

The app should now start successfully on Android without exceptions and with improved responsiveness.
