# Script to set Railway secrets for GitHub Actions
# Usage: .\scripts\Set-RailwaySecrets.ps1 -FunwashadProjectId "your-project-id"

param(
    [Parameter(Mandatory=$true)]
    [string]$FunwashadProjectId,
    
    [Parameter(Mandatory=$false)]
    [string]$FunwashadToken
)

Write-Host "🚂 Setting Railway Secrets for GitHub Actions" -ForegroundColor Cyan
Write-Host ""

# Set RAILWAY_FUNWASHAD_PROJECT_ID
Write-Host "Setting RAILWAY_FUNWASHAD_PROJECT_ID..." -ForegroundColor Yellow
gh secret set RAILWAY_FUNWASHAD_PROJECT_ID --body $FunwashadProjectId
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ RAILWAY_FUNWASHAD_PROJECT_ID set successfully" -ForegroundColor Green
} else {
    Write-Host "❌ Failed to set RAILWAY_FUNWASHAD_PROJECT_ID" -ForegroundColor Red
    exit 1
}

# Set RAILWAY_FUNWASHAD_TOKEN
if ($FunwashadToken) {
    Write-Host "Setting RAILWAY_FUNWASHAD_TOKEN..." -ForegroundColor Yellow
    gh secret set RAILWAY_FUNWASHAD_TOKEN --body $FunwashadToken
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ RAILWAY_FUNWASHAD_TOKEN set successfully" -ForegroundColor Green
    } else {
        Write-Host "❌ Failed to set RAILWAY_FUNWASHAD_TOKEN" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "⚠️  RAILWAY_FUNWASHAD_TOKEN not provided. If it's different from RAILWAY_TOKEN, set it manually:" -ForegroundColor Yellow
    Write-Host "   gh secret set RAILWAY_FUNWASHAD_TOKEN --body 'your-token'" -ForegroundColor Gray
    Write-Host ""
    Write-Host "   Or if it's the same as RAILWAY_TOKEN, you can copy it:" -ForegroundColor Yellow
    Write-Host "   # Get the token value (you'll need to retrieve it from Railway)" -ForegroundColor Gray
    Write-Host "   gh secret set RAILWAY_FUNWASHAD_TOKEN --body 'same-value-as-RAILWAY_TOKEN'" -ForegroundColor Gray
}

Write-Host ""
Write-Host "✅ Secrets setup complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Current Railway secrets:" -ForegroundColor Cyan
gh secret list | Select-String "RAILWAY"
