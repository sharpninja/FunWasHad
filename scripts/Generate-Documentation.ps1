<#
.SYNOPSIS
  Generates project documentation using DocFX.

.DESCRIPTION
  This script generates documentation for the FunWasHad project using DocFX.
  It builds the solution to generate XML documentation files, then uses DocFX
  to create HTML documentation. Optionally, it can serve the documentation
  locally for preview.

.PARAMETER Serve
  When specified, starts a local web server to preview the generated documentation.

.PARAMETER Port
  The port number to use when serving documentation. Defaults to 8080.
  Only used when -Serve is specified.

.EXAMPLE
  # Generate documentation
  .\Generate-Documentation.ps1

.EXAMPLE
  # Generate and serve documentation locally
  .\Generate-Documentation.ps1 -Serve

.EXAMPLE
  # Generate and serve on custom port
  .\Generate-Documentation.ps1 -Serve -Port 9000

.NOTES
  - Requires .NET SDK to be installed.
  - DocFX will be installed globally if not already present.
  - Documentation is generated in the docs\_site directory.
  - When serving, the script will attempt to use Python's http.server, falling back to Node.js http-server if Python is not available.
#>

param(
    [switch]$Serve,
    [int]$Port = 8080
)

$ErrorActionPreference = "Stop"

Write-Host "=== FunWasHad Documentation Generation ===" -ForegroundColor Cyan
Write-Host ""

# Check if DocFX is installed
Write-Host "Checking DocFX installation..." -ForegroundColor Yellow
$docfxInstalled = Get-Command docfx -ErrorAction SilentlyContinue

if (-not $docfxInstalled) {
    Write-Host "Installing DocFX tool..." -ForegroundColor Yellow
    dotnet tool install -g docfx
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to install DocFX" -ForegroundColor Red
        exit 1
    }
    Write-Host "DocFX installed successfully" -ForegroundColor Green
}

# Navigate to docs folder
$docsPath = Join-Path $PSScriptRoot "..\docs"
Push-Location $docsPath

try {
    Write-Host "Building solution to generate XML documentation..." -ForegroundColor Yellow
    Push-Location ..
    dotnet build --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed. Documentation may be incomplete." -ForegroundColor Yellow
    }
    Pop-Location

    Write-Host "Generating documentation with DocFX..." -ForegroundColor Yellow
    docfx docfx.json

    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "✅ Documentation generated successfully!" -ForegroundColor Green
        Write-Host "   Output: $docsPath\_site\index.html" -ForegroundColor Cyan

        if ($Serve) {
            Write-Host ""
            Write-Host "Starting local server on port $Port..." -ForegroundColor Yellow
            Write-Host "Open http://localhost:$Port in your browser" -ForegroundColor Cyan
            Write-Host "Press Ctrl+C to stop the server" -ForegroundColor Yellow
            Write-Host ""

            Push-Location "_site"
            python -m http.server $Port 2>$null
            if ($LASTEXITCODE -ne 0) {
                # Try with Node.js if Python fails
                npx http-server -p $Port
            }
            Pop-Location
        }
    }
    else {
        Write-Host "Documentation generation failed" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "Error: $_" -ForegroundColor Red
    exit 1
}
finally {
    Pop-Location
}

Write-Host ""
Write-Host "=== Documentation Generation Complete ===" -ForegroundColor Cyan
