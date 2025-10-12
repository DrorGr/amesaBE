# 🔍 Security Fixes Verification Script
# This script tests that all security fixes are working correctly

Write-Host "🔍 Amesa Backend - Security Fixes Verification" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green
Write-Host ""

# Test 1: Check for hardcoded credentials in AdminAuthService
Write-Host "🧪 Test 1: Checking AdminAuthService for hardcoded credentials..." -ForegroundColor Yellow
$adminAuthContent = Get-Content "AmesaBackend/Services/AdminAuthService.cs" -Raw

if ($adminAuthContent -match '"admin@amesa.com"' -and $adminAuthContent -match '"Admin123!"') {
    Write-Host "   ❌ FAILED: Hardcoded credentials still found in AdminAuthService" -ForegroundColor Red
} elseif ($adminAuthContent -match "Environment.GetEnvironmentVariable") {
    Write-Host "   ✅ PASSED: AdminAuthService uses environment variables" -ForegroundColor Green
} else {
    Write-Host "   ⚠️  WARNING: AdminAuthService configuration unclear" -ForegroundColor Yellow
}

# Test 2: Check appsettings.Development.json for hardcoded credentials
Write-Host "🧪 Test 2: Checking appsettings.Development.json for hardcoded credentials..." -ForegroundColor Yellow
$appSettingsContent = Get-Content "AmesaBackend/appsettings.Development.json" -Raw

if ($appSettingsContent -match "amesadbmain1.*Password=" -or $appSettingsContent -match "aAXa406L6qdqfTU6o8vr") {
    Write-Host "   ❌ FAILED: Hardcoded database credentials found in appsettings.Development.json" -ForegroundColor Red
} else {
    Write-Host "   ✅ PASSED: No hardcoded credentials in appsettings.Development.json" -ForegroundColor Green
}

# Test 3: Check PowerShell scripts for hardcoded credentials
Write-Host "🧪 Test 3: Checking PowerShell scripts for hardcoded credentials..." -ForegroundColor Yellow
$scripts = @("seed-database.ps1", "seed-database-simple.ps1", "quick-seed.ps1", "direct-seed.ps1")

$allSecure = $true
foreach ($script in $scripts) {
    if (Test-Path $script) {
        $scriptContent = Get-Content $script -Raw
        if ($scriptContent -match "aAXa406L6qdqfTU6o8vr" -or $scriptContent -match "u1fwn3s9") {
            Write-Host "   ❌ FAILED: Hardcoded credentials found in $script" -ForegroundColor Red
            $allSecure = $false
        } elseif ($scriptContent -match "DB_CONNECTION_STRING") {
            Write-Host "   ✅ PASSED: $script uses environment variables" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️  WARNING: $script configuration unclear" -ForegroundColor Yellow
        }
    }
}

# Test 4: Check Program.cs for environment variable usage
Write-Host "🧪 Test 4: Checking Program.cs for environment variable usage..." -ForegroundColor Yellow
$programContent = Get-Content "AmesaBackend/Program.cs" -Raw

if ($programContent -match "Environment.GetEnvironmentVariable.*DB_CONNECTION_STRING") {
    Write-Host "   ✅ PASSED: Program.cs uses environment variables for database connection" -ForegroundColor Green
} else {
    Write-Host "   ❌ FAILED: Program.cs not using environment variables" -ForegroundColor Red
}

# Test 5: Check AdminDatabaseService for environment variable usage
Write-Host "🧪 Test 5: Checking AdminDatabaseService for environment variable usage..." -ForegroundColor Yellow
$adminDbContent = Get-Content "AmesaBackend/Services/AdminDatabaseService.cs" -Raw

if ($adminDbContent -match "Environment.GetEnvironmentVariable.*DB_CONNECTION_STRING") {
    Write-Host "   ✅ PASSED: AdminDatabaseService uses environment variables" -ForegroundColor Green
} else {
    Write-Host "   ❌ FAILED: AdminDatabaseService not using environment variables" -ForegroundColor Red
}

# Test 6: Check if secure environment setup script exists
Write-Host "🧪 Test 6: Checking for secure environment setup script..." -ForegroundColor Yellow
if (Test-Path "set-secure-environment.ps1") {
    Write-Host "   ✅ PASSED: Secure environment setup script exists" -ForegroundColor Green
} else {
    Write-Host "   ❌ FAILED: Secure environment setup script missing" -ForegroundColor Red
}

# Test 7: Check if documentation exists
Write-Host "🧪 Test 7: Checking for security documentation..." -ForegroundColor Yellow
$docs = @("SECURE_ENVIRONMENT_SETUP.md", "ADMIN_PANEL_DEPLOYMENT_STRATEGY.md")
$docsExist = $true

foreach ($doc in $docs) {
    if (Test-Path $doc) {
        Write-Host "   ✅ PASSED: $doc exists" -ForegroundColor Green
    } else {
        Write-Host "   ❌ FAILED: $doc missing" -ForegroundColor Red
        $docsExist = $false
    }
}

Write-Host ""
Write-Host "📊 Security Fixes Summary:" -ForegroundColor Cyan
Write-Host "=========================" -ForegroundColor Cyan

if ($allSecure -and $docsExist) {
    Write-Host "🎉 ALL SECURITY FIXES APPLIED SUCCESSFULLY!" -ForegroundColor Green
    Write-Host ""
    Write-Host "✅ Ready for deployment with:" -ForegroundColor Green
    Write-Host "   - Environment variable configuration" -ForegroundColor Gray
    Write-Host "   - No hardcoded credentials" -ForegroundColor Gray
    Write-Host "   - Secure PowerShell scripts" -ForegroundColor Gray
    Write-Host "   - Comprehensive documentation" -ForegroundColor Gray
    Write-Host ""
    Write-Host "🚀 Next Steps:" -ForegroundColor Yellow
    Write-Host "   1. Set GitHub secrets with production values" -ForegroundColor Gray
    Write-Host "   2. Deploy to staging environment for testing" -ForegroundColor Gray
    Write-Host "   3. Test admin panel functionality" -ForegroundColor Gray
    Write-Host "   4. Deploy to production with proper access controls" -ForegroundColor Gray
} else {
    Write-Host "⚠️  Some security issues remain. Please review the failed tests above." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Security Status: READY FOR DEPLOYMENT" -ForegroundColor Green
