# Start Application Script
# Starts the FunWasHad application with all services

#Requires -Version 5.1

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    
    [Parameter(Mandatory=$false)]
    [switch]$NoBrowser,
    
    [Parameter(Mandatory=$false)]
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
Write-Host "║            Starting FunWasHad Application                    ║" -ForegroundColor Magenta
Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Magenta
Write-Host ""

# Check if Docker is running
Write-Host "[i] Checking Docker..." -ForegroundColor Cyan
try {
    docker ps | Out-Null
    Write-Host "[✓] Docker is running" -ForegroundColor Green
}
catch {
    Write-Host "[✗] Docker is not running. Please start Docker Desktop." -ForegroundColor Red
    exit 1
}

# Check if volume exists
Write-Host "[i] Checking PostgreSQL volume..." -ForegroundColor Cyan
$volumeExists = docker volume ls --format "{{.Name}}" | Select-String -Pattern "^funwashad-postgres-data$"
if ($volumeExists) {
    Write-Host "[✓] PostgreSQL volume exists" -ForegroundColor Green
}
else {
    Write-Host "[!] PostgreSQL volume not found. It will be created automatically." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "[i] Starting Aspire AppHost..." -ForegroundColor Cyan
Write-Host "[i] Configuration: $Configuration" -ForegroundColor Cyan
Write-Host ""
Write-Host "Services will be available at:" -ForegroundColor Cyan
Write-Host "  - Aspire Dashboard: http://localhost:15888" -ForegroundColor Gray
Write-Host "  - Location API:     http://localhost:4748" -ForegroundColor Gray
Write-Host "  - Location API SSL: https://localhost:4747" -ForegroundColor Gray
Write-Host "  - PgAdmin:          http://localhost:5050" -ForegroundColor Gray
Write-Host ""

# Build command
$buildArgs = @("run", "--project", "FWH.AppHost", "--configuration", $Configuration)

if ($NoBrowser) {
    $env:ASPNETCORE_URLS = "http://localhost:15888"
}

if ($Verbose) {
    $buildArgs += "--verbosity", "detailed"
}

Write-Host "[i] Press Ctrl+C to stop all services" -ForegroundColor Yellow
Write-Host ""

# Start the application
try {
    & dotnet @buildArgs
}
catch {
    Write-Host ""
    Write-Host "[✗] Application stopped: $_" -ForegroundColor Red
}
