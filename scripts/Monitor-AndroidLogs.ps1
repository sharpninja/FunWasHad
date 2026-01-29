# Monitor Android App Logs
# Usage: .\scripts\Monitor-AndroidLogs.ps1 [-Filter <pattern>] [-Clear] [-ErrorsOnly]

param(
    [string]$Filter = "FunWasHad|dotnet|Exception|Error|Fatal|Warning",
    [switch]$Clear,
    [switch]$ErrorsOnly
)

# Clear logs if requested
if ($Clear) {
    Write-Host "Clearing logcat buffer..." -ForegroundColor Yellow
    adb logcat -c
    Write-Host "Log buffer cleared." -ForegroundColor Green
    return
}

# Build filter arguments
$tagFilters = @("*:E", "*:W", "FunWasHad:*", "dotnet:*", "mono:*", "MonoDroid:*")

if ($ErrorsOnly) {
    $tagFilters = @("*:E", "FunWasHad:*", "dotnet:*")
    $Filter = "Exception|Error|Fatal|FunWasHad"
}

Write-Host "Monitoring Android logs..." -ForegroundColor Cyan
Write-Host "Filter: $Filter" -ForegroundColor Gray
Write-Host "Press Ctrl+C to stop" -ForegroundColor Gray
Write-Host ""

try {
    adb logcat $tagFilters | Select-String -Pattern $Filter -CaseSensitive:$false
}
catch {
    Write-Host "`nMonitoring stopped." -ForegroundColor Yellow
}
