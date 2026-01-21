# Detect the host machine's IP address for Android DEBUG builds
# This script is called by MSBuild during the build process

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
}

# Update appsettings.Development.json with detected IP
$appsettingsPath = Join-Path $projectDir '..\FWH.Mobile\appsettings.Development.json'
$objPath = Join-Path $projectDir 'obj'
$processedPath = Join-Path $objPath 'appsettings.Development.processed.json'

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
    }
    catch {
        Write-Error "Failed to process appsettings.Development.json: $_"
        exit 1
    }
}
else {
    Write-Warning "appsettings.Development.json not found at: $appsettingsPath"
    exit 1
}
