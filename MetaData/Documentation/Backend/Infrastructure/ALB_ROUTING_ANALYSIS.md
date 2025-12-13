# ALB Routing Analysis

## Current Status

### Health Checks
- ✅ **All 8 services are HEALTHY**
- ✅ Security groups properly configured
- ✅ ALB can reach ECS tasks

### Endpoint Issues
- ❌ `/api/v1/auth/health` returns 404
- ❌ `/admin` returns 503

## Analysis

### Issue 1: 404 on /api/v1/auth/health
**Possible Causes**:
1. ALB routing rule not matching the path correctly
2. Path pattern `/api/v1/auth/*` might not be matching `/api/v1/auth/health`
3. Rule priority issue (though low-priority rules were removed)

### Issue 2: 503 on /admin
**Possible Causes**:
1. Default target group has no healthy targets
2. Admin service target group not receiving traffic
3. Routing rule for `/admin/*` not working

## ALB Rule Priority

ALB checks rules in **ascending priority order**:
- Lower numbers are checked first
- If a rule matches, it's used (no further checking)
- Default action is used if no rules match

### Current Rule Structure
- Priority 100-107: Service-specific paths
- Priority default: Default target group

## Path Pattern Matching

ALB path patterns use wildcard matching:
- `/api/v1/auth/*` matches:
  - ✅ `/api/v1/auth/health`
  - ✅ `/api/v1/auth/login`
  - ✅ `/api/v1/auth/anything`
  - ❌ `/api/v1/auth` (no trailing slash)

## Next Steps

### 1. Verify Rules Exist
Check that rules for all service paths exist:
- `/api/v1/auth/*` → amesa-auth-service-tg
- `/api/v1/payment/*` → amesa-payment-service-tg
- `/api/v1/lottery/*` → amesa-lottery-service-tg
- `/api/v1/content/*` → amesa-content-service-tg
- `/api/v1/notification/*` → amesa-notification-service-tg
- `/api/v1/lottery-results/*` → amesa-lottery-results-service-tg
- `/api/v1/analytics/*` → amesa-analytics-service-tg
- `/admin/*` → amesa-admin-service-tg

### 2. Check Default Target Group
- Verify default target group has healthy targets OR
- Update default action to forward to a specific service

### 3. Test Direct Service Access
If possible, test services directly (bypassing ALB) to verify they respond correctly.

## Commands to Debug

```powershell
# List all rules
$listenerArn = aws elbv2 describe-listeners --load-balancer-arn <alb-arn> --query "Listeners[0].ListenerArn" --output text
aws elbv2 describe-rules --listener-arn $listenerArn

# Check default target group
$defaultTgArn = aws elbv2 describe-listeners --load-balancer-arn <alb-arn> --query "Listeners[0].DefaultActions[0].TargetGroupArn" --output text
aws elbv2 describe-target-health --target-group-arn $defaultTgArn

# Test specific rule
aws elbv2 describe-rules --listener-arn $listenerArn --query "Rules[?Conditions[?Type=='path-pattern' && Values[0]=='/api/v1/auth/*']]"
```

## Expected Behavior

Once routing is fixed:
- `/api/v1/auth/health` → Should return 200 OK from auth service
- `/admin` → Should return 200 OK from admin service
- All other service paths → Should route to respective services



