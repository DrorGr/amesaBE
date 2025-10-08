# Quick Secrets Setup Script for Amesa Backend
# This script sets up AWS Secrets Manager with your actual database password

param(
    [string]$AWSRegion = "eu-north-1",
    [string]$DBPassword = "aAXa406L6qdqfTU6o8vr"
)

# Function to print colored output
function Write-Status {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Blue
}

function Write-Success {
    param([string]$Message)
    Write-Host "[SUCCESS] $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

# Function to create a secret
function New-Secret {
    param(
        [string]$SecretName,
        [string]$SecretValue,
        [string]$Description
    )
    
    Write-Status "Creating secret: $SecretName"
    
    try {
        # Check if secret exists
        aws secretsmanager describe-secret --secret-id "amesa/$SecretName" --region $AWSRegion 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Warning "Secret $SecretName already exists. Updating..."
            aws secretsmanager update-secret `
                --secret-id "amesa/$SecretName" `
                --secret-string $SecretValue `
                --region $AWSRegion
        } else {
            aws secretsmanager create-secret `
                --name "amesa/$SecretName" `
                --description $Description `
                --secret-string $SecretValue `
                --region $AWSRegion
        }
        
        Write-Success "Secret $SecretName created/updated successfully"
    }
    catch {
        Write-Error "Failed to create secret $SecretName`: $_"
    }
}

# Function to generate secure random string
function Get-RandomString {
    param([int]$Length = 32)
    -join ((1..$Length) | ForEach {[char]((65..90) + (97..122) + (48..57) | Get-Random)})
}

# Main setup function
function Start-SecretsSetup {
    Write-Status "Setting up AWS Secrets Manager for Amesa Backend..."
    
    # Check if AWS CLI is configured
    try {
        $accountId = aws sts get-caller-identity --query Account --output text
        if (-not $accountId) {
            Write-Error "AWS CLI is not configured. Please run 'aws configure' first."
            exit 1
        }
        Write-Status "AWS Account ID: $accountId"
    }
    catch {
        Write-Error "AWS CLI is not configured. Please run 'aws configure' first."
        exit 1
    }
    
    # Generate secure passwords and keys
    Write-Status "Generating secure passwords and keys..."
    
    $JWT_SECRET = Get-RandomString -Length 64
    $QR_SECRET = Get-RandomString -Length 64
    
    # Create secrets with your actual database password
    New-Secret -SecretName "db-password" -SecretValue $DBPassword -Description "Aurora PostgreSQL database password"
    New-Secret -SecretName "jwt-secret" -SecretValue $JWT_SECRET -Description "JWT signing secret key"
    New-Secret -SecretName "qr-code-secret" -SecretValue $QR_SECRET -Description "QR code generation secret key"
    
    # Create placeholder secrets for other services (you can update these later)
    New-Secret -SecretName "smtp-username" -SecretValue "your-smtp-username" -Description "SMTP server username"
    New-Secret -SecretName "smtp-password" -SecretValue "your-smtp-password" -Description "SMTP server password"
    New-Secret -SecretName "stripe-publishable-key" -SecretValue "pk_test_your_stripe_publishable_key" -Description "Stripe publishable key"
    New-Secret -SecretName "stripe-secret-key" -SecretValue "sk_test_your_stripe_secret_key" -Description "Stripe secret key"
    New-Secret -SecretName "stripe-webhook-secret" -SecretValue "whsec_your_webhook_secret" -Description "Stripe webhook secret"
    New-Secret -SecretName "paypal-client-id" -SecretValue "your_paypal_client_id" -Description "PayPal client ID"
    New-Secret -SecretName "paypal-client-secret" -SecretValue "your_paypal_client_secret" -Description "PayPal client secret"
    New-Secret -SecretName "aws-access-key-id" -SecretValue "your_aws_access_key_id" -Description "AWS access key ID"
    New-Secret -SecretName "aws-secret-access-key" -SecretValue "your_aws_secret_access_key" -Description "AWS secret access key"
    
    Write-Success "All secrets have been created successfully!"
    Write-Status "You can now proceed with the deployment using the deployment script."
    Write-Warning "Note: You may want to update the placeholder secrets (SMTP, Stripe, PayPal, AWS keys) with your actual values later."
    
    # Save the generated secrets for reference
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
    Write-Warning "Secrets information saved to secrets-info.txt (keep this secure!)"
}

# Run main function
Start-SecretsSetup


