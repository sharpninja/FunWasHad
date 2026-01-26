<#
.SYNOPSIS
    Runs tests with code coverage and updates docs/Coverage-Report.md.

.DESCRIPTION
    MVP-SUPPORT-004: Runs dotnet test with coverlet, merges cobertura outputs
    via ReportGenerator, and writes docs/Coverage-Report.md. Use -SkipTests
    when tests (and coverage) were already run (e.g. in CI).

.PARAMETER SkipTests
    Do not run build or tests; only find existing coverage files and regenerate
    the markdown report. Use when CI has already run tests with coverage.

.PARAMETER Configuration
    Build configuration (Debug, Release). Default: Release.

.PARAMETER ProjectRoot
    Repository root. Default: parent of scripts folder.

.EXAMPLE
    .\scripts\Update-CoverageReport.ps1

.EXAMPLE
    .\scripts\Update-CoverageReport.ps1 -SkipTests
#>
[CmdletBinding()]
param(
    [switch] $SkipTests,
    [string] $Configuration = 'Release',
    [string] $ProjectRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
Set-Location $ProjectRoot

if (-not $SkipTests) {
    Write-Host 'Building solution...' -ForegroundColor Cyan
    dotnet build FunWasHad.sln --configuration $Configuration -v q
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

    Write-Host 'Running tests with coverage...' -ForegroundColor Cyan
    dotnet test FunWasHad.sln `
        --configuration $Configuration `
        --no-build `
        --settings coverlet.runsettings `
        --collect "XPlat code coverage" `
        -v n
    if ($LASTEXITCODE -ne 0) { Write-Host 'Some tests failed; continuing to generate report from any coverage produced.' -ForegroundColor Yellow }
}

# Find *cobertura*.xml: ./TestResults (when -r ./TestResults used) or under tests/**/bin/**/TestResults
$coverageFiles = @()
if (Test-Path ./TestResults) {
    $coverageFiles = @(Get-ChildItem -Path ./TestResults -Recurse -Filter "*.cobertura.xml" -ErrorAction SilentlyContinue)
}
if ($coverageFiles.Count -eq 0) {
    $coverageFiles = @(Get-ChildItem -Path . -Recurse -Include "*.cobertura.xml" -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch '[/\\]\.git[/\\]' -and $_.FullName -notmatch '[/\\]obj[/\\]' -and $_.FullName -match '[/\\]TestResults[/\\]' })
}

if ($coverageFiles.Count -eq 0) {
    $placeholder = @"
# Code Coverage Report

No coverage data found. Run tests with coverage first:

    .\scripts\Update-CoverageReport.ps1

Or in CI, ensure `dotnet test` uses `--collect "XPlat code coverage"` and `--settings coverlet.runsettings`.

*Last updated: $((Get-Date).ToString('yyyy-MM-dd HH:mm')) UTC (no data)*
"@
    Set-Content -Path ./docs/Coverage-Report.md -Value $placeholder -Encoding UTF8
    Write-Host 'No coverage files found; wrote placeholder to docs/Coverage-Report.md.' -ForegroundColor Yellow
    exit 0
}

$reports = ($coverageFiles | ForEach-Object { $_.FullName }) -join ';'
$outDir = (Join-Path $ProjectRoot 'coverage-report')
if (Test-Path $outDir) { Remove-Item -Recurse -Force $outDir }

Write-Host 'Restoring dotnet tools...' -ForegroundColor Cyan
dotnet tool restore
if ($LASTEXITCODE -ne 0) { throw 'dotnet tool restore failed. Ensure .config/dotnet-tools.json exists with dotnet-reportgenerator-globaltool.' }

Write-Host 'Generating coverage report...' -ForegroundColor Cyan
dotnet tool run reportgenerator -- "-reports:$reports" "-targetdir:$outDir" "-reporttypes:Markdown"
if ($LASTEXITCODE -ne 0) { throw 'ReportGenerator failed.' }

$md = Get-ChildItem -Path $outDir -Filter "*.md" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $md) {
    $placeholder = @"
# Code Coverage Report

Coverage data was merged but Markdown output was not produced.

*Last updated: $((Get-Date).ToString('yyyy-MM-dd HH:mm')) UTC*
"@
    Set-Content -Path ./docs/Coverage-Report.md -Value $placeholder -Encoding UTF8
    Write-Host 'No Markdown from ReportGenerator; wrote minimal docs/Coverage-Report.md.' -ForegroundColor Yellow
} else {
    $body = Get-Content -Path $md.FullName -Raw -Encoding UTF8
    $header = "Last updated: $((Get-Date).ToString('yyyy-MM-dd HH:mm')) UTC`n`n---`n`n"
    Set-Content -Path ./docs/Coverage-Report.md -Value ($header + $body) -Encoding UTF8
    Write-Host "Updated docs/Coverage-Report.md from $($md.Name)." -ForegroundColor Green
}

Remove-Item -Recurse -Force $outDir -ErrorAction SilentlyContinue
