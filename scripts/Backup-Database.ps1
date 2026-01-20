<#
.SYNOPSIS
  Creates a backup of the PostgreSQL database from a Docker volume.

.DESCRIPTION
  This script creates a backup of the PostgreSQL database stored in a Docker volume.
  The backup can be compressed (gzip) or uncompressed, and is saved with a timestamp
  in the filename. The script uses Docker to access the volume and create the backup.

.PARAMETER BackupPath
  The directory where backup files will be saved. Defaults to ".\backups".

.PARAMETER CompressBackup
  When specified, creates a compressed backup (.tar.gz). Defaults to $true.

.PARAMETER VolumeName
  The name of the Docker volume containing the PostgreSQL data. Defaults to "funwashad-postgres-data".

.EXAMPLE
  # Create a compressed backup in the default location
  .\Backup-Database.ps1

.EXAMPLE
  # Create an uncompressed backup
  .\Backup-Database.ps1 -CompressBackup:$false

.EXAMPLE
  # Create a backup in a custom location
  .\Backup-Database.ps1 -BackupPath "D:\Backups\FunWasHad"

.NOTES
  - Requires Docker to be installed and running.
  - The backup volume must exist before running this script.
  - Backup files are named with timestamp: postgres-backup-YYYYMMDD-HHMMSS.tar[.gz]
  - The backup directory will be created if it doesn't exist.
#>

#Requires -Version 5.1

param(
    [Parameter(Mandatory=$false)]
    [string]$BackupPath = ".\backups",

    [Parameter(Mandatory=$false)]
    [switch]$CompressBackup = $true,

    [Parameter(Mandatory=$false)]
    [string]$VolumeName = "funwashad-postgres-data"
)

$ErrorActionPreference = "Stop"

# Create backup directory if it doesn't exist
if (-not (Test-Path $BackupPath)) {
    New-Item -ItemType Directory -Path $BackupPath | Out-Null
    Write-Host "[✓] Created backup directory: $BackupPath" -ForegroundColor Green
}

# Generate timestamp for backup filename
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$backupFile = "postgres-backup-$timestamp"

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║            PostgreSQL Database Backup                        ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

Write-Host "[i] Backup Configuration:" -ForegroundColor Cyan
Write-Host "    Volume: $VolumeName" -ForegroundColor Gray
Write-Host "    Output: $BackupPath\$backupFile" -ForegroundColor Gray
Write-Host ""

# Check if volume exists
$volumeExists = docker volume ls --format "{{.Name}}" | Select-String -Pattern "^$VolumeName$"
if (-not $volumeExists) {
    Write-Host "[✗] Volume not found: $VolumeName" -ForegroundColor Red
    exit 1
}

Write-Host "[i] Starting backup..." -ForegroundColor Cyan

try {
    if ($CompressBackup) {
        # Create compressed backup
        $backupFileFull = "$BackupPath\$backupFile.tar.gz"

        docker run --rm `
            -v ${VolumeName}:/data `
            -v ${PWD}/${BackupPath}:/backup `
            alpine `
            tar czf /backup/$backupFile.tar.gz -C /data .

        Write-Host "[✓] Backup created: $backupFileFull" -ForegroundColor Green
    }
    else {
        # Create uncompressed backup
        $backupFileFull = "$BackupPath\$backupFile.tar"

        docker run --rm `
            -v ${VolumeName}:/data `
            -v ${PWD}/${BackupPath}:/backup `
            alpine `
            tar cf /backup/$backupFile.tar -C /data .

        Write-Host "[✓] Backup created: $backupFileFull" -ForegroundColor Green
    }

    # Get backup file size
    $fileSize = (Get-Item $backupFileFull).Length / 1MB
    Write-Host "[i] Backup size: $([math]::Round($fileSize, 2)) MB" -ForegroundColor Cyan

    Write-Host ""
    Write-Host "[✓] Backup completed successfully!" -ForegroundColor Green
    Write-Host ""
}
catch {
    Write-Host "[✗] Backup failed: $_" -ForegroundColor Red
    exit 1
}
