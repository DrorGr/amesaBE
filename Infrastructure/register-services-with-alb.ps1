# Register ECS Services with ALB Target Groups
# This script updates ECS services to register tasks with their target groups

param(
    [string]$Region = "eu-north-1",
    [string]$Cluster = "Amesa",
    [string]$ALBArn = "arn:aws:elasticloadbalancing:eu-north-1:129394705401:loadbalancer/app/amesa-backend-alb/d4dbb08b12e385fe"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Registering ECS Services with ALB Target Groups ===" -ForegroundColor Cyan
Write-Host ""

# Service to Target Group mapping
$ServiceConfig = @{
    "amesa-auth-service" = "amesa-auth-service-tg"
    "amesa-payment-service" = "amesa-payment-service-tg"
    "amesa-lottery-service" = "amesa-lottery-service-tg"
    "amesa-content-service" = "amesa-content-service-tg"
    "amesa-notification-service" = "amesa-notification-service-tg"
    "amesa-lottery-results-service" = "amesa-lottery-results-service-tg"
    "amesa-analytics-service" = "amesa-analytics-service-tg"
    "amesa-admin-service" = "amesa-admin-service-tg"
}

foreach ($serviceName in $ServiceConfig.Keys) {
    $tgName = $ServiceConfig[$serviceName]
    
    Write-Host "Processing: $serviceName -> $tgName" -ForegroundColor Yellow
    
    # Get target group ARN
    $tgArn = aws elbv2 describe-target-groups --region $Region --names $tgName --query "TargetGroups[0].TargetGroupArn" --output text 2>$null
    
    if (-not $tgArn -or $tgArn -eq "None") {
        Write-Host "  ⚠️  Target group $tgName not found, skipping..." -ForegroundColor Yellow
        continue
    }
    
    Write-Host "  Target Group ARN: $tgArn" -ForegroundColor Gray
    
    # Get current service configuration
    $service = aws ecs describe-services --region $Region --cluster $Cluster --services $serviceName --query "services[0]" | ConvertFrom-Json
    
    if (-not $service -or $service.status -ne "ACTIVE") {
        Write-Host "  ⚠️  Service $serviceName not found or not active, skipping..." -ForegroundColor Yellow
        continue
    }
    
    # Check if already has load balancer configured
    $hasLB = $service.loadBalancers.Count -gt 0
    
    if ($hasLB) {
        $existingTG = $service.loadBalancers[0].targetGroupArn
        if ($existingTG -eq $tgArn) {
            Write-Host "  ✅ Already registered with target group" -ForegroundColor Green
            continue
        } else {
            Write-Host "  ⚠️  Service has different target group: $existingTG" -ForegroundColor Yellow
        }
    }
    
    # Get container name from task definition
    $tdArn = $service.taskDefinition
    $td = aws ecs describe-task-definition --region $Region --task-definition $tdArn --query "taskDefinition.containerDefinitions[0].name" --output text
    $containerName = $td.Trim()
    
    # Update service with load balancer configuration
    Write-Host "  Registering with target group..." -ForegroundColor Cyan
    
    $updateResult = aws ecs update-service `
        --region $Region `
        --cluster $Cluster `
        --service $serviceName `
        --load-balancers "targetGroupArn=$tgArn,containerName=$containerName,containerPort=8080" `
        --force-new-deployment 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✅ Successfully registered $serviceName with $tgName" -ForegroundColor Green
    } else {
        Write-Host "  ❌ Failed to register: $updateResult" -ForegroundColor Red
    }
    
    Write-Host ""
}

Write-Host "=== Registration Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Waiting for services to stabilize..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# Verify registration
Write-Host ""
Write-Host "=== Verification ===" -ForegroundColor Cyan
foreach ($serviceName in $ServiceConfig.Keys) {
    $tgName = $ServiceConfig[$serviceName]
    $tgArn = aws elbv2 describe-target-groups --region $Region --names $tgName --query "TargetGroups[0].TargetGroupArn" --output text 2>$null
    
    if ($tgArn -and $tgArn -ne "None") {
        $health = aws elbv2 describe-target-health --region $Region --target-group-arn $tgArn --query "TargetHealthDescriptions[*].TargetHealth.State" --output text 2>$null
        $healthyCount = ($health -split "`t" | Where-Object { $_ -eq "healthy" }).Count
        $totalCount = ($health -split "`t").Count
        
        if ($totalCount -gt 0) {
            Write-Host "$serviceName : $healthyCount/$totalCount targets healthy" -ForegroundColor $(if($healthyCount -eq $totalCount){"Green"}else{"Yellow"})
        } else {
            Write-Host "$serviceName : No targets registered yet" -ForegroundColor Yellow
        }
    }
}

