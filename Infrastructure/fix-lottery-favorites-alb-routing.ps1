# Fix ALB Routing for Lottery Favorites & Tickets Endpoints
# Routes specific endpoints to lottery service while keeping general houses endpoints on main backend

$ErrorActionPreference = "Stop"

# ALB Configuration
$region = "eu-north-1"
$listenerArn = "arn:aws:elasticloadbalancing:eu-north-1:129394705401:listener/app/amesa-backend-alb/d4dbb08b12e385fe/c0013fb81f884976"

# Target Group ARNs
$lotteryServiceTG = "arn:aws:elasticloadbalancing:eu-north-1:129394705401:targetgroup/amesa-lottery-service-tg/94d91cf8300d30de"

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Fixing ALB Routing for Lottery Favorites" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Get existing rules to find available priorities
Write-Host "Checking existing rules..." -ForegroundColor Yellow
try {
    $existingRules = aws elbv2 describe-rules --listener-arn $listenerArn --region $region --output json | ConvertFrom-Json
    $existingPriorities = $existingRules.Rules | Where-Object { $_.Priority -ne "default" } | ForEach-Object { [int]$_.Priority } | Sort-Object
    $maxPriority = if ($existingPriorities.Count -gt 0) { $existingPriorities[-1] } else { 0 }
    Write-Host "Found existing rules with priorities: $($existingPriorities -join ', ')" -ForegroundColor Gray
    Write-Host "Max priority: $maxPriority" -ForegroundColor Gray
} catch {
    Write-Host "Warning: Could not fetch existing rules. Starting from priority 1..." -ForegroundColor Yellow
    $maxPriority = 0
}

# Rules to add - HIGH PRIORITY (must come before general /api/v1/houses/* rule)
# These routes must be checked BEFORE the general houses rule
$rulesToAdd = @(
    @{
        Priority = 1
        Path = "/api/v1/houses/favorites"
        Description = "Get user's favorite houses"
    },
    @{
        Priority = 2
        Path = "/api/v1/houses/recommendations"
        Description = "Get personalized house recommendations"
    },
    @{
        Priority = 3
        Path = "/api/v1/houses/*/favorite"
        Description = "Add/remove house from favorites (POST/DELETE)"
    },
    @{
        Priority = 4
        Path = "/api/v1/tickets/*"
        Description = "All tickets endpoints (active, history, analytics, quick-entry)"
    }
)

Write-Host ""
Write-Host "Adding high-priority rules for lottery service..." -ForegroundColor Cyan
Write-Host ""

# Add each rule
foreach ($rule in $rulesToAdd) {
    $priority = $rule.Priority
    $path = $rule.Path
    $description = $rule.Description
    
    Write-Host "Adding rule:" -ForegroundColor Cyan
    Write-Host "  Priority: $priority" -ForegroundColor White
    Write-Host "  Path: $path" -ForegroundColor White
    Write-Host "  Target: amesa-lottery-service-tg" -ForegroundColor White
    Write-Host "  Description: $description" -ForegroundColor White
    
    # Check if priority already exists
    if ($existingPriorities -contains $priority) {
        Write-Host "  ⚠️  Priority $priority already exists. Checking if it matches..." -ForegroundColor Yellow
        
        # Check if existing rule matches our path
        $existingRule = $existingRules.Rules | Where-Object { $_.Priority -eq $priority.ToString() }
        if ($existingRule) {
            $existingPath = ($existingRule.Conditions | Where-Object { $_.Field -eq "path-pattern" }).Values[0]
            if ($existingPath -eq $path) {
                Write-Host "  ✅ Rule already exists with correct path. Skipping..." -ForegroundColor Green
                continue
            } else {
                Write-Host "  ⚠️  Priority $priority exists with different path: $existingPath" -ForegroundColor Yellow
                Write-Host "  ⚠️  You may need to delete the existing rule first or use a different priority" -ForegroundColor Yellow
                continue
            }
        }
    }
    
    try {
        # Create the rule
        $result = aws elbv2 create-rule `
            --listener-arn $listenerArn `
            --priority $priority `
            --conditions "Field=path-pattern,Values=$path" `
            --actions "Type=forward,TargetGroupArn=$lotteryServiceTG" `
            --region $region `
            --output json 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ✅ Rule added successfully!" -ForegroundColor Green
        } else {
            if ($result -match "PriorityInUse") {
                Write-Host "  ⚠️  Priority $priority already in use. Skipping..." -ForegroundColor Yellow
            } else {
                Write-Host "  ❌ Failed to add rule. Exit code: $LASTEXITCODE" -ForegroundColor Red
                Write-Host "  Error: $result" -ForegroundColor Red
            }
        }
    } catch {
        Write-Host "  ❌ Error adding rule: $_" -ForegroundColor Red
    }
    
    Write-Host ""
}

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Verification" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Verify rules were added
Write-Host "Fetching updated rules list..." -ForegroundColor Yellow
try {
    $updatedRules = aws elbv2 describe-rules --listener-arn $listenerArn --region $region --output json | ConvertFrom-Json
    $lotteryRules = $updatedRules.Rules | Where-Object { 
        $_.Priority -ne "default" -and 
        ($_.Actions | Where-Object { $_.TargetGroupArn -eq $lotteryServiceTG })
    }
    
    Write-Host "Rules routing to lottery service:" -ForegroundColor Green
    foreach ($rule in $lotteryRules) {
        $path = ($rule.Conditions | Where-Object { $_.Field -eq "path-pattern" }).Values[0]
        Write-Host "  ✅ Priority $($rule.Priority): $path" -ForegroundColor Green
    }
    
    Write-Host ""
    Write-Host "Rule priority order (higher priority = checked first):" -ForegroundColor Cyan
    $allRules = $updatedRules.Rules | Where-Object { $_.Priority -ne "default" } | Sort-Object { [int]$_.Priority }
    foreach ($rule in $allRules) {
        $path = ($rule.Conditions | Where-Object { $_.Field -eq "path-pattern" }).Values[0]
        $target = ($rule.Actions | Where-Object { $_.Type -eq "forward" }).TargetGroupArn
        $targetName = if ($target -eq $lotteryServiceTG) { "lottery-service" } else { "other-service" }
        Write-Host "  Priority $($rule.Priority): $path → $targetName" -ForegroundColor Gray
    }
} catch {
    Write-Host "Warning: Could not verify rules. Please check manually in AWS Console." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Summary" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ ALB routing updated for lottery favorites endpoints" -ForegroundColor Green
Write-Host ""
Write-Host "Routes configured:" -ForegroundColor Yellow
Write-Host "  • /api/v1/houses/favorites → lottery-service" -ForegroundColor White
Write-Host "  • /api/v1/houses/recommendations → lottery-service" -ForegroundColor White
Write-Host "  • /api/v1/houses/*/favorite → lottery-service" -ForegroundColor White
Write-Host "  • /api/v1/tickets/* → lottery-service" -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Test endpoints:" -ForegroundColor White
Write-Host "   curl -H 'Authorization: Bearer {token}' http://amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com/api/v1/houses/favorites" -ForegroundColor Gray
Write-Host "   curl -H 'Authorization: Bearer {token}' http://amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com/api/v1/houses/recommendations" -ForegroundColor Gray
Write-Host "   curl -H 'Authorization: Bearer {token}' http://amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com/api/v1/tickets/active" -ForegroundColor Gray
Write-Host "2. Verify target group health in AWS Console" -ForegroundColor White
Write-Host "3. Test frontend integration" -ForegroundColor White
Write-Host ""

