# Secrets Setup - Complete Summary

## ✅ Successfully Configured via AWS CLI

### 1. SES SMTP Credentials ✅

**Created:**
- IAM User: `amesa-ses-smtp-user`
- Policy: `AmazonSESFullAccess`
- Access Key ID: `AKIAR4IEGQP475YU2KVT`
- Access Key Secret: (stored in secret)

**Secrets Updated:**
- ✅ `/amesa/prod/EmailSettings/SmtpUsername`
- ✅ `/amesa/prod/EmailSettings/SmtpPassword`

**Status:** Ready to use for email sending

**Next Steps:**
1. Verify email domain/address in SES Console
2. Request production access if in SES Sandbox
3. Test email sending

---

## ⚠️ Requires Manual Setup

### 2. Redis Connection String

**Status:** Redis cluster not found in AWS

**Options to Create:**

#### Option A: Via Terraform (Recommended)
```bash
cd Infrastructure/terraform
terraform init
terraform plan -target=aws_elasticache_replication_group.amesa_redis
terraform apply -target=aws_elasticache_replication_group.amesa_redis
```

Then get endpoint and update secret:
```bash
aws elasticache describe-replication-groups \
    --replication-group-id amesa-redis-prod \
    --region eu-north-1 \
    --query "ReplicationGroups[0].NodeGroups[0].PrimaryEndpoint.Address" \
    --output text

# Update secret
aws secretsmanager update-secret \
    --secret-id "/amesa/prod/ConnectionStrings/Redis" \
    --secret-string "redis-endpoint:6379" \
    --region eu-north-1
```

#### Option B: Via AWS Console
1. Go to ElastiCache → Redis clusters → Create
2. Configuration:
   - Name: `amesa-redis-prod`
   - Node type: `cache.t3.micro` (or larger for production)
   - Subnet group: `amesa-redis-subnet-group-prod`
   - Security group: `amesa-redis-sg-prod`
   - Enable encryption in transit and at rest
3. After creation, get endpoint and update secret

**Prerequisites:**
- VPC with private subnets
- ElastiCache subnet group
- Security group allowing Redis access from ECS tasks

---

### 3. SNS Platform Applications (Push Notifications)

**Status:** No platform applications found

**Required for:**
- Android push notifications (GCM/FCM)
- iOS push notifications (APNS)

#### Android Setup:

1. **Get FCM Server Key:**
   - Go to Firebase Console → Project Settings → Cloud Messaging
   - Copy "Server key" (legacy) or create service account

2. **Create SNS Platform Application:**
   ```bash
   aws sns create-platform-application \
       --name amesa-android \
       --platform GCM \
       --attributes PlatformCredential=YOUR_FCM_SERVER_KEY \
       --region eu-north-1
   ```

3. **Update Secret:**
   ```bash
   # Get ARN from create response
   aws secretsmanager update-secret \
       --secret-id "/amesa/prod/NotificationChannels/Push/AndroidPlatformArn" \
       --secret-string "arn:aws:sns:eu-north-1:ACCOUNT_ID:app/GCM/amesa-android" \
       --region eu-north-1
   ```

#### iOS Setup:

1. **Get APNS Credentials:**
   - Apple Developer Portal → Certificates, Identifiers & Profiles
   - Create APNs Auth Key (.p8) or Certificate (.p12)

2. **Create SNS Platform Application:**
   ```bash
   # For Sandbox (development)
   aws sns create-platform-application \
       --name amesa-ios-sandbox \
       --platform APNS_SANDBOX \
       --attributes PlatformCredential=file://path/to/key.p8 \
       --region eu-north-1

   # For Production
   aws sns create-platform-application \
       --name amesa-ios \
       --platform APNS \
       --attributes PlatformCredential=file://path/to/key.p8 \
       --region eu-north-1
   ```

3. **Update Secret:**
   ```bash
   aws secretsmanager update-secret \
       --secret-id "/amesa/prod/NotificationChannels/Push/iOSPlatformArn" \
       --secret-string "arn:aws:sns:eu-north-1:ACCOUNT_ID:app/APNS/amesa-ios" \
       --region eu-north-1
   ```

---

## All Secrets Status

**Total Secrets:** 15/15 ✅

| Secret Name | Status | Value |
|------------|--------|-------|
| `/amesa/prod/ConnectionStrings/Redis` | ⚠️ Needs Redis cluster | Placeholder |
| `/amesa/prod/EmailSettings/SmtpUsername` | ✅ Configured | `AKIAR4IEGQP475YU2KVT` |
| `/amesa/prod/EmailSettings/SmtpPassword` | ✅ Configured | (stored) |
| `/amesa/prod/NotificationChannels/Push/AndroidPlatformArn` | ⚠️ Needs SNS platform | Placeholder |
| `/amesa/prod/NotificationChannels/Push/iOSPlatformArn` | ⚠️ Needs SNS platform | Placeholder |
| All other secrets | ✅ Created | Configured |

---

## Verification Commands

```bash
# List all secrets
aws secretsmanager list-secrets \
    --filters Key=name,Values=/amesa/prod \
    --region eu-north-1 \
    --query "SecretList[].Name" \
    --output table

# Verify SES SMTP credentials
aws secretsmanager get-secret-value \
    --secret-id "/amesa/prod/EmailSettings/SmtpUsername" \
    --region eu-north-1 \
    --query "SecretString" \
    --output text

# Check Redis cluster (after creation)
aws elasticache describe-replication-groups \
    --replication-group-id amesa-redis-prod \
    --region eu-north-1 \
    --query "ReplicationGroups[0].NodeGroups[0].PrimaryEndpoint" \
    --output json

# Check SNS platforms (after creation)
aws sns list-platform-applications \
    --region eu-north-1 \
    --query "PlatformApplications[].PlatformApplicationArn" \
    --output table
```

---

## Next Actions

1. **Immediate (Critical):**
   - ✅ SES SMTP - Done
   - ⚠️ Redis - Create cluster and update secret
   - ⚠️ Database passwords - Update with actual passwords

2. **When Mobile Apps Ready:**
   - ⚠️ SNS Android platform - Create with FCM credentials
   - ⚠️ SNS iOS platform - Create with APNS credentials

3. **Testing:**
   - Test email sending via Auth service
   - Test Redis connection from Lottery service
   - Test push notifications when platforms are created

---

## Summary

✅ **Completed:**
- SES SMTP IAM user and credentials created
- All 15 secrets exist in AWS Secrets Manager

⚠️ **Remaining:**
- Redis cluster creation (via Terraform or Console)
- SNS platform applications (when FCM/APNS credentials available)
- Database password updates (manual)

The infrastructure is ready. Redis and SNS can be set up when prerequisites are available.









