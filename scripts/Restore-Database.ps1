# Database Restore Script
# Restores a PostgreSQL database from backup

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
