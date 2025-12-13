# Database Setup - Execution Status

**Date**: 2025-01-27  
**Status**: Partially Complete - Manual Steps Required

## ✅ Completed Automatically

### 1. ECR Network Access - FIXED ✅
- **IAM Role**: `ecsTaskExecutionRole` exists
- **ECR Permissions**: Attached `AmazonEC2ContainerRegistryReadOnly` policy
- **CloudWatch Logs**: Attached `CloudWatchLogsFullAccess` policy
- **Status**: ECS tasks can now pull images from ECR

**Verification:**
```bash
aws iam list-attached-role-policies --role-name ecsTaskExecutionRole --region eu-north-1
```

### 2. Verification Script - WORKING ✅
- All prerequisites checked
- Status reporting functional
- Ready for use

## ⚠️ Requires Manual Input

### 1. Database Password Update
**Status**: 7 services still have `CHANGE_ME` password

**Action Required:**
```powershell
cd BE/Infrastructure
.\update-database-password.ps1
```

**You will be prompted for:**
- Aurora PostgreSQL password

**Services to update:**
- AmesaBackend.Auth
- AmesaBackend.Payment
- AmesaBackend.Lottery
- AmesaBackend.Content
- AmesaBackend.Notification
- AmesaBackend.LotteryResults
- AmesaBackend.Analytics

### 2. Database Schema Creation
**Status**: Not yet created

**Action Required:**
```powershell
cd BE/Infrastructure
.\setup-database.ps1
```

**Alternative (if psql not available):**
Use AWS RDS Query Editor:
1. Go to AWS Console → RDS → Query Editor
2. Connect to `amesadbmain` cluster
3. Run SQL from `create-database-schemas.sql`

**Schemas to create:**
- `amesa_auth`
- `amesa_payment`
- `amesa_lottery`
- `amesa_content`
- `amesa_notification`
- `amesa_lottery_results`
- `amesa_analytics`

### 3. Database Migrations
**Status**: Projects need to be built first

**Action Required:**
```powershell
# First, build all projects
cd BE
dotnet build

# Then create migrations
cd scripts
.\database-migrations.ps1

# Finally, apply migrations
.\apply-database-migrations.ps1
```

**Note**: Migrations require:
- Database password updated (step 1)
- Database schemas created (step 2)
- Projects built successfully

## 📋 Execution Order

Execute in this order:

1. **Update Database Password** (requires password input)
   ```powershell
   .\update-database-password.ps1
   ```

2. **Create Database Schemas** (requires database access)
   ```powershell
   .\setup-database.ps1
   ```
   OR use AWS RDS Query Editor

3. **Build Projects** (fix any build errors)
   ```powershell
   cd ..
   dotnet build
   ```

4. **Create Migrations** (after build succeeds)
   ```powershell
   cd scripts
   .\database-migrations.ps1
   ```

5. **Apply Migrations** (after schemas exist)
   ```powershell
   .\apply-database-migrations.ps1
   ```

## 🔍 Current Status Summary

| Task | Status | Action |
|------|--------|--------|
| ECR Network Access | ✅ Complete | None - Ready |
| IAM Role Setup | ✅ Complete | None - Ready |
| Database Password | ⚠️ Pending | Run `update-database-password.ps1` |
| Database Schemas | ⚠️ Pending | Run `setup-database.ps1` or use RDS Query Editor |
| Project Build | ⚠️ Pending | Run `dotnet build` |
| Create Migrations | ⚠️ Pending | Run `database-migrations.ps1` (after build) |
| Apply Migrations | ⚠️ Pending | Run `apply-database-migrations.ps1` (after schemas) |

## 🚀 Quick Completion

To finish setup, run:

```powershell
cd BE/Infrastructure

# 1. Update password (will prompt)
.\update-database-password.ps1

# 2. Create schemas (will prompt for password)
.\setup-database.ps1

# 3. Build projects
cd ..
dotnet build

# 4. Create and apply migrations
cd scripts
.\database-migrations.ps1
.\apply-database-migrations.ps1

# 5. Verify everything
cd ..\Infrastructure
.\verify-database-setup.ps1
```

## 📝 Notes

### ECR Access
- ✅ **COMPLETE** - IAM policies attached
- ECS tasks can now pull Docker images from ECR
- Verify NAT Gateway exists for private subnet access

### Database Setup
- ⚠️ **REQUIRES MANUAL INPUT**
- Password update requires Aurora database password
- Schema creation requires database access (psql or RDS Query Editor)
- Migrations require projects to build successfully

### Next Steps After Completion
1. Verify all checks pass: `.\verify-database-setup.ps1`
2. Deploy services to ECS
3. Monitor service health

---

**Last Updated**: 2025-01-27  
**Next Action**: Run password update and schema creation scripts

