# 🔐 GitHub Secrets Setup Guide for Amesa Frontend

This guide shows you how to configure GitHub Secrets for automatic deployment to AWS S3 + CloudFront.

## 📋 Required GitHub Secrets

### **AWS Credentials (Required for all deployments)**
```
AWS_ACCESS_KEY_ID=your-aws-access-key-id
AWS_SECRET_ACCESS_KEY=your-aws-secret-access-key
```

### **Environment-Specific Frontend Secrets**

#### **Development Environment (Automatic deployment on dev branch push)**
```
DEV_API_URL=https://dev-api.amesa.com
DEV_BACKEND_URL=https://dev-api.amesa.com
DEV_FRONTEND_URL=https://dev.amesa.com
DEV_S3_BUCKET=amesa-frontend-dev
DEV_CLOUDFRONT_ID=your-dev-cloudfront-distribution-id
```

#### **Staging Environment (Automatic deployment on stage branch push)**
```
STAGE_API_URL=https://stage-api.amesa.com
STAGE_BACKEND_URL=https://stage-api.amesa.com
STAGE_FRONTEND_URL=https://stage.amesa.com
STAGE_S3_BUCKET=amesa-frontend-stage
STAGE_CLOUDFRONT_ID=your-stage-cloudfront-distribution-id
```

#### **Production Environment (Manual deployment only)**
```
PROD_API_URL=https://api.amesa.com
PROD_BACKEND_URL=https://api.amesa.com
PROD_FRONTEND_URL=https://amesa.com
PROD_S3_BUCKET=amesa-frontend-prod
PROD_CLOUDFRONT_ID=your-prod-cloudfront-distribution-id
```

## 🚀 How to Add Secrets

### **Method 1: GitHub Web Interface**
1. Go to your repository: `https://github.com/DrorGr/amesaFE`
2. Click **Settings** tab
3. In the left sidebar, click **Secrets and variables** → **Actions**
4. Click **New repository secret**
5. Add each secret with the exact name and value from the list above

### **Method 2: GitHub CLI**
```bash
# Set AWS credentials
gh secret set AWS_ACCESS_KEY_ID --repo DrorGr/amesaFE
gh secret set AWS_SECRET_ACCESS_KEY --repo DrorGr/amesaFE

# Set development secrets
gh secret set DEV_API_URL --repo DrorGr/amesaFE
gh secret set DEV_BACKEND_URL --repo DrorGr/amesaFE
gh secret set DEV_FRONTEND_URL --repo DrorGr/amesaFE
gh secret set DEV_S3_BUCKET --repo DrorGr/amesaFE
gh secret set DEV_CLOUDFRONT_ID --repo DrorGr/amesaFE

# Set staging secrets
gh secret set STAGE_API_URL --repo DrorGr/amesaFE
gh secret set STAGE_BACKEND_URL --repo DrorGr/amesaFE
gh secret set STAGE_FRONTEND_URL --repo DrorGr/amesaFE
gh secret set STAGE_S3_BUCKET --repo DrorGr/amesaFE
gh secret set STAGE_CLOUDFRONT_ID --repo DrorGr/amesaFE

# Set production secrets
gh secret set PROD_API_URL --repo DrorGr/amesaFE
gh secret set PROD_BACKEND_URL --repo DrorGr/amesaFE
gh secret set PROD_FRONTEND_URL --repo DrorGr/amesaFE
gh secret set PROD_S3_BUCKET --repo DrorGr/amesaFE
gh secret set PROD_CLOUDFRONT_ID --repo DrorGr/amesaFE
```

## 🔧 AWS Setup Requirements

### **S3 Buckets (Static Website Hosting)**
Create three S3 buckets for static website hosting:
- `amesa-frontend-dev` - Development environment
- `amesa-frontend-stage` - Staging environment  
- `amesa-frontend-prod` - Production environment

**S3 Configuration for each bucket:**
1. Enable static website hosting
2. Set index document: `index.html`
3. Set error document: `index.html` (for Angular routing)
4. Set bucket policy for public read access

### **CloudFront Distributions**
Create CloudFront distributions for each environment:
- **Dev Distribution**: Points to `amesa-frontend-dev` S3 bucket
- **Stage Distribution**: Points to `amesa-frontend-stage` S3 bucket
- **Prod Distribution**: Points to `amesa-frontend-prod` S3 bucket

**CloudFront Configuration:**
- Origin: S3 bucket website endpoint
- Default root object: `index.html`
- Error pages: 404 → 200 → `/index.html` (for Angular routing)

### **IAM Permissions**
Your AWS user/role needs these permissions:
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "s3:GetObject",
        "s3:PutObject",
        "s3:DeleteObject",
        "s3:ListBucket",
        "s3:GetBucketWebsite",
        "s3:PutBucketWebsite"
      ],
      "Resource": [
        "arn:aws:s3:::amesa-frontend-dev",
        "arn:aws:s3:::amesa-frontend-dev/*",
        "arn:aws:s3:::amesa-frontend-stage",
        "arn:aws:s3:::amesa-frontend-stage/*",
        "arn:aws:s3:::amesa-frontend-prod",
        "arn:aws:s3:::amesa-frontend-prod/*"
      ]
    },
    {
      "Effect": "Allow",
      "Action": [
        "cloudfront:CreateInvalidation",
        "cloudfront:GetInvalidation",
        "cloudfront:ListInvalidations"
      ],
      "Resource": "*"
    }
  ]
}
```

## ✅ Verification

After setting up all secrets:

1. **Test dev deployment**: Make a small change and push to the `dev` branch
   - Check GitHub Actions tab for successful workflow
   - Verify files are uploaded to S3 dev bucket
   - Check CloudFront dev distribution is updated
   - Visit your dev CloudFront URL to see the changes

2. **Test stage deployment**: Push to the `stage` branch
   - Same verification steps as dev
   - Check stage CloudFront URL

3. **Test production deployment**: Use manual workflow dispatch
   - Go to Actions → Run workflow → Select production
   - Verify production S3 bucket and CloudFront

## 🚨 Security Notes

- Never commit secrets to your repository
- Use different AWS credentials for different environments
- Regularly rotate your AWS access keys
- Monitor your AWS CloudTrail logs for any suspicious activity

## 🆘 Troubleshooting

### **Common Issues:**
1. **403 Forbidden**: Check AWS credentials and permissions
2. **Bucket not found**: Verify S3 bucket names and regions
3. **CloudFront not updating**: Check distribution ID and permissions

### **Getting Help:**
- Check the GitHub Actions logs for detailed error messages
- Verify all secrets are set correctly
- Ensure AWS resources exist and are accessible
