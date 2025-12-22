# PowerShell script to create SES SMTP credentials and update secrets
# This script creates an IAM user for SES SMTP and updates the secrets

param(
    [Parameter(Mandatory=$false)]
    [string]$Region = "eu-north-1",
    
    [Parameter(Mandatory=$false)]
    [string]$IamUserName = "amesa-ses-smtp-user",
    
    [Parameter(Mandatory=$false)]
    [string]$SecretsPrefix = "/amesa/prod"
)

$ErrorActionPreference = "Stop"

Write-Host "Setting up SES SMTP credentials..." -ForegroundColor Cyan
Write-Host ""

# Check if IAM user already exists
Write-Host "Checking for existing IAM user: $IamUserName..." -ForegroundColor Yellow
$userExists = aws iam get-user --user-name $IamUserName --region $Region 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host "Creating IAM user for SES SMTP..." -ForegroundColor Yellow
    
    # Create IAM user
    aws iam create-user --user-name $IamUserName --region $Region | Out-Null
    Write-Host "✅ IAM user created: $IamUserName" -ForegroundColor Green
    
    # Attach SES policy
    Write-Host "Attaching SES policy..." -ForegroundColor Yellow
    aws iam attach-user-policy --user-name $IamUserName --policy-arn arn:aws:iam::aws:policy/AmazonSESFullAccess --region $Region
    Write-Host "✅ SES policy attached" -ForegroundColor Green
} else {
    Write-Host "✅ IAM user already exists: $IamUserName" -ForegroundColor Green
}

# Create SMTP credentials
Write-Host ""
Write-Host "Creating SMTP credentials..." -ForegroundColor Yellow

# Check if access keys exist
$existingKeys = aws iam list-access-keys --user-name $IamUserName --region $Region --query "AccessKeyMetadata[].AccessKeyId" --output json | ConvertFrom-Json

if ($existingKeys.Count -gt 0) {
    Write-Host "⚠️  IAM user already has access keys." -ForegroundColor Yellow
    Write-Host "   Existing keys: $($existingKeys -join ', ')" -ForegroundColor Gray
    Write-Host ""
    Write-Host "To create SMTP credentials:" -ForegroundColor Cyan
    Write-Host "1. Go to AWS Console → IAM → Users → $IamUserName" -ForegroundColor White
    Write-Host "2. Security credentials tab → Create SMTP credentials" -ForegroundColor White
    Write-Host "3. Copy the username and password" -ForegroundColor White
    Write-Host ""
    
    $useExisting = Read-Host "Do you want to use existing access keys as SMTP credentials? (y/n)"
    
    if ($useExisting -eq "y") {
        # Get the access key secret (we can't retrieve it, user needs to provide)
        Write-Host ""
        Write-Host "⚠️  Note: AWS doesn't allow retrieving access key secrets after creation." -ForegroundColor Yellow
        Write-Host "   If you don't have the secret, create new SMTP credentials via Console." -ForegroundColor Yellow
        Write-Host ""
        $smtpUsername = Read-Host "Enter SMTP username (Access Key ID)"
        $smtpPassword = Read-Host "Enter SMTP password (Secret Access Key)" -AsSecureString
        $smtpPasswordPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($smtpPassword))
    } else {
        Write-Host "Please create SMTP credentials via AWS Console and run this script again." -ForegroundColor Yellow
        exit 0
    }
} else {
    # Create access key
    Write-Host "Creating access key for SMTP..." -ForegroundColor Yellow
    $accessKey = aws iam create-access-key --user-name $IamUserName --region $Region | ConvertFrom-Json
    
    $smtpUsername = $accessKey.AccessKey.AccessKeyId
    $smtpPassword = $accessKey.AccessKey.SecretAccessKey
    
    Write-Host "✅ Access key created" -ForegroundColor Green
    Write-Host ""
    Write-Host "⚠️  IMPORTANT: Save these credentials securely!" -ForegroundColor Yellow
    Write-Host "   SMTP Username: $smtpUsername" -ForegroundColor Cyan
    Write-Host "   SMTP Password: $smtpPassword" -ForegroundColor Cyan
    Write-Host ""
}

# Update secrets
Write-Host "Updating secrets..." -ForegroundColor Yellow

# Update SmtpUsername
$usernameSecret = "$SecretsPrefix/EmailSettings/SmtpUsername"
$tempFile = New-TemporaryFile
Set-Content -Path $tempFile -Value $smtpUsername
aws secretsmanager update-secret --secret-id $usernameSecret --secret-string file://$tempFile --region $Region | Out-Null
Remove-Item $tempFile
Write-Host "✅ Updated: $usernameSecret" -ForegroundColor Green

# Update SmtpPassword
$passwordSecret = "$SecretsPrefix/EmailSettings/SmtpPassword"
$tempFile = New-TemporaryFile
Set-Content -Path $tempFile -Value $smtpPassword
aws secretsmanager update-secret --secret-id $passwordSecret --secret-string file://$tempFile --region $Region | Out-Null
Remove-Item $tempFile
Write-Host "✅ Updated: $passwordSecret" -ForegroundColor Green

Write-Host ""
Write-Host "✅ SES SMTP credentials configured successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Verify your email domain/address in SES Console" -ForegroundColor White
Write-Host "2. Request production access if in SES Sandbox" -ForegroundColor White
Write-Host "3. Test email sending" -ForegroundColor White







