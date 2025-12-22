# 🚀 Next Steps for Complete Deployment

## Immediate Actions Required

### 1. Complete ElastiCache Redis Setup
```bash
# Check Redis cluster status
aws elasticache describe-cache-clusters --cache-cluster-id amesa-redis --region eu-north-1

# Once available, get connection endpoint
aws elasticache describe-cache-clusters --cache-cluster-id amesa-redis --region eu-north-1 --show-cache-node-info --query "CacheClusters[0].CacheNodes[0].Endpoint"
```

**Update services** with Redis connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "Redis": "<redis-endpoint>:6379"
  }
}
```

### 2. Build and Push Docker Images

**Option A: Use CI/CD (Recommended)**
- Push code to trigger GitHub Actions workflows
- Each service has its own workflow in `.github/workflows/`
- Workflows will build and push images automatically

**Option B: Manual Build**
```bash
cd BE/Infrastructure
chmod +x build-and-push-images.sh
./build-and-push-images.sh
```

**Prerequisites:**
- Docker installed and running
- AWS CLI configured
- ECR login permissions

### 3. Configure ALB Routing

```bash
cd BE/Infrastructure
chmod +x configure-alb-routing.sh
./configure-alb-routing.sh
```

This will:
- Create routing rules on existing ALB
- Map paths to target groups:
  - `/api/v1/auth/*` → auth service
  - `/api/v1/payment/*` → payment service
  - `/api/v1/lottery/*` → lottery service
  - `/api/v1/content/*` → content service
  - `/api/v1/notification/*` → notification service
  - `/api/v1/lottery-results/*` → lottery-results service
  - `/api/v1/analytics/*` → analytics service
  - `/admin/*` → admin service

### 4. Create RDS Schemas

Connect to Aurora cluster and create schemas:

```sql
-- Connect to amesadbmain cluster endpoint
CREATE SCHEMA IF NOT EXISTS amesa_auth;
CREATE SCHEMA IF NOT EXISTS amesa_payment;
CREATE SCHEMA IF NOT EXISTS amesa_lottery;
CREATE SCHEMA IF NOT EXISTS amesa_content;
CREATE SCHEMA IF NOT EXISTS amesa_notification;
CREATE SCHEMA IF NOT EXISTS amesa_lottery_results;
CREATE SCHEMA IF NOT EXISTS amesa_analytics;
```

**Update each service's DbContext** to use schema:
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.HasDefaultSchema("amesa_auth"); // Change per service
    // ... rest of configuration
}
```

**Update connection strings** in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=<aurora-endpoint>;Port=5432;Database=<db-name>;Username=<user>;Password=<pass>;SearchPath=amesa_auth;"
  }
}
```

### 5. Run Database Migrations

For each service:
```bash
cd BE/AmesaBackend.Auth
dotnet ef database update

cd ../AmesaBackend.Payment
dotnet ef database update

# ... repeat for all services
```

Or use the automated script:
```bash
cd BE
chmod +x scripts/database-migrations.sh
./scripts/database-migrations.sh
```

### 6. Activate ECS Services

Once Docker images are pushed:

```bash
cd BE/Infrastructure
chmod +x update-ecs-services.sh
./update-ecs-services.sh
```

This updates desired count from 0 to 1 for all services.

### 7. Register ECS Services with Target Groups

For each service, register the service with its target group:

```bash
# Get service ARN
SERVICE_ARN=$(aws ecs describe-services --cluster Amesa --services amesa-auth-service --region eu-north-1 --query "services[0].serviceName" --output text)

# Get target group ARN
TG_ARN=$(aws elbv2 describe-target-groups --names amesa-auth-service-tg --region eu-north-1 --query "TargetGroups[0].TargetGroupArn" --output text)

# Register service with target group (ECS does this automatically when service starts, but verify)
```

### 8. Verify Deployment

1. **Check service health:**
```bash
aws ecs describe-services --cluster Amesa --services amesa-auth-service --region eu-north-1 --query "services[0].[runningCount,desiredCount,status]"
```

2. **Check target group health:**
```bash
aws elbv2 describe-target-health --target-group-arn <tg-arn> --region eu-north-1
```

3. **Test endpoints:**
```bash
# Test health endpoint
curl http://<alb-dns>/api/v1/auth/health

# Test API endpoint
curl http://<alb-dns>/api/v1/auth/api/v1/users/me
```

## Configuration Checklist

- [ ] Redis endpoint configured in all services
- [ ] RDS connection strings updated with schemas
- [ ] Database migrations run for all services
- [ ] Docker images built and pushed
- [ ] ALB routing rules configured
- [ ] ECS services desired count set to 1
- [ ] Target groups healthy
- [ ] Services responding to health checks
- [ ] EventBridge events flowing
- [ ] X-Ray tracing enabled (if configured)

## Monitoring

- **CloudWatch Logs**: Check `/ecs/amesa-*-service` log groups
- **ECS Service Events**: Check service events for deployment status
- **ALB Target Health**: Monitor target group health
- **CloudWatch Metrics**: Monitor service metrics and alarms

## Troubleshooting

1. **Service not starting:**
   - Check ECR image exists
   - Check task definition references correct image
   - Check CloudWatch logs for errors
   - Verify security groups allow traffic

2. **Target group unhealthy:**
   - Verify health check path is correct (`/health`)
   - Check security groups allow ALB → ECS traffic
   - Verify service is listening on port 8080

3. **Database connection issues:**
   - Verify Aurora endpoint is correct
   - Check security groups allow ECS → RDS traffic
   - Verify credentials and schema names

4. **EventBridge not working:**
   - Verify event bus name: `amesa-event-bus`
   - Check IAM permissions for EventBridge
   - Verify event publishers are configured correctly

---

**Status**: Infrastructure is 75% complete. Ready for Docker builds and service activation!

