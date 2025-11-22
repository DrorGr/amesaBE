# Deploy Specific Service to ECS
# Usage: .\deploy-specific-service.ps1 -ServiceName "amesa-lottery-service"
#        .\deploy-specific-service.ps1 -ServiceName "amesa-lottery-service" -ProjectPath "AmesaBackend.Lottery"

param(
    [Parameter(Mandatory=$true)]
    [string]$ServiceName,
    
    [Parameter(Mandatory=$false)]
    [string]$ProjectPath = "",
    
    [Parameter(Mandatory=$false)]
    [string]$Region = "eu-north-1",
    
    [Parameter(Mandatory=$false)]
    [string]$Cluster = "Amesa"
)

$ErrorActionPreference = "Stop"

# Service mapping: ECS Service Name -> {ECR Repo, Dockerfile Path}
$serviceMap = @{
    "amesa-auth-service" = @{
        EcrRepo = "amesa-auth-service"
        Dockerfile = "AmesaBackend.Auth/Dockerfile"
    }
    "amesa-content-service" = @{
        EcrRepo = "amesa-content-service"
        Dockerfile = "AmesaBackend.Content/Dockerfile"
    }
    "amesa-notification-service" = @{
        EcrRepo = "amesa-notification-service"
        Dockerfile = "AmesaBackend.Notification/Dockerfile"
    }
    "amesa-payment-service" = @{
        EcrRepo = "amesa-payment-service"
        Dockerfile = "AmesaBackend.Payment/Dockerfile"
    }
    "amesa-lottery-service" = @{
        EcrRepo = "amesa-lottery-service"
        Dockerfile = "AmesaBackend.Lottery/Dockerfile"
    }
    "amesa-lottery-results-service" = @{
        EcrRepo = "amesa-lottery-results-service"
        Dockerfile = "AmesaBackend.LotteryResults/Dockerfile"
    }
    "amesa-analytics-service" = @{
        EcrRepo = "amesa-analytics-service"
        Dockerfile = "AmesaBackend.Analytics/Dockerfile"
    }
    "amesa-admin-service" = @{
        EcrRepo = "amesa-admin-service"
        Dockerfile = "AmesaBackend.Admin/Dockerfile"
    }
}

$Account = "129394705401"
$EcrRoot = "$Account.dkr.ecr.$Region.amazonaws.com"

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Deploying Specific Service: $ServiceName" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Validate service name
if (-not $serviceMap.ContainsKey($ServiceName)) {
    Write-Host "❌ Error: Unknown service name '$ServiceName'" -ForegroundColor Red
    Write-Host ""
    Write-Host "Available services:" -ForegroundColor Yellow
    foreach ($key in $serviceMap.Keys) {
        Write-Host "  - $key" -ForegroundColor White
    }
    exit 1
}

$serviceConfig = $serviceMap[$ServiceName]
$ecrRepo = $serviceConfig.EcrRepo
$dockerfile = $serviceConfig.Dockerfile

# Override Dockerfile path if provided
if ($ProjectPath) {
    $dockerfile = "$ProjectPath/Dockerfile"
    Write-Host "Using custom Dockerfile path: $dockerfile" -ForegroundColor Yellow
}

Write-Host "Service Configuration:" -ForegroundColor Yellow
Write-Host "  ECS Service: $ServiceName" -ForegroundColor White
Write-Host "  ECR Repository: $ecrRepo" -ForegroundColor White
Write-Host "  Dockerfile: $dockerfile" -ForegroundColor White
Write-Host "  Region: $Region" -ForegroundColor White
Write-Host "  Cluster: $Cluster" -ForegroundColor White
Write-Host ""

# Verify Dockerfile exists
$beDir = Join-Path $PSScriptRoot ".."
$dockerfilePath = Join-Path $beDir $dockerfile
if (-not (Test-Path $dockerfilePath)) {
    Write-Host "❌ Error: Dockerfile not found at: $dockerfilePath" -ForegroundColor Red
    exit 1
}

# Step 1: Login to ECR
Write-Host "Step 1: Logging into ECR..." -ForegroundColor Cyan
aws ecr get-login-password --region $Region | docker login --username AWS --password-stdin $EcrRoot
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ ECR login failed" -ForegroundColor Red
    exit 1
}
Write-Host "✅ ECR login successful" -ForegroundColor Green
Write-Host ""

# Step 2: Build Docker image
Write-Host "Step 2: Building Docker image..." -ForegroundColor Cyan
$image = "$EcrRoot/$ecrRepo:latest"
$env:DOCKER_BUILDKIT = "1"
Push-Location $beDir
try {
    Write-Host "  Building: $image" -ForegroundColor Gray
    Write-Host "  Dockerfile: $dockerfile" -ForegroundColor Gray
    docker build -f $dockerfile -t $image .
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Docker build failed" -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ Docker image built successfully" -ForegroundColor Green
} finally {
    Pop-Location
}
Write-Host ""

# Step 3: Push to ECR
Write-Host "Step 3: Pushing to ECR..." -ForegroundColor Cyan
docker push $image
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Docker push failed" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Image pushed to ECR: $image" -ForegroundColor Green
Write-Host ""

# Step 4: Force ECS service update
Write-Host "Step 4: Updating ECS service..." -ForegroundColor Cyan
Write-Host "  Service: $ServiceName" -ForegroundColor Gray
Write-Host "  Cluster: $Cluster" -ForegroundColor Gray
Write-Host "  This will trigger a new deployment..." -ForegroundColor Gray

$updateResult = aws ecs update-service `
    --region $Region `
    --cluster $Cluster `
    --service $ServiceName `
    --force-new-deployment `
    --output json 2>&1

if ($LASTEXITCODE -eq 0) {
    $serviceInfo = $updateResult | ConvertFrom-Json
    Write-Host "✅ ECS service update initiated" -ForegroundColor Green
    Write-Host "  Service ARN: $($serviceInfo.service.serviceArn)" -ForegroundColor Gray
    Write-Host "  Desired Count: $($serviceInfo.service.desiredCount)" -ForegroundColor Gray
    Write-Host "  Running Count: $($serviceInfo.service.runningCount)" -ForegroundColor Gray
} else {
    Write-Host "❌ ECS service update failed" -ForegroundColor Red
    Write-Host "  Error: $updateResult" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 5: Monitor deployment
Write-Host "Step 5: Monitoring deployment..." -ForegroundColor Cyan
Write-Host "  Waiting for service to stabilize..." -ForegroundColor Gray
Write-Host "  (This may take 1-2 minutes)" -ForegroundColor Gray
Write-Host ""

$maxWaitTime = 300 # 5 minutes
$waitInterval = 10 # Check every 10 seconds
$elapsed = 0

while ($elapsed -lt $maxWaitTime) {
    Start-Sleep -Seconds $waitInterval
    $elapsed += $waitInterval
    
    $serviceStatus = aws ecs describe-services `
        --region $Region `
        --cluster $Cluster `
        --services $ServiceName `
        --query "services[0].{Running:runningCount,Desired:desiredCount,Status:status}" `
        --output json | ConvertFrom-Json
    
    Write-Host "  [$elapsed s] Running: $($serviceStatus.Running)/$($serviceStatus.Desired) - Status: $($serviceStatus.Status)" -ForegroundColor Gray
    
    if ($serviceStatus.Running -eq $serviceStatus.Desired -and $serviceStatus.Status -eq "ACTIVE") {
        Write-Host ""
        Write-Host "✅ Deployment complete! Service is running." -ForegroundColor Green
        break
    }
}

if ($elapsed -ge $maxWaitTime) {
    Write-Host ""
    Write-Host "⚠️  Deployment monitoring timeout. Service may still be deploying." -ForegroundColor Yellow
    Write-Host "  Check status manually: aws ecs describe-services --cluster $Cluster --services $ServiceName --region $Region" -ForegroundColor Gray
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Deployment Summary" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ Service: $ServiceName" -ForegroundColor Green
Write-Host "✅ Image: $image" -ForegroundColor Green
Write-Host "✅ ECS Update: Initiated" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Monitor service in AWS Console: ECS > Clusters > $Cluster > Services > $ServiceName" -ForegroundColor White
Write-Host "2. Check service logs: aws logs tail /ecs/$ecrRepo --follow --region $Region" -ForegroundColor White
Write-Host "3. Test endpoints after deployment completes" -ForegroundColor White
Write-Host ""

