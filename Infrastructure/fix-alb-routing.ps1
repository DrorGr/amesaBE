# Fix ALB Routing Rules
# This script removes conflicting low-priority rules and ensures correct routing

param(
    [string]$Region = "eu-north-1",
    [string]$ALBArn = "arn:aws:elasticloadbalancing:eu-north-1:129394705401:loadbalancer/app/amesa-backend-alb/d4dbb08b12e385fe"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Fixing ALB Routing Rules ===" -ForegroundColor Cyan

# Get listener
$listenerArn = aws elbv2 describe-listeners --region $Region --load-balancer-arn $ALBArn --query "Listeners[0].ListenerArn" --output text
Write-Host "Listener ARN: $listenerArn" -ForegroundColor Yellow

# Get current rules
Write-Host "`nGetting current rules..." -ForegroundColor Cyan
$currentRules = aws elbv2 describe-rules --region $Region --listener-arn $listenerArn --output json | ConvertFrom-Json

# Identify and remove conflicting low-priority rules (1-10)
Write-Host "`nRemoving conflicting low-priority rules (1-10)..." -ForegroundColor Yellow
$lowPriorityRules = $currentRules.Rules | Where-Object { 
    $_.Priority -ne "default" -and 
    [int]$_.Priority -lt 100 -and 
    [int]$_.Priority -ge 1 
}

foreach($rule in $lowPriorityRules) {
    $path = "unknown"
    if($rule.Conditions) {
        $pathCondition = $rule.Conditions | Where-Object { $_.Type -eq "path-pattern" }
        if($pathCondition) {
            $path = $pathCondition.Values[0]
        }
    }
    Write-Host "  Deleting rule Priority $($rule.Priority): $path" -ForegroundColor Gray
    aws elbv2 delete-rule --region $Region --rule-arn $rule.RuleArn 2>&1 | Out-Null
    if($LASTEXITCODE -eq 0) {
        Write-Host "    ✅ Deleted" -ForegroundColor Green
    }
}

Write-Host "`n✅ Low-priority conflicting rules removed" -ForegroundColor Green
Write-Host "`nCurrent routing rules (100+) should now work correctly." -ForegroundColor Yellow
Write-Host "Wait 30 seconds for changes to propagate, then test endpoints." -ForegroundColor Cyan



