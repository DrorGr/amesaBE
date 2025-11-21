# 🚀 Amesa Microservices - Deployment Guide

## Infrastructure Status: ✅ COMPLETE

All AWS infrastructure has been successfully deployed. This guide covers the remaining application deployment steps.

---

## 📋 Quick Start

### 1. Build and Push Docker Images

**Option A: Using CI/CD (Recommended)**
- Push code to trigger GitHub Actions workflows
- Each service has its own workflow in `.github/workflows/`

**Option B: Manual Build**
```bash
cd BE/Infrastructure
chmod +x build-and-push-images.sh
./build-and-push-images.sh
```

### 2. Create Database Schemas

```bash
# Connect to Aurora cluster
psql -h amesadbmain.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com \
     -U dror \
     -d postgres \
     -f BE/Infrastructure/create-database-schemas.sql
```

### 3. Run Database Migrations

```bash
cd BE
chmod +x scripts/database-migrations.sh
./scripts/database-migrations.sh
```

### 4. Update Connection Strings

```bash
cd BE/Infrastructure
# Edit update-connection-strings.sh with actual credentials first
chmod +x update-connection-strings.sh
./update-connection-strings.sh
```

Or manually update each service's `appsettings.json`:
- Add `SearchPath=<schema_name>` to RDS connection string
- Add Redis connection: `amesa-redis.3ogg7e.0001.eun1.cache.amazonaws.com:6379`

### 5. Activate ECS Services

```bash
cd BE/Infrastructure
chmod +x update-ecs-services.sh
./update-ecs-services.sh
```

### 6. Verify Deployment

```bash
# Check service status
aws ecs describe-services --cluster Amesa --services amesa-auth-service --region eu-north-1

# Test health endpoint
curl http://amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com/api/v1/auth/health
```

---

## 🔗 Key Endpoints

- **ALB**: `amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com`
- **Redis**: `amesa-redis.3ogg7e.0001.eun1.cache.amazonaws.com:6379`
- **RDS**: `amesadbmain.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com:5432`

---

## 📚 Documentation

- **Complete Summary**: `DEPLOYMENT_COMPLETE_SUMMARY.md`
- **Final Status**: `DEPLOYMENT_FINAL_STATUS.md`
- **Next Steps**: `DEPLOYMENT_NEXT_STEPS.md`
- **Infrastructure Complete**: `DEPLOYMENT_INFRASTRUCTURE_COMPLETE.md`
- **Progress Tracking**: `DEPLOYMENT_PROGRESS.md`
- **Current Status**: `DEPLOYMENT_STATUS.md`

---

## ✅ Infrastructure Checklist

All infrastructure components are deployed:
- ✅ EventBridge event bus
- ✅ 8 ECR repositories
- ✅ 8 CloudWatch log groups
- ✅ 8 ECS task definitions
- ✅ 8 ECS services
- ✅ 8 Target groups
- ✅ ALB routing rules
- ✅ ElastiCache Redis cluster
- ✅ RDS configuration

**Status**: Ready for application deployment!

