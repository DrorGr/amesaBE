# Database Setup - Pre-Deployment Checklist

Use this checklist before deploying services to ECS.

## Prerequisites

- [ ] AWS CLI installed and configured
  ```powershell
  aws --version
  aws configure list
  ```

- [ ] PostgreSQL client (psql) installed OR AWS RDS Query Editor access
  ```powershell
  psql --version
  ```

- [ ] .NET SDK 8.0 installed
  ```powershell
  dotnet --version
  ```

- [ ] Aurora database password available
  - [ ] Stored securely (AWS Secrets Manager recommended)
  - [ ] Not stored in code or documentation

- [ ] Database access configured
  - [ ] Security group allows your IP (for local setup)
  - [ ] Security group allows ECS tasks (for production)
  - [ ] Aurora endpoint accessible

## ECR Network Access

- [ ] IAM role `ecsTaskExecutionRole` exists
  ```bash
  aws iam get-role --role-name ecsTaskExecutionRole --region eu-north-1
  ```

- [ ] IAM role has ECR permissions
  ```bash
  aws iam list-role-policies --role-name ecsTaskExecutionRole --region eu-north-1
  ```

- [ ] NAT Gateway exists (CRITICAL for private subnets)
  ```bash
  aws ec2 describe-nat-gateways --region eu-north-1 --filter 'Name=vpc-id,Values=<VPC_ID>'
  ```

- [ ] Route tables configured
  - [ ] Private subnet route: `0.0.0.0/0` → NAT Gateway
  - [ ] Public subnet route: `0.0.0.0/0` → Internet Gateway

- [ ] Security groups allow outbound HTTPS (443)
  ```bash
  aws ec2 describe-security-groups --group-ids <sg-id> --region eu-north-1
  ```

**Action if missing:** Run `.\fix-ecr-network-access.ps1`

## Database Setup

- [ ] Database schemas created
  ```sql
  SELECT schema_name FROM information_schema.schemata WHERE schema_name LIKE 'amesa_%';
  ```
  Expected: 7 schemas (auth, payment, lottery, content, notification, lottery_results, analytics)

- [ ] Database password updated in all services
  ```powershell
  Get-ChildItem -Path BE -Recurse -Filter "appsettings.json" | Select-String "Password=CHANGE_ME"
  ```
  Expected: No matches (all passwords updated)

- [ ] Migrations created (if needed)
  ```powershell
  Get-ChildItem -Path BE -Recurse -Filter "Migrations" -Directory
  ```
  Expected: 7 migration directories

- [ ] Migrations applied to database
  ```powershell
  cd BE/AmesaBackend.Auth
  dotnet ef migrations list --context AuthDbContext
  ```
  Expected: Migrations listed, database updated

**Actions if missing:**
1. Run `.\setup-database.ps1`
2. Run `.\update-database-password.ps1`
3. Run `..\scripts\apply-database-migrations.ps1`

## Verification

- [ ] Run verification script
  ```powershell
  .\verify-database-setup.ps1
  ```
  Expected: All checks pass

- [ ] Test database connection
  ```bash
  psql -h amesadbmain.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com -U dror -d postgres
  ```

- [ ] Verify schemas accessible
  ```sql
  SET search_path TO amesa_auth;
  \dn
  ```

- [ ] Check migration tables exist
  ```sql
  SELECT * FROM amesa_auth.__EFMigrationsHistory;
  ```

## ECS Configuration

- [ ] ECS cluster exists
  ```bash
  aws ecs describe-clusters --clusters Amesa --region eu-north-1
  ```

- [ ] Task definitions registered
  ```bash
  aws ecs list-task-definitions --family-prefix amesa --region eu-north-1
  ```
  Expected: 8 task definitions

- [ ] ECR repositories exist
  ```bash
  aws ecr describe-repositories --region eu-north-1 --query 'repositories[?contains(repositoryName, `amesa-`)].repositoryName'
  ```
  Expected: 8 repositories

- [ ] Docker images pushed to ECR
  ```bash
  aws ecr list-images --repository-name amesa-auth-service --region eu-north-1
  ```
  Expected: At least one image

## Security

- [ ] Passwords not in code
  - [ ] No `CHANGE_ME` in appsettings.json
  - [ ] Consider AWS Secrets Manager for production

- [ ] Security groups configured
  - [ ] Database: Allow only ECS tasks and admin IPs
  - [ ] ECS: Allow outbound HTTPS (443) for ECR
  - [ ] ECS: Allow inbound from ALB only

- [ ] IAM roles follow least privilege
  - [ ] ECS task execution role: ECR + CloudWatch Logs only
  - [ ] ECS task role: Application-specific permissions only

## Documentation

- [ ] All scripts documented
- [ ] Connection strings documented (without passwords)
- [ ] Architecture documented
- [ ] Troubleshooting guide available

## Final Steps

- [ ] All checklist items completed
- [ ] Verification script passes
- [ ] Ready to deploy services

**Next:** Deploy services to ECS or push code to trigger CI/CD

---

**Status Tracking:**
- ⬜ Not Started
- 🟡 In Progress
- ✅ Complete
- ❌ Blocked

---

**Last Updated**: 2025-01-27

