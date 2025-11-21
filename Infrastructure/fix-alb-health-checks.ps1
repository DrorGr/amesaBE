# Fix ALB Health Check Issues
# This script diagnoses and fixes common ALB health check problems

param(
    [string]$Region = "eu-north-1",
    [string]$Cluster = "Amesa"
)

$ErrorActionPreference = "Stop"

Write-Host "=== ALB Health Check Diagnostic and Fix ===" -ForegroundColor Cyan
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
        Write-Host "  ⚠️  Target group not found, skipping..." -ForegroundColor Yellow
        continue
    }
    
    # Get current health check config
    $tg = aws elbv2 describe-target-groups --region $Region --target-group-arns $tgArn --query "TargetGroups[0]" --output json | ConvertFrom-Json
    
    Write-Host "  Current health check:" -ForegroundColor Gray
    Write-Host "    Protocol: $($tg.HealthCheckProtocol)" -ForegroundColor Gray
    Write-Host "    Path: $($tg.HealthCheckPath)" -ForegroundColor Gray
    Write-Host "    Port: $($tg.HealthCheckPort)" -ForegroundColor Gray
    Write-Host "    Timeout: $($tg.HealthCheckTimeoutSeconds)s" -ForegroundColor Gray
    Write-Host "    Interval: $($tg.HealthCheckIntervalSeconds)s" -ForegroundColor Gray
    
    # Update health check with more lenient settings
    Write-Host "  Updating health check with optimized settings..." -ForegroundColor Cyan
    
    $updateParams = @(
        "--target-group-arn", $tgArn,
        "--region", $Region,
        "--health-check-timeout-seconds", "15",
        "--health-check-interval-seconds", "30",
        "--healthy-threshold-count", "2",
        "--unhealthy-threshold-count", "5"
    )
    
    # Only update protocol/path if HTTP
    if ($tg.HealthCheckProtocol -eq "HTTP") {
        $updateParams += "--health-check-path", "/health"
        $updateParams += "--matcher", "HttpCode=200"
    }
    
    aws elbv2 modify-target-group @updateParams 2>&1 | Out-Null
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✅ Health check updated" -ForegroundColor Green
    } else {
        Write-Host "  ⚠️  Failed to update (may need manual fix)" -ForegroundColor Yellow
    }
    
    Write-Host ""
}

Write-Host "=== Update Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Waiting 90 seconds for health checks to retry..." -ForegroundColor Yellow
Start-Sleep -Seconds 90

Write-Host ""
Write-Host "=== Health Check Status ===" -ForegroundColor Cyan
foreach ($serviceName in $ServiceConfig.Keys) {
    $tgName = $ServiceConfig[$serviceName]
    $tgArn = aws elbv2 describe-target-groups --region $Region --names $tgName --query "TargetGroups[0].TargetGroupArn" --output text 2>$null
    
    if ($tgArn -and $tgArn -ne "None") {
        $health = aws elbv2 describe-target-health --region $Region --target-group-arn $tgArn --query "TargetHealthDescriptions[0].TargetHealth.State" --output text 2>$null
        $color = if ($health -eq "healthy") { "Green" } else { "Yellow" }
        Write-Host "$serviceName : $health" -ForegroundColor $color
    }
}

