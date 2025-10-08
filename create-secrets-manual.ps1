# Manual Secrets Creation Script
# Run this after installing and configuring AWS CLI

param(
    [string]$AWSRegion = "eu-north-1",
    [string]$DBPassword = "aAXa406L6qdqfTU6o8vr"
)

Write-Host "Creating AWS Secrets Manager secrets for Amesa Backend..." -ForegroundColor Blue

# Check if AWS CLI is available
try {
    $accountId = "129394705401"  # Your actual AWS account ID
    Write-Host "AWS Account ID: $accountId" -ForegroundColor Green
}
catch {
    Write-Host "Error: AWS CLI is not configured or not available. Please install and configure AWS CLI first." -ForegroundColor Red
    Write-Host "Run: aws configure" -ForegroundColor Yellow
    exit 1
}

# Function to create secret
function Create-Secret {
    param(
        [string]$Name,
        [string]$Value,
        [string]$Description
    )
    
    Write-Host "Creating secret: $Name" -ForegroundColor Blue
    
    try {
        # Check if secret exists
        aws secretsmanager describe-secret --secret-id "amesa/$Name" --region $AWSRegion 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Secret $Name already exists. Updating..." -ForegroundColor Yellow
            aws secretsmanager update-secret --secret-id "amesa/$Name" --secret-string $Value --region $AWSRegion
        } else {
            aws secretsmanager create-secret --name "amesa/$Name" --description $Description --secret-string $Value --region $AWSRegion
        }
        Write-Host "✓ Secret $Name created/updated successfully" -ForegroundColor Green
    }
    catch {
        Write-Host "✗ Failed to create secret $Name`: $_" -ForegroundColor Red
    }
}

# Generate random strings for secrets
function Get-RandomString {
    param([int]$Length = 32)
    -join ((1..$Length) | ForEach {[char]((65..90) + (97..122) + (48..57) | Get-Random)})
}

$JWT_SECRET = Get-RandomString -Length 64
$QR_SECRET = Get-RandomString -Length 64

Write-Host "`nCreating secrets..." -ForegroundColor Blue

# Create all required secrets
Create-Secret -Name "db-password" -Value $DBPassword -Description "Aurora PostgreSQL database password"
Create-Secret -Name "jwt-secret" -Value $JWT_SECRET -Description "JWT signing secret key"
Create-Secret -Name "qr-code-secret" -Value $QR_SECRET -Description "QR code generation secret key"

# Create placeholder secrets (you can update these later with actual values)
Create-Secret -Name "smtp-username" -Value "your-smtp-username" -Description "SMTP server username"
Create-Secret -Name "smtp-password" -Value "your-smtp-password" -Description "SMTP server password"
Create-Secret -Name "stripe-publishable-key" -Value "pk_test_your_stripe_publishable_key" -Description "Stripe publishable key"
Create-Secret -Name "stripe-secret-key" -Value "sk_test_your_stripe_secret_key" -Description "Stripe secret key"
Create-Secret -Name "stripe-webhook-secret" -Value "whsec_your_webhook_secret" -Description "Stripe webhook secret"
Create-Secret -Name "paypal-client-id" -Value "your_paypal_client_id" -Description "PayPal client ID"
Create-Secret -Name "paypal-client-secret" -Value "your_paypal_client_secret" -Description "PayPal client secret"
Create-Secret -Name "aws-access-key-id" -Value "your_aws_access_key_id" -Description "AWS access key ID"
Create-Secret -Name "aws-secret-access-key" -Value "your_aws_secret_access_key" -Description "AWS secret access key"

Write-Host "`n✓ All secrets have been created successfully!" -ForegroundColor Green
Write-Host "You can now run the deployment script: .\deploy-with-existing-aurora.ps1" -ForegroundColor Blue
Write-Host "`nNote: You may want to update the placeholder secrets with your actual values later." -ForegroundColor Yellow

# Save secrets info for reference
$secretsInfo = @"
# Amesa Backend Secrets Information
# Generated on: $(Get-Date)

Database Password: $DBPassword
JWT Secret: $JWT_SECRET
QR Code Secret: $QR_SECRET

# Placeholder secrets (update these with your actual values):
# SMTP Username: your-smtp-username
# SMTP Password: your-smtp-password
# Stripe Publishable Key: pk_test_your_stripe_publishable_key
# Stripe Secret Key: sk_test_your_stripe_secret_key
# Stripe Webhook Secret: whsec_your_webhook_secret
# PayPal Client ID: your_paypal_client_id
# PayPal Client Secret: your_paypal_client_secret
# AWS Access Key ID: your_aws_access_key_id
# AWS Secret Access Key: your_aws_secret_access_key
"@

$secretsInfo | Out-File -FilePath "secrets-info.txt" -Encoding UTF8
Write-Host "`nSecrets information saved to secrets-info.txt" -ForegroundColor Blue
