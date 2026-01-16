# PowerShell Installation Scripts - Implementation Summary

**Date:** 2025-01-08  
**Status:** ✅ **COMPLETE**  
**Feature:** Automated installation and management scripts

---

## Overview

Created a comprehensive set of PowerShell scripts to automate the installation, configuration, and management of the FunWasHad application with Docker and PostgreSQL persistence.

---

## Scripts Created

### 1. Initialize-Installation.ps1 ✅

**Purpose:** Complete automated installation of FunWasHad application

**Location:** `scripts/Initialize-Installation.ps1`

**Features:**
- ✅ **Prerequisites Check**
  - Validates .NET 9 SDK installation
  - Checks Docker Desktop installation
  - Verifies Docker daemon is running
  - Confirms Git availability
  
- ✅ **Docker Volume Setup**
  - Creates `funwashad-postgres-data` volume
  - Configures persistent storage for PostgreSQL
  - Optional database reset with `-ResetDatabase` flag
  
- ✅ **Dependency Management**
  - Restores NuGet packages
  - Validates project structure
  - Checks for migration scripts
  
- ✅ **Build Process**
  - Builds solution in Release mode
  - Validates build success
  - Reports build errors clearly
  
- ✅ **Installation Verification**
  - Checks critical files exist
  - Verifies Docker volume creation
  - Confirms configuration
  
- ✅ **User-Friendly Output**
  - Color-coded messages
  - Progress indicators
  - Detailed next steps
  - Service URLs and configuration

**Usage Examples:**

```powershell
# Basic installation
.\scripts\Initialize-Installation.ps1

# Custom path with database reset
.\scripts\Initialize-Installation.ps1 `
    -InstallPath "D:\FunWasHad" `
    -ResetDatabase

# Skip checks for CI/CD
.\scripts\Initialize-Installation.ps1 `
    -SkipDockerCheck `
    -SkipDependencies
```

**Parameters:**

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `InstallPath` | String | `E:\FunWasHad` | Installation directory |
| `ResetDatabase` | Switch | False | Delete and recreate database |
| `SkipDependencies` | Switch | False | Skip NuGet restore |
| `SkipDockerCheck` | Switch | False | Skip Docker validation |
| `Verbose` | Switch | False | Detailed output |

---

### 2. Start-Application.ps1 ✅

**Purpose:** Start the FunWasHad application with all services

**Location:** `scripts/Start-Application.ps1`

**Features:**
- ✅ **Pre-flight Checks**
  - Verifies Docker is running
  - Confirms PostgreSQL volume exists
  - Warns if volume missing
  
- ✅ **Configuration Options**
  - Debug or Release mode
  - Browser launch control
  - Verbose output support
  
- ✅ **Service URLs Display**
  - Aspire Dashboard
  - Location API (HTTP/HTTPS)
  - PgAdmin
  - Mobile connection info

**Usage Examples:**

```powershell
# Start in Debug mode (default)
.\scripts\Start-Application.ps1

# Start in Release mode
.\scripts\Start-Application.ps1 -Configuration Release

# Start without browser
.\scripts\Start-Application.ps1 -NoBrowser

# Start with verbose logging
.\scripts\Start-Application.ps1 -Verbose
```

**Services Started:**

| Service | URL | Purpose |
|---------|-----|---------|
| Aspire Dashboard | http://localhost:15888 | Service monitoring |
| Location API (HTTP) | http://localhost:4748 | API endpoint |
| Location API (HTTPS) | https://localhost:4747 | Secure API |
| PgAdmin | http://localhost:5050 | Database management |
| PostgreSQL | localhost:5432 | Database server |

---

### 3. Backup-Database.ps1 ✅

**Purpose:** Create backups of PostgreSQL database volume

**Location:** `scripts/Backup-Database.ps1`

**Features:**
- ✅ **Automated Backup Creation**
  - Timestamped filenames
  - Compressed tar.gz format
  - Size reporting
  
- ✅ **Flexible Storage**
  - Custom backup directory
  - Compressed or uncompressed
  - Volume name override
  
- ✅ **Safety Features**
  - Creates backup directory automatically
  - Validates volume exists
  - Reports backup size

**Backup Format:**
- **Filename:** `postgres-backup-YYYYMMDD-HHMMSS.tar.gz`
- **Location:** `.\backups\` (default)
- **Contents:** Complete PostgreSQL data directory

**Usage Examples:**

```powershell
# Create default backup
.\scripts\Backup-Database.ps1

# Custom backup location
.\scripts\Backup-Database.ps1 -BackupPath "D:\Backups"

# Uncompressed backup
.\scripts\Backup-Database.ps1 -CompressBackup:$false

# Backup different volume
.\scripts\Backup-Database.ps1 -VolumeName "custom-volume"
```

**Automated Backup Script:**

```powershell
# Schedule daily backups
$date = Get-Date -Format "yyyyMMdd"
.\scripts\Backup-Database.ps1 -BackupPath "D:\Backups\Daily"

# Cleanup old backups (keep 7 days)
Get-ChildItem "D:\Backups\Daily" -Filter "*.tar.gz" |
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-7) } |
    Remove-Item
```

---

### 4. Restore-Database.ps1 ✅

**Purpose:** Restore PostgreSQL database from backup

**Location:** `scripts/Restore-Database.ps1`

**Features:**
- ✅ **Safe Restoration**
  - Confirmation prompt
  - Volume validation
  - Container stop handling
  
- ✅ **Flexible Options**
  - Force mode for automation
  - Custom volume targeting
  - Compressed/uncompressed support
  
- ✅ **Data Protection**
  - Clear warnings
  - Container management
  - Verification

**⚠️ WARNING:** Restoring **REPLACES ALL** existing database data!

**Usage Examples:**

```powershell
# Interactive restore
.\scripts\Restore-Database.ps1 `
    -BackupFile ".\backups\postgres-backup-20250108-120000.tar.gz"

# Force restore (no prompt)
.\scripts\Restore-Database.ps1 `
    -BackupFile ".\backups\backup.tar.gz" `
    -Force

# Restore to custom volume
.\scripts\Restore-Database.ps1 `
    -BackupFile ".\backups\backup.tar.gz" `
    -VolumeName "test-volume"
```

**Restoration Process:**
1. Validates backup file exists
2. Warns about data loss
3. Stops containers using volume
4. Removes existing data
5. Extracts backup to volume
6. Confirms completion

---

### 5. Clean-DockerResources.ps1 ✅

**Purpose:** Clean up Docker containers, volumes, and images

**Location:** `scripts/Clean-DockerResources.ps1`

**Features:**
- ✅ **Selective Cleanup**
  - Containers only
  - Volumes only
  - Images only
  - All resources
  
- ✅ **Safety Measures**
  - Interactive confirmations
  - Double-check for volumes
  - Force mode for automation
  
- ✅ **Resource Reporting**
  - Shows what will be removed
  - Displays remaining resources
  - Clear status messages

**Usage Examples:**

```powershell
# Show usage
.\scripts\Clean-DockerResources.ps1

# Clean everything (interactive)
.\scripts\Clean-DockerResources.ps1 -All

# Clean everything (no prompts)
.\scripts\Clean-DockerResources.ps1 -All -Force

# Clean only containers
.\scripts\Clean-DockerResources.ps1 -Containers

# Clean only volumes (⚠️ deletes data!)
.\scripts\Clean-DockerResources.ps1 -Volumes -Force

# Clean only images
.\scripts\Clean-DockerResources.ps1 -Images
```

**What Gets Cleaned:**

| Flag | Cleans | Data Loss? |
|------|--------|-----------|
| `-Containers` | FunWasHad containers | No |
| `-Volumes` | PostgreSQL volume | ⚠️ YES |
| `-Images` | FunWasHad Docker images | No |
| `-All` | All of the above | ⚠️ YES (volumes) |

---

## Common Workflows

### New Installation

```powershell
# 1. Clone repository
git clone https://github.com/sharpninja/FunWasHad
cd FunWasHad

# 2. Run installation
.\scripts\Initialize-Installation.ps1

# 3. Start application
.\scripts\Start-Application.ps1

# 4. Access services
# - Aspire Dashboard: http://localhost:15888
# - Location API: http://localhost:4748
```

### Daily Development

```powershell
# Start application
.\scripts\Start-Application.ps1

# Work on code...

# Stop with Ctrl+C when done
```

### Backup & Restore

```powershell
# Before major changes
.\scripts\Backup-Database.ps1

# Make changes...

# If something goes wrong
.\scripts\Restore-Database.ps1 -BackupFile ".\backups\postgres-backup-20250108-120000.tar.gz"
```

### Reset Environment

```powershell
# Stop application (Ctrl+C)

# Clean everything
.\scripts\Clean-DockerResources.ps1 -All -Force

# Reinitialize
.\scripts\Initialize-Installation.ps1 -ResetDatabase

# Start fresh
.\scripts\Start-Application.ps1
```

### Fresh Database for Testing

```powershell
# Backup current state
.\scripts\Backup-Database.ps1

# Remove database
.\scripts\Clean-DockerResources.ps1 -Volumes -Force

# Test with fresh database
.\scripts\Start-Application.ps1

# Restore original if needed
.\scripts\Restore-Database.ps1 -BackupFile ".\backups\postgres-backup-20250108-120000.tar.gz"
```

---

## Script Features

### Color-Coded Output

All scripts use consistent color coding:

| Color | Purpose | Example |
|-------|---------|---------|
| 🟢 Green | Success | `[✓] Installation complete` |
| 🔴 Red | Error | `[✗] Docker not found` |
| 🟡 Yellow | Warning | `[!] Volume will be deleted` |
| 🔵 Cyan | Info | `[i] Checking prerequisites` |
| 🟣 Magenta | Header | `=== Installation Script ===` |

### Error Handling

- ✅ **Comprehensive Validation**
  - Prerequisites checked before execution
  - Clear error messages
  - Suggested troubleshooting steps

- ✅ **Graceful Failures**
  - Safe cancellation (Ctrl+C)
  - Cleanup on error
  - State preservation

- ✅ **User Guidance**
  - Next steps after errors
  - Links to documentation
  - Command examples

### Interactive Confirmations

Destructive operations require confirmation:

```powershell
[!] WARNING: This will DELETE all database data!
Continue? (yes/no): _
```

Use `-Force` to skip confirmations for automation:

```powershell
.\scripts\Clean-DockerResources.ps1 -All -Force
```

---

## CI/CD Integration

### Azure DevOps

```yaml
# azure-pipelines.yml
steps:
  - task: PowerShell@2
    displayName: 'Initialize Installation'
    inputs:
      filePath: 'scripts/Initialize-Installation.ps1'
      arguments: '-SkipDockerCheck -Force'
      
  - task: PowerShell@2
    displayName: 'Start Application'
    inputs:
      filePath: 'scripts/Start-Application.ps1'
      arguments: '-Configuration Release -NoBrowser'
```

### GitHub Actions

```yaml
# .github/workflows/deploy.yml
jobs:
  deploy:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Initialize Installation
        shell: pwsh
        run: |
          .\scripts\Initialize-Installation.ps1 -Force
          
      - name: Start Application
        shell: pwsh
        run: |
          .\scripts\Start-Application.ps1 -Configuration Release -NoBrowser
```

---

## Troubleshooting

### Execution Policy Error

**Error:** "execution of scripts is disabled on this system"

**Solution:**
```powershell
# For current session only
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process

# For current user (requires admin)
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Docker Not Running

**Error:** "Docker daemon is not running"

**Solution:**
1. Open Docker Desktop
2. Wait for Docker to start
3. Verify: `docker ps`
4. Retry script

### Permission Denied

**Error:** "Access denied" or "Permission denied"

**Solution:**
1. Right-click PowerShell
2. Select "Run as Administrator"
3. Retry script

### Port Already in Use

**Error:** "Address already in use: 4748"

**Solution:**
```powershell
# Find process using port
netstat -ano | findstr :4748

# Kill process (replace PID)
taskkill /PID <process_id> /F
```

---

## File Structure

```
FunWasHad/
├── scripts/
│   ├── README.md                      # This file
│   ├── Initialize-Installation.ps1    # Main installation
│   ├── Start-Application.ps1          # Start services
│   ├── Backup-Database.ps1            # Backup database
│   ├── Restore-Database.ps1           # Restore database
│   └── Clean-DockerResources.ps1      # Cleanup resources
├── backups/                           # Created by backup script
│   └── postgres-backup-*.tar.gz       # Backup files
└── (application files...)
```

---

## Documentation

### Related Documentation

| Document | Purpose |
|----------|---------|
| `scripts/README.md` | Script usage guide |
| `PostgreSQL_LocalStorage_Configuration.md` | Docker volume details |
| `Aspire_QuickReference.md` | Aspire setup guide |

### Quick Reference

**Install:**
```powershell
.\scripts\Initialize-Installation.ps1
```

**Start:**
```powershell
.\scripts\Start-Application.ps1
```

**Backup:**
```powershell
.\scripts\Backup-Database.ps1
```

**Restore:**
```powershell
.\scripts\Restore-Database.ps1 -BackupFile "backup.tar.gz"
```

**Clean:**
```powershell
.\scripts\Clean-DockerResources.ps1 -All
```

---

## Summary

### Scripts Created

✅ **Initialize-Installation.ps1** - Complete installation automation  
✅ **Start-Application.ps1** - Application startup  
✅ **Backup-Database.ps1** - Database backup  
✅ **Restore-Database.ps1** - Database restore  
✅ **Clean-DockerResources.ps1** - Docker cleanup  
✅ **README.md** - Comprehensive documentation  

### Features

- **User-Friendly** - Color-coded output, clear messages
- **Safe** - Confirmation prompts, error handling
- **Flexible** - Configurable parameters, multiple modes
- **Automated** - CI/CD ready, scheduled tasks
- **Documented** - Comprehensive help and examples

### Benefits

- ✅ **One-Command Installation** - Complete setup in seconds
- ✅ **Safe Operations** - Confirmations and warnings
- ✅ **Easy Maintenance** - Simple backup/restore
- ✅ **Clean Uninstall** - Complete resource cleanup
- ✅ **Developer Friendly** - Daily workflow support

---

**Implementation Status:** ✅ **COMPLETE**  
**Testing Status:** ✅ **VERIFIED**  
**Documentation:** ✅ **COMPLETE**  
**Production Ready:** ✅ **YES**

---

*Document Version: 1.0*  
*Date: 2025-01-08*  
*Status: Complete*
