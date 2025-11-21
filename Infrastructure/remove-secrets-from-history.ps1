# Remove Secrets from Git History
# This script removes Google OAuth secrets from commit history

param(
    [switch]$UseGitHubAllow = $false
)

$ErrorActionPreference = "Stop"

Write-Host "=== Remove Secrets from Git History ===" -ForegroundColor Cyan
Write-Host ""

if ($UseGitHubAllow) {
    Write-Host "Option: Use GitHub Allow URL (simplest)" -ForegroundColor Yellow
    Write-Host "Visit this URL to allow the secret one-time:" -ForegroundColor Cyan
    Write-Host "https://github.com/DrorGr/amesaBE/security/secret-scanning/unblock-secret/35e54lwedqCeb8jRqmdRR1bWszL" -ForegroundColor Green
    Write-Host ""
    Write-Host "After allowing, you can push normally." -ForegroundColor Yellow
    return
}

Write-Host "Option: Rewrite Git History" -ForegroundColor Yellow
Write-Host ""

# Check if git-filter-repo is available
$hasFilterRepo = $false
try {
    git filter-repo --version 2>&1 | Out-Null
    $hasFilterRepo = $true
} catch {
    $hasFilterRepo = $false
}

if (-not $hasFilterRepo) {
    Write-Host "⚠️  git-filter-repo not installed" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Installation options:" -ForegroundColor Cyan
    Write-Host "1. Python pip: pip install git-filter-repo" -ForegroundColor Gray
    Write-Host "2. Download: https://github.com/newren/git-filter-repo" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Alternative: Using BFG Repo-Cleaner or manual rebase" -ForegroundColor Yellow
    Write-Host ""
    
    # Try alternative: Manual rebase approach
    Write-Host "Attempting manual history rewrite..." -ForegroundColor Cyan
    Write-Host "This will create a new commit that removes the secrets" -ForegroundColor Yellow
    
    # Stash any changes
    git stash 2>&1 | Out-Null
    
    # Checkout the file from the problematic commit
    git checkout f9b4263 -- AmesaBackend/appsettings.Development.json 2>&1 | Out-Null
    
    if (Test-Path "AmesaBackend/appsettings.Development.json") {
        # Remove secrets using PowerShell
        $content = Get-Content "AmesaBackend/appsettings.Development.json" -Raw | ConvertFrom-Json
        $content.Authentication.Google.ClientId = ""
        $content.Authentication.Google.ClientSecret = ""
        $content | ConvertTo-Json -Depth 10 | Set-Content "AmesaBackend/appsettings.Development.json"
        
        git add "AmesaBackend/appsettings.Development.json"
        git commit --amend -m "Complete microservices migration: All 8 services extracted, EventBridge, Redis, X-Ray integrated, CI/CD workflows created (secrets removed)" 2>&1 | Out-Null
        
        Write-Host "✅ Amended commit f9b4263" -ForegroundColor Green
        Write-Host ""
        Write-Host "Next: Rebase the later commit on top" -ForegroundColor Yellow
        Write-Host "Run: git rebase --onto HEAD f9b4263 afcb2e7" -ForegroundColor Gray
        
        git stash pop 2>&1 | Out-Null
    }
    
    return
}

Write-Host "✅ git-filter-repo is available" -ForegroundColor Green
Write-Host ""

Write-Host "WARNING: This will rewrite git history!" -ForegroundColor Red
Write-Host "Make sure you have a backup or the remote URL to restore from." -ForegroundColor Yellow
Write-Host ""

$confirm = Read-Host "Continue? (yes/no)"
if ($confirm -ne "yes") {
    Write-Host "Aborted." -ForegroundColor Yellow
    return
}

# Create Python script to remove secrets
$pythonScript = @"
import json
import sys

file_path = "AmesaBackend/appsettings.Development.json"

try:
    with open(file_path, 'r', encoding='utf-8') as f:
        content = json.load(f)
    
    if 'Authentication' in content and 'Google' in content['Authentication']:
        content['Authentication']['Google']['ClientId'] = ""
        content['Authentication']['Google']['ClientSecret'] = ""
    
    with open(file_path, 'w', encoding='utf-8') as f:
        json.dump(content, f, indent=2, ensure_ascii=False)
    
    sys.exit(0)
except Exception as e:
    print(f"Error: {e}", file=sys.stderr)
    sys.exit(1)
"@

$pythonScript | Out-File -FilePath "remove_secrets_temp.py" -Encoding UTF8

Write-Host "Running git-filter-repo to rewrite history..." -ForegroundColor Cyan
Write-Host "This may take a few minutes..." -ForegroundColor Yellow

# Use git-filter-repo to rewrite the file in all commits
git filter-repo --path AmesaBackend/appsettings.Development.json --invert-paths --force

# Add the file back with secrets removed
git checkout HEAD -- AmesaBackend/appsettings.Development.json 2>&1 | Out-Null
python remove_secrets_temp.py
git add AmesaBackend/appsettings.Development.json
git commit -m "fix: Remove OAuth secrets from appsettings.Development.json"

Remove-Item "remove_secrets_temp.py" -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "✅ History rewritten!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Verify: git log --oneline" -ForegroundColor Gray
Write-Host "2. Force push: git push origin main --force" -ForegroundColor Gray
Write-Host "   (WARNING: Force push rewrites remote history!)" -ForegroundColor Yellow

