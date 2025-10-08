# Test Docker Build and Push Script
# This script tests the Docker build and push process step by step

Write-Host "Docker Build and Push Test" -ForegroundColor Blue
Write-Host "=========================" -ForegroundColor Blue

# Configuration
$AWSRegion = "eu-north-1"
$ECRRepository = "amesabe"
$AccountId = "129394705401"
$ImageTag = "latest"

# Add AWS CLI to PATH if not already there
if (-not (Get-Command aws -ErrorAction SilentlyContinue)) {
    $env:PATH += ";C:\Program Files\Amazon\AWSCLIV2"
    Write-Host "Added AWS CLI to PATH" -ForegroundColor Green
}

# Step 1: Test AWS CLI
Write-Host "`nStep 1: Testing AWS CLI..." -ForegroundColor Blue
try {
    $identity = aws sts get-caller-identity
    $accountIdFromAWS = ($identity | ConvertFrom-Json).Account
    Write-Host "✓ AWS CLI working! Account ID: $accountIdFromAWS" -ForegroundColor Green
    
    if ($accountIdFromAWS -ne $AccountId) {
        Write-Host "⚠ Warning: Account ID mismatch. Expected: $AccountId, Got: $accountIdFromAWS" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "✗ AWS CLI not configured. Please run .\configure-aws.ps1 first" -ForegroundColor Red
    exit 1
}

# Step 2: Test Docker
Write-Host "`nStep 2: Testing Docker..." -ForegroundColor Blue
try {
    $dockerVersion = docker --version
    Write-Host "✓ Docker is available: $dockerVersion" -ForegroundColor Green
}
catch {
    Write-Host "✗ Docker is not available. Please install Docker Desktop" -ForegroundColor Red
    exit 1
}

# Step 3: Login to ECR
Write-Host "`nStep 3: Logging into ECR..." -ForegroundColor Blue
try {
    $loginCommand = aws ecr get-login-password --region $AWSRegion | docker login --username AWS --password-stdin "$AccountId.dkr.ecr.$AWSRegion.amazonaws.com"
    Write-Host "✓ Successfully logged into ECR" -ForegroundColor Green
}
catch {
    Write-Host "✗ Failed to login to ECR: $_" -ForegroundColor Red
    Write-Host "Make sure your ECR repository exists and you have permissions" -ForegroundColor Yellow
    exit 1
}

# Step 4: Build Docker Image
Write-Host "`nStep 4: Building Docker image..." -ForegroundColor Blue
try {
    Write-Host "Building image: $ECRRepository`:$ImageTag" -ForegroundColor Gray
    docker build -t "$ECRRepository`:$ImageTag" -f AmesaBackend/Dockerfile AmesaBackend/
    Write-Host "✓ Docker image built successfully" -ForegroundColor Green
}
catch {
    Write-Host "✗ Failed to build Docker image: $_" -ForegroundColor Red
    Write-Host "Make sure the Dockerfile exists in AmesaBackend/Dockerfile" -ForegroundColor Yellow
    exit 1
}

# Step 5: Tag Docker Image
Write-Host "`nStep 5: Tagging Docker image..." -ForegroundColor Blue
try {
    $fullImageName = "$AccountId.dkr.ecr.$AWSRegion.amazonaws.com/$ECRRepository`:$ImageTag"
    docker tag "$ECRRepository`:$ImageTag" $fullImageName
    Write-Host "✓ Docker image tagged as: $fullImageName" -ForegroundColor Green
}
catch {
    Write-Host "✗ Failed to tag Docker image: $_" -ForegroundColor Red
    exit 1
}

# Step 6: Push Docker Image
Write-Host "`nStep 6: Pushing Docker image to ECR..." -ForegroundColor Blue
try {
    Write-Host "Pushing to: $fullImageName" -ForegroundColor Gray
    docker push $fullImageName
    Write-Host "✓ Docker image pushed successfully!" -ForegroundColor Green
}
catch {
    Write-Host "✗ Failed to push Docker image: $_" -ForegroundColor Red
    Write-Host "Check your ECR repository permissions" -ForegroundColor Yellow
    exit 1
}

Write-Host "`n🎉 All steps completed successfully!" -ForegroundColor Green
Write-Host "Your Docker image is now available in ECR at:" -ForegroundColor Blue
Write-Host "$fullImageName" -ForegroundColor Cyan
Write-Host "`nYou can now proceed with the full deployment using .\deploy-final.ps1" -ForegroundColor Green


