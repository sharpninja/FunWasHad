<#
.SYNOPSIS
  Cleans up Docker containers, volumes, and images related to FunWasHad.

.DESCRIPTION
  This script provides a convenient way to clean up Docker resources created by
  the FunWasHad application. It can clean containers, volumes, and images separately
  or all at once. By default, the script shows usage information if no options are provided.

.PARAMETER All
  When specified, cleans all Docker resources (containers, volumes, and images).

.PARAMETER Containers
  When specified, cleans only containers related to FunWasHad.

.PARAMETER Volumes
  When specified, cleans only volumes related to FunWasHad.
  ⚠️ WARNING: This will DELETE all database data!

.PARAMETER Images
  When specified, cleans only Docker images related to FunWasHad.

.PARAMETER Force
  When specified, skips confirmation prompts and proceeds with cleanup immediately.

.EXAMPLE
  # Show usage information
  .\Clean-DockerResources.ps1

.EXAMPLE
  # Clean all resources (interactive)
  .\Clean-DockerResources.ps1 -All

.EXAMPLE
  # Clean all resources without confirmation
  .\Clean-DockerResources.ps1 -All -Force

.EXAMPLE
  # Clean only containers
  .\Clean-DockerResources.ps1 -Containers

.EXAMPLE
  # Clean only volumes (deletes data!)
  .\Clean-DockerResources.ps1 -Volumes

.EXAMPLE
  # Clean only images
  .\Clean-DockerResources.ps1 -Images

.NOTES
  - Requires Docker to be installed and running.
  - ⚠️ WARNING: Volume cleanup will DELETE all database data permanently!
  - The script only removes resources with "funwashad" or "fwh" in their names.
  - After cleanup, the script displays remaining resources for verification.
#>

#Requires -Version 5.1

param(
    [Parameter(Mandatory=$false)]
    [switch]$All,

    [Parameter(Mandatory=$false)]
    [switch]$Containers,

    [Parameter(Mandatory=$false)]
    [switch]$Volumes,

    [Parameter(Mandatory=$false)]
    [switch]$Images,

    [Parameter(Mandatory=$false)]
    [switch]$Force
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Yellow
Write-Host "║            Docker Resources Cleanup                          ║" -ForegroundColor Yellow
Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Yellow
Write-Host ""

# If no specific flags, show usage
if (-not ($All -or $Containers -or $Volumes -or $Images)) {
    Write-Host "Usage: .\Clean-DockerResources.ps1 [options]" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Options:" -ForegroundColor Cyan
    Write-Host "  -All         Clean all Docker resources" -ForegroundColor Gray
    Write-Host "  -Containers  Clean containers only" -ForegroundColor Gray
    Write-Host "  -Volumes     Clean volumes only" -ForegroundColor Gray
    Write-Host "  -Images      Clean images only" -ForegroundColor Gray
    Write-Host "  -Force       Skip confirmation prompts" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Example:" -ForegroundColor Cyan
    Write-Host "  .\Clean-DockerResources.ps1 -All -Force" -ForegroundColor Gray
    Write-Host ""
    exit 0
}

# Confirmation
if (-not $Force) {
    Write-Host "[!] This will remove Docker resources related to FunWasHad" -ForegroundColor Yellow

    if ($All -or $Volumes) {
        Write-Host "[!] WARNING: Volume cleanup will DELETE all database data!" -ForegroundColor Red
    }

    $confirm = Read-Host "Continue? (yes/no)"
    if ($confirm -ne "yes") {
        Write-Host "[i] Cleanup cancelled" -ForegroundColor Cyan
        exit 0
    }
}

Write-Host ""

# Clean Containers
if ($All -or $Containers) {
    Write-Host "[i] Cleaning containers..." -ForegroundColor Cyan

    # Stop running containers
    $runningContainers = docker ps --filter "name=funwashad" --format "{{.ID}}"
    if ($runningContainers) {
        Write-Host "    Stopping running containers..." -ForegroundColor Gray
        $runningContainers | ForEach-Object { docker stop $_ }
        Write-Host "[✓] Containers stopped" -ForegroundColor Green
    }

    # Remove stopped containers
    $stoppedContainers = docker ps -a --filter "name=funwashad" --format "{{.ID}}"
    if ($stoppedContainers) {
        Write-Host "    Removing containers..." -ForegroundColor Gray
        $stoppedContainers | ForEach-Object { docker rm $_ }
        Write-Host "[✓] Containers removed" -ForegroundColor Green
    }
    else {
        Write-Host "[i] No containers to remove" -ForegroundColor Cyan
    }
}

# Clean Volumes
if ($All -or $Volumes) {
    Write-Host "[i] Cleaning volumes..." -ForegroundColor Cyan
    Write-Host "[!] This will DELETE all database data!" -ForegroundColor Red

    $volumeToRemove = "funwashad-postgres-data"
    $volumeExists = docker volume ls --format "{{.Name}}" | Select-String -Pattern "^$volumeToRemove$"

    if ($volumeExists) {
        if (-not $Force) {
            $confirmVolume = Read-Host "Really delete volume '$volumeToRemove'? (yes/no)"
            if ($confirmVolume -ne "yes") {
                Write-Host "[i] Volume cleanup skipped" -ForegroundColor Cyan
            }
            else {
                docker volume rm $volumeToRemove
                Write-Host "[✓] Volume removed: $volumeToRemove" -ForegroundColor Green
            }
        }
        else {
            docker volume rm $volumeToRemove
            Write-Host "[✓] Volume removed: $volumeToRemove" -ForegroundColor Green
        }
    }
    else {
        Write-Host "[i] No volumes to remove" -ForegroundColor Cyan
    }
}

# Clean Images
if ($All -or $Images) {
    Write-Host "[i] Cleaning images..." -ForegroundColor Cyan

    # Find FunWasHad related images
    $images = docker images --format "{{.Repository}}:{{.Tag}} {{.ID}}" | Select-String -Pattern "fwh|funwashad"

    if ($images) {
        Write-Host "    Found images to remove:" -ForegroundColor Gray
        $images | ForEach-Object {
            $parts = $_ -split " "
            $imageId = $parts[-1]
            Write-Host "      - $($parts[0])" -ForegroundColor Gray
            docker rmi $imageId -f
        }
        Write-Host "[✓] Images removed" -ForegroundColor Green
    }
    else {
        Write-Host "[i] No images to remove" -ForegroundColor Cyan
    }
}

Write-Host ""
Write-Host "[✓] Cleanup completed!" -ForegroundColor Green
Write-Host ""

# Show remaining resources
Write-Host "[i] Remaining Docker resources:" -ForegroundColor Cyan
Write-Host ""
Write-Host "Containers:" -ForegroundColor Gray
docker ps -a --filter "name=funwashad" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
Write-Host ""
Write-Host "Volumes:" -ForegroundColor Gray
docker volume ls --filter "name=funwashad" --format "table {{.Name}}\t{{.Driver}}"
Write-Host ""
Write-Host "Images:" -ForegroundColor Gray
docker images --format "table {{.Repository}}\t{{.Tag}}\t{{.Size}}" | Select-String -Pattern "fwh|funwashad|REPOSITORY"
Write-Host ""
