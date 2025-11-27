# AWS Rekognition IAM Permissions Setup Guide

**Purpose**: Add Rekognition permissions to ECS task role for ID verification feature  
**Task Role**: `ecsTaskExecutionRole` (verify with `verify-task-role.ps1` script)  
**Account ID**: `129394705401`  
**Region**: `eu-north-1`

**Note**: Based on the task definition, the role is `ecsTaskExecutionRole`. Run `verify-task-role.ps1` to confirm the actual role used by your ECS service.

---

## Quick Setup (Automated)

### Option 1: PowerShell Script (Windows)

```powershell
cd BE/Infrastructure
.\add-rekognition-permissions.ps1
```

### Option 2: Bash Script (Linux/Mac)

```bash
cd BE/Infrastructure
chmod +x add-rekognition-permissions.sh
./add-rekognition-permissions.sh
```

---

## Manual Setup (AWS Console)

### Step 1: Navigate to IAM

1. Go to **AWS Console** → **IAM** → **Roles**
2. Search for: `ecsTaskRole`
3. Click on the role

### Step 2: Add Inline Policy

1. Click **"Add permissions"** → **"Create inline policy"**
2. Click **"JSON"** tab
3. Paste the following policy:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "rekognition:DetectFaces",
        "rekognition:CompareFaces",
        "rekognition:DetectText"
      ],
      "Resource": "*"
    }
  ]
}
```

4. Click **"Next"**
5. Policy name: `RekognitionIDVerificationPolicy`
6. Click **"Create policy"**

---

## Manual Setup (AWS CLI)

### Step 1: Create Policy JSON File

Create file `rekognition-policy.json`:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "rekognition:DetectFaces",
        "rekognition:CompareFaces",
        "rekognition:DetectText"
      ],
      "Resource": "*"
    }
  ]
}
```

### Step 2: Attach Policy to Role

```bash
aws iam put-role-policy \
  --role-name ecsTaskRole \
  --policy-name RekognitionIDVerificationPolicy \
  --policy-document file://rekognition-policy.json \
  --region eu-north-1
```

### Step 3: Verify Policy

```bash
aws iam get-role-policy \
  --role-name ecsTaskRole \
  --policy-name RekognitionIDVerificationPolicy \
  --region eu-north-1
```

---

## Verification

### Check Policy Attached

```bash
aws iam list-role-policies --role-name ecsTaskRole --region eu-north-1
```

**Expected Output**:
```json
{
  "PolicyNames": [
    "RekognitionIDVerificationPolicy",
    ...other policies...
  ]
}
```

### View Policy Document

```bash
aws iam get-role-policy \
  --role-name ecsTaskRole \
  --policy-name RekognitionIDVerificationPolicy \
  --region eu-north-1
```

---

## Permissions Explained

### Required Actions

- **`rekognition:DetectFaces`**: Detects faces in images (for liveness detection)
- **`rekognition:CompareFaces`**: Compares faces between ID photo and selfie
- **`rekognition:DetectText`**: Extracts text from ID documents (OCR, optional)

### Resource Scope

- **`"Resource": "*"`**: Allows access to all Rekognition resources
- Rekognition is a regional service, so this is safe and standard practice

---

## Testing After Setup

### 1. Deploy Backend

After adding permissions, deploy the `amesa-auth-service`:

```bash
cd BE
# Build and push Docker image
docker build -f AmesaBackend.Auth/Dockerfile -t 129394705401.dkr.ecr.eu-north-1.amazonaws.com/amesa-auth-service:latest .
aws ecr get-login-password --region eu-north-1 | docker login --username AWS --password-stdin 129394705401.dkr.ecr.eu-north-1.amazonaws.com
docker push 129394705401.dkr.ecr.eu-north-1.amazonaws.com/amesa-auth-service:latest

# Force ECS service update
aws ecs update-service --cluster Amesa --service amesa-auth-service --force-new-deployment --region eu-north-1
```

### 2. Test Verification Endpoint

After deployment, test the verification endpoint:

```bash
# Test with a sample request (will fail without proper images, but should not fail with IAM error)
curl -X POST https://amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com/api/v1/auth/identity/verify \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "idFrontImage": "base64_encoded_image",
    "selfieImage": "base64_encoded_image",
    "documentType": "id_card"
  }'
```

**Expected Behavior**:
- ✅ If IAM permissions correct: Should process request (may fail validation, but not IAM error)
- ❌ If IAM permissions missing: Will return `AccessDenied` error from Rekognition

### 3. Check ECS Logs

Check CloudWatch logs for `amesa-auth-service`:

```bash
aws logs tail /ecs/amesa-auth-service --follow --region eu-north-1
```

Look for:
- ✅ Rekognition API calls succeeding
- ❌ `AccessDenied` or `UnauthorizedOperation` errors

---

## Troubleshooting

### Issue: AccessDenied Error

**Symptoms**:
- Verification requests fail with `AccessDenied` error
- CloudWatch logs show Rekognition API errors

**Solution**:
1. Verify policy is attached: `aws iam list-role-policies --role-name ecsTaskRole`
2. Verify policy content: `aws iam get-role-policy --role-name ecsTaskRole --policy-name RekognitionIDVerificationPolicy`
3. Check task role ARN in ECS task definition matches `ecsTaskRole`
4. Ensure ECS task was restarted after policy was added

### Issue: Policy Not Found

**Symptoms**:
- Script reports policy doesn't exist
- Manual check shows no policy

**Solution**:
1. Verify role name is correct: `ecsTaskRole`
2. Check you have IAM permissions to manage policies
3. Try creating policy manually via AWS Console

### Issue: Wrong Account/Region

**Symptoms**:
- Script fails with account mismatch
- Cannot find role

**Solution**:
1. Verify AWS credentials: `aws sts get-caller-identity`
2. Verify region: `aws configure get region`
3. Ensure you're using the correct AWS account (129394705401)

---

## Security Notes

### Best Practices

✅ **DO**:
- Use IAM roles (not access keys) for ECS tasks
- Grant minimum required permissions
- Use resource-specific policies when possible
- Monitor Rekognition API usage and costs

❌ **DON'T**:
- Use root account credentials
- Grant broader permissions than needed
- Share IAM credentials
- Forget to monitor costs

### Cost Monitoring

Rekognition pricing:
- **DetectFaces**: ~$1.00 per 1,000 images
- **CompareFaces**: ~$0.001 per comparison
- **DetectText**: ~$1.50 per 1,000 images

Set up CloudWatch alarms to monitor:
- Rekognition API call volume
- Estimated costs
- Error rates

---

## Related Documentation

- **Implementation Summary**: `MetaData/Documentation/ID_VERIFICATION_IMPLEMENTATION.md`
- **Deployment Checklist**: `MetaData/Documentation/ID_VERIFICATION_DEPLOYMENT_CHECKLIST.md`
- **AWS Rekognition Docs**: https://docs.aws.amazon.com/rekognition/

---

**Last Updated**: 2025-01-XX  
**Status**: Ready for execution

