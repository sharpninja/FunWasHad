#Requires -Version 7.0

<#
.SYNOPSIS
    Synchronizes Functional Requirements, Technical Requirements, TODO list, and Status documents.

.DESCRIPTION
    This script analyzes the codebase and documentation to ensure consistency across:
    - Functional-Requirements.md
    - Technical-Requirements.md
    - TODO.md
    - Status.md
    
    It detects TODO identifiers in code, checks completion status, and updates all documents accordingly.

.PARAMETER Mode
    Operation mode: 'Check' (validate only), 'Sync' (update documents), 'Watch' (monitor for changes)
    
.PARAMETER ProjectRoot
    Root directory of the project. Defaults to script parent directory.

.EXAMPLE
    .\Sync-Documentation.ps1 -Mode Check
    Validates documentation consistency without making changes.

.EXAMPLE
    .\Sync-Documentation.ps1 -Mode Sync
    Updates all documentation files to reflect current state.

.EXAMPLE
    .\Sync-Documentation.ps1 -Mode Watch
    Monitors for changes and automatically syncs documentation.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [ValidateSet('Check', 'Sync', 'Watch')]
    [string]$Mode = 'Sync',
    
    [Parameter(Mandatory = $false)]
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'

# Document paths
$docsPath = Join-Path $ProjectRoot "docs"
$projectPath = Join-Path $docsPath "Project"
$todoPath = Join-Path $projectPath "TODO.md"
$statusPath = Join-Path $projectPath "Status.md"
$functionalReqPath = Join-Path $projectPath "Functional-Requirements.md"
$technicalReqPath = Join-Path $projectPath "Technical-Requirements.md"

# Validate document paths exist
$requiredFiles = @($todoPath, $statusPath, $functionalReqPath, $technicalReqPath)
foreach ($file in $requiredFiles) {
    if (-not (Test-Path $file)) {
        Write-Error "Required document not found: $file"
        exit 1
    }
}

Write-Host "📋 Documentation Synchronization Agent" -ForegroundColor Cyan
Write-Host "Mode: $Mode" -ForegroundColor Yellow
Write-Host "Project Root: $ProjectRoot" -ForegroundColor Gray
Write-Host ""

function Get-TodoItems {
    param([string]$TodoPath)
    
    $content = Get-Content $TodoPath -Raw
    $todoItems = @()
    
    # Match TODO items with identifiers: - [ ] **MVP-XXX-XXX:**
    $pattern = '- \[ \]\s+\*\*([A-Z]+-[A-Z]+-\d+):\*\*'
    $matches = [regex]::Matches($content, $pattern)
    
    foreach ($match in $matches) {
        $identifier = $match.Groups[1].Value
        $lineNumber = ($content.Substring(0, $match.Index) -split "`n").Count
        
        # Extract the full TODO item
        $lines = Get-Content $TodoPath
        $itemStart = $lineNumber - 1
        $itemLines = @()
        $inItem = $false
        
        for ($i = $itemStart; $i -lt $lines.Count; $i++) {
            $line = $lines[$i]
            if ($line -match "^\s*- \[ \]\s+\*\*$([regex]::Escape($identifier))") {
                $inItem = $true
                $itemLines += $line
            }
            elseif ($inItem) {
                if ($line -match "^\s*- \[ \]|^\s*##|^##|^---") {
                    if ($line -match "^\s*- \[ \]\s+\*\*[A-Z]+-[A-Z]+-\d+:") {
                        break
                    }
                    if ($line -match "^##|^---") {
                        break
                    }
                }
                $itemLines += $line
            }
        }
        
        $todoItems += @{
            Identifier = $identifier
            LineNumber = $lineNumber
            Content = $itemLines -join "`n"
            IsCompleted = $false
        }
    }
    
    # Check for completed items (marked with [x])
    $completedPattern = '- \[x\]\s+\*\*([A-Z]+-[A-Z]+-\d+):\*\*'
    $completedMatches = [regex]::Matches($content, $completedPattern)
    foreach ($match in $completedMatches) {
        $identifier = $match.Groups[1].Value
        $item = $todoItems | Where-Object { $_.Identifier -eq $identifier }
        if ($item) {
            $item.IsCompleted = $true
        }
    }
    
    return $todoItems
}

function Get-StatusFromCode {
    param([string]$ProjectRoot)
    
    $statusMap = @{}
    
    # Search for TODO identifiers in code comments
    $codeFiles = Get-ChildItem -Path $ProjectRoot -Include *.cs,*.csproj,*.puml -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\|\\node_modules\\|\\\.git\\' }
    
    foreach ($file in $codeFiles) {
        $content = Get-Content $file.FullName -Raw -ErrorAction SilentlyContinue
        if ($content) {
            # Look for TODO identifiers in comments
            $pattern = '(MVP-[A-Z]+-\d+)'
            $matches = [regex]::Matches($content, $pattern)
            foreach ($match in $matches) {
                $identifier = $match.Value
                if (-not $statusMap.ContainsKey($identifier)) {
                    $statusMap[$identifier] = @{
                        FoundInCode = $true
                        Files = @()
                    }
                }
                $statusMap[$identifier].Files += $file.Name
            }
        }
    }
    
    return $statusMap
}

function Update-StatusDocument {
    param(
        [string]$StatusPath,
        [array]$TodoItems
    )
    
    $content = Get-Content $StatusPath -Raw
    $updated = $false
    
    # Count completed items per project
    $projectStats = @{
        'MVP-App' = @{ High = 0; Medium = 0; Completed = 0 }
        'MVP-Marketing' = @{ High = 0; Medium = 0; Completed = 0 }
        'MVP-Support' = @{ High = 0; Medium = 0; Completed = 0 }
        'MVP-Legal' = @{ High = 0; Medium = 0; Completed = 0 }
    }
    
    foreach ($item in $TodoItems) {
        $project = switch -Regex ($item.Identifier) {
            'MVP-APP-' { 'MVP-App' }
            'MVP-MARKETING-' { 'MVP-Marketing' }
            'MVP-SUPPORT-' { 'MVP-Support' }
            'MVP-LEGAL-' { 'MVP-Legal' }
            default { $null }
        }
        
        if ($project) {
            $priority = if ($item.Content -match '### High Priority') { 'High' } else { 'Medium' }
            $projectStats[$project][$priority]++
            if ($item.IsCompleted) {
                $projectStats[$project].Completed++
            }
        }
    }
    
    # Update overview table
    $tablePattern = '\| MVP-App \| 🔴 Planning \| \d+ \| \d+ \| \d+ \|'
    $newTable = @"
| MVP-App | 🔴 Planning | $($projectStats['MVP-App'].High) | $($projectStats['MVP-App'].Medium) | $($projectStats['MVP-App'].High + $projectStats['MVP-App'].Medium) |
| MVP-Marketing | 🔴 Planning | $($projectStats['MVP-Marketing'].High) | $($projectStats['MVP-Marketing'].Medium) | $($projectStats['MVP-Marketing'].High + $projectStats['MVP-Marketing'].Medium) |
| MVP-Support | 🔴 Planning | $($projectStats['MVP-Support'].High) | $($projectStats['MVP-Support'].Medium) | $($projectStats['MVP-Support'].High + $projectStats['MVP-Support'].Medium) |
| MVP-Legal | 🔴 Planning | $($projectStats['MVP-Legal'].High) | $($projectStats['MVP-Legal'].Medium) | $($projectStats['MVP-Legal'].High + $projectStats['MVP-Legal'].Medium) |
"@
    
    if ($content -match $tablePattern) {
        $content = $content -replace $tablePattern, $newTable
        $updated = $true
    }
    
    # Update last updated date
    $datePattern = '\*Last updated: \d{4}-\d{2}-\d{2}\*'
    $newDate = "*Last updated: $(Get-Date -Format 'yyyy-MM-dd')*"
    if ($content -match $datePattern) {
        $content = $content -replace $datePattern, $newDate
        $updated = $true
    }
    
    if ($updated) {
        Set-Content -Path $StatusPath -Value $content -NoNewline
        Write-Host "✅ Updated Status.md" -ForegroundColor Green
        return $true
    }
    
    return $false
}

function Remove-BrokenLinks {
    param(
        [string]$FilePath,
        [string]$DocsRoot
    )
    
    if (-not (Test-Path $FilePath)) {
        return $false
    }
    
    $content = Get-Content $FilePath -Raw
    $originalContent = $content
    $removedCount = 0
    
    # Pattern for markdown links: [text](path) or [text](~/path)
    $linkPattern = '\[([^\]]+)\]\(([^\)]+)\)'
    $matches = [regex]::Matches($content, $linkPattern)
    
    foreach ($match in $matches) {
        $linkText = $match.Groups[1].Value
        $linkPath = $match.Groups[2].Value
        
        # Skip external links (http/https)
        if ($linkPath -match '^https?://') {
            continue
        }
        
        # Skip anchor links (#)
        if ($linkPath -match '^#') {
            continue
        }
        
        # Resolve relative paths
        $resolvedPath = $linkPath
        if ($linkPath -match '^~/') {
            # DocFX format: ~/ means relative to docs root
            $resolvedPath = $linkPath -replace '^~/', ''
            $resolvedPath = Join-Path $DocsRoot $resolvedPath
        }
        elseif ($linkPath -match '^\.\./') {
            # Relative path: ../ means go up from current file
            $fileDir = Split-Path -Parent $FilePath
            $resolvedPath = Join-Path $fileDir $linkPath
            $resolvedPath = Resolve-Path $resolvedPath -ErrorAction SilentlyContinue
        }
        else {
            # Relative to current file
            $fileDir = Split-Path -Parent $FilePath
            $resolvedPath = Join-Path $fileDir $linkPath
            $resolvedPath = Resolve-Path $resolvedPath -ErrorAction SilentlyContinue
        }
        
        # Check if file exists
        if (-not (Test-Path $resolvedPath)) {
            # Remove the link, keep just the text
            $content = $content -replace [regex]::Escape($match.Value), $linkText
            $removedCount++
            Write-Host "  ⚠️  Removed broken link: $linkPath" -ForegroundColor Yellow
        }
    }
    
    if ($removedCount -gt 0) {
        Set-Content -Path $FilePath -Value $content -NoNewline
        Write-Host "✅ Removed $removedCount broken link(s) from $(Split-Path -Leaf $FilePath)" -ForegroundColor Green
        return $true
    }
    
    return $false
}

function Remove-BrokenLinksFromDocs {
    param(
        [string]$DocsPath
    )
    
    Write-Host "🔗 Checking for broken links..." -ForegroundColor Cyan
    
    $markdownFiles = Get-ChildItem -Path $DocsPath -Filter "*.md" -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch '\\_site\\|\\\.git\\' }
    
    $totalRemoved = 0
    foreach ($file in $markdownFiles) {
        if (Remove-BrokenLinks -FilePath $file.FullName -DocsRoot $DocsPath) {
            $totalRemoved++
        }
    }
    
    if ($totalRemoved -gt 0) {
        Write-Host "✅ Cleaned broken links from $totalRemoved file(s)" -ForegroundColor Green
    }
    else {
        Write-Host "✅ No broken links found" -ForegroundColor Green
    }
}

function Build-Documentation {
    param(
        [string]$DocsPath
    )
    
    Write-Host "📚 Building documentation..." -ForegroundColor Cyan
    
    $docfxPath = Join-Path $DocsPath "docfx.json"
    if (-not (Test-Path $docfxPath)) {
        Write-Host "⚠️  docfx.json not found, skipping build" -ForegroundColor Yellow
        return $false
    }
    
    # Check if docfx is available
    $docfx = Get-Command docfx -ErrorAction SilentlyContinue
    if (-not $docfx) {
        Write-Host "⚠️  DocFX not found. Install with: dotnet tool install -g docfx" -ForegroundColor Yellow
        return $false
    }
    
    try {
        Push-Location $DocsPath
        $output = & docfx build docfx.json 2>&1
        $exitCode = $LASTEXITCODE
        
        if ($exitCode -eq 0) {
            Write-Host "✅ Documentation built successfully" -ForegroundColor Green
            return $true
        }
        else {
            Write-Host "⚠️  Documentation build completed with warnings/errors" -ForegroundColor Yellow
            # Show first few errors/warnings
            $errors = $output | Select-String -Pattern "error|warning" | Select-Object -First 5
            foreach ($err in $errors) {
                Write-Host "  $err" -ForegroundColor Gray
            }
            return $false
        }
    }
    catch {
        Write-Host "❌ Error building documentation: $_" -ForegroundColor Red
        return $false
    }
    finally {
        Pop-Location
    }
}

function Validate-Documentation {
    param(
        [string]$TodoPath,
        [string]$StatusPath,
        [string]$FunctionalReqPath,
        [string]$TechnicalReqPath
    )
    
    Write-Host "🔍 Validating documentation consistency..." -ForegroundColor Cyan
    
    $issues = @()
    
    # Get all TODO identifiers
    $todoItems = Get-TodoItems -TodoPath $TodoPath
    $todoIdentifiers = $todoItems | ForEach-Object { $_.Identifier }
    
    # Check Status.md references
    $statusContent = Get-Content $StatusPath -Raw
    foreach ($identifier in $todoIdentifiers) {
        if ($statusContent -notmatch [regex]::Escape($identifier)) {
            $issues += "Status.md missing reference to $identifier"
        }
    }
    
    # Check Functional Requirements references
    $functionalContent = Get-Content $FunctionalReqPath -Raw
    foreach ($identifier in $todoIdentifiers) {
        if ($functionalContent -notmatch [regex]::Escape($identifier)) {
            $issues += "Functional-Requirements.md missing reference to $identifier"
        }
    }
    
    # Check Technical Requirements references
    $technicalContent = Get-Content $TechnicalReqPath -Raw
    foreach ($identifier in $todoIdentifiers) {
        if ($technicalContent -notmatch [regex]::Escape($identifier)) {
            $issues += "Technical-Requirements.md missing reference to $identifier"
        }
    }
    
    if ($issues.Count -gt 0) {
        Write-Host "⚠️  Found $($issues.Count) consistency issue(s):" -ForegroundColor Yellow
        foreach ($issue in $issues) {
            Write-Host "  - $issue" -ForegroundColor Yellow
        }
        return $false
    }
    else {
        Write-Host "✅ All documentation is consistent" -ForegroundColor Green
        return $true
    }
}

# Main execution
try {
    switch ($Mode) {
        'Check' {
            $isValid = Validate-Documentation -TodoPath $todoPath -StatusPath $statusPath `
                -FunctionalReqPath $functionalReqPath -TechnicalReqPath $technicalReqPath
            exit $(if ($isValid) { 0 } else { 1 })
        }
        
        'Sync' {
            Write-Host "🔄 Synchronizing documentation..." -ForegroundColor Cyan
            
            # Get current TODO items
            $todoItems = Get-TodoItems -TodoPath $todoPath
            Write-Host "Found $($todoItems.Count) TODO items" -ForegroundColor Gray
            
            # Update Status document
            $statusUpdated = Update-StatusDocument -StatusPath $statusPath -TodoItems $todoItems
            
            # Remove broken links
            Remove-BrokenLinksFromDocs -DocsPath $docsPath
            
            # Validate consistency
            $isValid = Validate-Documentation -TodoPath $todoPath -StatusPath $statusPath `
                -FunctionalReqPath $functionalReqPath -TechnicalReqPath $technicalReqPath
            
            # Rebuild documentation
            Build-Documentation -DocsPath $docsPath
            
            Write-Host ""
            Write-Host "✅ Synchronization complete" -ForegroundColor Green
        }
        
        'Watch' {
            Write-Host "👀 Watching for changes..." -ForegroundColor Cyan
            Write-Host "Press Ctrl+C to stop" -ForegroundColor Gray
            
            $watcher = New-Object System.IO.FileSystemWatcher
            $watcher.Path = $ProjectRoot
            $watcher.IncludeSubdirectories = $true
            $watcher.Filter = "*.cs"
            $watcher.EnableRaisingEvents = $true
            
            $action = {
                $path = $Event.SourceEventArgs.FullPath
                $changeType = $Event.SourceEventArgs.ChangeType
                Write-Host "[$(Get-Date -Format 'HH:mm:ss')] $changeType : $path" -ForegroundColor Gray
                
                Start-Sleep -Seconds 2
                & $PSScriptRoot\Sync-Documentation.ps1 -Mode Sync -ProjectRoot $ProjectRoot | Out-Null
            }
            
            Register-ObjectEvent -InputObject $watcher -EventName "Changed" -Action $action | Out-Null
            Register-ObjectEvent -InputObject $watcher -EventName "Created" -Action $action | Out-Null
            
            try {
                while ($true) {
                    Start-Sleep -Seconds 1
                }
            }
            finally {
                $watcher.Dispose()
            }
        }
    }
}
catch {
    Write-Error "Error during synchronization: $_"
    Write-Error $_.ScriptStackTrace
    exit 1
}
