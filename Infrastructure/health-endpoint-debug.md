# Health Endpoint Debugging

## Current Status
- **Error**: `Target.FailedHealthChecks`
- **Meaning**: ALB can reach the target, but health check is failing
- **Expected**: HTTP 200 response from `/health` endpoint

## Health Check Configuration

### Target Group Settings
- **Path**: `/health`
- **Protocol**: HTTP
- **Port**: 8080 (traffic-port)
- **Timeout**: 15s
- **Interval**: 30s
- **Matcher**: HTTP 200
- **Healthy Threshold**: 2
- **Unhealthy Threshold**: 5

### Service Configuration
- **Health Endpoint**: `app.MapHealthChecks("/health")`
- **Health Checks**: `builder.Services.AddHealthChecks()`
- **Binding**: `http://+:8080` (all interfaces)

## Possible Issues

### 1. Health Endpoint Response Format
ASP.NET Core health checks return:
- **JSON** by default: `{"status":"Healthy"}`
- **Plain text** if configured: `Healthy`

ALB expects HTTP 200 status code, which should work with either format.

### 2. Health Check Not Ready
If the service takes time to start, health checks might fail initially.

### 3. Database Dependency
If health checks include database checks and the database is slow/unavailable, health checks will fail.

### 4. Response Time
If health endpoint takes longer than 15s to respond, it will timeout.

## Debugging Steps

### Check Service Logs
```powershell
aws logs tail /ecs/amesa-auth-service --region eu-north-1 --follow
```

Look for:
- Health endpoint requests: `GET /health`
- Response status: Should be `200`
- Response time: Should be < 15s
- Any errors during health check

### Test Health Endpoint Directly
If you have access to the VPC:
```bash
# Get task IP
TASK_IP=$(aws ecs list-tasks --cluster Amesa --service-name amesa-auth-service --query "taskArns[0]" --output text | xargs -I {} aws ecs describe-tasks --cluster Amesa --tasks {} --query "tasks[0].attachments[0].details[?name=='privateIPv4Address'].value" --output text)

# Test health endpoint
curl -v http://$TASK_IP:8080/health
```

Expected response:
```
HTTP/1.1 200 OK
Content-Type: application/json

{"status":"Healthy"}
```

### Check Health Check Details
```powershell
$tgArn = aws elbv2 describe-target-groups --region eu-north-1 --names amesa-auth-service-tg --query "TargetGroups[0].TargetGroupArn" --output text
aws elbv2 describe-target-health --region eu-north-1 --target-group-arn $tgArn
```

Look for:
- `TargetHealth.State`: Should be "healthy"
- `TargetHealth.Reason`: If unhealthy, shows why
- `TargetHealth.Description`: Additional details

## Solutions

### If Health Endpoint Returns Non-200
- Check service logs for errors
- Verify database connection (if health check includes DB)
- Check if service is fully started

### If Health Endpoint Times Out
- Increase health check timeout (currently 15s)
- Check if service is overloaded
- Verify service is listening on correct port

### If Health Endpoint Not Found (404)
- Verify route is configured: `app.MapHealthChecks("/health")`
- Check if middleware order is correct
- Verify service is binding to `0.0.0.0:8080`

## Next Steps

1. Monitor logs for health endpoint requests
2. Verify health endpoint returns HTTP 200
3. Check response time is < 15s
4. If still failing, increase timeout or check for database dependencies

