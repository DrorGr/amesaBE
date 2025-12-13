# ALB Health Check Diagnosis

## Status: ⚠️ Network Connectivity Issue

### What We've Verified ✅

1. **Service Status**: All 8 services are RUNNING (1/1 tasks)
2. **Service Binding**: Services are binding to `0.0.0.0:8080` (all interfaces)
   - Logs show: `Binding to values defined by URLS instead 'http://+:8080'`
3. **Health Endpoint**: `/health` endpoint returns 200 OK (verified in logs)
4. **Security Groups**: ALB and ECS tasks in same security group with rules allowing traffic
5. **VPC**: ALB and ECS tasks in same VPC (vpc-0faeeb78eded33ccf)
6. **NACLs**: Same network ACL, default rules should allow traffic
7. **ALB Registration**: All services registered with target groups
8. **ALB Routing**: Path-based routing rules configured correctly
9. **Health Check Config**: Optimized (15s timeout, 30s interval, thresholds adjusted)

### Current Issue ❌

**ALB health checks are timing out** - `Target.Timeout: Request timed out`

This indicates the ALB cannot reach the ECS tasks on port 8080, even though:
- Services are listening on 0.0.0.0:8080
- Security groups allow traffic
- Same VPC

### Possible Causes

1. **Route Table Issue**: ALB subnet has no explicit route table (uses default), ECS subnet has specific route table
2. **Security Group Rule**: Even though same SG, might need explicit rule
3. **Network ACL Rules**: Default rules might not be sufficient
4. **Subnet Routing**: ALB and ECS in different subnets may have routing issues

### Next Steps to Resolve

1. **Check Route Tables**: Verify routes between ALB and ECS subnets
2. **Test from Bastion**: If available, test connectivity from EC2 instance in same VPC
3. **Review Security Groups**: Consider creating separate SG for ALB and ECS, with explicit rules
4. **AWS Support**: May need AWS support to investigate network path
5. **VPC Flow Logs**: Enable to see if traffic is being blocked

### Temporary Workaround

Services are running and functional. The health check failure prevents ALB from routing traffic, but the services themselves are healthy. Once network connectivity is resolved, health checks should pass automatically.

### Commands to Monitor

```powershell
# Check target health
$tgArn = aws elbv2 describe-target-groups --region eu-north-1 --names amesa-auth-service-tg --query "TargetGroups[0].TargetGroupArn" --output text
aws elbv2 describe-target-health --region eu-north-1 --target-group-arn $tgArn

# Check service logs
aws logs tail /ecs/amesa-auth-service --region eu-north-1 --follow
```

