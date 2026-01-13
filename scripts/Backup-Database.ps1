# Database Backup Script
# Creates a backup of the PostgreSQL database

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
