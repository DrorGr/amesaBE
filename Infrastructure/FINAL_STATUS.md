# Infrastructure Changes - Final Status

## ✅ Successfully Completed via AWS CLI

### 1. ALB Routing Rules ✅
- ✅ `/api/v1/draws/*` → `amesa-lottery-service-tg` (Priority 8)
  - Rule ARN: `arn:aws:elasticloadbalancing:eu-north-1:129394705401:listener-rule/app/amesa-backend-alb/d4dbb08b12e385fe/c0013fb81f884976/f72b53ff4106bf97`
- ✅ `/api/v1/devices/*` → `amesa-notification-service-tg` (Priority 9)
  - Rule ARN: `arn:aws:elasticloadbalancing:eu-north-1:129394705401:listener-rule/app/amesa-backend-alb/d4dbb08b12e385fe/c0013fb81f884976/a02a6b730959fdaf`

### 2. ECS Task Definitions ✅
- ✅ **Lottery Service**: Registered as **Revision 6**
  - Added secrets: `ServiceAuth__ApiKey`, `ServiceAuth__IpWhitelist`, `PaymentService__BaseUrl`
- ✅ **Payment Service**: Registered as **Revision 9**
  - Added secret: `SERVICE_AUTH_API_KEY`
- ✅ **Notification Service**: Registered as **Revision 5**
  - Added secrets: Email settings, service URLs, push platform ARNs

### 3. AWS Secrets Manager ✅
**Total Created**: 12 out of 15 (3 service URLs need manual creation due to AWS CLI URL parsing)

**Created Secrets**:
1. ✅ `/amesa/prod/ServiceAuth/ApiKey`
2. ✅ `/amesa/prod/ServiceAuth/IpWhitelist`
3. ✅ `/amesa/prod/ConnectionStrings/Lottery`
4. ✅ `/amesa/prod/ConnectionStrings/Payment`
5. ✅ `/amesa/prod/ConnectionStrings/Notification`
6. ✅ `/amesa/prod/ConnectionStrings/Redis`
7. ✅ `/amesa/prod/EmailSettings/SmtpHost`
8. ✅ `/amesa/prod/EmailSettings/SmtpPort`
9. ✅ `/amesa/prod/EmailSettings/SmtpUsername`
10. ✅ `/amesa/prod/EmailSettings/SmtpPassword`
11. ✅ `/amesa/prod/NotificationChannels/Push/AndroidPlatformArn`
12. ✅ `/amesa/prod/NotificationChannels/Push/iOSPlatformArn`

**Missing (need manual creation)**:
- ❌ `/amesa/prod/Services/AuthService/Url`
- ❌ `/amesa/prod/Services/LotteryService/Url`
- ❌ `/amesa/prod/Services/PaymentService/Url`

**Reason**: AWS CLI on Windows interprets `http://` URLs as file:// URLs

**Solution**: Create via AWS Console or use this PowerShell workaround:
```powershell
$urls = @{
    "AuthService" = "http://auth-service.amesa.local:8080"
    "LotteryService" = "http://lottery-service.amesa.local:8080"
    "PaymentService" = "http://payment-service.amesa.local:8080"
}
foreach ($service in $urls.Keys) {
    $url = $urls[$service]
    aws secretsmanager create-secret `
        --name "/amesa/prod/Services/$service/Url" `
        --description "Internal URL for $service" `
        --secret-string $url `
        --region eu-north-1
}
```

### 4. Database Migration Scripts ✅
- ✅ Fixed column names: `HouseId`, `TicketNumber` (PascalCase)
- ✅ Removed `\c` commands (psql meta-commands)
- ✅ Created separate files for easier execution

---

## ⚠️ Remaining Manual Steps

### 1. Create 3 Service URL Secrets
**Use AWS Console or the PowerShell script above**

### 2. Update ECS Services
```bash
aws ecs update-service --cluster amesa-microservices-cluster --service amesa-lottery-service --task-definition amesa-lottery-service:6 --region eu-north-1

aws ecs update-service --cluster amesa-microservices-cluster --service amesa-payment-service --task-definition amesa-payment-service:9 --region eu-north-1

aws ecs update-service --cluster amesa-microservices-cluster --service amesa-notification-service --task-definition amesa-notification-service:5 --region eu-north-1
```

### 3. Run Database Migrations
```bash
# Lottery
psql -h YOUR_DB_HOST -U postgres -d amesa_lottery_db -f Infrastructure/sql/migrations/run_migrations_separate.sql

# Notification
psql -h YOUR_DB_HOST -U postgres -d amesa_notification_db -f Infrastructure/sql/migrations/run_notification_migrations.sql
```

### 4. Update Secret Values
Update placeholder values in secrets with actual:
- Database connection strings
- Redis endpoint
- Email SMTP credentials
- Push notification platform ARNs

---

## Summary

**Completed**: ✅
- 2 ALB routes added
- 3 ECS task definitions registered
- 12 secrets created
- Migration scripts fixed

**Remaining**: ⚠️
- 3 service URL secrets (manual creation needed)
- Update ECS services
- Run database migrations
- Update secret placeholder values
