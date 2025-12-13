# GitHub Repository Setup Guide

## 🚀 **Step 1: Create GitHub Repositories**

### **Frontend Repository (amesaFE)**
1. Go to [GitHub.com](https://github.com) and sign in
2. Click the "+" icon → "New repository"
3. Repository name: `amesaFE`
4. Description: `Amesa Lottery Frontend - Angular application for lottery management system`
5. Set to **Public** or **Private** (your choice)
6. **DO NOT** initialize with README, .gitignore, or license (we already have these)
7. Click "Create repository"

### **Backend Repository (amesaBE)**
1. Click the "+" icon → "New repository"
2. Repository name: `amesaBE`
3. Description: `Amesa Lottery Backend - .NET 8 API for lottery management system`
4. Set to **Public** or **Private** (your choice)
5. **DO NOT** initialize with README, .gitignore, or license
6. Click "Create repository"

### **DevOps Repository (amesaDevOps)** - Optional
1. Click the "+" icon → "New repository"
2. Repository name: `amesaDevOps`
3. Description: `Amesa Lottery Infrastructure - Deployment scripts and AWS configurations`
4. Set to **Private** (contains sensitive infrastructure details)
5. Initialize with README
6. Click "Create repository"

## 📤 **Step 2: Push Frontend Code (amesaFE)**

```bash
# Navigate to frontend directory
cd C:\Users\dror0\Curser-Repos\amesaFE-temp

# Add remote origin (replace YOUR_USERNAME with your GitHub username)
git remote add origin https://github.com/YOUR_USERNAME/amesaFE.git

# Push all branches to GitHub
git push -u origin main
git push -u origin dev
git push -u origin stage

# Verify branches are created
git branch -r
```

## 📤 **Step 3: Setup Backend Repository (amesaBE)**

```bash
# Navigate to backend directory
cd C:\Users\dror0\Curser-Repos\amesaBE-temp

# Initialize git repository
git init
git add .
git commit -m "Initial commit: Clean .NET backend with CI/CD setup"

# Create branches
git checkout -b main
git checkout -b dev
git checkout -b stage
git checkout main

# Add remote origin (replace YOUR_USERNAME with your GitHub username)
git remote add origin https://github.com/YOUR_USERNAME/amesaBE.git

# Push all branches to GitHub
git push -u origin main
git push -u origin dev
git push -u origin stage
```

## 🔐 **Step 4: Configure GitHub Secrets**

### **Frontend Repository Secrets (amesaFE)**

Go to: `Settings` → `Secrets and variables` → `Actions` → `New repository secret`

**Required Secrets:**
```
DEV_API_URL=https://dev-api.amesa.com
DEV_BACKEND_URL=https://dev-api.amesa.com
DEV_FRONTEND_URL=https://dev.amesa.com
DEV_S3_BUCKET=amesa-frontend-dev
DEV_CLOUDFRONT_ID=YOUR_DEV_CLOUDFRONT_ID

STAGE_API_URL=https://stage-api.amesa.com
STAGE_BACKEND_URL=https://stage-api.amesa.com
STAGE_FRONTEND_URL=https://stage.amesa.com
STAGE_S3_BUCKET=amesa-frontend-stage
STAGE_CLOUDFRONT_ID=YOUR_STAGE_CLOUDFRONT_ID

PROD_API_URL=https://api.amesa.com
PROD_BACKEND_URL=https://api.amesa.com
PROD_FRONTEND_URL=https://amesa.com
PROD_S3_BUCKET=amesa-frontend-prod
PROD_CLOUDFRONT_ID=YOUR_PROD_CLOUDFRONT_ID

AWS_ACCESS_KEY_ID=YOUR_AWS_ACCESS_KEY_ID
AWS_SECRET_ACCESS_KEY=YOUR_AWS_SECRET_ACCESS_KEY
```

### **Backend Repository Secrets (amesaBE)**

**Required Secrets:**
```
DEV_ECS_CLUSTER=amesa-backend-dev
DEV_ECS_SERVICE=amesa-backend-dev-service
DEV_DB_CONNECTION_STRING=Host=your-dev-db;Database=amesa_lottery;Username=your-user;Password=your-password;Port=5432
DEV_REDIS_CONNECTION_STRING=your-dev-redis:6379
DEV_JWT_SECRET_KEY=your-dev-jwt-secret-key-32-chars-minimum
DEV_SMTP_USERNAME=your-dev-email@gmail.com
DEV_SMTP_PASSWORD=your-dev-app-password
DEV_STRIPE_SECRET_KEY=sk_test_your_dev_stripe_secret
DEV_AWS_ACCESS_KEY_ID=your-dev-aws-access-key
DEV_AWS_SECRET_ACCESS_KEY=your-dev-aws-secret-key

STAGE_ECS_CLUSTER=amesa-backend-stage
STAGE_ECS_SERVICE=amesa-backend-stage-service
STAGE_DB_CONNECTION_STRING=Host=your-stage-db;Database=amesa_lottery;Username=your-user;Password=your-password;Port=5432
STAGE_REDIS_CONNECTION_STRING=your-stage-redis:6379
STAGE_JWT_SECRET_KEY=your-stage-jwt-secret-key-32-chars-minimum
STAGE_SMTP_USERNAME=your-stage-email@gmail.com
STAGE_SMTP_PASSWORD=your-stage-app-password
STAGE_STRIPE_SECRET_KEY=sk_test_your_stage_stripe_secret
STAGE_AWS_ACCESS_KEY_ID=your-stage-aws-access-key
STAGE_AWS_SECRET_ACCESS_KEY=your-stage-aws-secret-key

PROD_ECS_CLUSTER=amesa-backend-prod
PROD_ECS_SERVICE=amesa-backend-prod-service
PROD_DB_CONNECTION_STRING=Host=your-prod-db;Database=amesa_lottery;Username=your-user;Password=your-password;Port=5432
PROD_REDIS_CONNECTION_STRING=your-prod-redis:6379
PROD_JWT_SECRET_KEY=your-prod-jwt-secret-key-32-chars-minimum
PROD_SMTP_USERNAME=your-prod-email@gmail.com
PROD_SMTP_PASSWORD=your-prod-app-password
PROD_STRIPE_SECRET_KEY=sk_live_your_prod_stripe_secret
PROD_AWS_ACCESS_KEY_ID=your-prod-aws-access-key
PROD_AWS_SECRET_ACCESS_KEY=your-prod-aws-secret-key

AWS_ACCESS_KEY_ID=YOUR_AWS_ACCESS_KEY_ID
AWS_SECRET_ACCESS_KEY=YOUR_AWS_SECRET_ACCESS_KEY
```

## ✅ **Step 5: Verify Setup**

### **Check Frontend Repository:**
1. Go to `https://github.com/YOUR_USERNAME/amesaFE`
2. Verify you see 3 branches: `main`, `dev`, `stage`
3. Check that `.github/workflows/deploy.yml` exists
4. Verify environment files exist in `src/environments/`

### **Check Backend Repository:**
1. Go to `https://github.com/YOUR_USERNAME/amesaBE`
2. Verify you see 3 branches: `main`, `dev`, `stage`
3. Check that `.github/workflows/deploy.yml` exists
4. Verify `scripts/` directory exists with Docker configs

### **Test CI/CD Pipeline:**
1. Make a small change to any file
2. Commit and push to `dev` branch
3. Go to `Actions` tab in GitHub
4. Verify the workflow runs successfully

## 🎯 **Step 6: Next Steps**

1. **Set up AWS Infrastructure** using the scripts from the original AmesaBase repository
2. **Configure your actual URLs and credentials** in the GitHub Secrets
3. **Test deployments** by pushing to different branches
4. **Set up branch protection rules** to require PR reviews for main branch
5. **Configure notifications** for deployment status

## 📋 **Repository URLs**

After setup, your repositories will be available at:
- Frontend: `https://github.com/YOUR_USERNAME/amesaFE`
- Backend: `https://github.com/YOUR_USERNAME/amesaBE`
- DevOps: `https://github.com/YOUR_USERNAME/amesaDevOps` (optional)

## 🆘 **Troubleshooting**

### **If push fails:**
```bash
# Check remote URL
git remote -v

# Update remote URL if needed
git remote set-url origin https://github.com/YOUR_USERNAME/REPO_NAME.git

# Force push if needed (be careful!)
git push -f origin main
```

### **If secrets don't work:**
1. Double-check secret names match exactly (case-sensitive)
2. Ensure secrets are added to the correct repository
3. Check workflow file syntax in `.github/workflows/deploy.yml`
