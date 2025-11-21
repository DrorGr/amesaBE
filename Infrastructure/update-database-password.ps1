# PowerShell script to update database password in all appsettings.json files
# Usage: .\update-database-password.ps1

$ErrorActionPreference = "Stop"

Write-Output "=== Database Password Update Script ==="
Write-Output ""

# Get the script directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$beDir = Split-Path -Parent $scriptDir

# Prompt for new password
Write-Output "⚠️  SECURITY WARNING: This will update database passwords in appsettings.json files"
Write-Output ""
$securePassword = Read-Host "Enter the new Aurora PostgreSQL password" -AsSecureString
$newPassword = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword))

if ([string]::IsNullOrWhiteSpace($newPassword)) {
    Write-Output "❌ Error: Password cannot be empty"
    exit 1
}

Write-Output ""
Write-Output "Updating password in appsettings.json files..."
Write-Output ""

# List of services with appsettings.json files
$services = @(
    "AmesaBackend.Auth",
    "AmesaBackend.Payment",
    "AmesaBackend.Lottery",
    "AmesaBackend.Content",
    "AmesaBackend.Notification",
    "AmesaBackend.LotteryResults",
    "AmesaBackend.Analytics",
    "AmesaBackend.Admin"
)

$updatedCount = 0
$skippedCount = 0

foreach ($service in $services) {
    $appsettingsPath = Join-Path $beDir "$service\appsettings.json"
    
    if (Test-Path $appsettingsPath) {
        Write-Output "Processing: $service"
        
        try {
            # Read the file
            $content = Get-Content $appsettingsPath -Raw -Encoding UTF8
            
            # Replace CHANGE_ME with new password in connection string
            $oldPattern = 'Password=CHANGE_ME'
            $newPattern = "Password=$newPassword"
            
            if ($content -match $oldPattern) {
                $content = $content -replace [regex]::Escape($oldPattern), $newPattern
                
                # Write back to file
                [System.IO.File]::WriteAllText($appsettingsPath, $content, [System.Text.Encoding]::UTF8)
                
                Write-Output "  ✅ Updated password in $service"
                $updatedCount++
            } else {
                Write-Output "  ⚠️  No CHANGE_ME found in $service (may already be updated)"
                $skippedCount++
            }
        } catch {
            Write-Output "  ❌ Error updating $service : $_"
        }
    } else {
        Write-Output "  ⚠️  File not found: $appsettingsPath"
        $skippedCount++
    }
}

Write-Output ""
Write-Output "========================================"
Write-Output "Password Update Summary:"
Write-Output "  ✅ Updated: $updatedCount files"
Write-Output "  ⚠️  Skipped: $skippedCount files"
Write-Output ""
Write-Output "⚠️  SECURITY RECOMMENDATION:"
Write-Output "  Consider using AWS Secrets Manager instead of storing passwords in appsettings.json"
Write-Output "  See: https://docs.aws.amazon.com/secretsmanager/"
Write-Output ""

