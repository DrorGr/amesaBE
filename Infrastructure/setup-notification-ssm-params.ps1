# PowerShell script to create missing SSM parameters for notification service
# This script creates all required SSM parameters for amesa-notification-service

param(
    [Parameter(Mandatory=$false)]
    [string]$Region = "eu-north-1",
    
    [Parameter(Mandatory=$false)]
    [string]$AlbDns = "amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com",
    
    [Parameter(Mandatory=$false)]
    [string]$SmtpHost = "email-smtp.eu-north-1.amazonaws.com",
    
    [Parameter(Mandatory=$false)]
    [int]$SmtpPort = 587,
    
    [Parameter(Mandatory=$false)]
    [string]$SmtpUsername = "",
    
    [Parameter(Mandatory=$false)]
    [string]$SmtpPassword = "",
    
    [Parameter(Mandatory=$false)]
    [string]$AndroidPlatformArn = ""
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Setting up SSM Parameters for Notification Service" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "https://$AlbDns"

# 1. Service URLs
Write-Host "[1/7] Creating service URL parameters..." -ForegroundColor Yellow

$authServiceUrl = "$baseUrl"
$lotteryServiceUrl = "$baseUrl"

aws ssm put-parameter `
    --name "/amesa/prod/Services/AuthService/Url" `
    --value "$authServiceUrl" `
    --type "String" `
    --region $Region `
    --overwrite | Out-Null
Write-Host "  ✅ Created: /amesa/prod/Services/AuthService/Url = $authServiceUrl" -ForegroundColor Green

aws ssm put-parameter `
    --name "/amesa/prod/Services/LotteryService/Url" `
    --value "$lotteryServiceUrl" `
    --type "String" `
    --region $Region `
    --overwrite | Out-Null
Write-Host "  ✅ Created: /amesa/prod/Services/LotteryService/Url = $lotteryServiceUrl" -ForegroundColor Green

# 2. SMTP Settings
Write-Host ""
Write-Host "[2/7] Creating SMTP host parameter..." -ForegroundColor Yellow
aws ssm put-parameter `
    --name "/amesa/prod/EmailSettings/SmtpHost" `
    --value "$SmtpHost" `
    --type "String" `
    --region $Region `
    --overwrite | Out-Null
Write-Host "  ✅ Created: /amesa/prod/EmailSettings/SmtpHost = $SmtpHost" -ForegroundColor Green

Write-Host ""
Write-Host "[3/7] Creating SMTP port parameter..." -ForegroundColor Yellow
aws ssm put-parameter `
    --name "/amesa/prod/EmailSettings/SmtpPort" `
    --value "$SmtpPort" `
    --type "String" `
    --region $Region `
    --overwrite | Out-Null
Write-Host "  ✅ Created: /amesa/prod/EmailSettings/SmtpPort = $SmtpPort" -ForegroundColor Green

# 3. SMTP Credentials (if provided)
if ($SmtpUsername -ne "") {
    Write-Host ""
    Write-Host "[4/7] Creating SMTP username parameter..." -ForegroundColor Yellow
    aws ssm put-parameter `
        --name "/amesa/prod/EmailSettings/SmtpUsername" `
        --value $SmtpUsername `
        --type "SecureString" `
        --region $Region `
        --overwrite | Out-Null
    Write-Host "  ✅ Created: /amesa/prod/EmailSettings/SmtpUsername" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "[4/7] ⚠️  SMTP username not provided. Skipping..." -ForegroundColor Yellow
    Write-Host "      Run setup-ses-smtp.ps1 first or provide -SmtpUsername parameter" -ForegroundColor Gray
}

if ($SmtpPassword -ne "") {
    Write-Host ""
    Write-Host "[5/7] Creating SMTP password parameter..." -ForegroundColor Yellow
    aws ssm put-parameter `
        --name "/amesa/prod/EmailSettings/SmtpPassword" `
        --value $SmtpPassword `
        --type "SecureString" `
        --region $Region `
        --overwrite | Out-Null
    Write-Host "  ✅ Created: /amesa/prod/EmailSettings/SmtpPassword" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "[5/7] ⚠️  SMTP password not provided. Skipping..." -ForegroundColor Yellow
    Write-Host "      Run setup-ses-smtp.ps1 first or provide -SmtpPassword parameter" -ForegroundColor Gray
}

# 4. SNS Platform ARN
if ($AndroidPlatformArn -ne "") {
    Write-Host ""
    Write-Host "[6/7] Creating Android platform ARN parameter..." -ForegroundColor Yellow
    aws ssm put-parameter `
        --name "/amesa/prod/NotificationChannels/Push/AndroidPlatformArn" `
        --value $AndroidPlatformArn `
        --type "String" `
        --region $Region `
        --overwrite | Out-Null
    Write-Host "  ✅ Created: /amesa/prod/NotificationChannels/Push/AndroidPlatformArn = $AndroidPlatformArn" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "[6/7] ⚠️  Android Platform ARN not provided. Checking for existing..." -ForegroundColor Yellow
    
    # Try to find existing platform application
    $platformApps = aws sns list-platform-applications --region $Region --query 'PlatformApplications[?contains(PlatformApplicationArn, `Android`)].PlatformApplicationArn' --output json | ConvertFrom-Json
    
    if ($platformApps.Count -gt 0) {
        $arn = $platformApps[0]
        aws ssm put-parameter `
            --name "/amesa/prod/NotificationChannels/Push/AndroidPlatformArn" `
            --value $arn `
            --type "String" `
            --region $Region `
            --overwrite | Out-Null
        Write-Host "  ✅ Created: /amesa/prod/NotificationChannels/Push/AndroidPlatformArn = $arn" -ForegroundColor Green
    } else {
        Write-Host "  ⚠️  No Android platform application found. Skipping..." -ForegroundColor Yellow
        Write-Host "      Run setup-sns-platforms.ps1 first or provide -AndroidPlatformArn parameter" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Setup Complete!" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Created SSM Parameters:" -ForegroundColor Cyan
Write-Host "  ✅ /amesa/prod/Services/AuthService/Url" -ForegroundColor Green
Write-Host "  ✅ /amesa/prod/Services/LotteryService/Url" -ForegroundColor Green
Write-Host "  ✅ /amesa/prod/EmailSettings/SmtpHost" -ForegroundColor Green
Write-Host "  ✅ /amesa/prod/EmailSettings/SmtpPort" -ForegroundColor Green
if ($SmtpUsername -ne "") {
    Write-Host "  ✅ /amesa/prod/EmailSettings/SmtpUsername" -ForegroundColor Green
}
if ($SmtpPassword -ne "") {
    Write-Host "  ✅ /amesa/prod/EmailSettings/SmtpPassword" -ForegroundColor Green
}
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. If SMTP credentials are missing, run: .\setup-ses-smtp.ps1" -ForegroundColor White
Write-Host "  2. If Android Platform ARN is missing, run: .\setup-sns-platforms.ps1" -ForegroundColor White
Write-Host "  3. ECS service should automatically pick up the new parameters" -ForegroundColor White
Write-Host ""
