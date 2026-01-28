# Android ANR Diagnosis and Re-test

This document describes how to capture and diagnose Application Not Responding (ANR) on the FunWasHad Android app, and how to re-test after applying ANR mitigations.

## Prerequisites

- Android device or emulator connected via USB (or emulator with `adb connect`)
- `adb` on PATH (Android SDK platform-tools)
- FunWasHad Android app installed and running (or reproducing ANR)

## 1. Capture ANR diagnostics (quick)

Run the diagnosis script from the repo root:

```powershell
.\scripts\Diagnose-AndroidANR.ps1
```

Options:

- **`-PullTraces`** – Try to pull `/data/anr/traces.txt` (often requires root).
- **`-CaptureBugreport`** – Capture a full bugreport (see below; takes 1–2 minutes).
- **`-CopyToClipboard`** – Copy the ANR excerpt (or logcat) to the clipboard so you can paste (Ctrl+V) into Cursor Composer to share with AI.
- **`-PackageId <id>`** – Use a different package (default: `app.funwashad`).

Outputs (under repo root):

- **`android-anr-logcat.txt`** – Recent logcat filtered for ANR, Input dispatching, and app tag.
- **`android-anr-bugreports/`** – Full bugreport zip(s) when using `-CaptureBugreport`.
- **`android-anr-stack-excerpt.txt`** – Extracted ANR-related lines for the package when using `-CaptureBugreport`.

## 2. Capture full bugreport (for stack traces)

When you see an ANR dialog (or shortly after), capture a full bugreport so you have the main-thread stack:

```powershell
.\scripts\Diagnose-AndroidANR.ps1 -CaptureBugreport
```

This will:

1. Run `adb bugreport` and save the zip under **`android-anr-bugreports/`** (e.g. `bugreport-20260128-104500.zip`).
2. Optionally extract the zip and search for ANR traces for `app.funwashad`, writing an excerpt to **`android-anr-stack-excerpt.txt`**.

**Manual alternative:** Run `adb bugreport` from a folder where you want the zip, then:

- Open the generated **`bugreport-<device>-<date>.zip`**.
- Look under **`FS/data/anr/`** for trace files, or search the zip for **"ANR"** or **"app.funwashad"** to find the main-thread stack.

The stack shows what the main thread was doing when the system decided the app was not responding (e.g. blocking I/O, lock wait, or heavy work on the UI thread).

## 3. Re-test after ANR fixes

After applying code changes to reduce ANR (e.g. deferring ChatViewModel resolution, timeouts on image loading), re-test as follows:

1. **Clean run**
   - Force-stop the app on the device (Settings → Apps → FunWasHad → Force stop), or uninstall and reinstall.
   - Start the app from the device (or `dotnet build -t:Run` for the Android project).

2. **Startup**
   - Confirm the app opens without an ANR during the first few seconds.
   - If ANR still occurs at startup, capture a bugreport with `-CaptureBugreport` and inspect the main-thread stack (e.g. static `App` constructor, `BuildServiceProvider`, or first view load).

3. **Chat**
   - Open the **Chat** tab.
   - Confirm the chat UI appears and no ANR occurs when the view loads (ChatViewModel is now resolved off the UI thread).

4. **Places and images**
   - Open the **Places** tab.
   - Scroll the list (places with images use `UrlToImageConverter` with a 2s timeout).
   - Confirm no ANR while scrolling; images may appear with a short delay or placeholder if slow.

5. **Map and location**
   - Open the **Map** tab and leave it open for a short period.
   - Location updates are dispatched on a background thread and marshalled to the UI; confirm no ANR.

6. **Repeat**
   - Switch between Chat, Map, Places, and Settings a few times.
   - If ANR recurs, run **`.\scripts\Diagnose-AndroidANR.ps1 -CaptureBugreport`** and use the stack trace to find the next hotspot.

## 4. VS Code tasks

- **Run Task → android-anr-diagnose** – Quick diagnostics; copies logcat to clipboard. Paste (Ctrl+V) in Composer to share with AI.
- **Run Task → android-anr-capture-bugreport** – Full bugreport (1–2 min); copies ANR excerpt to clipboard. Paste (Ctrl+V) in Composer to share with AI.

## 5. Related

- **Script:** [scripts/Diagnose-AndroidANR.ps1](../../scripts/Diagnose-AndroidANR.ps1)
- **Scripts overview:** [scripts/README.md](../../scripts/README.md#-diagnose-androidanrps1)
- **ANR mitigations in code:** ChatViewModel resolution deferred in `ChatView.axaml.cs`; image load timeout in `FavoriteIconConverter.cs` (UrlToImageConverter); location and logging updates marshalled to UI thread in ViewModels and MapView.
