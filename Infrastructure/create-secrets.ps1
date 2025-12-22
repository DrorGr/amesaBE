# PowerShell script to create required AWS Secrets Manager secrets for lottery component fixes
# Run this script after implementing the code changes
# Requires: AWS CLI configured with appropriate credentials

$ErrorActionPreference = "Stop"
$REGION = "eu-north-1"
$SECRETS_PREFIX = "/amesa/prod"

Write-Host "Creating AWS Secrets Manager secrets for lottery component fixes..." -ForegroundColor Cyan
Write-Host "Region: $REGION" -ForegroundColor Yellow
Write-Host ""

# Function to create or update secret
function Create-Or-Update-Secret {
    param(
        [string]$SecretName,
        [string]$Description,
        [string]$SecretValue,
        [hashtable]$Tags = @{}
    )
    
    $fullSecretName = "$SECRETS_PREFIX/$SecretName"
    
    try {
        # Check if secret exists
        $existing = aws secretsmanager describe-secret --secret-id $fullSecretName --region $REGION 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  Updating existing secret: $fullSecretName" -ForegroundColor Yellow
            aws secretsmanager update-secret `
                --secret-id $fullSecretName `
                --secret-string $SecretValue `
                --region $REGION | Out-Null
        } else {
            Write-Host "  Creating new secret: $fullSecretName" -ForegroundColor Green
            $tagArgs = @()
            foreach ($key in $Tags.Keys) {
                $tagArgs += "Key=$key,Value=$($Tags[$key])"
            }
            $tagString = if ($tagArgs.Count -gt 0) { "--tags $($tagArgs -join ' ')" } else { "" }
            
            $cmd = "aws secretsmanager create-secret --name '$fullSecretName' --description '$Description' --secret-string '$SecretValue' --region $REGION"
            if ($tagString) {
                $cmd += " $tagString"
            }
            Invoke-Expression $cmd | Out-Null
        }
    } catch {
        Write-Host "  Error with secret $fullSecretName : $_" -ForegroundColor Red
    }
}

# 1. Service-to-Service Authentication API Key
Write-Host "1. Creating/Updating ServiceAuth/ApiKey..." -ForegroundColor Cyan
$apiKey = -join ((48..57) + (65..90) + (97..122) | Get-Random -Count 64 | ForEach-Object {[char]$_})
Create-Or-Update-Secret `
    -SecretName "ServiceAuth/ApiKey" `
    -Description "API key for service-to-service authentication between microservices" `
    -SecretValue $apiKey `
    -Tags @{Service="Shared"; Purpose="ServiceAuth"}

# 2. Service-to-Service IP Whitelist
Write-Host "2. Creating/Updating ServiceAuth/IpWhitelist..." -ForegroundColor Cyan
$ipWhitelist = '["10.0.0.0/16", "*"]'  # Update with actual VPC CIDR
Create-Or-Update-Secret `
    -SecretName "ServiceAuth/IpWhitelist" `
    -Description "IP whitelist for service-to-service authentication (JSON array)" `
    -SecretValue $ipWhitelist

# 3. Connection Strings
Write-Host "3. Creating/Updating connection string secrets..." -ForegroundColor Cyan
$services = @("Lottery", "Payment", "Notification")
foreach ($service in $services) {
    Create-Or-Update-Secret `
        -SecretName "ConnectionStrings/$service" `
        -Description "PostgreSQL connection string for $service service" `
        -SecretValue "Host=UPDATE_THIS;Port=5432;Database=amesa_$($service.ToLower())_db;Username=postgres;Password=UPDATE_THIS;SearchPath=amesa_$($service.ToLower());"
}

# 4. Redis Connection String
Write-Host "4. Creating/Updating Redis connection string..." -ForegroundColor Cyan
Create-Or-Update-Secret `
    -SecretName "ConnectionStrings/Redis" `
    -Description "Redis connection string (shared across services)" `
    -SecretValue "UPDATE_THIS_WITH_ACTUAL_REDIS_ENDPOINT"

# 5. Service URLs
Write-Host "5. Creating/Updating service URL secrets..." -ForegroundColor Cyan
$serviceUrls = @{
    "AuthService" = "http://auth-service.amesa.local:8080"
    "LotteryService" = "http://lottery-service.amesa.local:8080"
    "PaymentService" = "http://payment-service.amesa.local:8080"
}

foreach ($service in $serviceUrls.Keys) {
    Create-Or-Update-Secret `
        -SecretName "Services/$service/Url" `
        -Description "Internal URL for $service" `
        -SecretValue $serviceUrls[$service]
}

# 6. Email Settings
Write-Host "6. Creating/Updating email settings secrets..." -ForegroundColor Cyan
$emailSettings = @{
    "SmtpHost" = "email-smtp.eu-north-1.amazonaws.com"
    "SmtpPort" = "587"
    "SmtpUsername" = "UPDATE_WITH_SES_SMTP_USERNAME"
    "SmtpPassword" = "UPDATE_WITH_SES_SMTP_PASSWORD"
}

foreach ($setting in $emailSettings.Keys) {
    Create-Or-Update-Secret `
        -SecretName "EmailSettings/$setting" `
        -Description "Email setting: $setting" `
        -SecretValue $emailSettings[$setting]
}

# 7. Push Notification Platform ARNs
Write-Host "7. Creating/Updating push notification platform ARN secrets..." -ForegroundColor Cyan
$platforms = @("Android", "iOS")
foreach ($platform in $platforms) {
    Create-Or-Update-Secret `
        -SecretName "NotificationChannels/Push/${platform}PlatformArn" `
        -Description "SNS Platform ARN for $platform push notifications" `
        -SecretValue "UPDATE_WITH_ACTUAL_${platform}_PLATFORM_ARN"
}

Write-Host ""
Write-Host "✅ Secret creation/update complete!" -ForegroundColor Green
Write-Host ""
Write-Host "⚠️  IMPORTANT: Update the placeholder values with actual values:" -ForegroundColor Yellow
Write-Host "   - Connection strings with actual database endpoints" -ForegroundColor Yellow
Write-Host "   - Redis connection string" -ForegroundColor Yellow
Write-Host "   - Email SMTP credentials" -ForegroundColor Yellow
Write-Host "   - Push notification platform ARNs" -ForegroundColor Yellow
Write-Host ""
Write-Host "To update a secret manually:" -ForegroundColor Cyan
Write-Host "  aws secretsmanager update-secret --secret-id <secret-name> --secret-string '<new-value>' --region $REGION" -ForegroundColor Gray







