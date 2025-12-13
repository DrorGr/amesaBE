# AWS Access Key Setup Guide for Amesa Deployment

## Required AWS Access Key Type

You need an **IAM User Access Key** (not root account keys) with specific permissions for:

- **S3** (for uploading frontend files)
- **CloudFront** (for cache invalidation)
- **ECR** (for backend Docker images)
- **ECS** (for backend deployment)

---

## Step-by-Step: Create IAM User with Proper Permissions

### Step 1: Create IAM User

1. **Go to AWS Console** → **IAM** → **Users**
2. **Click "Create user"**
3. **User name**: `amesa-deployment-user`
4. **Select**: "Provide user access to the AWS Management Console" (optional)
5. **Click "Next"**

### Step 2: Attach Policies

**Attach these AWS managed policies:**

1. **AmazonS3FullAccess** (for S3 bucket operations)
2. **CloudFrontFullAccess** (for CloudFront cache invalidation)
3. **AmazonEC2ContainerRegistryFullAccess** (for ECR Docker images)
4. **AmazonECS_FullAccess** (for ECS deployment)

### Step 3: Create Access Keys

1. **Select the user** → **Security credentials** tab
2. **Click "Create access key"**
3. **Use case**: "Command Line Interface (CLI)"
4. **Check "I understand..."** → **Next**
5. **Description**: "Amesa deployment keys"
6. **Click "Create access key"**

### Step 4: Save Your Keys

**IMPORTANT: Save these keys immediately!**

```
Access Key ID: AKIA... (starts with AKIA)
Secret Access Key: ... (long random string)
```

**These are the values you'll use in GitHub Secrets:**
- `AWS_ACCESS_KEY_ID = AKIA...`
- `AWS_SECRET_ACCESS_KEY = ...`

---

## Security Best Practices

### DO:
- Use IAM user (not root account)
- Attach minimal required permissions
- Rotate keys regularly
- Use different keys for different environments

### DON'T:
- Use root account access keys
- Share keys in code or documentation
- Use overly broad permissions
- Forget to rotate keys

---

## Region Configuration

Your AWS resources are in **eu-north-1** region, so make sure your access keys work in this region.

---

## Test Your Access Keys

After creating the keys, test them:

```bash
# Configure AWS CLI with your new keys
aws configure

# Test S3 access
aws s3 ls s3://amesa-frontend-dev --region eu-north-1

# Test CloudFront access
aws cloudfront list-distributions --region eu-north-1
```

---

## GitHub Secrets Configuration

Once you have your access keys, add them to GitHub:

**Go to**: https://github.com/DrorGr/amesaFE/settings/secrets/actions

**Add these secrets:**
```
AWS_ACCESS_KEY_ID = AKIA... (your access key ID)
AWS_SECRET_ACCESS_KEY = ... (your secret access key)
```

---

## Troubleshooting

### Common Issues:

1. **"Access Denied" errors**
   - Check IAM permissions
   - Verify region (eu-north-1)
   - Ensure policies are attached

2. **"Invalid credentials"**
   - Double-check key format
   - Ensure no extra spaces in secrets
   - Verify keys are active

3. **"Region mismatch"**
   - All resources are in eu-north-1
   - Ensure your keys work in this region

---

## Quick Summary

**You need:**
1. **IAM User** (not root account)
2. **Access Key ID** (starts with AKIA)
3. **Secret Access Key** (long random string)
4. **Permissions** for S3, CloudFront, ECR, ECS
5. **Region**: eu-north-1

**Add to GitHub Secrets:**
- `AWS_ACCESS_KEY_ID`
- `AWS_SECRET_ACCESS_KEY`

That's it! Your deployment system will then have the permissions it needs to deploy to AWS.
