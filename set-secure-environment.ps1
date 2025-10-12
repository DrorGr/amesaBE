# 🔐 Secure Environment Setup Script
# This script helps set up environment variables securely for the Amesa backend

Write-Host "🔐 Amesa Backend - Secure Environment Setup" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green
Write-Host ""

# Function to securely read password
function Read-SecurePassword {
    param([string]$Prompt)
    $securePassword = Read-Host -Prompt $Prompt -AsSecureString
    $BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    return [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
}

# Function to set environment variable
function Set-EnvironmentVariable {
    param([string]$Name, [string]$Value, [bool]$Secure = $false)
    
    if ($Secure) {
        Write-Host "✅ $Name set (hidden for security)" -ForegroundColor Green
    } else {
        Write-Host "✅ $Name = $Value" -ForegroundColor Green
    }
    
    [Environment]::SetEnvironmentVariable($Name, $Value, "Process")
    [Environment]::SetEnvironmentVariable($Name, $Value, "User")
}

Write-Host "This script will help you set up secure environment variables for the Amesa backend." -ForegroundColor Yellow
Write-Host ""

# Database Configuration
Write-Host "📊 Database Configuration:" -ForegroundColor Cyan
$dbHost = Read-Host -Prompt "Database Host (e.g., amesadbmain1.cruuae28ob7m.eu-north-1.rds.amazonaws.com)"
$dbName = Read-Host -Prompt "Database Name (default: amesa_lottery)" -Default "amesa_lottery"
$dbUser = Read-Host -Prompt "Database Username"
$dbPassword = Read-SecurePassword -Prompt "Database Password"
$dbPort = Read-Host -Prompt "Database Port (default: 5432)" -Default "5432"

$dbConnectionString = "Host=$dbHost;Database=$dbName;Username=$dbUser;Password=$dbPassword;Port=$dbPort;"
Set-EnvironmentVariable -Name "DB_CONNECTION_STRING" -Value $dbConnectionString -Secure $true

# Admin Panel Configuration
Write-Host ""
Write-Host "👑 Admin Panel Configuration:" -ForegroundColor Cyan
$adminEmail = Read-Host -Prompt "Admin Email (default: admin@amesa.com)" -Default "admin@amesa.com"
$adminPassword = Read-SecurePassword -Prompt "Admin Password"

Set-EnvironmentVariable -Name "ADMIN_EMAIL" -Value $adminEmail
Set-EnvironmentVariable -Name "ADMIN_PASSWORD" -Value $adminPassword -Secure $true

# JWT Configuration
Write-Host ""
Write-Host "🔑 JWT Configuration:" -ForegroundColor Cyan
$jwtSecret = Read-SecurePassword -Prompt "JWT Secret Key (min 32 characters)"
Set-EnvironmentVariable -Name "JWT_SECRET_KEY" -Value $jwtSecret -Secure $true

Write-Host ""
Write-Host "✅ Environment variables set successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "🔍 Verification:" -ForegroundColor Yellow
Write-Host "   DB_CONNECTION_STRING: $(if ($env:DB_CONNECTION_STRING) { 'SET' } else { 'NOT SET' })" -ForegroundColor Gray
Write-Host "   ADMIN_EMAIL: $(if ($env:ADMIN_EMAIL) { 'SET' } else { 'NOT SET' })" -ForegroundColor Gray
Write-Host "   ADMIN_PASSWORD: $(if ($env:ADMIN_PASSWORD) { 'SET' } else { 'NOT SET' })" -ForegroundColor Gray
Write-Host "   JWT_SECRET_KEY: $(if ($env:JWT_SECRET_KEY) { 'SET' } else { 'NOT SET' })" -ForegroundColor Gray

Write-Host ""
Write-Host "🚀 You can now run the backend application securely!" -ForegroundColor Green
Write-Host "   dotnet run --project AmesaBackend" -ForegroundColor Gray
Write-Host ""
Write-Host "⚠️  Security Notes:" -ForegroundColor Yellow
Write-Host "   - Environment variables are set for current session and user profile" -ForegroundColor Gray
Write-Host "   - For production, use GitHub Secrets or AWS Secrets Manager" -ForegroundColor Gray
Write-Host "   - Never commit these values to version control" -ForegroundColor Gray
