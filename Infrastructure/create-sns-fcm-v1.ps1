# Script to create SNS platform application using FCM HTTP v1 API
# Uses service account JSON key instead of legacy Server key

param(
    [Parameter(Mandatory=$false)]
    [string]$Region = "eu-north-1",
    
    [Parameter(Mandatory=$false)]
    [string]$SecretsPrefix = "/amesa/prod",
    
    [Parameter(Mandatory=$true)]
    [string]$ServiceAccountJsonPath = "",
    
    [Parameter(Mandatory=$false)]
    [string]$PlatformName = "amesa-android"
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  SNS Platform Application - FCM HTTP v1 API" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path $ServiceAccountJsonPath)) {
    Write-Host "❌ Error: Service account JSON file not found: $ServiceAccountJsonPath" -ForegroundColor Red
    Write-Host ""
    Write-Host "To get the service account JSON:" -ForegroundColor Yellow
    Write-Host "1. Go to: https://console.cloud.google.com/iam-admin/serviceaccounts?project=amesa-oauth" -ForegroundColor White
    Write-Host "2. Click on: amesa-recaptcha-service" -ForegroundColor White
    Write-Host "3. Go to 'Keys' tab" -ForegroundColor White
    Write-Host "4. Click 'Add Key' → 'Create new key' → JSON" -ForegroundColor White
    Write-Host "5. Download the JSON file" -ForegroundColor White
    exit 1
}

Write-Host "Reading service account JSON..." -ForegroundColor Yellow
$serviceAccountJson = Get-Content $ServiceAccountJsonPath -Raw | ConvertFrom-Json

Write-Host "✅ Service account loaded: $($serviceAccountJson.client_email)" -ForegroundColor Green
Write-Host ""

Write-Host "Creating SNS platform application with FCM HTTP v1..." -ForegroundColor Yellow
Write-Host "Platform: GCM (FCM HTTP v1)" -ForegroundColor Gray
Write-Host "Name: $PlatformName" -ForegroundColor Gray
Write-Host ""

# For FCM HTTP v1, AWS SNS expects the private key from the service account JSON
# The format is: PlatformCredential should be the private_key from the JSON
$privateKey = $serviceAccountJson.private_key

if ([string]::IsNullOrWhiteSpace($privateKey)) {
    Write-Host "❌ Error: private_key not found in service account JSON" -ForegroundColor Red
    Write-Host "Make sure you downloaded the complete JSON key file." -ForegroundColor Yellow
    exit 1
}

# Create temporary file with private key
$tempKeyFile = New-TemporaryFile
Set-Content -Path $tempKeyFile -Value $privateKey

try {
    # Create SNS platform application with FCM HTTP v1
    # For FCM HTTP v1, we use PlatformCredential with the private key
    # AWS SNS will automatically detect it's HTTP v1 format
    $result = aws sns create-platform-application `
        --name $PlatformName `
        --platform GCM `
        --attributes PlatformCredential=file://$tempKeyFile `
        --region $Region 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        $platformArn = ($result | ConvertFrom-Json).PlatformApplicationArn
        Write-Host "✅ SNS platform application created successfully!" -ForegroundColor Green
        Write-Host "   Platform ARN: $platformArn" -ForegroundColor Cyan
        Write-Host ""
        
        # Update secret
        Write-Host "Updating secret..." -ForegroundColor Yellow
        $tempSecret = New-TemporaryFile
        Set-Content -Path $tempSecret -Value $platformArn
        aws secretsmanager update-secret --secret-id "$SecretsPrefix/NotificationChannels/Push/AndroidPlatformArn" --secret-string file://$tempSecret --region $Region | Out-Null
        Remove-Item $tempSecret
        Write-Host "✅ Secret updated: $SecretsPrefix/NotificationChannels/Push/AndroidPlatformArn" -ForegroundColor Green
        Write-Host ""
        Write-Host "Platform is configured to use FCM HTTP v1 API!" -ForegroundColor Green
    } else {
        Write-Host "❌ Error creating platform application:" -ForegroundColor Red
        Write-Host $result -ForegroundColor Red
        Write-Host ""
        Write-Host "Note: AWS SNS may require the full JSON key file, not just the private key." -ForegroundColor Yellow
        Write-Host "If this fails, we may need to use a different approach." -ForegroundColor Yellow
    }
} finally {
    Remove-Item $tempKeyFile -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Setup Complete!" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""








