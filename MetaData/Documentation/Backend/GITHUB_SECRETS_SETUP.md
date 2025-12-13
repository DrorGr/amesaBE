# 🔐 GitHub Secrets Setup Guide for Amesa Backend

This guide shows you how to configure GitHub Secrets for automatic deployment to AWS ECS/ECR.

## 📋 Required GitHub Secrets

### **AWS Credentials (Required for both Frontend and Backend)**
```
AWS_ACCESS_KEY_ID=your-aws-access-key-id
AWS_SECRET_ACCESS_KEY=your-aws-secret-access-key
```

### **Environment-Specific Backend Secrets**

#### **Development Environment**
```
DEV_ECS_CLUSTER=amesa-dev-cluster
DEV_ECS_SERVICE=amesa-backend-dev-service
DEV_DB_CONNECTION_STRING=your-dev-database-connection-string
DEV_JWT_SECRET_KEY=your-dev-jwt-secret-key
```

#### **Staging Environment**
```
STAGE_ECS_CLUSTER=amesa-stage-cluster
STAGE_ECS_SERVICE=amesa-backend-stage-service
STAGE_DB_CONNECTION_STRING=your-stage-database-connection-string
STAGE_JWT_SECRET_KEY=your-stage-jwt-secret-key
```

#### **Production Environment**
```
PROD_ECS_CLUSTER=amesa-prod-cluster
PROD_ECS_SERVICE=amesa-backend-prod-service
PROD_DB_CONNECTION_STRING=your-prod-database-connection-string
PROD_JWT_SECRET_KEY=your-prod-jwt-secret-key
```

## 🚀 How to Add Secrets

### **Method 1: GitHub Web Interface**
1. Go to your repository: `https://github.com/DrorGr/amesaBE`
2. Click **Settings** tab
3. In the left sidebar, click **Secrets and variables** → **Actions**
4. Click **New repository secret**
5. Add each secret with the exact name and value from the list above

### **Method 2: GitHub CLI**
```bash
# Set AWS credentials
gh secret set AWS_ACCESS_KEY_ID --repo DrorGr/amesaBE
gh secret set AWS_SECRET_ACCESS_KEY --repo DrorGr/amesaBE

# Set development secrets
gh secret set DEV_ECS_CLUSTER --repo DrorGr/amesaBE
gh secret set DEV_ECS_SERVICE --repo DrorGr/amesaBE
gh secret set DEV_DB_CONNECTION_STRING --repo DrorGr/amesaBE
gh secret set DEV_JWT_SECRET_KEY --repo DrorGr/amesaBE

# Set staging secrets
gh secret set STAGE_ECS_CLUSTER --repo DrorGr/amesaBE
gh secret set STAGE_ECS_SERVICE --repo DrorGr/amesaBE
gh secret set STAGE_DB_CONNECTION_STRING --repo DrorGr/amesaBE
gh secret set STAGE_JWT_SECRET_KEY --repo DrorGr/amesaBE

# Set production secrets
gh secret set PROD_ECS_CLUSTER --repo DrorGr/amesaBE
gh secret set PROD_ECS_SERVICE --repo DrorGr/amesaBE
gh secret set PROD_DB_CONNECTION_STRING --repo DrorGr/amesaBE
gh secret set PROD_JWT_SECRET_KEY --repo DrorGr/amesaBE
```

## 🔧 AWS Setup Requirements

### **ECR Repository**
Your ECR repository should be: `amesabe` in region `eu-north-1`

### **ECS Clusters and Services**
Based on your existing setup, you should have:
- ECS Cluster: Your existing cluster name
- ECS Service: Your existing service name

### **IAM Permissions**
Your AWS user/role needs these permissions:
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "ecr:GetAuthorizationToken",
        "ecr:BatchCheckLayerAvailability",
        "ecr:GetDownloadUrlForLayer",
        "ecr:BatchGetImage",
        "ecr:InitiateLayerUpload",
        "ecr:UploadLayerPart",
        "ecr:CompleteLayerUpload",
        "ecr:PutImage"
      ],
      "Resource": "*"
    },
    {
      "Effect": "Allow",
      "Action": [
        "ecs:UpdateService",
        "ecs:DescribeServices",
        "ecs:DescribeClusters",
        "ecs:DescribeTaskDefinition",
        "ecs:RegisterTaskDefinition"
      ],
      "Resource": "*"
    },
    {
      "Effect": "Allow",
      "Action": [
        "logs:CreateLogGroup",
        "logs:CreateLogStream",
        "logs:PutLogEvents"
      ],
      "Resource": "arn:aws:logs:eu-north-1:*:*"
    }
  ]
}
```

## 🐳 Docker Configuration

The workflow expects:
- Dockerfile in `./AmesaBackend/` directory
- .NET 8.0 application
- Port 8080 exposed
- Health check endpoint at `/health`

## 🔄 Deployment Flow

1. **Build**: .NET application is built and tested
2. **Docker**: Docker image is built from the AmesaBackend directory
3. **ECR**: Image is pushed to Amazon ECR with environment-specific tags
4. **ECS**: ECS service is updated to use the new image

## ✅ Verification

After setting up all secrets:

1. **Test the workflow**: Make a small change and push to the `dev` branch
2. **Check the Actions tab**: Verify the workflow runs successfully
3. **Verify deployment**: Check ECS service is updated with new task definition
4. **Health check**: Verify the application responds at `/health`

## 🚨 Security Notes

- Database connection strings should use AWS Secrets Manager
- JWT secrets should be strong and unique per environment
- Never commit secrets to your repository
- Use different AWS credentials for different environments
- Regularly rotate your AWS access keys

## 🆘 Troubleshooting

### **Common Issues:**
1. **ECR login failed**: Check AWS credentials and ECR permissions
2. **Docker build failed**: Verify Dockerfile exists in AmesaBackend directory
3. **ECS update failed**: Check cluster and service names
4. **Health check failed**: Verify application is running on port 8080

### **Getting Help:**
- Check the GitHub Actions logs for detailed error messages
- Verify all secrets are set correctly
- Ensure AWS resources exist and are accessible
- Check ECS service logs in CloudWatch

## 📊 Monitoring

Monitor your deployments through:
- **GitHub Actions**: Check workflow execution logs
- **AWS ECS**: Monitor service health and task status
- **CloudWatch**: View application logs and metrics
- **AWS ECR**: Verify image pushes and tags
