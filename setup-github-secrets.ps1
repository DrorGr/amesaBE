# Setup GitHub Secrets for Backend Repository
# This script configures all required secrets for the amesaBE repository

Write-Host "🔐 Setting up GitHub Secrets for Backend Repository..." -ForegroundColor Cyan
Write-Host ""

# Prompt for AWS credentials (these should be the same as FE repo)
Write-Host "📝 Please provide your AWS credentials:" -ForegroundColor Yellow
Write-Host "   (These should be the same credentials used in the FE repository)" -ForegroundColor Gray
Write-Host ""

$awsAccessKeyId = Read-Host "AWS Access Key ID"
$awsSecretAccessKey = Read-Host "AWS Secret Access Key" -AsSecureString
$awsSecretAccessKeyPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($awsSecretAccessKey)
)

Write-Host ""
Write-Host "⚙️ Configuring secrets..." -ForegroundColor Cyan

# Set AWS Credentials
Write-Host "  → Setting AWS_ACCESS_KEY_ID..." -NoNewline
gh secret set AWS_ACCESS_KEY_ID --body "$awsAccessKeyId" --repo DrorGr/amesaBE
Write-Host " ✅" -ForegroundColor Green

Write-Host "  → Setting AWS_SECRET_ACCESS_KEY..." -NoNewline
gh secret set AWS_SECRET_ACCESS_KEY --body "$awsSecretAccessKeyPlain" --repo DrorGr/amesaBE
Write-Host " ✅" -ForegroundColor Green

# Set Development Environment Secrets
Write-Host "  → Setting DEV_ECS_CLUSTER..." -NoNewline
gh secret set DEV_ECS_CLUSTER --body "Amesa" --repo DrorGr/amesaBE
Write-Host " ✅" -ForegroundColor Green

Write-Host "  → Setting DEV_ECS_SERVICE..." -NoNewline
gh secret set DEV_ECS_SERVICE --body "amesa-backend-stage-service" --repo DrorGr/amesaBE
Write-Host " ✅" -ForegroundColor Green

# Set Staging Environment Secrets (same as dev)
Write-Host "  → Setting STAGE_ECS_CLUSTER..." -NoNewline
gh secret set STAGE_ECS_CLUSTER --body "Amesa" --repo DrorGr/amesaBE
Write-Host " ✅" -ForegroundColor Green

Write-Host "  → Setting STAGE_ECS_SERVICE..." -NoNewline
gh secret set STAGE_ECS_SERVICE --body "amesa-backend-stage-service" --repo DrorGr/amesaBE
Write-Host " ✅" -ForegroundColor Green

# Set Production Environment Secrets
Write-Host "  → Setting PROD_ECS_CLUSTER..." -NoNewline
gh secret set PROD_ECS_CLUSTER --body "Amesa" --repo DrorGr/amesaBE
Write-Host " ✅" -ForegroundColor Green

Write-Host "  → Setting PROD_ECS_SERVICE..." -NoNewline
gh secret set PROD_ECS_SERVICE --body "amesa-backend-service" --repo DrorGr/amesaBE
Write-Host " ✅" -ForegroundColor Green

Write-Host ""
Write-Host "✅ All GitHub Secrets configured successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "🚀 Next Steps:" -ForegroundColor Cyan
Write-Host "   1. Verify secrets at: https://github.com/DrorGr/amesaBE/settings/secrets/actions"
Write-Host "   2. Re-run failed GitHub Actions workflow"
Write-Host "   3. Monitor deployment at: https://github.com/DrorGr/amesaBE/actions"
Write-Host ""
Write-Host "📋 Configured Secrets:" -ForegroundColor Yellow
Write-Host "   ✅ AWS_ACCESS_KEY_ID"
Write-Host "   ✅ AWS_SECRET_ACCESS_KEY"
Write-Host "   ✅ DEV_ECS_CLUSTER = Amesa"
Write-Host "   ✅ DEV_ECS_SERVICE = amesa-backend-stage-service"
Write-Host "   ✅ STAGE_ECS_CLUSTER = Amesa"
Write-Host "   ✅ STAGE_ECS_SERVICE = amesa-backend-stage-service"
Write-Host "   ✅ PROD_ECS_CLUSTER = Amesa"
Write-Host "   ✅ PROD_ECS_SERVICE = amesa-backend-service"
Write-Host ""

