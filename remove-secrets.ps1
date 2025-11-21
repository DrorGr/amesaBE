# Remove OAuth secrets from commit f9b4263
$ErrorActionPreference = "Stop"

Write-Host "=== Removing OAuth secrets from commit f9b4263 ===" -ForegroundColor Cyan

# Checkout the file from that commit
Write-Host "Checking out file from commit f9b4263..." -ForegroundColor Yellow
git checkout f9b4263 -- AmesaBackend/appsettings.Development.json

if (Test-Path "AmesaBackend/appsettings.Development.json") {
    Write-Host "Removing secrets from file..." -ForegroundColor Yellow
    
    # Read and modify JSON
    $content = Get-Content "AmesaBackend/appsettings.Development.json" -Raw | ConvertFrom-Json
    
    # Remove secrets
    $content.Authentication.Google.ClientId = ""
    $content.Authentication.Google.ClientSecret = ""
    
    # Save back
    $content | ConvertTo-Json -Depth 10 | Set-Content "AmesaBackend/appsettings.Development.json" -NoNewline
    
    Write-Host "✅ Secrets removed from file" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next: Use interactive rebase to edit commit f9b4263" -ForegroundColor Yellow
    Write-Host "Run: git rebase -i f9b4263^" -ForegroundColor Cyan
    Write-Host "Then change 'pick' to 'edit' for commit f9b4263" -ForegroundColor Cyan
    Write-Host "After rebase starts, run: git add AmesaBackend/appsettings.Development.json && git commit --amend --no-edit" -ForegroundColor Cyan
    Write-Host "Then: git rebase --continue" -ForegroundColor Cyan
} else {
    Write-Host "❌ File not found" -ForegroundColor Red
}

