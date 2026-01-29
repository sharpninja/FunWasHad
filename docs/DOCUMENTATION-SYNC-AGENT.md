# Documentation Synchronization Agent

This document describes the automated agent that keeps Functional Requirements, Technical Requirements, TODO list, and Status documents synchronized.

## Overview

The Documentation Synchronization Agent ensures that changes to features and technology in the project are automatically reflected across all documentation files:

- `Project/Functional-Requirements.md`
- `Project/Technical-Requirements.md`
- `Project/TODO.md`
- `Project/Status.md`

## Components

### 1. PowerShell Script (`scripts/Sync-Documentation.ps1`)

A PowerShell script that can:
- **Check Mode**: Validate documentation consistency without making changes
- **Sync Mode**: Update all documentation files to reflect current state
- **Watch Mode**: Monitor for file changes and automatically sync documentation

### 2. .NET Console Application (`src/FWH.Documentation.Sync`)

A .NET 9 console application that provides:
- More robust parsing of documentation files
- Better error handling and logging
- Cross-platform compatibility
- Integration with .NET hosting model

## Usage

### PowerShell Script

```powershell
# Validate documentation consistency
.\scripts\Sync-Documentation.ps1 -Mode Check

# Synchronize all documentation
.\scripts\Sync-Documentation.ps1 -Mode Sync

# Watch for changes and auto-sync
.\scripts\Sync-Documentation.ps1 -Mode Watch
```

### .NET Application

```bash
# Build the application
dotnet build src/FWH.Documentation.Sync

# Run in check mode
dotnet run --project src/FWH.Documentation.Sync -- check

# Run in sync mode
dotnet run --project src/FWH.Documentation.Sync -- sync

# Run in watch mode
dotnet run --project src/FWH.Documentation.Sync -- watch
```

## What It Does

### Automatic Updates

The agent performs the following operations:

1. **Parses Project/TODO.md**
   - Extracts all TODO items with identifiers (MVP-APP-001, etc.)
   - Detects completion status ([ ] vs [x])
   - Identifies priority levels (High/Medium)

2. **Updates Project/Status.md**
   - Recalculates project statistics (High/Medium/Total items)
   - Updates completion counts
   - Updates "Last updated" date
   - Adjusts project status indicators

3. **Validates Requirements Documents**
   - Ensures all TODO identifiers are referenced in Functional Requirements
   - Ensures all TODO identifiers are referenced in Technical Requirements
   - Checks for missing or orphaned references

4. **Updates Change History**
   - Updates "Last Updated" dates in requirements documents
   - Maintains version tracking

## Integration Options

### Option 1: Git Hooks

Add a pre-commit hook to automatically sync documentation:

```bash
# .git/hooks/pre-commit
#!/bin/sh
pwsh -File scripts/Sync-Documentation.ps1 -Mode Sync
```

### Option 2: GitHub Actions

Add to `.github/workflows/main.yml`:

```yaml
- name: Sync Documentation
  run: pwsh scripts/Sync-Documentation.ps1 -Mode Sync
```

### Option 3: Scheduled Task (Windows)

Create a Windows Scheduled Task to run the sync agent periodically:

```powershell
$action = New-ScheduledTaskAction -Execute "pwsh.exe" -Argument "-File $PSScriptRoot\Sync-Documentation.ps1 -Mode Sync"
$trigger = New-ScheduledTaskTrigger -Daily -At "02:00"
Register-ScheduledTask -TaskName "Sync Documentation" -Action $action -Trigger $trigger
```

### Option 4: File Watcher

Run the agent in watch mode to automatically sync on file changes:

```powershell
# Run in background
Start-Process pwsh -ArgumentList "-File scripts/Sync-Documentation.ps1 -Mode Watch" -WindowStyle Hidden
```

## Best Practices

1. **Run Before Commits**: Always run `Sync` mode before committing documentation changes
2. **Validate in CI/CD**: Include `Check` mode in your build pipeline
3. **Regular Syncs**: Run sync mode daily or after major feature implementations
4. **Manual Review**: Review auto-generated changes before committing

## TODO Identifier Format

All TODO items must follow this format:

```markdown
- [ ] **MVP-APP-001:** Description of task
```

Where:
- `MVP-APP-001` is the unique identifier
- Format: `MVP-{PROJECT}-{NUMBER}`
- Projects: `APP`, `MARKETING`, `SUPPORT`, `LEGAL`

## Requirements Document References

When adding new features, ensure:

1. **Project/TODO.md**: Add item with identifier
2. **Project/Status.md**: Reference will be auto-added
3. **Project/Functional-Requirements.md**: Add functional requirement with TODO reference
4. **Project/Technical-Requirements.md**: Add technical requirement with TODO reference

The agent will validate these references exist and are consistent.

## Troubleshooting

### Issue: "Missing reference to MVP-XXX-XXX"

**Solution**: Ensure the TODO identifier is referenced in all requirements documents. The agent will report which documents are missing references.

### Issue: Status counts don't match

**Solution**: Run `Sync` mode to recalculate all statistics based on current Project/TODO.md state.

### Issue: Last updated dates not changing

**Solution**: The agent only updates dates when content actually changes. Ensure you're making meaningful updates to trigger date changes.

## Future Enhancements

Potential improvements to the agent:

- [ ] Automatic requirement generation from code comments
- [ ] Integration with git to track requirement changes over time
- [ ] Webhook support for real-time updates
- [ ] Slack/Teams notifications for documentation changes
- [ ] Automatic Gantt chart updates based on TODO completion
- [ ] Cross-reference validation between requirements

---

*For questions or issues with the documentation sync agent, see the [Documentation README](README.md) or open an issue.*
