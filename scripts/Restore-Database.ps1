<#
.SYNOPSIS
  Restores a PostgreSQL database from a backup file to a Docker volume.

.DESCRIPTION
  This script restores a PostgreSQL database from a backup file (compressed or uncompressed)
  to a Docker volume. The script will stop any running containers using the volume before
  restoring, and will prompt for confirmation unless -Force is specified.

.PARAMETER BackupFile
  The path to the backup file to restore. Can be .tar or .tar.gz format. Required.

.PARAMETER VolumeName
  The name of the Docker volume to restore to. Defaults to "funwashad-postgres-data".

.PARAMETER Force
  When specified, skips the confirmation prompt and proceeds with restoration immediately.

.EXAMPLE
  # Restore from backup (with confirmation)
  .\Restore-Database.ps1 -BackupFile ".\backups\postgres-backup-20250108-120000.tar.gz"

.EXAMPLE
  # Restore without confirmation
  .\Restore-Database.ps1 -BackupFile ".\backups\backup.tar.gz" -Force

.EXAMPLE
  # Restore to custom volume
  .\Restore-Database.ps1 -BackupFile ".\backups\backup.tar.gz" -VolumeName "my-volume"

.NOTES
  - Requires Docker to be installed and running.
  - ⚠️ WARNING: This will REPLACE all existing data in the target volume!
  - The script automatically detects if the backup is compressed (.gz) or not.
  - Any running containers using the volume will be stopped before restoration.
#>

#Requires -Version 5.1

param(
    [Parameter(Mandatory=$true)]
    [string]$BackupFile,

    [Parameter(Mandatory=$false)]
    [string]$VolumeName = "funwashad-postgres-data",

    [Parameter(Mandatory=$false)]
    [switch]$Force
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║            PostgreSQL Database Restore                       ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Check if backup file exists
if (-not (Test-Path $BackupFile)) {
    Write-Host "[✗] Backup file not found: $BackupFile" -ForegroundColor Red
    exit 1
}

Write-Host "[i] Restore Configuration:" -ForegroundColor Cyan
Write-Host "    Source: $BackupFile" -ForegroundColor Gray
Write-Host "    Target Volume: $VolumeName" -ForegroundColor Gray
Write-Host ""

# Warning about data loss
if (-not $Force) {
    Write-Host "[!] WARNING: This will REPLACE all existing data in the volume!" -ForegroundColor Yellow
    $confirm = Read-Host "Are you sure you want to continue? (yes/no)"

    if ($confirm -ne "yes") {
        Write-Host "[i] Restore cancelled" -ForegroundColor Cyan
        exit 0
    }
}

# Stop any running containers using the volume
Write-Host "[i] Checking for running containers..." -ForegroundColor Cyan
$containers = docker ps -a --filter "volume=$VolumeName" --format "{{.ID}} {{.Names}}"

if ($containers) {
    Write-Host "[!] Stopping containers using the volume..." -ForegroundColor Yellow
    docker ps --filter "volume=$VolumeName" --format "{{.ID}}" | ForEach-Object {
        docker stop $_
    }
    Write-Host "[✓] Containers stopped" -ForegroundColor Green
}

# Restore backup
Write-Host "[i] Restoring backup..." -ForegroundColor Cyan

try {
    # Determine if backup is compressed
    $isCompressed = $BackupFile -match "\.gz$"

    $backupDir = Split-Path -Parent (Resolve-Path $BackupFile)
    $backupFileName = Split-Path -Leaf $BackupFile

    if ($isCompressed) {
        docker run --rm `
            -v ${VolumeName}:/data `
            -v ${backupDir}:/backup `
            alpine `
            sh -c "rm -rf /data/* && tar xzf /backup/$backupFileName -C /data"
    }
    else {
        docker run --rm `
            -v ${VolumeName}:/data `
            -v ${backupDir}:/backup `
            alpine `
            sh -c "rm -rf /data/* && tar xf /backup/$backupFileName -C /data"
    }

    Write-Host "[✓] Restore completed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "[i] You can now start the application" -ForegroundColor Cyan
    Write-Host ""
}
catch {
    Write-Host "[✗] Restore failed: $_" -ForegroundColor Red
    exit 1
}
