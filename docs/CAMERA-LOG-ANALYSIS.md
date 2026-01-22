# Camera Log Analysis

## Summary (Latest Check)

**Logcat source:** `logcat_dump.txt` (adb logcat -d -t 3000), device ZD222QH58Q.

**Logcat review:** No camera-related logs were found. Grep for `FWH_CAMERA`, `TakePhoto`, and `OnActivityResult` returns zero matches.

**Observed in logcat:**
- **App (PID 15667):** `app.funwashad` GC and `monodroid-lref` / `monodroid-gref` only. No `FWH_CAMERA`, `AndroidCameraService`, or `MainActivity` camera logs.
- **GPS:** No `monodroid-assembly` / `AndroidGpsService` stack traces in this dump (those sometimes appear in other runs).
- **Conclusion:** Camera code path is not producing any log output — either not executed, or an older build without `FWH_CAMERA` logging is deployed.

## Implications

1. **Camera path may not be running**
   If you tap "Open Camera" and our code ran, we would see at least:
   - `FWH_CAMERA: TakePhotoAsync: ENTRY`
   - Then either `IsCameraAvailable` / activity / intent logs or `OnActivityResult` when returning from the camera.

2. **Likely causes**
   - **Old build deployed:** The running app might not include the latest camera + `FWH_CAMERA` logging. Rebuild (clean + build) and redeploy, then reproduce.
   - **Camera UI not hit:** The "Open Camera" button or the flow that triggers `TakePhotoAsync` might not be used (e.g. different screen or workflow).
   - **Logcat window:** Our logs might have scrolled out of the buffer before capture. Clear logcat, reproduce, then capture immediately.

## Changes Made for Easier Diagnosis

- **Tag `FWH_CAMERA`:** All camera-related `Log` calls use this tag.
- **`Log.Error` at key points:**
  - `TakePhotoAsync: ENTRY`
  - `MainActivity.OnActivityResult: RequestCode=...`
  - `OnActivityResult: Forwarding to camera service`
  - etc.
  These show up in `*:E` logcat, so they’re easier to find.

## How to Capture Logs After Rebuild/Redeploy

1. **Clean, rebuild, redeploy**
   ```bash
   dotnet build src\FWH.Mobile\FWH.Mobile.Android\FWH.Mobile.Android.csproj -c Debug -t:Clean,Build
   ```
   Then deploy from Visual Studio (or your usual method) to the device.

2. **Clear logcat and reproduce**
   ```bash
   adb logcat -c
   ```
   Open the app, go to the screen with "Open Camera", tap it, and (if the system camera opens) take a photo and return.

3. **Dump logs**
   ```bash
   adb logcat -d -t 5000 > logcat.txt
   ```

4. **Search for camera-related lines**
   ```bash
   findstr /i "FWH_CAMERA TakePhoto OnActivityResult AndroidCameraService MainActivity" logcat.txt
   ```
   Or with PowerShell:
   ```powershell
   Get-Content logcat.txt | Select-String -Pattern "FWH_CAMERA|TakePhoto|OnActivityResult|AndroidCameraService|MainActivity" -CaseSensitive:$false
   ```

5. **Error-level only (optional)**
   ```bash
   adb logcat -d *:E AndroidRuntime:E
   ```
   Our `Log.Error` messages use tag `FWH_CAMERA`.

## What We’re Looking For

- **`TakePhotoAsync: ENTRY`**
  Confirms the camera button flow reaches `TakePhotoAsync`.

- **`TakePhotoAsync: Camera not available`**
  `IsCameraAvailable` is false (e.g. permission or activity).

- **`TakePhotoAsync: Activity not available`**
  `Platform.CurrentActivity` is null.

- **`TakePhotoAsync: Camera intent could not be resolved`**
  No app handles `ACTION_IMAGE_CAPTURE` or permission/config issue.

- **`TakePhotoAsync: Starting camera activity`**
  Intent started successfully.

- **`MainActivity.OnActivityResult: RequestCode=...`**
  System delivered the result to our activity.

- **`OnActivityResult: ... HasTaskCompletionSource=...`**
  Camera service received the result and whether we still have a pending `TaskCompletionSource`.

Share the relevant `FWH_CAMERA` / `MainActivity` / `TakePhoto` / `OnActivityResult` excerpts from `logcat.txt` (or error-only dump) after following these steps.
