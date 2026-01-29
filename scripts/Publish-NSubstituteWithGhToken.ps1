<#
.SYNOPSIS
    Ensures gh has write:packages, runs Push-NSubstituteToGitHubPackages.ps1, then removes that scope.

.DESCRIPTION
    Uses gh to grant the current session write:packages (one auth flow), captures the token once,
    runs the NSubstitute push script with that token and -SkipTokenValidation so it does not
    prompt or call the API again, then removes write:packages from gh. Pass -SkipScopeCleanup
    to avoid the second auth when removing the scope.

.PARAMETER CachePath
    Folder containing the NSubstitute 6.0.0 package(s). Passed to Push-NSubstituteToGitHubPackages.ps1.

.PARAMETER SkipGlobalConfig
    If set, only push; do not add the feed to the global NuGet config. Passed through.

.PARAMETER SkipScopeCleanup
    If set, do not run gh auth refresh -r write:packages after the push (leave write:packages on the token).

.EXAMPLE
    .\scripts\Publish-NSubstituteWithGhToken.ps1

.EXAMPLE
    .\scripts\Publish-NSubstituteWithGhToken.ps1 -SkipGlobalConfig -SkipScopeCleanup
#>
param(
    [string] $CachePath = 'E:\packages\NuGet\cache\nsubstitute\6.0.0',
    [switch] $SkipGlobalConfig,
    [switch] $SkipScopeCleanup
)

$ErrorActionPreference = 'Stop'
$pushScript = Join-Path $PSScriptRoot 'Push-NSubstituteToGitHubPackages.ps1'

# 1) Ensure gh has write:packages (single auth flow; may open browser)
Write-Host 'Ensuring gh credentials have write:packages scope...'
gh auth refresh -s write:packages
if ($LASTEXITCODE -ne 0) {
    throw 'gh auth refresh -s write:packages failed. Run gh auth login first.'
}

# 2) Capture token once and run the push script with it (avoids push script calling gh auth token or GET /user again)
$token = (gh auth token 2>$null).Trim()
if (-not $token) { throw 'gh auth token failed after refresh.' }

$pushArgs = @{
    Token               = $token
    SkipTokenValidation = $true
    CachePath           = $CachePath
}
if ($SkipGlobalConfig) { $pushArgs['SkipGlobalConfig'] = $true }
try {
    & $pushScript @pushArgs
    $pushExit = $LASTEXITCODE
} catch {
    $pushExit = 1
    if (-not $SkipScopeCleanup) {
        Write-Host 'Removing write:packages from gh credentials after failure...'
        gh auth refresh -r write:packages 2>$null
    }
    throw
}

# 3) Remove write:packages from gh session so the token no longer has package write
if (-not $SkipScopeCleanup) {
    Write-Host 'Removing write:packages from gh credentials...'
    gh auth refresh -r write:packages
    if ($LASTEXITCODE -ne 0) {
        Write-Warning 'gh auth refresh -r write:packages failed; token still has write:packages. Run it manually to remove the scope.'
    }
}

if ($pushExit -ne 0) { exit $pushExit }
