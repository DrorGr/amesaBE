# 🔐 GitHub Secrets Configuration Required

## ⚠️ Current Issue
The deployment is failing with:
```
Credentials could not be loaded, please check your action inputs: 
Could not load credentials from any providers
```

## Required GitHub Secrets

You need to configure these secrets in your GitHub repository:
**Settings → Secrets and variables → Actions → New repository secret**

### **1. AWS Credentials (Required for all environments)**
```
AWS_ACCESS_KEY_ID=<your-aws-access-key>
AWS_SECRET_ACCESS_KEY=<your-aws-secret-key>
```

### **2. Development Environment Secrets**
```
DEV_ECS_CLUSTER=Amesa
DEV_ECS_SERVICE=amesa-backend-stage-service
```

### **3. Staging Environment Secrets**
```
STAGE_ECS_CLUSTER=Amesa
STAGE_ECS_SERVICE=amesa-backend-stage-service
```

### **4. Production Environment Secrets** (for future use)
```
PROD_ECS_CLUSTER=Amesa
PROD_ECS_SERVICE=amesa-backend-service
```

## How to Configure

1. Go to: https://github.com/DrorGr/amesaBE/settings/secrets/actions
2. Click "New repository secret"
3. Add each secret from the list above
4. Click "Add secret"

## Quick Copy-Paste Values

Here are the exact values you need to add (replace `<your-aws-*>` with your actual credentials):

| Secret Name | Value |
|-------------|-------|
| `AWS_ACCESS_KEY_ID` | `<your-aws-access-key>` |
| `AWS_SECRET_ACCESS_KEY` | `<your-aws-secret-key>` |
| `DEV_ECS_CLUSTER` | `Amesa` |
| `DEV_ECS_SERVICE` | `amesa-backend-stage-service` |
| `STAGE_ECS_CLUSTER` | `Amesa` |
| `STAGE_ECS_SERVICE` | `amesa-backend-stage-service` |
| `PROD_ECS_CLUSTER` | `Amesa` |
| `PROD_ECS_SERVICE` | `amesa-backend-service` |

## After Configuration

Once you've added these secrets:
1. Go to: https://github.com/DrorGr/amesaBE/actions
2. Find the failed workflow run
3. Click "Re-run failed jobs" or push a new commit

The deployment should then succeed and the admin panel will be accessible at:
- **Dev/Stage**: http://amesa-backend-stage-alb-467028641.eu-north-1.elb.amazonaws.com/admin

## Notes

- Dev and Stage share the same ECS service (`amesa-backend-stage-service`)
- Production has its own isolated service (`amesa-backend-service`)
- Both services run in the same ECS cluster (`Amesa`)
- All infrastructure is in region: `eu-north-1`

