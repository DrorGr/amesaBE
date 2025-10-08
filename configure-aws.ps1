# AWS CLI Configuration Script
# This script helps you configure AWS CLI with your credentials

Write-Host "AWS CLI Configuration Helper" -ForegroundColor Blue
Write-Host "=============================" -ForegroundColor Blue

# Add AWS CLI to PATH if not already there
if (-not (Get-Command aws -ErrorAction SilentlyContinue)) {
    $env:PATH += ";C:\Program Files\Amazon\AWSCLIV2"
    Write-Host "Added AWS CLI to PATH" -ForegroundColor Green
}

Write-Host "`nPlease provide your AWS credentials:" -ForegroundColor Yellow
Write-Host "You can find these in your AWS Console under IAM > Users > Security credentials" -ForegroundColor Gray

# Get credentials from user
$accessKey = Read-Host "AWS Access Key ID"
$secretKey = Read-Host "AWS Secret Access Key" -AsSecureString
$region = Read-Host "Default region name [eu-north-1]"
$outputFormat = Read-Host "Default output format [json]"

# Set defaults
if (-not $region) { $region = "eu-north-1" }
if (-not $outputFormat) { $outputFormat = "json" }

# Convert secure string to plain text
$secretKeyPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($secretKey))

Write-Host "`nConfiguring AWS CLI..." -ForegroundColor Blue

try {
    # Configure AWS CLI
    aws configure set aws_access_key_id $accessKey
    aws configure set aws_secret_access_key $secretKeyPlain
    aws configure set default.region $region
    aws configure set default.output $outputFormat
    
    Write-Host "✓ AWS CLI configured successfully!" -ForegroundColor Green
    
    # Test the configuration
    Write-Host "`nTesting configuration..." -ForegroundColor Blue
    $identity = aws sts get-caller-identity
    Write-Host "✓ Configuration test successful!" -ForegroundColor Green
    Write-Host "Account ID: $($identity | ConvertFrom-Json | Select-Object -ExpandProperty Account)" -ForegroundColor Cyan
    
    Write-Host "`nYou can now proceed with the deployment!" -ForegroundColor Green
}
catch {
    Write-Host "✗ Failed to configure AWS CLI: $_" -ForegroundColor Red
    Write-Host "Please check your credentials and try again." -ForegroundColor Yellow
}

# Clear the secret key from memory
$secretKeyPlain = $null


