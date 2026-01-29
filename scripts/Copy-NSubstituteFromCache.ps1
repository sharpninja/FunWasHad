<#
.SYNOPSIS
    Copies NSubstitute 6.0.0 from the NuGet cache into lib/NSubstitute.6.0.0 so the solution can reference it as a local library.

.DESCRIPTION
    Copies the contents of the NSubstitute 6.0.0 package from the NuGet cache into lib/NSubstitute.6.0.0/.
    Run once (e.g. after building NSubstitute from source or restoring it from a feed), then commit lib/NSubstitute.6.0.0/ so CI can build without GitHub Packages.

.PARAMETER CachePath
    Folder containing the NSubstitute 6.0.0 package (the nsubstitute/6.0.0 cache folder). Default: tries common locations.

.EXAMPLE
    .\scripts\Copy-NSubstituteFromCache.ps1
#>
param(
    [string] $CachePath
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$target = Join-Path $root 'lib\NSubstitute.6.0.0'

if (-not $CachePath) {
    $candidates = @(
        (Join-Path $env:USERPROFILE '.nuget\packages\nsubstitute\6.0.0'),
        'E:\packages\NuGet\cache\nsubstitute\6.0.0',
        (Join-Path $env:NUGET_PACKAGES 'nsubstitute\6.0.0')
    )
    foreach ($c in $candidates) {
        if ($c -and (Test-Path -LiteralPath $c -PathType Container)) {
            $CachePath = $c
            break
        }
    }
}

if (-not $CachePath -or -not (Test-Path -LiteralPath $CachePath -PathType Container)) {
    Write-Error "NSubstitute 6.0.0 cache folder not found. Set -CachePath to the nsubstitute/6.0.0 folder (e.g. %USERPROFILE%\.nuget\packages\nsubstitute\6.0.0)."
}

if (-not (Test-Path (Join-Path $CachePath 'lib') -PathType Container)) {
    Write-Error "Cache path does not contain 'lib' (expected extracted package). Path: $CachePath"
}

if (Test-Path -LiteralPath $target -PathType Container) {
    Remove-Item -LiteralPath $target -Recurse -Force
}
New-Item -Path $target -ItemType Directory -Force | Out-Null

Copy-Item -Path (Join-Path $CachePath '*') -Destination $target -Recurse -Force
Write-Host "Copied NSubstitute 6.0.0 from $CachePath to $target"
Write-Host "Commit lib/NSubstitute.6.0.0/ so CI can build without GitHub Packages."
