# Database Setup Guide

This guide covers the complete database setup process for Amesa microservices, including ECR network access, schema creation, password updates, and migrations.

## Quick Start

Run the master script to execute all setup tasks:

```powershell
cd BE/Infrastructure
.\deploy-database-setup.ps1
```

This script will:
1. Fix ECR network access for ECS tasks
2. Create database schemas in Aurora
3. Update database password in all appsettings.json files
4. Apply database migrations

## Prerequisites

Before running the setup scripts, ensure you have:

1. **AWS CLI** installed and configured
   ```powershell
   aws --version
   aws configure
   ```

2. **PostgreSQL client (psql)** installed
   - Windows: Install PostgreSQL client tools or use AWS RDS Query Editor
   - Verify: `psql --version`

3. **.NET SDK 8.0** installed
   - Verify: `dotnet --version`

4. **Aurora Database Password** available
   - Get from AWS Secrets Manager or your secure storage

5. **Database Access** configured
   - Security groups allow your IP to connect to Aurora
   - Aurora endpoint: `amesadbmain.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com`

## Step-by-Step Instructions

### Step 1: Fix ECR Network Access

This ensures ECS tasks can pull Docker images from ECR.

```powershell
cd BE/Infrastructure
.\fix-ecr-network-access.ps1
```

**What it does:**
- Verifies/creates IAM role `ecsTaskExecutionRole`
- Attaches ECR permissions (GetAuthorizationToken, BatchGetImage, etc.)
- Attaches CloudWatch Logs permissions
- Provides guidance on VPC configuration

**Manual verification required:**
- Verify VPC has NAT Gateway (for private subnets) or Internet Gateway (for public subnets)
- Verify security groups allow outbound HTTPS (443) to ECR endpoints
- Check route tables have proper routes

### Step 2: Create Database Schemas

Creates the required schemas in Aurora PostgreSQL.

```powershell
cd BE/Infrastructure
.\setup-database.ps1
```

**What it does:**
- Connects to Aurora PostgreSQL
- Creates schemas: `amesa_auth`, `amesa_payment`, `amesa_lottery`, `amesa_content`, `amesa_notification`, `amesa_lottery_results`, `amesa_analytics`
- Verifies schemas were created

**Alternative (Manual):**
```bash
psql -h amesadbmain.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com \
     -U dror \
     -d postgres \
     -f BE/Infrastructure/create-database-schemas.sql
```

### Step 3: Update Database Password

Updates the database password in all `appsettings.json` files.

```powershell
cd BE/Infrastructure
.\update-database-password.ps1
```

**What it does:**
- Prompts for new Aurora PostgreSQL password
- Updates `Password=CHANGE_ME` to actual password in all service `appsettings.json` files
- Updates: Auth, Payment, Lottery, Content, Notification, LotteryResults, Analytics, Admin

**Files updated:**
- `BE/AmesaBackend.Auth/appsettings.json`
- `BE/AmesaBackend.Payment/appsettings.json`
- `BE/AmesaBackend.Lottery/appsettings.json`
- `BE/AmesaBackend.Content/appsettings.json`
- `BE/AmesaBackend.Notification/appsettings.json`
- `BE/AmesaBackend.LotteryResults/appsettings.json`
- `BE/AmesaBackend.Analytics/appsettings.json`
- `BE/AmesaBackend.Admin/appsettings.json`

**Security Recommendation:**
Consider using AWS Secrets Manager instead of storing passwords in appsettings.json files.

### Step 4: Apply Database Migrations

Applies Entity Framework Core migrations to create database tables.

```powershell
cd BE/scripts
.\apply-database-migrations.ps1
```

**What it does:**
- Checks for existing migrations in each service
- Creates migrations if they don't exist
- Applies migrations to Aurora PostgreSQL
- Uses schema-specific SearchPath from connection strings

**Services migrated:**
- Auth (AuthDbContext)
- Content (ContentDbContext)
- Notification (NotificationDbContext)
- Payment (PaymentDbContext)
- Lottery (LotteryDbContext)
- LotteryResults (LotteryResultsDbContext)
- Analytics (AnalyticsDbContext)

## Troubleshooting

### ECR Network Access Issues

**Problem:** ECS tasks cannot pull images from ECR

**Solutions:**
1. Verify IAM role has ECR permissions:
   ```bash
   aws iam get-role-policy --role-name ecsTaskExecutionRole --policy-name ECR-Access-Policy
   ```

2. Check VPC configuration:
   - Private subnets need NAT Gateway
   - Public subnets need Internet Gateway
   - Route tables must have routes to NAT/IGW

3. Verify security groups:
   - Allow outbound HTTPS (443) to `0.0.0.0/0`
   - ECR endpoints are accessible from VPC

### Database Schema Creation Issues

**Problem:** Cannot connect to Aurora

**Solutions:**
1. Verify security group allows your IP:
   ```bash
   aws rds describe-db-clusters --db-cluster-identifier amesadbmain --region eu-north-1
   ```

2. Test connection manually:
   ```bash
   psql -h amesadbmain.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com -U dror -d postgres
   ```

3. Use AWS RDS Query Editor as alternative

### Migration Issues

**Problem:** Migrations fail with connection errors

**Solutions:**
1. Verify password is updated in appsettings.json
2. Verify schemas exist in Aurora
3. Check connection string format:
   ```
   Host=amesadbmain.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com;Port=5432;Database=postgres;Username=dror;Password=<ACTUAL_PASSWORD>;SearchPath=amesa_<service>;
   ```

4. Test connection from service directory:
   ```powershell
   cd BE/AmesaBackend.Auth
   dotnet ef database update --context AuthDbContext --verbose
   ```

## Verification

After completing all steps, verify:

1. **ECR Access:**
   ```bash
   aws ecs describe-services --cluster Amesa --services amesa-auth-service --region eu-north-1
   # Check for ResourceInitializationError
   ```

2. **Database Schemas:**
   ```sql
   SELECT schema_name 
   FROM information_schema.schemata 
   WHERE schema_name LIKE 'amesa_%'
   ORDER BY schema_name;
   ```

3. **Database Tables:**
   ```sql
   SELECT table_schema, table_name 
   FROM information_schema.tables 
   WHERE table_schema LIKE 'amesa_%'
   ORDER BY table_schema, table_name;
   ```

4. **Migrations Applied:**
   ```powershell
   cd BE/AmesaBackend.Auth
   dotnet ef migrations list --context AuthDbContext
   ```

## Next Steps

After database setup is complete:

1. **Deploy Services to ECS:**
   ```bash
   # Push code to trigger CI/CD
   cd BE
   git add .
   git commit -m "Database setup complete"
   git push origin main
   ```

2. **Verify Services:**
   - Check ECS service status
   - Verify health checks passing
   - Test API endpoints

3. **Monitor Logs:**
   ```bash
   aws logs tail /ecs/amesa-auth-service --follow --region eu-north-1
   ```

## Security Best Practices

1. **Use AWS Secrets Manager** for database passwords instead of appsettings.json
2. **Rotate passwords** regularly
3. **Use IAM database authentication** if possible
4. **Enable encryption at rest** for Aurora
5. **Restrict security group** access to ECS tasks only

## Support

For issues or questions:
- Review `BE/HANDOFF.md` for architecture details
- Check `BE/DEPLOYMENT_FINAL_CHECKLIST.md` for deployment steps
- Review AWS CloudWatch logs for service errors

---

**Last Updated:** 2025-01-27  
**Status:** Ready for use

