<#
.SYNOPSIS
  Start the AppHost, then invoke health check endpoints for Location API and Marketing API.

.DESCRIPTION
  Starts the Aspire AppHost in the background (Debug or Staging configuration), waits for
  the APIs to respond, then invokes GET /health and GET /alive and prints the results.
  Does not use unit tests; calls the real running servers.

.PARAMETER Configuration
  Build configuration: Debug or Staging.

.PARAMETER TimeoutSeconds
  Seconds to wait for health endpoints to become available before failing.

.PARAMETER LeaveRunning
  If set, do not stop the AppHost after invoking health checks (default: stop it).

.EXAMPLE
  .\Test-HealthChecks.ps1 -Configuration Debug
.EXAMPLE
  .\Test-HealthChecks.ps1 -Configuration Staging -LeaveRunning
#>

param(
    [ValidateSet('Debug', 'Staging')]
    [string]$Configuration = 'Debug',
    [int]$TimeoutSeconds = 120,
    [switch]$LeaveRunning
)

$ErrorActionPreference = 'Stop'
# Script lives in <repo>/scripts; repo root is parent of scripts folder
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

$appHostProject = Join-Path $repoRoot 'src\FWH.AppHost\FWH.AppHost.csproj'
$endpoints = @(
    @{ Name = 'Location API'; Base = 'http://localhost:4748'; Paths = @('/health', '/alive') },
    @{ Name = 'Marketing API'; Base = 'http://localhost:4750'; Paths = @('/health', '/alive') }
)

function Wait-ForHealthEndpoint {
    param([string]$Url, [int]$TimeoutSec)
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSec)
    while ([DateTime]::UtcNow -lt $deadline) {
        try {
            $r = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
            if ($r.StatusCode -eq 200) { return $true }
        } catch {
            Start-Sleep -Seconds 2
        }
    }
    return $false
}

function Invoke-HealthCheck {
    param([string]$Url)
    try {
        $r = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 10
        return @{ StatusCode = $r.StatusCode; Content = $r.Content.Trim() }
    } catch {
        return @{ StatusCode = $null; Error = $_.Exception.Message }
    }
}

Write-Host "Building and starting AppHost (Configuration: $Configuration)..." -ForegroundColor Cyan
# Run AppHost from repo root so it can resolve and run child projects
$job = Start-Job -ScriptBlock {
    param($path, $config, $root)
    Set-Location $root
    & dotnet run --project $path -c $config 2>&1
} -ArgumentList $appHostProject, $Configuration, $repoRoot

try {
    Write-Host "Waiting for health endpoints (up to $TimeoutSeconds s)..." -ForegroundColor Gray
    $locationReady = Wait-ForHealthEndpoint -Url 'http://localhost:4748/health' -TimeoutSec $TimeoutSeconds
    $marketingReady = Wait-ForHealthEndpoint -Url 'http://localhost:4750/health' -TimeoutSec 30
    if (-not $locationReady) {
        Write-Warning "Location API /health did not respond within $TimeoutSeconds s."
    }
    if (-not $marketingReady) {
        Write-Warning "Marketing API /health did not respond within 30 s."
    }
    if (-not $locationReady -and -not $marketingReady) {
        Write-Error "No health endpoints became available. Ensure Docker is running, and that ports 4748, 4750, and the Aspire dashboard port (e.g. 22299) are not in use by another process."
    }

    Write-Host "`nInvoking health check endpoints:" -ForegroundColor Cyan
    foreach ($api in $endpoints) {
        Write-Host "  $($api.Name) ($($api.Base))" -ForegroundColor Gray
        foreach ($path in $api.Paths) {
            $url = $api.Base + $path
            $result = Invoke-HealthCheck -Url $url
            if ($result.StatusCode -eq 200) {
                Write-Host "    $path -> $($result.StatusCode) $($result.Content)" -ForegroundColor Green
            } else {
                Write-Host "    $path -> $($result.Error)" -ForegroundColor Red
            }
        }
    }
} finally {
    if (-not $LeaveRunning) {
        Write-Host "`nStopping AppHost..." -ForegroundColor Gray
        Stop-Job -Job $job -ErrorAction SilentlyContinue
        $output = Receive-Job -Job $job
        Remove-Job -Job $job -Force -ErrorAction SilentlyContinue
        if ($output -and -not $locationReady -and -not $marketingReady) {
            Write-Host "`nAppHost job output (last 50 lines):" -ForegroundColor Yellow
            $output | Select-Object -Last 50 | ForEach-Object { Write-Host $_ }
        }
    } else {
        Write-Host "`nAppHost left running. Stop with: Get-Job | Stop-Job; Get-Job | Remove-Job" -ForegroundColor Gray
    }
}
