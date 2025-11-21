# Add Missing ALB Rules for Frontend-Backend Integration
# This script adds the missing listener rules to the production ALB

$ErrorActionPreference = "Stop"

# ALB Configuration
$region = "eu-north-1"
$listenerArn = "arn:aws:elasticloadbalancing:eu-north-1:129394705401:listener/app/amesa-backend-alb/d4dbb08b12e385fe/c0013fb81f884976"

# Target Group ARNs
$targetGroups = @{
    "auth" = "arn:aws:elasticloadbalancing:eu-north-1:129394705401:targetgroup/amesa-auth-service-tg/d13b0c45a29c7ff4"
    "lottery" = "arn:aws:elasticloadbalancing:eu-north-1:129394705401:targetgroup/amesa-lottery-service-tg/94d91cf8300d30de"
    "content" = "arn:aws:elasticloadbalancing:eu-north-1:129394705401:targetgroup/amesa-content-service-tg/2770ab44b773d027"
    "admin" = "arn:aws:elasticloadbalancing:eu-north-1:129394705401:targetgroup/amesa-admin-service-tg/eb49ec606339ef45"
    "lottery-results" = "arn:aws:elasticloadbalancing:eu-north-1:129394705401:targetgroup/amesa-lottery-results-service-tg/2637524defd5575f"
    "payment" = "arn:aws:elasticloadbalancing:eu-north-1:129394705401:targetgroup/amesa-payment-service-tg/679ecfc20718c19f"
}

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Adding Missing ALB Rules for Frontend Integration" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Rules to add (Priority, Path, Target Group Key, Description)
$rulesToAdd = @(
    @{
        Priority = 1
        Path = "/api/v1/oauth/*"
        TargetGroupKey = "auth"
        Description = "OAuth endpoints (Google, Meta login)"
    },
    @{
        Priority = 2
        Path = "/api/v1/houses/*"
        TargetGroupKey = "lottery"
        Description = "Houses endpoints (listings, details, tickets)"
    },
    @{
        Priority = 3
        Path = "/api/v1/translations/*"
        TargetGroupKey = "content"
        Description = "Translations endpoints (i18n)"
    },
    @{
        Priority = 4
        Path = "/_blazor/*"
        TargetGroupKey = "admin"
        Description = "Blazor SignalR hub (WebSocket support)"
    },
    @{
        Priority = 5
        Path = "/api/v1/lotteryresults/*"
        TargetGroupKey = "lottery-results"
        Description = "Lottery results (no hyphen - frontend format)"
    },
    @{
        Priority = 6
        Path = "/api/v1/payments/*"
        TargetGroupKey = "payment"
        Description = "Payments endpoints (plural - frontend format)"
    }
)

# Check existing rules to avoid conflicts
Write-Host "Checking existing rules..." -ForegroundColor Yellow
try {
    $existingRules = aws elbv2 describe-rules --listener-arn $listenerArn --region $region --output json | ConvertFrom-Json
    $existingPriorities = $existingRules.Rules | Where-Object { $_.Priority -ne "default" } | ForEach-Object { [int]$_.Priority }
    Write-Host "Found existing rules with priorities: $($existingPriorities -join ', ')" -ForegroundColor Gray
} catch {
    Write-Host "Warning: Could not fetch existing rules. Continuing anyway..." -ForegroundColor Yellow
    $existingPriorities = @()
}

# Add each rule
foreach ($rule in $rulesToAdd) {
    $priority = $rule.Priority
    $path = $rule.Path
    $targetGroupArn = $targetGroups[$rule.TargetGroupKey]
    $description = $rule.Description
    
    Write-Host ""
    Write-Host "Adding rule:" -ForegroundColor Cyan
    Write-Host "  Priority: $priority" -ForegroundColor White
    Write-Host "  Path: $path" -ForegroundColor White
    Write-Host "  Target: $($rule.TargetGroupKey)" -ForegroundColor White
    Write-Host "  Description: $description" -ForegroundColor White
    
    # Check if priority already exists
    if ($existingPriorities -contains $priority) {
        Write-Host "  ⚠️  Priority $priority already exists. Skipping..." -ForegroundColor Yellow
        continue
    }
    
    # Create rule JSON
    $ruleJson = @{
        ListenerArn = $listenerArn
        Priority = $priority
        Conditions = @(
            @{
                Field = "path-pattern"
                Values = @($path)
            }
        )
        Actions = @(
            @{
                Type = "forward"
                TargetGroupArn = $targetGroupArn
            }
        )
    } | ConvertTo-Json -Depth 10
    
    try {
        # Create the rule
        $result = aws elbv2 create-rule `
            --listener-arn $listenerArn `
            --priority $priority `
            --conditions "Field=path-pattern,Values=$path" `
            --actions "Type=forward,TargetGroupArn=$targetGroupArn" `
            --region $region `
            --output json
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ✅ Rule added successfully!" -ForegroundColor Green
        } else {
            Write-Host "  ❌ Failed to add rule. Exit code: $LASTEXITCODE" -ForegroundColor Red
            Write-Host "  Error output: $result" -ForegroundColor Red
        }
    } catch {
        Write-Host "  ❌ Error adding rule: $_" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Verification" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Verify rules were added
Write-Host "Fetching updated rules list..." -ForegroundColor Yellow
try {
    $updatedRules = aws elbv2 describe-rules --listener-arn $listenerArn --region $region --output json | ConvertFrom-Json
    $newRules = $updatedRules.Rules | Where-Object { 
        $_.Priority -ne "default" -and 
        ($_.Conditions | Where-Object { $_.Values -contains "/api/v1/oauth/*" -or 
                                         $_.Values -contains "/api/v1/houses/*" -or 
                                         $_.Values -contains "/api/v1/translations/*" -or 
                                         $_.Values -contains "/_blazor/*" -or 
                                         $_.Values -contains "/api/v1/lotteryresults/*" -or 
                                         $_.Values -contains "/api/v1/payments/*" })
    }
    
    Write-Host "Newly added rules:" -ForegroundColor Green
    foreach ($rule in $newRules) {
        $path = ($rule.Conditions | Where-Object { $_.Field -eq "path-pattern" }).Values[0]
        Write-Host "  ✅ Priority $($rule.Priority): $path" -ForegroundColor Green
    }
} catch {
    Write-Host "Warning: Could not verify rules. Please check manually in AWS Console." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Summary" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Verify rules in AWS Console: EC2 > Load Balancers > amesa-backend-alb > Listeners" -ForegroundColor White
Write-Host "2. Test endpoints:" -ForegroundColor White
Write-Host "   - curl http://amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com/api/v1/oauth/google" -ForegroundColor Gray
Write-Host "   - curl http://amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com/api/v1/houses" -ForegroundColor Gray
Write-Host "   - curl http://amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com/api/v1/translations/en" -ForegroundColor Gray
Write-Host "3. Check target group health in AWS Console" -ForegroundColor White
Write-Host "4. Test frontend integration" -ForegroundColor White
Write-Host ""

