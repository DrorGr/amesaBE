# PowerShell script to create SNS platform applications for push notifications
# This script creates Android (GCM/FCM) and iOS (APNS) platform applications

param(
    [Parameter(Mandatory=$false)]
    [string]$Region = "eu-north-1",
    
    [Parameter(Mandatory=$false)]
    [string]$SecretsPrefix = "/amesa/prod",
    
    [Parameter(Mandatory=$false)]
    [string]$AndroidAppName = "amesa-android",
    
    [Parameter(Mandatory=$false)]
    [string]$IOSAppName = "amesa-ios"
)

$ErrorActionPreference = "Stop"

Write-Host "Setting up SNS Platform Applications for Push Notifications..." -ForegroundColor Cyan
Write-Host ""

# Function to create platform application
function Create-PlatformApplication {
    param(
        [string]$Name,
        [string]$Platform,
        [string]$Credential,
        [string]$SecretName
    )
    
    Write-Host "Creating $Platform platform application: $Name..." -ForegroundColor Yellow
    
    # Check if platform app already exists
    $existingApps = aws sns list-platform-applications --region $Region --query "PlatformApplications[?PlatformApplicationArn =~ `".*$Name`"].PlatformApplicationArn" --output json | ConvertFrom-Json
    
    if ($existingApps.Count -gt 0) {
        Write-Host "✅ Platform application already exists: $($existingApps[0])" -ForegroundColor Green
        return $existingApps[0]
    }
    
    # Create platform application
    $tempCredFile = New-TemporaryFile
    Set-Content -Path $tempCredFile -Value $Credential
    
    $result = aws sns create-platform-application `
        --name $Name `
        --platform $Platform `
        --attributes PlatformCredential=file://$tempCredFile `
        --region $Region | ConvertFrom-Json
    
    Remove-Item $tempCredFile
    
    Write-Host "✅ Platform application created: $($result.PlatformApplicationArn)" -ForegroundColor Green
    
    # Update secret
    $tempFile = New-TemporaryFile
    Set-Content -Path $tempFile -Value $result.PlatformApplicationArn
    aws secretsmanager update-secret --secret-id $SecretName --secret-string file://$tempFile --region $Region | Out-Null
    Remove-Item $tempFile
    Write-Host "✅ Secret updated: $SecretName" -ForegroundColor Green
    
    return $result.PlatformApplicationArn
}

# Android (GCM/FCM)
Write-Host "=== Android Platform (GCM/FCM) ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "To create Android platform application, you need:" -ForegroundColor Yellow
Write-Host "1. Firebase project with FCM (Firebase Cloud Messaging) enabled" -ForegroundColor White
Write-Host "2. FCM Server Key from Firebase Console" -ForegroundColor White
Write-Host "   (Firebase Console → Project Settings → Cloud Messaging → Server key)" -ForegroundColor Gray
Write-Host ""

$createAndroid = Read-Host "Do you want to create Android platform application now? (y/n)"

if ($createAndroid -eq "y") {
    $fcmKey = Read-Host "Enter FCM Server Key" -AsSecureString
    $fcmKeyPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($fcmKey))
    
    $androidArn = Create-PlatformApplication `
        -Name $AndroidAppName `
        -Platform "GCM" `
        -Credential $fcmKeyPlain `
        -SecretName "$SecretsPrefix/NotificationChannels/Push/AndroidPlatformArn"
    
    Write-Host ""
} else {
    Write-Host "Skipping Android platform. You can create it later via AWS Console or run this script again." -ForegroundColor Yellow
    Write-Host ""
}

# iOS (APNS)
Write-Host "=== iOS Platform (APNS) ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "To create iOS platform application, you need:" -ForegroundColor Yellow
Write-Host "1. Apple Developer account" -ForegroundColor White
Write-Host "2. APNs Auth Key (.p8 file) or APNs Certificate (.p12 file)" -ForegroundColor White
Write-Host "   (Apple Developer Portal → Certificates, Identifiers & Profiles)" -ForegroundColor Gray
Write-Host ""
Write-Host "Options:" -ForegroundColor Cyan
Write-Host "  - APNS_SANDBOX: For development/testing" -ForegroundColor White
Write-Host "  - APNS: For production" -ForegroundColor White
Write-Host ""

$createIOS = Read-Host "Do you want to create iOS platform application now? (y/n)"

if ($createIOS -eq "y") {
    $iosPlatform = Read-Host "Platform type (APNS_SANDBOX or APNS) [APNS_SANDBOX]"
    if ([string]::IsNullOrWhiteSpace($iosPlatform)) {
        $iosPlatform = "APNS_SANDBOX"
    }
    
    Write-Host ""
    Write-Host "For APNS, you need to provide:" -ForegroundColor Yellow
    Write-Host "  - PlatformCredential: Content of .p8 key file OR .p12 certificate" -ForegroundColor White
    Write-Host "  - PlatformPrincipal: (Optional) For .p12 certificates" -ForegroundColor White
    Write-Host ""
    
    $apnsCredential = Read-Host "Enter APNS credential (key/certificate content)" -AsSecureString
    $apnsCredentialPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($apnsCredential))
    
    # For APNS, we might need PlatformPrincipal too
    $usePrincipal = Read-Host "Do you have PlatformPrincipal (for .p12 certificates)? (y/n)"
    $apnsPrincipal = $null
    if ($usePrincipal -eq "y") {
        $apnsPrincipalSecure = Read-Host "Enter PlatformPrincipal" -AsSecureString
        $apnsPrincipal = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($apnsPrincipalSecure))
    }
    
    # Create platform application with credential
    Write-Host "Creating iOS platform application..." -ForegroundColor Yellow
    
    $tempCredFile = New-TemporaryFile
    Set-Content -Path $tempCredFile -Value $apnsCredentialPlain
    
    $createParams = @(
        "sns", "create-platform-application",
        "--name", $IOSAppName,
        "--platform", $iosPlatform,
        "--attributes", "PlatformCredential=file://$tempCredFile",
        "--region", $Region
    )
    
    if ($apnsPrincipal) {
        $tempPrincFile = New-TemporaryFile
        Set-Content -Path $tempPrincFile -Value $apnsPrincipal
        $createParams += "PlatformPrincipal=file://$tempPrincFile"
    }
    
    $result = & aws $createParams | ConvertFrom-Json
    
    Remove-Item $tempCredFile
    if ($apnsPrincipal) { Remove-Item $tempPrincFile }
    
    Write-Host "✅ Platform application created: $($result.PlatformApplicationArn)" -ForegroundColor Green
    
    # Update secret
    $tempFile = New-TemporaryFile
    Set-Content -Path $tempFile -Value $result.PlatformApplicationArn
    aws secretsmanager update-secret --secret-id "$SecretsPrefix/NotificationChannels/Push/iOSPlatformArn" --secret-string file://$tempFile --region $Region | Out-Null
    Remove-Item $tempFile
    Write-Host "✅ Secret updated: $SecretsPrefix/NotificationChannels/Push/iOSPlatformArn" -ForegroundColor Green
} else {
    Write-Host "Skipping iOS platform. You can create it later via AWS Console or run this script again." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "✅ SNS Platform Applications setup complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Test push notifications from your mobile apps" -ForegroundColor White
Write-Host "2. Register devices via /api/v1/devices/register endpoint" -ForegroundColor White
Write-Host "3. Send test notifications via Notification Service" -ForegroundColor White








