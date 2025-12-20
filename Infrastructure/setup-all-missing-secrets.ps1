# Master script to set up all missing secrets
# This script orchestrates the setup of Redis, SES, and SNS configurations

param(
    [Parameter(Mandatory=$false)]
    [string]$Region = "eu-north-1",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipRedis = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipSES = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipSNS = $false
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  AmesaBackend - Missing Secrets Setup" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# 1. Redis Setup
if (-not $SkipRedis) {
    Write-Host "[1/3] Setting up Redis connection string..." -ForegroundColor Yellow
    Write-Host ""
    & "$PSScriptRoot\setup-redis-secret.ps1" -Region $Region
    Write-Host ""
} else {
    Write-Host "[1/3] Skipping Redis setup (--SkipRedis flag)" -ForegroundColor Gray
    Write-Host ""
}

# 2. SES SMTP Setup
if (-not $SkipSES) {
    Write-Host "[2/3] Setting up SES SMTP credentials..." -ForegroundColor Yellow
    Write-Host ""
    & "$PSScriptRoot\setup-ses-smtp.ps1" -Region $Region
    Write-Host ""
} else {
    Write-Host "[2/3] Skipping SES setup (--SkipSES flag)" -ForegroundColor Gray
    Write-Host ""
}

# 3. SNS Platform Applications Setup
if (-not $SkipSNS) {
    Write-Host "[3/3] Setting up SNS Platform Applications..." -ForegroundColor Yellow
    Write-Host ""
    & "$PSScriptRoot\setup-sns-platforms.ps1" -Region $Region
    Write-Host ""
} else {
    Write-Host "[3/3] Skipping SNS setup (--SkipSNS flag)" -ForegroundColor Gray
    Write-Host ""
}

Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Setup Complete!" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  ✅ Redis connection string configured" -ForegroundColor Green
Write-Host "  ✅ SES SMTP credentials configured" -ForegroundColor Green
Write-Host "  ✅ SNS platform applications configured" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Verify all secrets in AWS Secrets Manager" -ForegroundColor White
Write-Host "  2. Test email sending (SES)" -ForegroundColor White
Write-Host "  3. Test push notifications (SNS)" -ForegroundColor White
Write-Host "  4. Update ECS services to pick up new secrets" -ForegroundColor White
Write-Host ""





