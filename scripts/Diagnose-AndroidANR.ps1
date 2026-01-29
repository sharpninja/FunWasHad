# Diagnose Android ANR (Application Not Responding)
# Attaches to the running FunWasHad app via adb and collects ANR diagnostics.
# Usage: .\scripts\Diagnose-AndroidANR.ps1 [-PackageId <id>] [-PullTraces] [-CaptureBugreport] [-CopyToClipboard]

param(
    [string]$PackageId = "app.funwashad",
    [switch]$PullTraces,
    [switch]$CaptureBugreport,
    [switch]$CopyToClipboard
)

$ErrorActionPreference = "Stop"

# Ensure adb is available
$adb = Get-Command adb -ErrorAction SilentlyContinue
if (-not $adb) {
    Write-Error "adb not found. Ensure Android SDK platform-tools is on PATH."
}

Write-Host "=== Android ANR diagnostics (package: $PackageId) ===" -ForegroundColor Cyan
Write-Host ""

# 1. Device and app process
Write-Host "1. Devices" -ForegroundColor Yellow
adb devices -l
$devices = (adb devices | Select-String "device$").Count
if ($devices -eq 0) {
    Write-Error "No device/emulator connected. Run: adb devices"
}
Write-Host ""

Write-Host "2. App process (PID)" -ForegroundColor Yellow
$pidLine = adb shell "ps -A -o PID,NAME" 2>$null | Select-String $PackageId
if ($pidLine) {
    Write-Host $pidLine
    if ($pidLine -match '\s*(\d+)\s+') { $appPid = $Matches[1] } else { $appPid = ($pidLine.ToString().Trim() -split "\s+", 2)[0] }
    Write-Host "PID: $appPid" -ForegroundColor Green
} else {
    Write-Host "App not running. Start the app on the device first." -ForegroundColor Red
    $appPid = $null
}
Write-Host ""

# Paths for outputs (repo root)
$repoRoot = Join-Path $PSScriptRoot ".."
$logPath = Join-Path $repoRoot "android-anr-logcat.txt"
$bugreportDir = Join-Path $repoRoot "android-anr-bugreports"

# 2. Recent logcat: ANR and app-related
Write-Host "3. Recent logcat (ANR + app)" -ForegroundColor Yellow
adb logcat -d -t 3000 2>$null | Select-String -Pattern "ANR|am_anr|not responding|Input dispatching|FATAL|$PackageId|FunWasHad" -CaseSensitive:$false | Set-Content $logPath -Encoding utf8
$lineCount = (Get-Content $logPath -ErrorAction SilentlyContinue | Measure-Object -Line).Lines
Write-Host "Saved $lineCount lines to $logPath"
if ($lineCount -gt 0) {
    Get-Content $logPath -Tail 80
}
Write-Host ""

# 3. Dropbox ANR entries (system may store ANR traces here)
Write-Host "4. Dropbox ANR entries (data_app_anr)" -ForegroundColor Yellow
$dropbox = adb shell "dumpsys dropbox --print" 2>$null
$anrLines = $dropbox | Select-String -Pattern "anr|ANR|data_app_anr" -CaseSensitive:$false
if ($anrLines) {
    $anrLines | ForEach-Object { Write-Host $_ }
} else {
    Write-Host "(No ANR entries in dropbox, or dumpsys not available)" -ForegroundColor Gray
}
Write-Host ""

# 4. Input and window state (what the system thinks is blocking)
Write-Host "5. Input dispatching state" -ForegroundColor Yellow
adb shell "dumpsys input" 2>$null | Select-String -Pattern "Application|Window|focus|ANR|dispatching" -Context 0,1 | Select-Object -First 30
Write-Host ""

# 5. CPU and load (optional context)
Write-Host "6. CPU load (top, 1 sample)" -ForegroundColor Yellow
adb shell "top -b -n 1 -m 15" 2>$null
Write-Host ""

# 6. Pull traces if requested (often requires root on device)
if ($PullTraces) {
    Write-Host "7. Pulling ANR traces" -ForegroundColor Yellow
    $traceDir = Join-Path $PSScriptRoot "..\android-anr-traces"
    if (-not (Test-Path $traceDir)) { New-Item -ItemType Directory -Path $traceDir -Force | Out-Null }
    # Modern Android: traces may be in /data/anr/ or reported via bugreport
    $traces = adb shell "ls -la /data/anr 2>/dev/null || echo 'No /data/anr access (root may be required)'"
    Write-Host $traces
    $pulled = adb pull "/data/anr/traces.txt" (Join-Path $traceDir "traces.txt") 2>&1
    Write-Host $pulled
    if ($pulled -match "0 files pulled") {
        Write-Host "Tip: Full ANR traces often require root. Use -CaptureBugreport for a full dump including ANR." -ForegroundColor Gray
    }
}
Write-Host ""

# 7. Capture full bugreport (includes ANR stacks; can take 1-2 minutes)
if ($CaptureBugreport) {
    Write-Host "7. Capturing full bugreport (may take 1-2 minutes)" -ForegroundColor Yellow
    if (-not (Test-Path $bugreportDir)) { New-Item -ItemType Directory -Path $bugreportDir -Force | Out-Null }
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $zipPath = Join-Path $bugreportDir "bugreport-$timestamp.zip"
    Write-Host "Output: $zipPath"
    $prevCwd = Get-Location
    try {
        Set-Location $bugreportDir
        adb bugreport "bugreport-$timestamp.zip" 2>&1 | ForEach-Object { Write-Host $_ }
    }
    finally {
        Set-Location $prevCwd
    }
    if (Test-Path $zipPath) {
        Write-Host "Bugreport saved: $zipPath" -ForegroundColor Green
        # Extract and search for ANR traces for this package
        $extractDir = Join-Path $bugreportDir "bugreport-$timestamp"
        $excerptPath = Join-Path $repoRoot "android-anr-stack-excerpt.txt"
        try {
            Expand-Archive -Path $zipPath -DestinationPath $extractDir -Force
            $anrDir = Join-Path $extractDir "FS\data\anr"
            $found = @()
            $excerptLines = @()
            if (Test-Path $anrDir) {
                Get-ChildItem -Path $anrDir -File -ErrorAction SilentlyContinue | ForEach-Object {
                    $content = Get-Content $_.FullName -Raw -ErrorAction SilentlyContinue
                    if ($content -match [regex]::Escape($PackageId) -or $content -match "ANR in") {
                        $found += $_.Name
                        $lines = Get-Content $_.FullName -ErrorAction SilentlyContinue
                        $block = $lines | Select-String -Pattern "ANR in|$PackageId|at |FWH\.|Avalonia" -Context 0,0 | Select-Object -First 200
                        $excerptLines += $block | ForEach-Object { $_.Line }
                    }
                }
            }
            if ($found.Count -eq 0) {
                Get-ChildItem -Path $extractDir -Recurse -File -Filter "*.txt" -ErrorAction SilentlyContinue | ForEach-Object {
                    $content = Get-Content $_.FullName -Raw -ErrorAction SilentlyContinue
                    if ($content -match [regex]::Escape($PackageId) -and $content -match "ANR") {
                        $rel = $_.FullName.Replace($extractDir, "").TrimStart("\")
                        $found += $rel
                    }
                }
            }
            $header = "ANR-related files: " + ($found -join ", ")
            $body = $excerptLines -join "`n"
            if ($found.Count -gt 0) {
                ($header + "`n`n" + $body) | Out-File -FilePath $excerptPath -Encoding utf8
                Write-Host "ANR excerpt written to: $excerptPath" -ForegroundColor Green
            }
            else {
                "No ANR trace files found for $PackageId in $zipPath. Open the zip and search for 'ANR' or '$PackageId'." | Out-File -FilePath $excerptPath -Encoding utf8
                Write-Host "No ANR files matched in zip. Open zip and search for 'ANR' or '$PackageId'. Excerpt path: $excerptPath" -ForegroundColor Gray
            }
        }
        catch {
            Write-Host "Could not extract/search bugreport: $_" -ForegroundColor Yellow
            Write-Host "Open the zip manually: $zipPath -> FS/data/anr/ or search for 'ANR'" -ForegroundColor Gray
        }
    }
    else {
        Write-Host "Bugreport may have been written to device current directory. Check $bugreportDir" -ForegroundColor Yellow
    }
}
Write-Host ""

Write-Host "=== Summary ===" -ForegroundColor Cyan
Write-Host "  Logcat excerpt:  $logPath"
Write-Host "  Bugreport dir:   $bugreportDir (use -CaptureBugreport to capture full dump)"
Write-Host "  To watch live:   adb logcat *:E | Select-String -Pattern ANR,am_anr,$PackageId"

# Copy bugreport data to clipboard for pasting into Composer
if ($CopyToClipboard) {
    $excerptPath = Join-Path $repoRoot "android-anr-stack-excerpt.txt"
    $parts = @()
    $parts += "--- Android ANR diagnostics (package: $PackageId) ---"
    $parts += "Logcat: $logPath"
    $parts += "Bugreport dir: $bugreportDir"
    $parts += ""
    $excerptContent = $null
    if (Test-Path $excerptPath) {
        $excerptContent = Get-Content $excerptPath -Raw -ErrorAction SilentlyContinue
    }
    if ($excerptContent -and $excerptContent.Trim().Length -gt 0) {
        $parts += "--- android-anr-stack-excerpt.txt ---"
        $parts += $excerptContent
    }
    else {
        $parts += "--- android-anr-logcat.txt (recent) ---"
        if (Test-Path $logPath) {
            $parts += Get-Content $logPath -Raw -ErrorAction SilentlyContinue
        }
    }
    $text = $parts -join "`n"
    if ($text.Trim().Length -gt 0) {
        Set-Clipboard -Value $text
        Write-Host ""
        Write-Host "Copied ANR data to clipboard. Paste (Ctrl+V) in Composer to share with AI." -ForegroundColor Green
    }
    else {
        Write-Host ""
        Write-Host "No ANR excerpt or logcat to copy. Run with -CaptureBugreport first." -ForegroundColor Yellow
    }
}
