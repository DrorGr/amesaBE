# Infrastructure Changes - Execution Summary

## ✅ Successfully Completed

### 1. ALB Routing Rules ✅
- ✅ `/api/v1/draws/*` → `amesa-lottery-service-tg` (Priority 8)
- ✅ `/api/v1/devices/*` → `amesa-notification-service-tg` (Priority 9)

### 2. ECS Task Definitions ✅
- ✅ **Lottery Service**: Registered as Revision 6
- ✅ **Payment Service**: Registered as Revision 9
- ✅ **Notification Service**: Registered as Revision 5

### 3. Database Migration Scripts ✅
- ✅ Fixed column names (`HouseId`, `TicketNumber` - PascalCase)
- ✅ Removed `\c` commands (psql meta-commands)
- ✅ Created separate migration files for easier execution

### 4. AWS Secrets (12/15 created) ✅
- ✅ `/amesa/prod/ServiceAuth/ApiKey`
- ✅ `/amesa/prod/ServiceAuth/IpWhitelist`
- ✅ `/amesa/prod/ConnectionStrings/Lottery`
- ✅ `/amesa/prod/ConnectionStrings/Payment`
- ✅ `/amesa/prod/ConnectionStrings/Notification`
- ✅ `/amesa/prod/ConnectionStrings/Redis`
- ✅ `/amesa/prod/EmailSettings/SmtpHost`
- ✅ `/amesa/prod/EmailSettings/SmtpPort`
- ✅ `/amesa/prod/EmailSettings/SmtpUsername`
- ✅ `/amesa/prod/EmailSettings/SmtpPassword`
- ✅ `/amesa/prod/NotificationChannels/Push/AndroidPlatformArn`
- ✅ `/amesa/prod/NotificationChannels/Push/iOSPlatformArn`

---

## ⚠️ Remaining Manual Steps

### 1. Create 3 Service URL Secrets

**Issue**: AWS CLI interprets URLs starting with `http://` as file:// URLs

**Solution**: Use these exact commands (copy-paste):

```bash
aws secretsmanager create-secret --name "/amesa/prod/Services/AuthService/Url" --description "Internal URL for AuthService" --secret-string "http://auth-service.amesa.local:8080" --region eu-north-1

aws secretsmanager create-secret --name "/amesa/prod/Services/LotteryService/Url" --description "Internal URL for LotteryService" --secret-string "http://lottery-service.amesa.local:8080" --region eu-north-1

aws secretsmanager create-secret --name "/amesa/prod/Services/PaymentService/Url" --description "Internal URL for PaymentService" --secret-string "http://payment-service.amesa.local:8080" --region eu-north-1
```

**OR** create them via AWS Console:
1. Go to AWS Secrets Manager
2. Create secret with name `/amesa/prod/Services/AuthService/Url`
3. Value: `http://auth-service.amesa.local:8080`
4. Repeat for LotteryService and PaymentService

---

### 2. Update ECS Services to Use New Task Definitions

**New Task Definition Revisions**:
- Lottery Service: **Revision 6**
- Payment Service: **Revision 9**
- Notification Service: **Revision 5**

**Commands to Update Services**:
```bash
aws ecs update-service \
    --cluster amesa-microservices-cluster \
    --service amesa-lottery-service \
    --task-definition amesa-lottery-service:6 \
    --region eu-north-1

aws ecs update-service \
    --cluster amesa-microservices-cluster \
    --service amesa-payment-service \
    --task-definition amesa-payment-service:9 \
    --region eu-north-1

aws ecs update-service \
    --cluster amesa-microservices-cluster \
    --service amesa-notification-service \
    --task-definition amesa-notification-service:5 \
    --region eu-north-1
```

---

### 3. Run Database Migrations

**Lottery Service**:
```bash
psql -h YOUR_DB_HOST -U postgres -d amesa_lottery_db -f Infrastructure/sql/migrations/run_migrations_separate.sql
```

**Notification Service**:
```bash
psql -h YOUR_DB_HOST -U postgres -d amesa_notification_db -f Infrastructure/sql/migrations/run_notification_migrations.sql
```

---

### 4. Update Secret Values

**Secrets that need actual values** (currently have placeholders):
- `/amesa/prod/ConnectionStrings/Lottery` - Update with actual DB host/password
- `/amesa/prod/ConnectionStrings/Payment` - Update with actual DB host/password
- `/amesa/prod/ConnectionStrings/Notification` - Update with actual DB host/password
- `/amesa/prod/ConnectionStrings/Redis` - Update with actual Redis endpoint
- `/amesa/prod/EmailSettings/SmtpUsername` - Update with SES username
- `/amesa/prod/EmailSettings/SmtpPassword` - Update with SES password
- `/amesa/prod/NotificationChannels/Push/AndroidPlatformArn` - Update with actual ARN
- `/amesa/prod/NotificationChannels/Push/iOSPlatformArn` - Update with actual ARN

**Update Command**:
```bash
aws secretsmanager update-secret \
    --secret-id "/amesa/prod/ConnectionStrings/Lottery" \
    --secret-string "Host=ACTUAL_HOST;Port=5432;Database=amesa_lottery_db;Username=postgres;Password=ACTUAL_PASSWORD;SearchPath=amesa_lottery;" \
    --region eu-north-1
```

---

## Summary

### ✅ Completed (via AWS CLI):
- 2 ALB routing rules added
- 3 ECS task definitions registered
- 12 AWS secrets created
- Database migration scripts fixed

### ⚠️ Manual Steps Required:
- Create 3 service URL secrets (AWS CLI URL parsing issue)
- Update ECS services to use new task definitions
- Run database migrations
- Update placeholder secret values with actual credentials

---

## Verification Commands

```bash
# Verify all secrets exist
aws secretsmanager list-secrets --filters Key=name,Values=/amesa/prod --region eu-north-1 --query "SecretList[].Name" --output table

# Verify ALB routes
aws elbv2 describe-rules --listener-arn arn:aws:elasticloadbalancing:eu-north-1:129394705401:listener/app/amesa-backend-alb/d4dbb08b12e385fe/c0013fb81f884976 --region eu-north-1 --query "Rules[?Priority=='8' || Priority=='9'].{Path:Conditions[0].Values[0],Priority:Priority}" --output table

# Verify task definitions
aws ecs describe-task-definition --task-definition amesa-lottery-service:6 --region eu-north-1 --query "taskDefinition.containerDefinitions[0].secrets[*].name" --output json
```
