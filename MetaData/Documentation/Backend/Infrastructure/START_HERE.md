# 🚀 START HERE - Database Setup

**Welcome!** This guide will help you set up the database for Amesa microservices.

## What You Need

Before starting, ensure you have:

1. ✅ **AWS CLI** installed and configured
2. ✅ **PostgreSQL client (psql)** OR access to AWS RDS Query Editor
3. ✅ **.NET SDK 8.0** installed
4. ✅ **Aurora database password** (get from AWS Secrets Manager or secure storage)

## Quick Start (5 Minutes)

### Step 1: Verify Prerequisites

```powershell
cd BE/Infrastructure
.\verify-database-setup.ps1
```

Fix any missing prerequisites before proceeding.

### Step 2: Run Setup (All-in-One)

```powershell
.\deploy-database-setup.ps1
```

This script will:
- ✅ Fix ECR network access
- ✅ Create database schemas
- ✅ Update database password (will prompt you)
- ✅ Apply migrations

### Step 3: Verify Setup

```powershell
.\verify-database-setup.ps1
```

All checks should pass! ✅

## Important Notes

### ⚠️ NAT Gateway Required

ECS tasks are in **private subnets** and require a **NAT Gateway** for ECR access.

**Check if NAT Gateway exists:**
```bash
aws ec2 describe-nat-gateways --region eu-north-1 --filter 'Name=vpc-id,Values=<VPC_ID>'
```

**If missing:** See `TROUBLESHOOTING.md` section 7 for creation steps.

### 🔐 Database Password

You'll be prompted for the Aurora PostgreSQL password when running the setup.

**Where to find it:**
- AWS Secrets Manager
- AWS RDS Console
- Your secure password storage

## Need Help?

- **Quick Commands**: See `QUICK_REFERENCE.md`
- **Detailed Guide**: See `DATABASE_SETUP_GUIDE.md`
- **Troubleshooting**: See `TROUBLESHOOTING.md`
- **Next Steps**: See `NEXT_STEPS.md`

## File Structure

```
BE/Infrastructure/
├── START_HERE.md                    ← You are here
├── deploy-database-setup.ps1       ← Master script (run this!)
├── verify-database-setup.ps1       ← Check prerequisites
├── NEXT_STEPS.md                    ← Detailed execution guide
├── QUICK_REFERENCE.md               ← Command cheat sheet
├── DATABASE_SETUP_GUIDE.md          ← Comprehensive guide
├── TROUBLESHOOTING.md                ← Common issues
└── CHECKLIST.md                     ← Pre-deployment checklist
```

## What Gets Set Up

1. **ECR Network Access**
   - IAM role with ECR permissions
   - CloudWatch Logs permissions
   - VPC configuration guidance

2. **Database Schemas**
   - 7 schemas created in Aurora:
     - `amesa_auth`
     - `amesa_payment`
     - `amesa_lottery`
     - `amesa_content`
     - `amesa_notification`
     - `amesa_lottery_results`
     - `amesa_analytics`

3. **Database Password**
   - Updated in all 8 service `appsettings.json` files

4. **Migrations**
   - Applied to all 7 microservices

## Ready?

```powershell
cd BE/Infrastructure
.\verify-database-setup.ps1
.\deploy-database-setup.ps1
```

**That's it!** 🎉

---

**Questions?** See `NEXT_STEPS.md` for detailed instructions.

