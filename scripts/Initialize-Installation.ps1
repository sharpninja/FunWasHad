# FunWasHad Installation Script
# This script initializes a new installation of the FunWasHad application
# including Docker setup, PostgreSQL configuration, and initial database setup

#Requires -Version 5.1

param(
    [Parameter(Mandatory=$false)]
    [string]$InstallPath = "E:\GitHub\FunWasHad",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipDockerCheck,
    
    [Parameter(Mandatory=$false)]
    [switch]$ResetDatabase,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipDependencies
)

# Script configuration
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# Color output functions
function Write-ColorOutput {
    param(
        [Parameter(Mandatory=$true)]
        [string]$Message,
        
        [Parameter(Mandatory=$false)]
        [ValidateSet("Success", "Error", "Warning", "Info", "Header")]
        [string]$Type = "Info"
    )
    
    $color = switch ($Type) {
        "Success" { "Green" }
        "Error"   { "Red" }
        "Warning" { "Yellow" }
        "Info"    { "Cyan" }
        "Header"  { "Magenta" }
    }
    
    $prefix = switch ($Type) {
        "Success" { "[✓]" }
        "Error"   { "[✗]" }
        "Warning" { "[!]" }
        "Info"    { "[i]" }
        "Header"  { "===" }
    }
    
    Write-Host "$prefix $Message" -ForegroundColor $color
}

function Write-Step {
    param([string]$Message)
    Write-ColorOutput "STEP: $Message" -Type Header
}

function Write-Success {
    param([string]$Message)
    Write-ColorOutput $Message -Type Success
}

function Write-ErrorMsg {
    param([string]$Message)
    Write-ColorOutput $Message -Type Error
}

function Write-Warning {
    param([string]$Message)
    Write-ColorOutput $Message -Type Warning
}

function Write-Info {
    param([string]$Message)
    Write-ColorOutput $Message -Type Info
}

# Main installation function
function Start-Installation {
    Write-Host ""
    Write-Host "╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
    Write-Host "║       FunWasHad Application Installation Script             ║" -ForegroundColor Magenta
    Write-Host "║       Version 1.0 - .NET 9 + Aspire + PostgreSQL            ║" -ForegroundColor Magenta
    Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Magenta
    Write-Host ""
    
    Write-Info "Installation Path: $InstallPath"
    Write-Info "Reset Database: $ResetDatabase"
    Write-Host ""
    
    # Step 1: Check prerequisites
    Write-Step "Checking Prerequisites"
    Test-Prerequisites
    
    # Step 2: Check Docker
    if (-not $SkipDockerCheck) {
        Write-Step "Checking Docker Installation"
        Test-DockerInstallation
    }
    
    # Step 3: Setup Docker volumes
    Write-Step "Setting Up Docker Volumes"
    Initialize-DockerVolumes
    
    # Step 4: Install dependencies
    if (-not $SkipDependencies) {
        Write-Step "Installing .NET Dependencies"
        Install-DotNetDependencies
    }
    
    # Step 5: Setup database
    Write-Step "Setting Up PostgreSQL Database"
    Initialize-Database
    
    # Step 6: Build solution
    Write-Step "Building Solution"
    Build-Solution
    
    # Step 7: Verify installation
    Write-Step "Verifying Installation"
    Test-Installation
    
    # Step 8: Display summary
    Show-InstallationSummary
}

function Test-Prerequisites {
    Write-Info "Checking required tools..."
    
    # Check .NET SDK
    try {
        $dotnetVersion = dotnet --version
        if ($dotnetVersion -match "^9\.") {
            Write-Success ".NET SDK 9.x found: $dotnetVersion"
        } else {
            Write-Warning ".NET SDK version is $dotnetVersion (requires 9.x)"
            Write-Info "Download from: https://dotnet.microsoft.com/download/dotnet/9.0"
            throw ".NET 9.x SDK is required"
        }
    }
    catch {
        Write-ErrorMsg ".NET SDK not found or not in PATH"
        throw ".NET SDK 9.x is required. Download from: https://dotnet.microsoft.com/download/dotnet/9.0"
    }
    
    # Check Git
    try {
        $gitVersion = git --version
        Write-Success "Git found: $gitVersion"
    }
    catch {
        Write-Warning "Git not found. Git is recommended but not required."
    }
    
    Write-Success "Prerequisites check completed"
}

function Test-DockerInstallation {
    Write-Info "Verifying Docker installation..."
    
    try {
        $dockerVersion = docker --version
        Write-Success "Docker found: $dockerVersion"
    }
    catch {
        Write-ErrorMsg "Docker not found or not running"
        Write-Info "Please install Docker Desktop from: https://www.docker.com/products/docker-desktop"
        throw "Docker is required"
    }
    
    # Test Docker is running
    try {
        docker ps | Out-Null
        Write-Success "Docker daemon is running"
    }
    catch {
        Write-ErrorMsg "Docker daemon is not running"
        Write-Info "Please start Docker Desktop"
        throw "Docker daemon must be running"
    }
    
    # Check docker-compose
    try {
        $composeVersion = docker compose version
        Write-Success "Docker Compose found: $composeVersion"
    }
    catch {
        Write-Warning "Docker Compose not found (might be using older Docker version)"
    }
}

function Initialize-DockerVolumes {
    Write-Info "Setting up Docker volumes for persistent storage..."
    
    $volumeName = "funwashad-postgres-data"
    
    # Check if volume already exists
    $existingVolume = docker volume ls --format "{{.Name}}" | Select-String -Pattern "^$volumeName$"
    
    if ($existingVolume) {
        if ($ResetDatabase) {
            Write-Warning "Existing volume found: $volumeName"
            $confirm = Read-Host "Do you want to DELETE the existing database volume? (yes/no)"
            
            if ($confirm -eq "yes") {
                Write-Info "Removing existing volume..."
                
                # Stop any containers using the volume
                $containers = docker ps -a --filter "volume=$volumeName" --format "{{.ID}}"
                if ($containers) {
                    Write-Info "Stopping containers using the volume..."
                    $containers | ForEach-Object { docker stop $_ }
                    $containers | ForEach-Object { docker rm $_ }
                }
                
                # Remove the volume
                docker volume rm $volumeName
                Write-Success "Volume removed"
                
                # Create new volume
                Write-Info "Creating new volume: $volumeName"
                docker volume create $volumeName
                Write-Success "New volume created"
            }
            else {
                Write-Info "Keeping existing volume"
            }
        }
        else {
            Write-Success "Using existing volume: $volumeName"
        }
    }
    else {
        Write-Info "Creating new volume: $volumeName"
        docker volume create $volumeName
        Write-Success "Volume created successfully"
    }
    
    # Inspect volume
    Write-Info "Volume details:"
    $volumeInfo = docker volume inspect $volumeName | ConvertFrom-Json
    Write-Host "  Name: $($volumeInfo.Name)" -ForegroundColor Gray
    Write-Host "  Driver: $($volumeInfo.Driver)" -ForegroundColor Gray
    Write-Host "  Mountpoint: $($volumeInfo.Mountpoint)" -ForegroundColor Gray
    
    Write-Success "Docker volumes configured"
}

function Install-DotNetDependencies {
    Write-Info "Restoring NuGet packages..."
    
    Push-Location $InstallPath
    
    try {
        # Restore packages
        dotnet restore --verbosity minimal
        Write-Success "NuGet packages restored"
    }
    catch {
        Write-ErrorMsg "Failed to restore packages: $_"
        throw
    }
    finally {
        Pop-Location
    }
}

function Initialize-Database {
    Write-Info "Initializing PostgreSQL database..."
    
    $volumeName = "funwashad-postgres-data"
    
    # Check if PostgreSQL container is already running
    $existingContainer = docker ps --format "{{.Names}}" | Select-String -Pattern "postgres"
    
    if ($existingContainer) {
        Write-Info "PostgreSQL container is already running: $existingContainer"
    }
    else {
        Write-Info "PostgreSQL will be started by Aspire when you run the application"
    }
    
    # Check for migration scripts
    $migrationPath = Join-Path $InstallPath "FWH.Location.Api\Migrations"
    if (Test-Path $migrationPath) {
        $migrationFiles = Get-ChildItem $migrationPath -Filter "*.sql"
        Write-Info "Found $($migrationFiles.Count) migration script(s)"
        
        foreach ($file in $migrationFiles) {
            Write-Info "  - $($file.Name)"
        }
        
        Write-Info "Migrations will be applied automatically when the application starts"
    }
    else {
        Write-Warning "No migration scripts found at: $migrationPath"
    }
    
    Write-Success "Database configuration complete"
}

function Build-Solution {
    Write-Info "Building the solution..."
    
    Push-Location $InstallPath
    
    try {
        # Build in Release mode
        Write-Info "Building in Release configuration..."
        dotnet build --configuration Release --no-restore --verbosity minimal
        Write-Success "Solution built successfully"
    }
    catch {
        Write-ErrorMsg "Build failed: $_"
        throw
    }
    finally {
        Pop-Location
    }
}

function Test-Installation {
    Write-Info "Running installation verification..."
    
    # Check critical files exist
    $criticalFiles = @(
        "FWH.AppHost\Program.cs",
        "FWH.Location.Api\Program.cs",
        "FWH.Mobile\FWH.Mobile\App.axaml.cs"
    )
    
    $missingFiles = @()
    foreach ($file in $criticalFiles) {
        $fullPath = Join-Path $InstallPath $file
        if (-not (Test-Path $fullPath)) {
            $missingFiles += $file
        }
    }
    
    if ($missingFiles.Count -gt 0) {
        Write-Warning "Some critical files are missing:"
        $missingFiles | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
    }
    else {
        Write-Success "All critical files present"
    }
    
    # Check Docker volume
    $volumeName = "funwashad-postgres-data"
    $volume = docker volume ls --format "{{.Name}}" | Select-String -Pattern "^$volumeName$"
    
    if ($volume) {
        Write-Success "Docker volume verified: $volumeName"
    }
    else {
        Write-Warning "Docker volume not found: $volumeName"
    }
    
    Write-Success "Installation verification complete"
}

function Show-InstallationSummary {
    Write-Host ""
    Write-Host "╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║              Installation Complete! ✓                        ║" -ForegroundColor Green
    Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Green
    Write-Host ""
    
    Write-Info "Installation Summary:"
    Write-Host ""
    Write-Host "  Installation Path:" -ForegroundColor Cyan
    Write-Host "    $InstallPath" -ForegroundColor White
    Write-Host ""
    Write-Host "  Docker Volume:" -ForegroundColor Cyan
    Write-Host "    funwashad-postgres-data (PostgreSQL data storage)" -ForegroundColor White
    Write-Host ""
    Write-Host "  Next Steps:" -ForegroundColor Cyan
    Write-Host "    1. Run the AppHost to start all services:" -ForegroundColor White
    Write-Host "       cd $InstallPath" -ForegroundColor Gray
    Write-Host "       dotnet run --project FWH.AppHost" -ForegroundColor Gray
    Write-Host ""
    Write-Host "    2. Access the application:" -ForegroundColor White
    Write-Host "       - Aspire Dashboard: http://localhost:15888" -ForegroundColor Gray
    Write-Host "       - Location API: http://localhost:4748" -ForegroundColor Gray
    Write-Host "       - PgAdmin: http://localhost:5050" -ForegroundColor Gray
    Write-Host ""
    Write-Host "    3. For Android emulator:" -ForegroundColor White
    Write-Host "       - Use API URL: http://10.0.2.2:4748" -ForegroundColor Gray
    Write-Host ""
    Write-Host "    4. For physical devices:" -ForegroundColor White
    Write-Host "       - Set LOCATION_API_BASE_URL to your machine's IP" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  Database Information:" -ForegroundColor Cyan
    Write-Host "    - Data persists in Docker volume" -ForegroundColor White
    Write-Host "    - Automatic migrations on startup" -ForegroundColor White
    Write-Host "    - PgAdmin available for management" -ForegroundColor White
    Write-Host ""
    Write-Host "  Volume Management Commands:" -ForegroundColor Cyan
    Write-Host "    - List volumes:   docker volume ls" -ForegroundColor Gray
    Write-Host "    - Inspect volume: docker volume inspect funwashad-postgres-data" -ForegroundColor Gray
    Write-Host "    - Backup volume:  .\scripts\backup-database.ps1" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  Documentation:" -ForegroundColor Cyan
    Write-Host "    - See PostgreSQL_LocalStorage_Configuration.md for details" -ForegroundColor White
    Write-Host "    - See Aspire_QuickReference.md for Aspire usage" -ForegroundColor White
    Write-Host ""
    
    Write-Success "Happy coding! 🚀"
}

# Error handling
trap {
    Write-Host ""
    Write-ErrorMsg "Installation failed: $_"
    Write-Host ""
    Write-Info "Troubleshooting tips:"
    Write-Host "  1. Ensure Docker Desktop is running" -ForegroundColor Gray
    Write-Host "  2. Verify .NET 9 SDK is installed" -ForegroundColor Gray
    Write-Host "  3. Check you have sufficient disk space" -ForegroundColor Gray
    Write-Host "  4. Run PowerShell as Administrator if permission issues occur" -ForegroundColor Gray
    Write-Host ""
    
    exit 1
}

# Run installation
try {
    Start-Installation
}
catch {
    Write-ErrorMsg "Installation aborted: $_"
    exit 1
}
