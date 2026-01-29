<#
.SYNOPSIS
    Pulls workflow logs from the last GitHub Actions run on the current branch.

.DESCRIPTION
    If no -RunId is provided, gets the most recently completed run on the current
    (or -Branch) for the given workflow, then pulls its logs via "gh run view --log"
    and writes them to OutputPath\run.log.

.PARAMETER Branch
    Git branch to use. Defaults to the current branch (e.g. develop, staging).

.PARAMETER RunId
    Specific run ID. If set, skip branch/workflow lookup and pull that run's logs.

.PARAMETER OutputPath
    Directory to save run.log. Defaults to .\logs at repo root.

.PARAMETER Workflow
    Workflow filename or name to filter runs. Default: "staging.yml". Ignored when -RunId is set.

.EXAMPLE
    .\scripts\Get-ActionsLogs.ps1
    # Pulls logs for latest completed run on current branch to .\logs\run.log

.EXAMPLE
    .\scripts\Get-ActionsLogs.ps1 -RunId 21415574493
    # Pulls logs for that run to .\logs\run.log

.EXAMPLE
    .\scripts\Get-ActionsLogs.ps1 -Branch staging -OutputPath ".\my-logs"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string] $Branch = (git branch --show-current 2>$null),

    [Parameter(Mandatory = $false)]
    [string] $RunId,

    [Parameter(Mandatory = $false)]
    [string] $OutputPath = (Join-Path (Split-Path -Parent $PSScriptRoot) "logs"),

    [Parameter(Mandatory = $false)]
    [string] $Workflow = "staging.yml"
)

$ErrorActionPreference = "Stop"

# Require gh
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Error "GitHub CLI (gh) is required. Install it from https://cli.github.com/"
}

# Resolve repo
$repo = gh repo view --json nameWithOwner -q .nameWithOwner 2>$null
if (-not $repo) {
    Write-Error "Could not determine repository. Run from a git repository with gh authenticated."
}

if ($RunId) {
    Write-Host "[i] Run ID: $RunId" -ForegroundColor Cyan
} else {
    if (-not $Branch) {
        Write-Error "Could not determine current branch. Run from a git repository or pass -Branch or -RunId."
    }
    Write-Host "[i] Branch: $Branch" -ForegroundColor Cyan
    Write-Host "[i] Workflow: $Workflow" -ForegroundColor Cyan

    # If no RunId: get the most recently completed run on this branch
    $runJson = gh run list --repo $repo --branch $Branch --workflow $Workflow --status completed --limit 1 --json databaseId,displayTitle,conclusion,createdAt 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to list runs: $runJson"
    }
    $runs = $runJson | ConvertFrom-Json
    if (-not $runs -or $runs.Count -eq 0) {
        Write-Warning "No completed runs found for branch '$Branch' and workflow '$Workflow'. In-progress runs are skipped."
        exit 1
    }
    $RunId = $runs[0].databaseId
    $title = $runs[0].displayTitle
    $conclusion = $runs[0].conclusion
    $created = $runs[0].createdAt
    Write-Host "[i] Latest completed run: $RunId - $title ($conclusion) @ $created" -ForegroundColor Cyan
}

$null = New-Item -ItemType Directory -Force -Path $OutputPath
$logPath = Join-Path $OutputPath "run.log"

Write-Host "[i] Pulling logs (gh run view --log)..." -ForegroundColor Cyan
$logContent = gh run view $RunId --repo $repo --log 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Warning "Failed to pull logs: $logContent"
    exit 1
}
if (Test-Path $logPath) { Remove-Item $logPath -Force }
$logContent | Set-Content -Path $logPath -Encoding utf8
Write-Host "[✓] Logs written to: $logPath" -ForegroundColor Green
Write-Host ""
Write-Host "View run: gh run view $RunId --repo $repo" -ForegroundColor Gray
