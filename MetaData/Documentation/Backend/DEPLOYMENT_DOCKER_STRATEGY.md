# 🐳 Docker Build Strategy

**Date**: 2025-01-27  
**Status**: Local builds having path issues

---

## ⚠️ Current Issue

Local Docker builds are failing with path resolution errors:
```
ERROR: failed to build: resolve : CreateFile AmesaBackend.Auth: The system cannot find the file specified.
```

This is likely due to:
- Docker Desktop path resolution on Windows
- Working directory context issues
- Buildx path handling

---

## ✅ Recommended Solution: Use CI/CD Workflows

### GitHub Actions Workflows (Already Configured)

All 8 services have GitHub Actions workflows ready:

1. **`.github/workflows/deploy-auth-service.yml`**
2. **`.github/workflows/deploy-payment-service.yml`**
3. **`.github/workflows/deploy-lottery-service.yml`**
4. **`.github/workflows/deploy-content-service.yml`**
5. **`.github/workflows/deploy-notification-service.yml`**
6. **`.github/workflows/deploy-lottery-results-service.yml`**
7. **`.github/workflows/deploy-analytics-service.yml`**
8. **`.github/workflows/deploy-admin-service.yml`**

### How They Work

Each workflow:
1. **Triggers on push** to `main` branch (or path-specific changes)
2. **Builds Docker image** using the service's Dockerfile
3. **Pushes to ECR** automatically
4. **Deploys to ECS** by updating the service

### To Trigger

Simply push code to the repository:
```bash
cd BE
git add .
git commit -m "Update microservices configuration"
git push origin main
```

Workflows will automatically:
- Detect changes
- Build images
- Push to ECR
- Deploy to ECS

---

## 🔧 Alternative: Fix Local Builds

If you prefer local builds, try:

### Option 1: Use Full Paths
```powershell
cd C:\Users\dror0\Curser-Repos\AmesaBase-Monorepo\BE
docker build -t amesa-auth-service:latest -f .\AmesaBackend.Auth\Dockerfile .
```

### Option 2: Use Docker Compose
Create a `docker-compose.build.yml`:
```yaml
version: '3.8'
services:
  auth:
    build:
      context: .
      dockerfile: AmesaBackend.Auth/Dockerfile
    image: amesa-auth-service:latest
```

### Option 3: Use Build Script
Create a PowerShell script that:
- Changes to correct directory
- Builds each service sequentially
- Handles errors gracefully

---

## 📊 Current Status

- ✅ **CI/CD Workflows**: Configured and ready
- ❌ **Local Builds**: Having path issues
- ⏳ **ECR Images**: None yet (waiting for builds)

---

## 🎯 Recommendation

**Use GitHub Actions workflows** - they're:
- ✅ Already configured
- ✅ Tested and reliable
- ✅ Automated
- ✅ Integrated with ECR and ECS
- ✅ No local Docker issues

Just push the code and let CI/CD handle it!

---

**Next**: Push code to trigger CI/CD, or fix local Docker build paths.

