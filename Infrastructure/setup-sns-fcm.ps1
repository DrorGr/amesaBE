# Setup SNS FCM Platform
$ErrorActionPreference = "Stop"

# Read the JSON file from the provided path
$jsonPath = "c:\Users\dror0\OneDrive\שולחן העבודה\amesa-oauth-c2a25fdbc092.json"

Write-Host "Reading service account JSON..." -ForegroundColor Yellow
$jsonContent = Get-Content $jsonPath -Raw -Encoding UTF8
$json = $jsonContent | ConvertFrom-Json

Write-Host "Service Account: $($json.client_email)" -ForegroundColor Green
Write-Host ""

# Try with full JSON file first (AWS SNS FCM HTTP v1 may prefer this)
Write-Host "Creating SNS platform with full JSON file..." -ForegroundColor Yellow
$tempJson = New-TemporaryFile
Set-Content -Path $tempJson -Value $jsonContent -Encoding UTF8

try {
    $result = aws sns create-platform-application `
        --name amesa-android `
        --platform GCM `
        --attributes PlatformCredential=file://$tempJson `
        --region eu-north-1 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        $response = $result | ConvertFrom-Json
        $platformArn = $response.PlatformApplicationArn
        
        Write-Host "✅ SNS platform created successfully!" -ForegroundColor Green
        Write-Host "   Platform ARN: $platformArn" -ForegroundColor Cyan
        Write-Host ""
        
        # Update secret
        Write-Host "Updating secret..." -ForegroundColor Yellow
        $tempSecret = New-TemporaryFile
        Set-Content -Path $tempSecret -Value $platformArn
        aws secretsmanager update-secret `
            --secret-id "/amesa/prod/NotificationChannels/Push/AndroidPlatformArn" `
            --secret-string file://$tempSecret `
            --region eu-north-1 | Out-Null
        Remove-Item $tempSecret
        
        Write-Host "✅ Secret updated!" -ForegroundColor Green
    } else {
        Write-Host "❌ Error: $result" -ForegroundColor Red
    }
} finally {
    Remove-Item $tempJson -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "Done!" -ForegroundColor Cyan





