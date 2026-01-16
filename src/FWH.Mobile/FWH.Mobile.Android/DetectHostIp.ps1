# Detect the host machine's IP address for Android DEBUG builds
# This script is called by MSBuild during the build process

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Android Host IP Detection" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan

$projectDir = $args[0]

# Detect the host machine's IP address
try {
    # Method 1: Get network adapters with valid IP addresses
    $hostIp = Get-NetIPAddress -AddressFamily IPv4 -PrefixOrigin Dhcp, Manual -ErrorAction SilentlyContinue |
        Where-Object { 
            $_.IPAddress -notlike '127.*' -and 
            $_.IPAddress -notlike '169.254.*' -and
            $_.IPAddress -match '^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$'
        } |
        Sort-Object -Property PrefixOrigin -Descending |
        Select-Object -First 1 -ExpandProperty IPAddress
    
    # Fallback: Try DNS method
    if ([string]::IsNullOrEmpty($hostIp)) {
        Write-Host "Method 1 failed, trying DNS-based detection..." -ForegroundColor Yellow
        $hostName = [System.Net.Dns]::GetHostName()
        $hostIp = [System.Net.Dns]::GetHostEntry($hostName).AddressList |
            Where-Object { 
                $_.AddressFamily -eq 'InterNetwork' -and
                $_.ToString() -notlike '127.*' -and
                $_.ToString() -notlike '169.254.*'
            } |
            Select-Object -First 1 -ExpandProperty IPAddressToString
    }
}
catch {
    Write-Warning "IP detection failed: $_"
}

# Final fallback to Android emulator alias
if ([string]::IsNullOrEmpty($hostIp)) {
    $hostIp = '10.0.2.2'
    Write-Host "Using Android emulator default: $hostIp" -ForegroundColor Yellow
}
else {
    Write-Host "Detected Host IP: $hostIp" -ForegroundColor Green
}

# Update appsettings.Development.json with detected IP
$appsettingsPath = Join-Path $projectDir '..\FWH.Mobile\appsettings.Development.json'
$objPath = Join-Path $projectDir 'obj'
$processedPath = Join-Path $objPath 'appsettings.Development.processed.json'

Write-Host "Source: $appsettingsPath" -ForegroundColor Gray
Write-Host "Target: $processedPath" -ForegroundColor Gray

if (Test-Path $appsettingsPath) {
    try {
        # Create obj directory if it doesn't exist
        if (-not (Test-Path $objPath)) {
            New-Item -ItemType Directory -Path $objPath -Force | Out-Null
        }
        
        # Read and update the JSON
        $content = Get-Content $appsettingsPath -Raw
        $updatedContent = $content -replace '"HOST_IP_PLACEHOLDER"', """$hostIp"""
        
        # Save to obj folder (temporary location)
        Set-Content -Path $processedPath -Value $updatedContent -NoNewline -Force
        
        Write-Host "✓ Generated processed config" -ForegroundColor Green
        Write-Host "✓ HostIpAddress set to: $hostIp" -ForegroundColor Cyan
        Write-Host "=====================================" -ForegroundColor Cyan
    }
    catch {
        Write-Error "Failed to process appsettings.Development.json: $_"
        Write-Host "=====================================" -ForegroundColor Cyan
        exit 1
    }
}
else {
    Write-Warning "appsettings.Development.json not found at: $appsettingsPath"
    Write-Host "=====================================" -ForegroundColor Cyan
    exit 1
}
