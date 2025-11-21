# Database Setup - Quick Reference Card

## 🚀 One-Command Setup

```powershell
cd BE/Infrastructure
.\deploy-database-setup.ps1
```

## 📋 Individual Commands

### 1. Fix ECR Access
```powershell
.\fix-ecr-network-access.ps1
```

### 2. Create Schemas
```powershell
.\setup-database.ps1
```

### 3. Update Password
```powershell
.\update-database-password.ps1
```

### 4. Apply Migrations
```powershell
cd ..\scripts
.\apply-database-migrations.ps1
```

### 5. Verify Setup
```powershell
cd ..\Infrastructure
.\verify-database-setup.ps1
```

## ⚙️ Prerequisites

- ✅ AWS CLI: `aws --version`
- ✅ psql: `psql --version` (or use AWS RDS Query Editor)
- ✅ .NET SDK: `dotnet --version`
- ✅ Aurora password available

## 🔍 Quick Checks

### Check IAM Role
```bash
aws iam get-role --role-name ecsTaskExecutionRole --region eu-north-1
```

### Check NAT Gateway (CRITICAL for private subnets)
```bash
aws ec2 describe-nat-gateways --region eu-north-1 --filter 'Name=vpc-id,Values=<VPC_ID>'
```

### Check Database Schemas
```sql
SELECT schema_name 
FROM information_schema.schemata 
WHERE schema_name LIKE 'amesa_%';
```

### Check Migrations
```powershell
cd BE/AmesaBackend.Auth
dotnet ef migrations list --context AuthDbContext
```

## 🐛 Common Issues

| Issue | Solution |
|-------|----------|
| ECR pull fails | Check NAT Gateway exists, run `fix-ecr-network-access.ps1` |
| Password still CHANGE_ME | Run `update-database-password.ps1` |
| Schema not found | Run `setup-database.ps1` |
| Migration fails | Verify password updated, schemas exist |

## 📁 Key Files

- **Master Script**: `deploy-database-setup.ps1`
- **SQL Schemas**: `create-database-schemas.sql`
- **Full Guide**: `DATABASE_SETUP_GUIDE.md`
- **Verification**: `verify-database-setup.ps1`

## 🔗 Aurora Connection

- **Host**: `amesadbmain.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com`
- **Database**: `postgres`
- **Username**: `dror`
- **Port**: `5432`

## 📊 Services & Schemas

| Service | Schema |
|---------|--------|
| Auth | `amesa_auth` |
| Payment | `amesa_payment` |
| Lottery | `amesa_lottery` |
| Content | `amesa_content` |
| Notification | `amesa_notification` |
| Lottery Results | `amesa_lottery_results` |
| Analytics | `amesa_analytics` |

## ⚠️ Critical Notes

1. **ECS tasks are in PRIVATE subnets** → NAT Gateway REQUIRED
2. **Password in appsettings.json** → Consider AWS Secrets Manager
3. **Schemas must exist** before running migrations

---

**Need help?** See `DATABASE_SETUP_GUIDE.md` for detailed instructions.

