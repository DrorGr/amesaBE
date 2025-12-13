# GitHub CLI Commands - Ready to Execute

## 🔧 **First, authenticate with GitHub CLI**

Open a **new PowerShell window** (to ensure GitHub CLI is in PATH) and run:

```bash
# Authenticate with GitHub CLI
gh auth login

# Follow the prompts:
# - Choose "GitHub.com"
# - Choose "HTTPS" 
# - Choose "Login with a web browser"
# - Copy the one-time code and authenticate in browser
```

## 🚀 **Create and Push Frontend Repository (amesaFE)**

```bash
# Navigate to frontend directory
cd C:\Users\dror0\Curser-Repos\amesaFE-temp

# Create GitHub repository and push
gh repo create amesaFE --public --description "Amesa Lottery Frontend - Angular application for lottery management system" --source=. --remote=origin --push

# Push all branches
git push -u origin main
git push -u origin dev  
git push -u origin stage

# Verify branches were created
gh repo view amesaFE
```

## 🚀 **Create and Push Backend Repository (amesaBE)**

```bash
# Navigate to backend directory
cd C:\Users\dror0\Curser-Repos\amesaBE-temp

# Create GitHub repository and push
gh repo create amesaBE --public --description "Amesa Lottery Backend - .NET 8 API for lottery management system" --source=. --remote=origin --push

# Push all branches
git push -u origin main
git push -u origin dev
git push -u origin stage

# Verify branches were created
gh repo view amesaBE
```

## 🔐 **Set GitHub Secrets (Optional - can be done via web interface)**

### **Frontend Secrets:**
```bash
# Set frontend secrets
gh secret set DEV_API_URL --repo amesaFE --body "https://dev-api.amesa.com"
gh secret set DEV_S3_BUCKET --repo amesaFE --body "amesa-frontend-dev"
gh secret set DEV_CLOUDFRONT_ID --repo amesaFE --body "YOUR_DEV_CLOUDFRONT_ID"

gh secret set STAGE_API_URL --repo amesaFE --body "https://stage-api.amesa.com"
gh secret set STAGE_S3_BUCKET --repo amesaFE --body "amesa-frontend-stage"
gh secret set STAGE_CLOUDFRONT_ID --repo amesaFE --body "YOUR_STAGE_CLOUDFRONT_ID"

gh secret set PROD_API_URL --repo amesaFE --body "https://api.amesa.com"
gh secret set PROD_S3_BUCKET --repo amesaFE --body "amesa-frontend-prod"
gh secret set PROD_CLOUDFRONT_ID --repo amesaFE --body "YOUR_PROD_CLOUDFRONT_ID"

gh secret set AWS_ACCESS_KEY_ID --repo amesaFE --body "YOUR_AWS_ACCESS_KEY_ID"
gh secret set AWS_SECRET_ACCESS_KEY --repo amesaFE --body "YOUR_AWS_SECRET_ACCESS_KEY"
```

### **Backend Secrets:**
```bash
# Set backend secrets
gh secret set DEV_ECS_CLUSTER --repo amesaBE --body "amesa-backend-dev"
gh secret set DEV_ECS_SERVICE --repo amesaBE --body "amesa-backend-dev-service"
gh secret set DEV_DB_CONNECTION_STRING --repo amesaBE --body "Host=your-dev-db;Database=amesa_lottery;Username=your-user;Password=your-password;Port=5432"

gh secret set STAGE_ECS_CLUSTER --repo amesaBE --body "amesa-backend-stage"
gh secret set STAGE_ECS_SERVICE --repo amesaBE --body "amesa-backend-stage-service"
gh secret set STAGE_DB_CONNECTION_STRING --repo amesaBE --body "Host=your-stage-db;Database=amesa_lottery;Username=your-user;Password=your-password;Port=5432"

gh secret set PROD_ECS_CLUSTER --repo amesaBE --body "amesa-backend-prod"
gh secret set PROD_ECS_SERVICE --repo amesaBE --body "amesa-backend-prod-service"
gh secret set PROD_DB_CONNECTION_STRING --repo amesaBE --body "Host=your-prod-db;Database=amesa_lottery;Username=your-user;Password=your-password;Port=5432"

gh secret set AWS_ACCESS_KEY_ID --repo amesaBE --body "YOUR_AWS_ACCESS_KEY_ID"
gh secret set AWS_SECRET_ACCESS_KEY --repo amesaBE --body "YOUR_AWS_SECRET_ACCESS_KEY"
```

## ✅ **Verify Everything Works**

```bash
# Check repositories were created
gh repo list

# View repository details
gh repo view amesaFE
gh repo view amesaBE

# Check workflows (after first push)
gh run list --repo amesaFE
gh run list --repo amesaBE

# Test CI/CD by making a small change
echo "# Test change" >> README.md
git add README.md
git commit -m "Test CI/CD pipeline"
git push origin dev

# Check workflow run
gh run list --repo amesaFE --branch dev
```

## 🎯 **Alternative: Manual Commands (if GitHub CLI doesn't work)**

If GitHub CLI isn't working, you can still use the manual Git commands:

```bash
# Frontend
cd C:\Users\dror0\Curser-Repos\amesaFE-temp
git remote add origin https://github.com/YOUR_USERNAME/amesaFE.git
git push -u origin main
git push -u origin dev
git push -u origin stage

# Backend
cd C:\Users\dror0\Curser-Repos\amesaBE-temp
git remote add origin https://github.com/YOUR_USERNAME/amesaBE.git
git push -u origin main
git push -u origin dev
git push -u origin stage
```

## 📋 **Quick Checklist**

- [ ] Authenticate with `gh auth login`
- [ ] Create amesaFE repository and push code
- [ ] Create amesaBE repository and push code
- [ ] Verify both repositories exist on GitHub
- [ ] Check that all branches (main, dev, stage) exist
- [ ] Verify CI/CD workflows are set up
- [ ] Set GitHub Secrets (optional for now)
- [ ] Test CI/CD by pushing to dev branch

## 🚀 **Expected Results**

After running these commands, you should have:
- ✅ Two new repositories on GitHub: `amesaFE` and `amesaBE`
- ✅ All branches pushed: main, dev, stage
- ✅ CI/CD workflows visible in GitHub Actions
- ✅ Clean, professional repository structure
- ✅ Ready for deployment with proper secrets
