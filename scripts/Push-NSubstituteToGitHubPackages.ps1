<#
.SYNOPSIS
    Pushes NSubstitute 6.0.0 from the local NuGet cache to the GitHub Packages NuGet feed
    for the FunWasHad project and adds that feed to the global NuGet configuration.

.DESCRIPTION
    Publishes each .nupkg under CachePath to GitHub Packages. Validates the token (GET /user) and
    ensures it has write:packages via the X-OAuth-Scopes header before pushing. Token is taken from
    -Token, then $env:GITHUB_TOKEN, then gh auth token when gh is logged in. Then registers the
    feed in the user-level NuGet config for restore.

.PARAMETER CachePath
    Folder containing the NSubstitute 6.0.0 package(s). Default: E:\packages\NuGet\cache\nsubstitute\6.0.0

.PARAMETER Token
    GitHub token with write:packages. If omitted, uses $env:GITHUB_TOKEN or gh auth token.

.PARAMETER SkipGlobalConfig
    If set, only push; do not add the feed to the global NuGet config.

.PARAMETER FeedName
    Name for the NuGet source when adding to config. Default: github-sharpninja

.PARAMETER SkipTokenValidation
    When -Token is provided by the caller, skip GET /user and scope check (avoids redundant auth).

.EXAMPLE
    gh auth login
    .\scripts\Push-NSubstituteToGitHubPackages.ps1

.EXAMPLE
    .\scripts\Push-NSubstituteToGitHubPackages.ps1 -Token (Get-Content .\pat.txt -Raw) -SkipGlobalConfig
#>
param(
    [string] $CachePath = 'E:\packages\NuGet\cache\nsubstitute\6.0.0',
    [string] $Token,
    [switch] $SkipGlobalConfig,
    [string] $FeedName = 'github-sharpninja',
    [switch] $SkipTokenValidation
)

$ErrorActionPreference = 'Stop'
$feedUrl = 'https://nuget.pkg.github.com/sharpninja/index.json'
$owner = 'sharpninja'

if (-not $Token) {
    $Token = $env:GITHUB_TOKEN
}
if (-not $Token) {
    try {
        $t = gh auth token 2>$null
        if ($t) { $Token = $t.Trim() }
    } catch {}
}
if (-not $Token) {
    Write-Error 'No token. Run gh auth login, or set -Token or $env:GITHUB_TOKEN (PAT with write:packages).'
}

# Ensure the token is valid and has write:packages (skip when caller already validated, e.g. wrapper passed -Token)
if (-not $SkipTokenValidation) {
    $apiHeaders = @{
        Authorization = "Bearer $Token"
        Accept        = 'application/vnd.github+json'
    }
    try {
        $authResponse = Invoke-WebRequest -Uri 'https://api.github.com/user' -Headers $apiHeaders -Method Get -UseBasicParsing -ErrorAction Stop
    } catch {
        $status = try { [int]$_.Exception.Response.StatusCode } catch { 0 }
        if ($status -in 401, 403) {
            Write-Error 'Token is invalid or expired. Use a PAT (classic) with write:packages scope, or run gh auth login.'
        }
        throw
    }
    $scopes = $authResponse.Headers['X-OAuth-Scopes']
    if ($scopes -and $scopes -notmatch '\bwrite:packages\b') {
        Write-Error "Token must have write:packages scope. Current scopes: $scopes. Create a PAT at https://github.com/settings/tokens with write:packages."
    }
}

if (-not (Test-Path -LiteralPath $CachePath -PathType Container)) {
    Write-Error "Cache path not found: $CachePath"
}

$nupkgs = Get-ChildItem -Path $CachePath -Filter '*.nupkg' -File -ErrorAction SilentlyContinue
if (-not $nupkgs) {
    Write-Error "No *.nupkg found under: $CachePath"
}

foreach ($nupkg in $nupkgs) {
    Write-Host "Publishing $($nupkg.Name) to GitHub Packages ..."
    dotnet nuget push $nupkg.FullName --source $feedUrl --api-key $Token --skip-duplicate
    if ($LASTEXITCODE -ne 0) {
        throw "Publish failed for $($nupkg.Name)"
    }
}

if (-not $SkipGlobalConfig) {
    $configPath = Join-Path $env:APPDATA 'NuGet\NuGet.Config'
    $configDir = Split-Path $configPath -Parent
    if (-not (Test-Path -LiteralPath $configDir -PathType Container)) {
        New-Item -Path $configDir -ItemType Directory -Force | Out-Null
    }
    Write-Host "Adding source '$FeedName' to global NuGet config: $configPath"
    dotnet nuget add source $feedUrl --name $FeedName --username $owner --password $Token --store-password-in-clear-text --configfile $configPath
    if ($LASTEXITCODE -ne 0) {
        throw 'dotnet nuget add source failed'
    }
    Write-Host "Done. NSubstitute 6.0 is on GitHub Packages and the feed is in your global NuGet config."
} else {
    Write-Host "Push complete. Run without -SkipGlobalConfig to add the feed to global NuGet config."
}
