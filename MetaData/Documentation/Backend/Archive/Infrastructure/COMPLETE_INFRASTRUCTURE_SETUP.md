# Complete Infrastructure Setup - Final Commands

## ✅ Already Completed (via AWS CLI)

### 1. ALB Routing Rules ✅
- ✅ `/api/v1/draws/*` → lottery-service-tg (Priority 8)
- ✅ `/api/v1/devices/*` → notification-service-tg (Priority 9)

### 2. ECS Task Definitions ✅
- ✅ Lottery Service: Revision 6
- ✅ Payment Service: Revision 9
- ✅ Notification Service: Revision 5

### 3. AWS Secrets (12/15) ✅
- ✅ All connection strings, email settings, push ARNs created
- ⚠️ 3 service URLs need manual creation (see below)

---

## ⚠️ Manual Steps Required

### Step 1: Create 3 Service URL Secrets

**Issue**: AWS CLI on Windows interprets `http://` as file:// URLs

**Solution - Use AWS Console**:
1. Go to AWS Secrets Manager Console
2. Click "Store a new secret"
3. For each service:
   - **Secret name**: `/amesa/prod/Services/AuthService/Url`
   - **Secret value**: `http://auth-service.amesa.local:8080`
   - Repeat for `LotteryService` and `PaymentService`

**OR Use this workaround** (create a batch file):
```batch
@echo off
set REGION=eu-north-1
set PREFIX=/amesa/prod

aws secretsmanager create-secret --name "%PREFIX%/Services/AuthService/Url" --description "Internal URL for AuthService" --secret-string "http://auth-service.amesa.local:8080" --region %REGION%

aws secretsmanager create-secret --name "%PREFIX%/Services/LotteryService/Url" --description "Internal URL for LotteryService" --secret-string "http://lottery-service.amesa.local:8080" --region %REGION%

aws secretsmanager create-secret --name "%PREFIX%/Services/PaymentService/Url" --description "Internal URL for PaymentService" --secret-string "http://payment-service.amesa.local:8080" --region %REGION%
```

---

### Step 2: Update ECS Services

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

### Step 3: Run Database Migrations

**Lottery Service**:
```bash
psql -h YOUR_DB_HOST -U postgres -d amesa_lottery_db -f Infrastructure/sql/migrations/run_migrations_separate.sql
```

**Notification Service**:
```bash
psql -h YOUR_DB_HOST -U postgres -d amesa_notification_db -f Infrastructure/sql/migrations/run_notification_migrations.sql
```

---

### Step 4: Update Secret Values

Update these secrets with actual values (currently have placeholders):

```bash
# Update Lottery connection string
aws secretsmanager update-secret \
    --secret-id "/amesa/prod/ConnectionStrings/Lottery" \
    --secret-string "Host=ACTUAL_HOST;Port=5432;Database=amesa_lottery_db;Username=postgres;Password=ACTUAL_PASSWORD;SearchPath=amesa_lottery;" \
    --region eu-north-1

# Update Payment connection string
aws secretsmanager update-secret \
    --secret-id "/amesa/prod/ConnectionStrings/Payment" \
    --secret-string "Host=ACTUAL_HOST;Port=5432;Database=amesa_payment_db;Username=postgres;Password=ACTUAL_PASSWORD;SearchPath=amesa_payment;" \
    --region eu-north-1

# Update Notification connection string
aws secretsmanager update-secret \
    --secret-id "/amesa/prod/ConnectionStrings/Notification" \
    --secret-string "Host=ACTUAL_HOST;Port=5432;Database=amesa_notification_db;Username=postgres;Password=ACTUAL_PASSWORD;SearchPath=amesa_notification;" \
    --region eu-north-1

# Update Redis
aws secretsmanager update-secret \
    --secret-id "/amesa/prod/ConnectionStrings/Redis" \
    --secret-string "redis://your-redis-endpoint.cache.amazonaws.com:6379" \
    --region eu-north-1

# Update Email credentials
aws secretsmanager update-secret \
    --secret-id "/amesa/prod/EmailSettings/SmtpUsername" \
    --secret-string "YOUR_SES_USERNAME" \
    --region eu-north-1

aws secretsmanager update-secret \
    --secret-id "/amesa/prod/EmailSettings/SmtpPassword" \
    --secret-string "YOUR_SES_PASSWORD" \
    --region eu-north-1

# Update Push ARNs
aws secretsmanager update-secret \
    --secret-id "/amesa/prod/NotificationChannels/Push/AndroidPlatformArn" \
    --secret-string "arn:aws:sns:eu-north-1:ACCOUNT_ID:app/GCM/your-android-app" \
    --region eu-north-1

aws secretsmanager update-secret \
    --secret-id "/amesa/prod/NotificationChannels/Push/iOSPlatformArn" \
    --secret-string "arn:aws:sns:eu-north-1:ACCOUNT_ID:app/APNS/your-ios-app" \
    --region eu-north-1
```

---

## Verification

```bash
# Verify all secrets exist (should show 15)
aws secretsmanager list-secrets --filters Key=name,Values=/amesa/prod --region eu-north-1 --query "SecretList[].Name" --output table

# Verify ALB routes
aws elbv2 describe-rules \
    --listener-arn arn:aws:elasticloadbalancing:eu-north-1:129394705401:listener/app/amesa-backend-alb/d4dbb08b12e385fe/c0013fb81f884976 \
    --region eu-north-1 \
    --query "Rules[?Priority=='8' || Priority=='9'].{Path:Conditions[0].Values[0],Priority:Priority}" \
    --output table

# Verify task definitions
aws ecs describe-task-definition --task-definition amesa-lottery-service:6 --region eu-north-1 --query "taskDefinition.containerDefinitions[0].secrets[*].name" --output json
```

---

## Summary

**✅ Completed**:
- 2 ALB routes
- 3 ECS task definitions registered
- 12 secrets created
- Migration scripts fixed

**⚠️ Remaining**:
- 3 service URL secrets (use AWS Console or batch file)
- Update ECS services (3 commands)
- Run database migrations (2 commands)
- Update secret values (8 commands)









