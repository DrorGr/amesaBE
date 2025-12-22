# Script to create SNS platform applications when you have real credentials
# This script will create Android and iOS platforms with your actual FCM/APNS credentials

param(
    [Parameter(Mandatory=$false)]
    [string]$Region = "eu-north-1",
    
    [Parameter(Mandatory=$false)]
    [string]$SecretsPrefix = "/amesa/prod",
    
    [Parameter(Mandatory=$false)]
    [string]$FcmServerKey = "",
    
    [Parameter(Mandatory=$false)]
    [string]$ApnsKeyPath = "",
    
    [Parameter(Mandatory=$false)]
    [string]$ApnsCertificatePath = "",
    
    [Parameter(Mandatory=$false)]
    [string]$ApnsPrivateKeyPath = ""
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  SNS Platform Applications Setup" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Android Platform
if ($FcmServerKey) {
    Write-Host "Creating Android (GCM/FCM) platform application..." -ForegroundColor Yellow
    
    $tempFile = New-TemporaryFile
    Set-Content -Path $tempFile -Value $FcmServerKey
    
    $result = aws sns create-platform-application `
        --name amesa-android `
        --platform GCM `
        --attributes PlatformCredential=file://$tempFile `
        --region $Region 2>&1
    
    Remove-Item $tempFile
    
    if ($LASTEXITCODE -eq 0) {
        $androidArn = ($result | ConvertFrom-Json).PlatformApplicationArn
        Write-Host "✅ Android platform created: $androidArn" -ForegroundColor Green
        
        # Update secret
        $tempSecret = New-TemporaryFile
        Set-Content -Path $tempSecret -Value $androidArn
        aws secretsmanager update-secret --secret-id "$SecretsPrefix/NotificationChannels/Push/AndroidPlatformArn" --secret-string file://$tempSecret --region $Region | Out-Null
        Remove-Item $tempSecret
        Write-Host "✅ Android platform ARN secret updated" -ForegroundColor Green
    } else {
        Write-Host "❌ Error creating Android platform: $result" -ForegroundColor Red
    }
} else {
    Write-Host "⚠️  Skipping Android platform (no FCM Server Key provided)" -ForegroundColor Yellow
    Write-Host "   To create later, provide --FcmServerKey parameter" -ForegroundColor Gray
}

Write-Host ""

# iOS Platform
if ($ApnsKeyPath -or ($ApnsCertificatePath -and $ApnsPrivateKeyPath)) {
    Write-Host "Creating iOS (APNS) platform application..." -ForegroundColor Yellow
    
    if ($ApnsKeyPath) {
        # Using .p8 key file
        $result = aws sns create-platform-application `
            --name amesa-ios `
            --platform APNS `
            --attributes PlatformCredential=file://$ApnsKeyPath `
            --region $Region 2>&1
    } elseif ($ApnsCertificatePath -and $ApnsPrivateKeyPath) {
        # Using .p12 certificate (requires both certificate and private key)
        $result = aws sns create-platform-application `
            --name amesa-ios `
            --platform APNS `
            --attributes PlatformCredential=file://$ApnsCertificatePath,PlatformPrincipal=file://$ApnsPrivateKeyPath `
            --region $Region 2>&1
    }
    
    if ($LASTEXITCODE -eq 0) {
        $iosArn = ($result | ConvertFrom-Json).PlatformApplicationArn
        Write-Host "✅ iOS platform created: $iosArn" -ForegroundColor Green
        
        # Update secret
        $tempSecret = New-TemporaryFile
        Set-Content -Path $tempSecret -Value $iosArn
        aws secretsmanager update-secret --secret-id "$SecretsPrefix/NotificationChannels/Push/iOSPlatformArn" --secret-string file://$tempSecret --region $Region | Out-Null
        Remove-Item $tempSecret
        Write-Host "✅ iOS platform ARN secret updated" -ForegroundColor Green
    } else {
        Write-Host "❌ Error creating iOS platform: $result" -ForegroundColor Red
    }
} else {
    Write-Host "⚠️  Skipping iOS platform (no APNS credentials provided)" -ForegroundColor Yellow
    Write-Host "   To create later, provide --ApnsKeyPath or --ApnsCertificatePath" -ForegroundColor Gray
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Setup Complete!" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""







