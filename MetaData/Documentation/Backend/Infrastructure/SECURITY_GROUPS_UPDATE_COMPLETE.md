# Security Groups Update Complete

## ✅ Actions Completed

### 1. Created Separate Security Groups
- **ALB Security Group**: `amesa-alb-sg`
  - Allows HTTP (port 80) from internet
  - Allows HTTPS (port 443) from internet
  
- **ECS Security Group**: `amesa-ecs-sg`
  - Allows port 8080 from ALB security group only

### 2. Updated ECS Services
All 8 ECS services have been updated to use the new ECS security group:
- amesa-auth-service
- amesa-content-service
- amesa-notification-service
- amesa-payment-service
- amesa-lottery-service
- amesa-lottery-results-service
- amesa-analytics-service
- amesa-admin-service

**Note**: Services are being redeployed with the new security group configuration.

### 3. Enabled ALB Access Logs
- Created S3 bucket: `amesa-alb-access-logs-129394705401`
- Set bucket policy for ALB log delivery
- Enabled access logs on ALB

## 📋 Next Steps

### Immediate
1. **Wait for ECS services to redeploy** (2-3 minutes)
   - Check service status: `aws ecs describe-services --cluster Amesa --services <service-name>`
   - Verify tasks are running with new security group

2. **Update ALB Security Group** (Manual step required)
   - ALB security group cannot be changed via CLI easily
   - Use AWS Console: EC2 → Load Balancers → Select ALB → Security tab
   - Change security group to: `amesa-alb-sg`

3. **Monitor Health Checks**
   - Wait 2-3 minutes after ALB security group update
   - Check target health: `aws elbv2 describe-target-health --target-group-arn <tg-arn>`
   - Health checks should now pass

### Verification
4. **Check ALB Access Logs** (after logs start appearing)
   - Logs will appear in S3 bucket: `amesa-alb-access-logs-129394705401`
   - Look for health check requests to `/health` endpoint
   - Verify requests are reaching ECS tasks

5. **Test Endpoints**
   - Once health checks pass, test:
     - Auth API: `http://amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com/api/v1/auth/health`
     - Admin Panel: `http://amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com/admin`

## 🔍 Troubleshooting

### If Health Checks Still Fail
1. Verify ALB security group was updated in AWS Console
2. Check ECS tasks are using new security group:
   ```powershell
   aws ecs describe-tasks --cluster Amesa --tasks <task-arn> --query "tasks[0].attachments[0].details[?name=='groupSet'].value"
   ```
3. Verify security group rules:
   - ALB SG should allow HTTP/HTTPS from internet
   - ECS SG should allow port 8080 from ALB SG
4. Check ALB access logs for health check requests

### If Services Don't Start
1. Check ECS service events for errors
2. Verify security group allows outbound traffic
3. Check CloudWatch logs for startup errors

## 📊 Expected Results

After completing these steps:
- ✅ ALB and ECS use separate, properly configured security groups
- ✅ ALB can reach ECS tasks on port 8080
- ✅ Health checks should pass
- ✅ Services should be accessible via ALB

## Security Group IDs

Get the IDs with:
```powershell
# ALB Security Group
aws ec2 describe-security-groups --region eu-north-1 --filters "Name=group-name,Values=amesa-alb-sg" "Name=vpc-id,Values=vpc-0faeeb78eded33ccf" --query "SecurityGroups[0].GroupId" --output text

# ECS Security Group
aws ec2 describe-security-groups --region eu-north-1 --filters "Name=group-name,Values=amesa-ecs-sg" "Name=vpc-id,Values=vpc-0faeeb78eded33ccf" --query "SecurityGroups[0].GroupId" --output text
```



