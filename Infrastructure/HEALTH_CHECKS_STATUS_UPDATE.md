# Health Checks Status Update

## ✅ Security Groups Updated

### ALB Security Group
- **ID**: `sg-08dbfaaf3cd9e31c8` (amesa-alb-sg)
- **Status**: ✅ Updated in AWS Console
- **Rules**: 
  - HTTP (port 80) from internet
  - HTTPS (port 443) from internet

### ECS Security Group
- **ID**: `sg-0dfd6533a07fde81b` (amesa-ecs-sg)
- **Status**: ✅ All services updated
- **Rules**:
  - Port 8080 from ALB security group only

## 🔄 Services Status

All 8 ECS services have been updated and are redeploying:
- amesa-auth-service
- amesa-content-service
- amesa-notification-service
- amesa-payment-service
- amesa-lottery-service
- amesa-lottery-results-service
- amesa-analytics-service
- amesa-admin-service

**Note**: Some services may show multiple running tasks during deployment. This is normal during the transition.

## ⏳ Health Checks

Health checks are in transition:
- Services are redeploying with new security groups
- ALB needs time to retry health checks with new configuration
- Expected time: 2-3 minutes for health checks to stabilize

### Current Status
- Health checks may show: `initial`, `draining`, `unhealthy`, or `healthy`
- This is expected during the transition period
- Wait 2-3 minutes for health checks to retry

## 📋 Next Steps

### 1. Wait for Stabilization (2-3 minutes)
```powershell
# Check health checks again
$services = @("amesa-auth-service","amesa-content-service","amesa-notification-service","amesa-payment-service","amesa-lottery-service","amesa-lottery-results-service","amesa-analytics-service","amesa-admin-service")
foreach($s in $services) {
    $tgName = "$s-tg"
    $tgArn = aws elbv2 describe-target-groups --region eu-north-1 --names $tgName --query "TargetGroups[0].TargetGroupArn" --output text
    $health = aws elbv2 describe-target-health --region eu-north-1 --target-group-arn $tgArn --query "TargetHealthDescriptions[0].TargetHealth.State" --output text
    Write-Host "$s : $health"
}
```

### 2. Test Endpoints (Once Health Checks Pass)
- **Auth API**: `http://amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com/api/v1/auth/health`
- **Admin Panel**: `http://amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com/admin`

### 3. Verify All Services
Once health checks are healthy, all services should be accessible via ALB with proper routing.

## 🔍 Troubleshooting

### If Health Checks Still Fail After 3 Minutes
1. **Verify Security Group Rules**:
   ```powershell
   # Check ECS security group allows ALB
   aws ec2 describe-security-groups --group-ids sg-0dfd6533a07fde81b --query "SecurityGroups[0].IpPermissions"
   
   # Check ALB security group
   aws ec2 describe-security-groups --group-ids sg-08dbfaaf3cd9e31c8 --query "SecurityGroups[0].IpPermissions"
   ```

2. **Check ECS Task Security Group**:
   ```powershell
   $taskArn = aws ecs list-tasks --cluster Amesa --service-name amesa-auth-service --query "taskArns[0]" --output text
   aws ecs describe-tasks --cluster Amesa --tasks $taskArn --query "tasks[0].attachments[0].details[?name=='groupSet'].value"
   ```

3. **Check ALB Access Logs** (if enabled):
   - S3 bucket: `amesa-alb-access-logs-129394705401`
   - Look for health check requests to `/health` endpoint

4. **Check Service Logs**:
   ```powershell
   aws logs tail /ecs/amesa-auth-service --region eu-north-1 --follow
   ```
   - Look for health check requests from ALB (not just localhost)

## ✅ Expected Outcome

After security groups are properly configured:
- ✅ ALB can reach ECS tasks on port 8080
- ✅ Health checks should pass
- ✅ Services should be accessible via ALB
- ✅ All endpoints should respond correctly

## 📊 Security Group Configuration

### ALB Security Group (sg-08dbfaaf3cd9e31c8)
```
Inbound:
  - Port 80 (HTTP) from 0.0.0.0/0
  - Port 443 (HTTPS) from 0.0.0.0/0

Outbound:
  - All traffic (default)
```

### ECS Security Group (sg-0dfd6533a07fde81b)
```
Inbound:
  - Port 8080 (TCP) from ALB Security Group (sg-08dbfaaf3cd9e31c8)

Outbound:
  - All traffic (default)
```

This configuration ensures:
- ALB can receive traffic from internet
- ALB can send health checks to ECS tasks
- ECS tasks can only receive traffic from ALB (more secure)

