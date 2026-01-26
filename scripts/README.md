# FunWasHad PowerShell Scripts

This directory contains PowerShell scripts for managing the FunWasHad application installation, Docker resources, and database operations.

## Prerequisites

- **PowerShell 5.1 or later** (Windows PowerShell or PowerShell Core)
- **.NET 9 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Docker Desktop** - [Download](https://www.docker.com/products/docker-desktop)

## Scripts Overview

### 🚀 Initialize-Installation.ps1

Complete installation script that sets up a new FunWasHad installation.

**Usage:**
```powershell
# Basic installation
.\scripts\Initialize-Installation.ps1

# Custom installation path
.\scripts\Initialize-Installation.ps1 -InstallPath "D:\MyApps\FunWasHad"

# Reset existing database and reinstall
.\scripts\Initialize-Installation.ps1 -ResetDatabase

# Skip dependency installation
.\scripts\Initialize-Installation.ps1 -SkipDependencies

# Skip Docker checks (use with caution)
.\scripts\Initialize-Installation.ps1 -SkipDockerCheck
```

**What it does:**
1. ✅ Checks prerequisites (.NET 9 SDK, Docker, Git)
2. ✅ Verifies Docker installation and status
3. ✅ Creates PostgreSQL Docker volume for persistent storage
4. ✅ Restores NuGet packages
5. ✅ Sets up database configuration
6. ✅ Builds the solution
7. ✅ Verifies installation
8. ✅ Displays next steps

**Parameters:**
- `-InstallPath` - Installation directory (default: `E:\FunWasHad`)
- `-ResetDatabase` - Delete and recreate database volume
- `-SkipDependencies` - Skip NuGet restore
- `-SkipDockerCheck` - Skip Docker verification
- `-Verbose` - Show detailed output

---

### ▶️ Start-Application.ps1

Starts the FunWasHad application using Aspire AppHost.

**Usage:**
```powershell
# Start in Debug mode
.\scripts\Start-Application.ps1

# Start in Release mode
.\scripts\Start-Application.ps1 -Configuration Release

# Start without opening browser
.\scripts\Start-Application.ps1 -NoBrowser

# Start with verbose output
.\scripts\Start-Application.ps1 -Verbose
```

**What it does:**
1. ✅ Verifies Docker is running
2. ✅ Checks PostgreSQL volume exists
3. ✅ Starts Aspire AppHost
4. ✅ Displays service URLs

**Services Started:**
- 🌐 **Aspire Dashboard** - http://localhost:15888
- 🌍 **Location API (HTTP)** - http://localhost:4748
- 🔒 **Location API (HTTPS)** - https://localhost:4747
- 🐘 **PgAdmin** - http://localhost:5050
- 🗄️ **PostgreSQL** - localhost:5432

**Parameters:**
- `-Configuration` - Build configuration (`Debug` or `Release`, default: `Debug`)
- `-NoBrowser` - Don't open browser automatically
- `-Verbose` - Show detailed build output

---

### 💾 Backup-Database.ps1

Creates a backup of the PostgreSQL database volume.

**Usage:**
```powershell
# Create compressed backup (default)
.\scripts\Backup-Database.ps1

# Create backup in specific directory
.\scripts\Backup-Database.ps1 -BackupPath "D:\Backups"

# Create uncompressed backup
.\scripts\Backup-Database.ps1 -CompressBackup:$false

# Backup custom volume
.\scripts\Backup-Database.ps1 -VolumeName "my-postgres-volume"
```

**What it does:**
1. ✅ Creates backup directory if needed
2. ✅ Generates timestamped backup file
3. ✅ Creates compressed tar.gz archive
4. ✅ Displays backup size and location

**Backup Format:**
- **Filename:** `postgres-backup-YYYYMMDD-HHMMSS.tar.gz`
- **Location:** `.\backups\` (by default)
- **Content:** Complete PostgreSQL data directory

**Parameters:**
- `-BackupPath` - Directory for backup files (default: `.\backups`)
- `-CompressBackup` - Create compressed backup (default: `$true`)
- `-VolumeName` - Docker volume name (default: `funwashad-postgres-data`)

---

### 🔄 Restore-Database.ps1

Restores a PostgreSQL database from a backup file.

**Usage:**
```powershell
# Restore from backup (interactive)
.\scripts\Restore-Database.ps1 -BackupFile ".\backups\postgres-backup-20250108-120000.tar.gz"

# Restore without confirmation
.\scripts\Restore-Database.ps1 -BackupFile ".\backups\postgres-backup-20250108-120000.tar.gz" -Force

# Restore to custom volume
.\scripts\Restore-Database.ps1 -BackupFile ".\backups\backup.tar.gz" -VolumeName "my-volume"
```

**What it does:**
1. ⚠️ Warns about data loss
2. ✅ Stops containers using the volume
3. ✅ Restores backup data to volume
4. ✅ Verifies restoration

**⚠️ WARNING:** This will **REPLACE** all existing data in the volume!

**Parameters:**
- `-BackupFile` - Path to backup file (required)
- `-VolumeName` - Docker volume name (default: `funwashad-postgres-data`)
- `-Force` - Skip confirmation prompt

---

### 🧹 Clean-DockerResources.ps1

Cleans up Docker containers, volumes, and images related to FunWasHad.

**Usage:**
```powershell
# Show usage
.\scripts\Clean-DockerResources.ps1

# Clean all resources (interactive)
.\scripts\Clean-DockerResources.ps1 -All

# Clean all resources without confirmation
.\scripts\Clean-DockerResources.ps1 -All -Force

# Clean only containers
.\scripts\Clean-DockerResources.ps1 -Containers

# Clean only volumes (deletes data!)
.\scripts\Clean-DockerResources.ps1 -Volumes

# Clean only images
.\scripts\Clean-DockerResources.ps1 -Images
```

**What it does:**
- 🛑 Stops running containers
- 🗑️ Removes stopped containers
- 💾 Removes volumes (deletes data!)
- 🖼️ Removes Docker images
- 📊 Shows remaining resources

**Parameters:**
- `-All` - Clean all resources
- `-Containers` - Clean containers only
- `-Volumes` - Clean volumes only (⚠️ deletes data!)
- `-Images` - Clean images only
- `-Force` - Skip confirmation prompts

---

### 📋 Sync-Documentation.ps1

Synchronizes Functional Requirements, Technical Requirements, TODO list, and Status documents to ensure consistency.

**Usage:**
```powershell
# Validate documentation consistency (no changes)
.\scripts\Sync-Documentation.ps1 -Mode Check

# Synchronize all documentation
.\scripts\Sync-Documentation.ps1 -Mode Sync

# Watch for changes and auto-sync
.\scripts\Sync-Documentation.ps1 -Mode Watch
```

**What it does:**
1. ✅ Parses TODO.md for all items with identifiers (MVP-APP-001, etc.)
2. ✅ Validates all TODO identifiers are referenced in requirements documents
3. ✅ Updates Status.md statistics and completion counts
4. ✅ Updates "Last updated" dates in all documents
5. ✅ Detects inconsistencies and reports issues

**Parameters:**
- `-Mode` - Operation mode: `Check` (validate only), `Sync` (update documents), `Watch` (monitor for changes)
- `-ProjectRoot` - Root directory of the project (default: script parent directory)

**See also:** [Documentation Sync Agent Guide](../docs/DOCUMENTATION-SYNC-AGENT.md)

---

### 📊 Update-CoverageReport.ps1

Runs tests with code coverage and updates `docs/Coverage-Report.md` (MVP-SUPPORT-004). Uses coverlet and ReportGenerator.

**Usage:**
```powershell
# Run all tests with coverage and update the report
.\scripts\Update-CoverageReport.ps1

# Only regenerate the report from existing TestResults (e.g. after CI tests)
.\scripts\Update-CoverageReport.ps1 -SkipTests

# Use Debug configuration
.\scripts\Update-CoverageReport.ps1 -Configuration Debug
```

**What it does:**
1. Builds the solution and runs `dotnet test` with `--collect "XPlat code coverage"` and `coverlet.runsettings` (unless `-SkipTests`)
2. Finds `*.cobertura.xml` under `./TestResults` (or under the repo when `-SkipTests`)
3. Runs `dotnet tool run reportgenerator` to merge and produce Markdown
4. Writes `docs/Coverage-Report.md` with a "Last updated" line

**Prerequisites:** `dotnet tool restore` (uses `.config/dotnet-tools.json` with `dotnet-reportgenerator-globaltool`).

**Parameters:**
- `-SkipTests` - Do not run build/tests; only regenerate from existing coverage files
- `-Configuration` - Build configuration (default: `Release`)
- `-ProjectRoot` - Repository root (default: parent of `scripts`)

---

### 🧹 Cleanup-Actions.ps1

Cleans up GitHub Actions workflow runs and optionally Docker images from GitHub Container Registry.

**Usage:**
```powershell
# Simulate cleanup (see what would be deleted)
.\scripts\cleanup-actions.ps1 -Repo "owner/FunWasHad" -WhatIf

# Actually delete (after confirmation)
.\scripts\cleanup-actions.ps1 -Repo "owner/FunWasHad"

# Force delete without interactive confirmation
.\scripts\cleanup-actions.ps1 -Repo "owner/FunWasHad" -Force

# Clean up both workflow runs and Docker images
.\scripts\cleanup-actions.ps1 -Repo "owner/FunWasHad" -CleanupDockerImages

# Keep only the first 3 successful runs per workflow, delete the rest
.\scripts\cleanup-actions.ps1 -Repo "owner/FunWasHad" -LastThree

# Keep only the most recent successful run per workflow, delete everything else
.\scripts\cleanup-actions.ps1 -Repo "owner/FunWasHad" -KeepLatest
```

**What it does:**
- 🗑️ Deletes failed workflow runs (keeps most recent per workflow)
- 🗑️ Deletes ALL cancelled runs
- 🗑️ Deletes ALL runs tagged with "no-build" check run
- 🗑️ Optionally deletes excess successful runs (with -LastThree or -KeepLatest)
- 🐳 Optionally cleans up old Docker images from GHCR (with -CleanupDockerImages or -KeepLatest)

**Cleanup Rules:**
- **Failed runs:** Keeps the most recent failed run per workflow, deletes all others
- **Cancelled runs:** Deletes ALL cancelled runs (none are kept)
- **No-build runs:** Deletes ALL runs tagged with "no-build" check run
- **Successful runs (LastThree):** Keeps only the first 3 most recent successful runs per workflow
- **KeepLatest mode:** Keeps only the most recent successful run per existing workflow, deletes all other runs. Also deletes ALL runs for workflows that no longer exist. Automatically includes Docker image cleanup.

**Parameters:**
- `-Repo` - Repository in owner/repo format (auto-detected if omitted)
- `-Force` - Skip confirmation prompts
- `-WhatIf` - Simulate only, don't actually delete
- `-CleanupDockerImages` - Also clean up old Docker images from GHCR
- `-LastThree` - Keep only first 3 successful runs per workflow
- `-KeepLatest` - Keep only most recent successful run per workflow (aggressive cleanup, includes Docker cleanup)

**⚠️ WARNING:** Deletions are irreversible. Use `-WhatIf` first to verify what will be deleted.

---

## Common Workflows

### 🆕 New Installation

```powershell
# Clone repository
git clone https://github.com/sharpninja/FunWasHad
cd FunWasHad

# Run installation
.\scripts\Initialize-Installation.ps1

# Start application
.\scripts\Start-Application.ps1
```

### 📅 Daily Development

```powershell
# Start application
.\scripts\Start-Application.ps1

# Application runs...
# Press Ctrl+C to stop
```

### 💾 Backup Before Major Changes

```powershell
# Create backup
.\scripts\Backup-Database.ps1

# Make changes...
# If something goes wrong, restore:
.\scripts\Restore-Database.ps1 -BackupFile ".\backups\postgres-backup-20250108-120000.tar.gz"
```

### 🔄 Reset Development Environment

```powershell
# Stop application (Ctrl+C)

# Clean all resources
.\scripts\Clean-DockerResources.ps1 -All -Force

# Reinitialize
.\scripts\Initialize-Installation.ps1 -ResetDatabase

# Start fresh
.\scripts\Start-Application.ps1
```

### 🧪 Testing with Fresh Database

```powershell
# Backup current state
.\scripts\Backup-Database.ps1

# Clean database
.\scripts\Clean-DockerResources.ps1 -Volumes -Force

# Test with fresh database
.\scripts\Start-Application.ps1

# Restore if needed
.\scripts\Restore-Database.ps1 -BackupFile ".\backups\postgres-backup-20250108-120000.tar.gz"
```

---

## Troubleshooting

### Script Execution Policy Error

If you get "execution of scripts is disabled on this system":

```powershell
# Set execution policy for current session
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process

# Or for current user (requires admin)
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Docker Not Running

```powershell
# Check Docker status
docker ps

# If not running, start Docker Desktop
# Then retry the script
```

### Permission Denied

```powershell
# Run PowerShell as Administrator
# Right-click PowerShell → "Run as Administrator"
```

### Volume Already Exists

```powershell
# To recreate volume, use -ResetDatabase
.\scripts\Initialize-Installation.ps1 -ResetDatabase

# Or manually remove it
docker volume rm funwashad-postgres-data
```

### Port Already in Use

```bash
# Find process using port 4748
netstat -ano | findstr :4748

# Kill the process
taskkill /PID <process_id> /F
```

---

## Script Features

### ✅ Color-Coded Output

Scripts use color-coded output for better readability:
- 🟢 **Green** - Success messages
- 🔴 **Red** - Error messages
- 🟡 **Yellow** - Warning messages
- 🔵 **Cyan** - Information messages
- 🟣 **Magenta** - Headers and titles

### ✅ Error Handling

All scripts include comprehensive error handling:
- Validates prerequisites before execution
- Provides clear error messages
- Suggests troubleshooting steps
- Safely handles interruptions (Ctrl+C)

### ✅ Interactive Confirmations

Destructive operations prompt for confirmation:
- Database reset
- Volume deletion
- Data restoration

Use `-Force` parameter to skip confirmations for automation.

### ✅ Detailed Logging

Scripts provide detailed logging:
- Step-by-step progress
- Configuration details
- Resource information
- Next steps guidance

---

## Advanced Usage

### Automated Backups (Task Scheduler)

Create a scheduled task to backup daily:

```powershell
# backup-scheduled.ps1
$date = Get-Date -Format "yyyyMMdd"
.\scripts\Backup-Database.ps1 -BackupPath "D:\Backups\Daily"

# Keep only last 7 days
Get-ChildItem "D:\Backups\Daily" -Filter "postgres-backup-*.tar.gz" |
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-7) } |
    Remove-Item
```

### CI/CD Integration

Use scripts in CI/CD pipelines:

```yaml
# Azure DevOps pipeline
steps:
  - task: PowerShell@2
    inputs:
      filePath: 'scripts/Initialize-Installation.ps1'
      arguments: '-SkipDockerCheck -Force'

  - task: PowerShell@2
    inputs:
      filePath: 'scripts/Start-Application.ps1'
      arguments: '-Configuration Release -NoBrowser'
```

### Custom Installation Paths

```powershell
# Install to specific drive/path
.\scripts\Initialize-Installation.ps1 -InstallPath "D:\Projects\FunWasHad"

# Install to network location (if Docker supports it)
.\scripts\Initialize-Installation.ps1 -InstallPath "\\server\share\FunWasHad"
```

---

## File Locations

### Default Paths

| Resource | Default Location |
|----------|-----------------|
| Installation | `E:\FunWasHad` |
| Backups | `.\backups` |
| Docker Volume | Docker managed (see below) |
| Logs | Application directory |

### Docker Volume Location

**Windows with Docker Desktop:**
```
\\wsl$\docker-desktop-data\data\docker\volumes\funwashad-postgres-data\_data
```

**Linux:**
```
/var/lib/docker/volumes/funwashad-postgres-data/_data
```

**macOS:**
```
~/Library/Containers/com.docker.docker/Data/vms/0/data/docker/volumes/funwashad-postgres-data/_data
```

---

## Support

For issues or questions:
1. Check the main documentation in the root directory
2. Review `PostgreSQL_LocalStorage_Configuration.md`
3. Check Aspire documentation: `Aspire_QuickReference.md`
4. Open an issue on GitHub

---

## Version History

### Version 1.0 (2025-01-08)
- ✅ Initial release
- ✅ Installation script
- ✅ Database backup/restore
- ✅ Application startup
- ✅ Docker cleanup
- ✅ Comprehensive documentation

---

### 📝 FWH.Prompts Module

PowerShell module providing templatized prompts for AI interactions with parameterized commands.

**Installation:**
```powershell
# Import the module
Import-Module .\scripts\modules\FWH.Prompts\FWH.Prompts.psd1
```

**Usage:**
```powershell
# List available prompts
Get-AvailablePrompts

# Get a filled prompt
Get-Prompt -Name 'code-review' -Parameters @{
    FeatureName = 'User Authentication'
    FilePath = 'src/AuthService.cs'
    Code = Get-Content 'src/AuthService.cs' -Raw
}

# Invoke prompt and copy to clipboard
Invoke-Prompt -Name 'code-review' -Parameters @{...} -OutputToClipboard

# Create custom prompt template
New-PromptTemplate -Name 'custom-review' `
    -Description 'Custom code review' `
    -Template 'Review {Code} for {Issues}' `
    -Parameters @('Code', 'Issues')
```

**Available Built-in Prompts:**
- `code-review` - Request code review
- `implement-feature` - Request feature implementation
- `debug-issue` - Request debugging help
- `refactor-code` - Request code refactoring
- `write-tests` - Request unit test generation
- `document-code` - Request code documentation
- `optimize-performance` - Request performance optimization
- `add-feature` - Request adding new feature
- `fix-bug` - Request bug fix
- `security-audit` - Request security audit

**Functions:**
- `Get-Prompt` - Get a filled prompt from template
- `Invoke-Prompt` - Get prompt and optionally output/copy/save
- `Get-AvailablePrompts` - List all available prompt templates
- `Get-PromptTemplate` - Get template details
- `New-PromptTemplate` - Create custom prompt template
- `Remove-PromptTemplate` - Remove prompt template

**Example:**
```powershell
# Import module
Import-Module .\scripts\modules\FWH.Prompts\FWH.Prompts.psd1

# Get code review prompt
$prompt = Get-Prompt -Name 'code-review' -Parameters @{
    FeatureName = 'User Authentication'
    FilePath = 'src/FWH.Mobile/Services/AuthService.cs'
    Code = Get-Content 'src/FWH.Mobile/Services/AuthService.cs' -Raw
}

# Copy to clipboard for use with AI
Invoke-Prompt -Name 'code-review' -Parameters @{...} -OutputToClipboard
```

---

**Happy Development! 🚀**
