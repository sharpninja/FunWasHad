# Generate-Documentation.ps1
# Generates project documentation using DocFX

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
