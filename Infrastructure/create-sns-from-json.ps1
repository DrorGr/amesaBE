# Create SNS platform from service account JSON
param(
    [Parameter(Mandatory=$true)]
    [string]$JsonFilePath
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "Creating SNS Platform Application with FCM HTTP v1..." -ForegroundColor Cyan
Write-Host ""

# Read JSON file
if (-not (Test-Path $JsonFilePath)) {
    Write-Host "Error: File not found: $JsonFilePath" -ForegroundColor Red
    exit 1
}

Write-Host "Reading service account JSON..." -ForegroundColor Yellow
$jsonContent = Get-Content $JsonFilePath -Raw -Encoding UTF8
$json = $jsonContent | ConvertFrom-Json

Write-Host "Service Account: $($json.client_email)" -ForegroundColor Green
Write-Host "Project ID: $($json.project_id)" -ForegroundColor Green
Write-Host ""

# Extract private key
$privateKey = $json.private_key

if ([string]::IsNullOrWhiteSpace($privateKey)) {
    Write-Host "Error: private_key not found in JSON" -ForegroundColor Red
    exit 1
}

# Create temporary file with private key
$tempKeyFile = New-TemporaryFile
# Replace \n with actual newlines
$privateKeyFormatted = $privateKey -replace '\\n', "`n"
Set-Content -Path $tempKeyFile -Value $privateKeyFormatted -NoNewline

try {
    Write-Host "Creating SNS platform application..." -ForegroundColor Yellow
    $result = aws sns create-platform-application `
        --name amesa-android `
        --platform GCM `
        --attributes PlatformCredential=file://$tempKeyFile `
        --region eu-north-1 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        $response = $result | ConvertFrom-Json
        $platformArn = $response.PlatformApplicationArn
        
        Write-Host "✅ SNS platform application created successfully!" -ForegroundColor Green
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
        
        Write-Host "✅ Secret updated: /amesa/prod/NotificationChannels/Push/AndroidPlatformArn" -ForegroundColor Green
        Write-Host ""
        Write-Host "Platform configured to use FCM HTTP v1 API!" -ForegroundColor Green
    } else {
        Write-Host "❌ Error creating platform application:" -ForegroundColor Red
        Write-Host $result -ForegroundColor Red
        Write-Host ""
        Write-Host "Note: AWS SNS FCM HTTP v1 may require the full JSON file instead of just the private key." -ForegroundColor Yellow
        Write-Host "Trying with full JSON file..." -ForegroundColor Yellow
        
        # Try with full JSON file
        $result2 = aws sns create-platform-application `
            --name amesa-android `
            --platform GCM `
            --attributes PlatformCredential=file://$JsonFilePath `
            --region eu-north-1 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            $response2 = $result2 | ConvertFrom-Json
            $platformArn2 = $response2.PlatformApplicationArn
            
            Write-Host "✅ Success with full JSON file!" -ForegroundColor Green
            Write-Host "   Platform ARN: $platformArn2" -ForegroundColor Cyan
            Write-Host ""
            
            # Update secret
            $tempSecret2 = New-TemporaryFile
            Set-Content -Path $tempSecret2 -Value $platformArn2
            aws secretsmanager update-secret `
                --secret-id "/amesa/prod/NotificationChannels/Push/AndroidPlatformArn" `
                --secret-string file://$tempSecret2 `
                --region eu-north-1 | Out-Null
            Remove-Item $tempSecret2
            
            Write-Host "✅ Secret updated!" -ForegroundColor Green
        } else {
            Write-Host "❌ Error with full JSON file:" -ForegroundColor Red
            Write-Host $result2 -ForegroundColor Red
        }
    }
} finally {
    Remove-Item $tempKeyFile -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "Done!" -ForegroundColor Cyan
Write-Host ""








