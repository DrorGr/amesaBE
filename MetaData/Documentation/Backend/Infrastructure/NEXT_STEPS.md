# Next Steps - Database Setup Execution

**Status**: ✅ All scripts and documentation ready  
**Action Required**: Execute the setup scripts

## Immediate Actions

### Step 1: Verify Prerequisites

Run the verification script to check your environment:

```powershell
cd BE/Infrastructure
.\verify-database-setup.ps1
```

**Fix any missing prerequisites before proceeding.**

### Step 2: Execute Database Setup

You have two options:

#### Option A: Master Script (Recommended - All-in-One)

```powershell
cd BE/Infrastructure
.\deploy-database-setup.ps1
```

This will execute all 4 tasks automatically:
1. Fix ECR network access
2. Create database schemas
3. Update database password
4. Apply migrations

#### Option B: Individual Scripts (Step-by-Step)

```powershell
cd BE/Infrastructure

# 1. Fix ECR network access
.\fix-ecr-network-access.ps1

# 2. Create database schemas
.\setup-database.ps1

# 3. Update database password (will prompt for password)
.\update-database-password.ps1

# 4. Apply migrations
cd ..\scripts
.\apply-database-migrations.ps1
```

### Step 3: Verify Setup Complete

After running the scripts, verify everything is set up correctly:

```powershell
cd BE/Infrastructure
.\verify-database-setup.ps1
```

## Critical Manual Steps

### 1. Verify NAT Gateway (CRITICAL)

ECS tasks are in private subnets and **REQUIRE** a NAT Gateway for ECR access.

**Check if NAT Gateway exists:**
```bash
# First, get your VPC ID
aws ec2 describe-vpcs --region eu-north-1 --query 'Vpcs[?IsDefault==`false`].VpcId'

# Then check for NAT Gateway
aws ec2 describe-nat-gateways --region eu-north-1 --filter 'Name=vpc-id,Values=<YOUR_VPC_ID>' --query 'NatGateways[?State==`available`]'
```

**If NAT Gateway doesn't exist:**
- See `TROUBLESHOOTING.md` section 7 for creation steps
- Or create via AWS Console: VPC → NAT Gateways → Create NAT Gateway

### 2. Get Aurora Database Password

You'll need the actual Aurora PostgreSQL password when running `update-database-password.ps1`.

**Where to find it:**
- AWS Secrets Manager (if stored there)
- AWS RDS Console → Database → View credentials
- Your secure password storage

**Security Note:** Consider storing in AWS Secrets Manager for production.

### 3. Verify Database Access

Before running schema creation, verify you can connect:

```bash
psql -h amesadbmain.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com \
     -U dror \
     -d postgres
```

**If connection fails:**
- Check security group allows your IP
- Verify password is correct
- Check Aurora endpoint is correct

## Post-Setup Verification

### 1. Check Database Schemas

```sql
-- Connect to Aurora
psql -h amesadbmain.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com -U dror -d postgres

-- Check schemas
SELECT schema_name 
FROM information_schema.schemata 
WHERE schema_name LIKE 'amesa_%'
ORDER BY schema_name;
```

**Expected:** 7 schemas (auth, payment, lottery, content, notification, lottery_results, analytics)

### 2. Check Migrations Applied

```powershell
cd BE/AmesaBackend.Auth
dotnet ef migrations list --context AuthDbContext
```

**Expected:** Migrations listed, database updated

### 3. Verify ECR Access

```bash
# Check IAM role
aws iam get-role --role-name ecsTaskExecutionRole --region eu-north-1

# Check ECR permissions
aws iam list-role-policies --role-name ecsTaskExecutionRole --region eu-north-1
```

## After Setup Complete

### 1. Deploy Services to ECS

Once database setup is complete, you can deploy services:

**Option A: Via CI/CD (Recommended)**
```bash
cd BE
git add .
git commit -m "Database setup complete - ready for deployment"
git push origin main
```

This will trigger GitHub Actions to:
- Build Docker images
- Push to ECR
- Deploy to ECS

**Option B: Manual Deployment**
```bash
# Build and push images
cd BE/Infrastructure
.\build-and-push-images.sh

# Create/update ECS services
.\create-ecs-services.sh
```

### 2. Monitor Deployment

**Check ECS Services:**
```bash
aws ecs describe-services \
    --cluster Amesa \
    --services amesa-auth-service \
    --region eu-north-1 \
    --query 'services[0].[status,runningCount,desiredCount]'
```

**Check Service Logs:**
```bash
aws logs tail /ecs/amesa-auth-service --follow --region eu-north-1
```

**Check Service Health:**
```bash
curl https://amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com/api/v1/auth/health
```

### 3. Verify Services Running

**Check all services:**
```bash
aws ecs list-services --cluster Amesa --region eu-north-1

# For each service, check status
aws ecs describe-services \
    --cluster Amesa \
    --services <service-name> \
    --region eu-north-1 \
    --query 'services[0].[serviceName,status,runningCount,desiredCount]'
```

## Troubleshooting

If you encounter issues:

1. **Run verification script:**
   ```powershell
   .\verify-database-setup.ps1
   ```

2. **Check troubleshooting guide:**
   - See `TROUBLESHOOTING.md` for common issues

3. **Review logs:**
   - AWS CloudWatch: `/ecs/amesa-*-service`
   - GitHub Actions: Repository → Actions tab

4. **Common issues:**
   - ECR pull fails → Check NAT Gateway
   - Database connection fails → Check password and security groups
   - Migrations fail → Verify schemas exist

## Documentation Reference

- **Quick Reference**: `QUICK_REFERENCE.md` - Command cheat sheet
- **Full Guide**: `DATABASE_SETUP_GUIDE.md` - Comprehensive instructions
- **Troubleshooting**: `TROUBLESHOOTING.md` - Common issues and solutions
- **Checklist**: `CHECKLIST.md` - Pre-deployment checklist
- **Architecture**: `../HANDOFF.md` - Complete system architecture

## Summary Checklist

Before deploying services, ensure:

- [ ] Prerequisites installed (AWS CLI, psql, .NET SDK)
- [ ] NAT Gateway exists (CRITICAL for ECR access)
- [ ] Database schemas created (7 schemas)
- [ ] Database password updated (all services)
- [ ] Migrations applied (all services)
- [ ] Verification script passes
- [ ] ECR images pushed (for deployment)
- [ ] ECS services configured

## Quick Commands Reference

```powershell
# Verify setup
.\verify-database-setup.ps1

# Run all setup
.\deploy-database-setup.ps1

# Check NAT Gateway
aws ec2 describe-nat-gateways --region eu-north-1 --filter 'Name=vpc-id,Values=<VPC_ID>'

# Check schemas
psql -h <aurora-endpoint> -U dror -d postgres -c "SELECT schema_name FROM information_schema.schemata WHERE schema_name LIKE 'amesa_%';"

# Check migrations
cd BE/AmesaBackend.Auth
dotnet ef migrations list --context AuthDbContext
```

---

**Ready to proceed?** Start with Step 1: Verify Prerequisites

**Questions?** See `TROUBLESHOOTING.md` or `DATABASE_SETUP_GUIDE.md`

---

**Last Updated**: 2025-01-27

