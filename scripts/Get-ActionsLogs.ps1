<#
.SYNOPSIS
    Downloads workflow logs from the last GitHub Actions run on the current branch.

.DESCRIPTION
    Uses the GitHub CLI (gh) to find the most recent workflow run for the current
    (or specified) branch and downloads the workflow-logs artifact produced by
    the staging workflow's upload_logs job.

.PARAMETER Branch
    Git branch to use. Defaults to the current branch (e.g. develop, staging).

.PARAMETER OutputPath
    Directory to save the logs artifact and extracted run.log. Defaults to .\logs at repo root.

.PARAMETER Extract
    Decompress the downloaded .gz to run.log in OutputPath (e.g. .\logs\run.log). Default: $true.

.PARAMETER Workflow
    Workflow filename or name to filter runs. Default: "staging.yml".

.EXAMPLE
    .\scripts\Get-ActionsLogs.ps1
    # Downloads and extracts to .\logs\run.log

.EXAMPLE
    .\scripts\Get-ActionsLogs.ps1 -OutputPath ".\my-logs"

.EXAMPLE
    .\scripts\Get-ActionsLogs.ps1 -Branch staging -Extract:$false
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string] $Branch = (git branch --show-current 2>$null),

    [Parameter(Mandatory = $false)]
    [string] $OutputPath = (Join-Path (Split-Path -Parent $PSScriptRoot) "logs"),

    [Parameter(Mandatory = $false)]
    [bool] $Extract = $true,

    [Parameter(Mandatory = $false)]
    [string] $Workflow = "staging.yml"
)

$ErrorActionPreference = "Stop"

# Require gh
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Error "GitHub CLI (gh) is required. Install it from https://cli.github.com/"
}

if (-not $Branch) {
    Write-Error "Could not determine current branch. Run from a git repository or pass -Branch."
}

Write-Host "[i] Branch: $Branch" -ForegroundColor Cyan
Write-Host "[i] Workflow: $Workflow" -ForegroundColor Cyan

# Get latest run for this branch
$runJson = gh run list --branch $Branch --workflow $Workflow --limit 1 --json databaseId,displayTitle,conclusion,createdAt 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to list runs: $runJson"
}

$runs = $runJson | ConvertFrom-Json
if (-not $runs -or $runs.Count -eq 0) {
    Write-Warning "No runs found for branch '$Branch' and workflow '$Workflow'."
    exit 1
}

$runId = $runs[0].databaseId
$title = $runs[0].displayTitle
$conclusion = $runs[0].conclusion
$created = $runs[0].createdAt

Write-Host "[i] Latest run: $runId - $title ($conclusion) @ $created" -ForegroundColor Cyan

# Artifact name produced by staging.yml upload_logs job
$artifactName = "workflow-logs-$runId"

# Download into a run-specific subdir to avoid "file exists" when run.log.gz is already in OutputPath
$downloadDir = Join-Path $OutputPath $artifactName
$null = New-Item -ItemType Directory -Force -Path $downloadDir

# Download only this artifact into the run-specific dir
$downloadOut = gh run download $runId --name $artifactName --dir $downloadDir 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Warning "Run $runId has no artifact named '$artifactName'. Logs may not have been uploaded for this run (e.g. run failed before upload_logs, or upload_logs was skipped)."
    Write-Host $downloadOut
    Write-Host ""
    Write-Host "Try an older run, or re-run the workflow so a new run produces the artifact." -ForegroundColor Yellow
    exit 1
}

# gh run download extracts into downloadDir (path from upload: _logs/run.log.gz or run.log.gz)
$artifactDir = $downloadDir
$gzPath = Get-ChildItem -Path $artifactDir -Recurse -Filter "run.log.gz" -ErrorAction SilentlyContinue | Select-Object -First 1
$gzPath = if ($gzPath) { $gzPath.FullName } else { $null }

if ($gzPath -and (Test-Path $gzPath)) {
    Write-Host "[✓] Logs saved to: $gzPath" -ForegroundColor Green
    if ($Extract) {
        $logPath = Join-Path $OutputPath "run.log"
        try {
            if (Test-Path $logPath) { Remove-Item $logPath -Force }
            $inStream = [System.IO.File]::OpenRead($gzPath)
            $gzip = [System.IO.Compression.GZipStream]::new($inStream, [System.IO.Compression.CompressionMode]::Decompress)
            $outStream = [System.IO.File]::Create($logPath)
            $gzip.CopyTo($outStream)
            $outStream.Close()
            $gzip.Close()
            $inStream.Close()
            Write-Host "[✓] Decompressed to: $logPath" -ForegroundColor Green
        } catch {
            Write-Warning "Could not decompress: $_"
        }
    }
} else {
    Write-Host "[✓] Artifact downloaded to: $artifactDir" -ForegroundColor Green
    Get-ChildItem -Path $artifactDir -Recurse | ForEach-Object { Write-Host "    $($_.FullName)" -ForegroundColor Gray }
}

Write-Host ""
Write-Host "View run: gh run view $runId" -ForegroundColor Gray
