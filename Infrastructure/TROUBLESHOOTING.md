# Database Setup - Troubleshooting Guide

## Common Issues and Solutions

### 1. ECR Network Access Failures

#### Symptom
```
ResourceInitializationError: unable to pull secrets or registry auth
The task cannot pull registry auth from Amazon ECR
```

#### Causes
- Missing NAT Gateway (ECS tasks in private subnets)
- IAM role missing ECR permissions
- Security group blocking outbound HTTPS (443)
- Route table not configured correctly

#### Solutions

**Step 1: Verify IAM Role**
```powershell
.\fix-ecr-network-access.ps1
```

**Step 2: Check NAT Gateway**
```bash
# Get VPC ID from ECS service
aws ecs describe-services --cluster Amesa --services amesa-auth-service --region eu-north-1 --query 'services[0].networkConfiguration.awsvpcConfiguration.subnets[0]'

# Get VPC ID from subnet
aws ec2 describe-subnets --subnet-ids <subnet-id> --region eu-north-1 --query 'Subnets[0].VpcId'

# Check NAT Gateway
aws ec2 describe-nat-gateways --region eu-north-1 --filter 'Name=vpc-id,Values=<VPC_ID>' --query 'NatGateways[?State==`available`]'
```

**Step 3: Verify Route Table**
```bash
# Get route table for subnet
aws ec2 describe-route-tables --region eu-north-1 --filters "Name=association.subnet-id,Values=<subnet-id>" --query 'RouteTables[0].Routes'
```

**Step 4: Check Security Group**
```bash
# Verify outbound HTTPS allowed
aws ec2 describe-security-groups --group-ids <security-group-id> --region eu-north-1 --query 'SecurityGroups[0].IpPermissionsEgress'
```

**Expected Configuration:**
- Route table: `0.0.0.0/0` → NAT Gateway
- Security group: Outbound HTTPS (443) to `0.0.0.0/0`

---

### 2. Database Connection Failures

#### Symptom
```
Npgsql.NpgsqlException: Connection refused
or
Password authentication failed
```

#### Causes
- Incorrect password in appsettings.json
- Security group blocking database access
- Wrong endpoint or port
- Database not accessible from your IP

#### Solutions

**Step 1: Verify Password Updated**
```powershell
# Check all appsettings.json files
Get-ChildItem -Path BE -Recurse -Filter "appsettings.json" | Select-String "Password=CHANGE_ME"
```

If found, run:
```powershell
.\update-database-password.ps1
```

**Step 2: Test Connection Manually**
```bash
psql -h amesadbmain.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com \
     -U dror \
     -d postgres \
     -p 5432
```

**Step 3: Check Security Group**
```bash
# Get RDS security group
aws rds describe-db-clusters --db-cluster-identifier amesadbmain --region eu-north-1 --query 'DBClusters[0].VpcSecurityGroups'

# Check inbound rules
aws ec2 describe-security-groups --group-ids <sg-id> --region eu-north-1 --query 'SecurityGroups[0].IpPermissions'
```

**Step 4: Verify Connection String Format**
```
Host=amesadbmain.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com;Port=5432;Database=postgres;Username=dror;Password=<ACTUAL_PASSWORD>;SearchPath=amesa_<service>;
```

---

### 3. Schema Creation Failures

#### Symptom
```
ERROR: permission denied to create schema
or
ERROR: schema "amesa_auth" already exists
```

#### Causes
- Insufficient database permissions
- Schema already exists (not an error)
- Connection issues

#### Solutions

**Step 1: Verify Connection**
```bash
psql -h <aurora-endpoint> -U dror -d postgres
```

**Step 2: Check Existing Schemas**
```sql
SELECT schema_name 
FROM information_schema.schemata 
WHERE schema_name LIKE 'amesa_%';
```

**Step 3: Create Schemas Manually (if needed)**
```sql
CREATE SCHEMA IF NOT EXISTS amesa_auth;
CREATE SCHEMA IF NOT EXISTS amesa_payment;
-- ... etc
```

**Step 4: Verify Permissions**
```sql
-- Check current user permissions
SELECT * FROM information_schema.role_table_grants WHERE grantee = 'dror';
```

---

### 4. Migration Failures

#### Symptom
```
Npgsql.NpgsqlException: relation "schema_migrations" does not exist
or
ERROR: schema "amesa_auth" does not exist
```

#### Causes
- Schemas not created before migrations
- Wrong SearchPath in connection string
- Migration targeting wrong schema

#### Solutions

**Step 1: Verify Schemas Exist**
```sql
SELECT schema_name FROM information_schema.schemata WHERE schema_name LIKE 'amesa_%';
```

If missing, run:
```powershell
.\setup-database.ps1
```

**Step 2: Verify Connection String**
Check `appsettings.json` has correct `SearchPath`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=...;SearchPath=amesa_auth;"
  }
}
```

**Step 3: Run Migrations with Verbose**
```powershell
cd BE/AmesaBackend.Auth
dotnet ef database update --context AuthDbContext --verbose
```

**Step 4: Check DbContext Configuration**
Verify `OnModelCreating` has:
```csharp
modelBuilder.HasDefaultSchema("amesa_auth");
```

---

### 5. Password Update Script Issues

#### Symptom
```
Script runs but passwords not updated
or
Access denied errors
```

#### Causes
- File permissions
- Files in use
- Path issues

#### Solutions

**Step 1: Run as Administrator**
```powershell
# Right-click PowerShell → Run as Administrator
cd BE/Infrastructure
.\update-database-password.ps1
```

**Step 2: Check File Permissions**
```powershell
Get-ChildItem -Path BE -Recurse -Filter "appsettings.json" | Get-Acl
```

**Step 3: Verify Files Updated**
```powershell
Get-ChildItem -Path BE -Recurse -Filter "appsettings.json" | Select-String "Password=" | Select-String -NotMatch "CHANGE_ME"
```

---

### 6. Verification Script Issues

#### Symptom
```
Verification script shows errors
or
Prerequisites not found
```

#### Solutions

**Install Missing Prerequisites:**

**AWS CLI:**
```powershell
# Download from: https://aws.amazon.com/cli/
# Or use winget
winget install Amazon.AWSCLI
```

**PostgreSQL Client:**
```powershell
# Download from: https://www.postgresql.org/download/windows/
# Or use chocolatey
choco install postgresql
```

**.NET SDK:**
```powershell
# Download from: https://dotnet.microsoft.com/download
# Or use winget
winget install Microsoft.DotNet.SDK.8
```

---

### 7. NAT Gateway Not Found

#### Symptom
```
ECS tasks cannot pull images
NAT Gateway check returns empty
```

#### Solutions

**Create NAT Gateway:**

1. **Create Elastic IP:**
```bash
aws ec2 allocate-address --domain vpc --region eu-north-1
```

2. **Get Public Subnet ID:**
```bash
aws ec2 describe-subnets --region eu-north-1 --filters "Name=vpc-id,Values=<VPC_ID>" --query 'Subnets[?MapPublicIpOnLaunch==`true`]'
```

3. **Create NAT Gateway:**
```bash
aws ec2 create-nat-gateway \
    --subnet-id <public-subnet-id> \
    --allocation-id <elastic-ip-allocation-id> \
    --region eu-north-1
```

4. **Update Route Table:**
```bash
aws ec2 create-route \
    --route-table-id <private-subnet-route-table-id> \
    --destination-cidr-block 0.0.0.0/0 \
    --nat-gateway-id <nat-gateway-id> \
    --region eu-north-1
```

**Note:** NAT Gateway costs ~$0.045/hour + data transfer costs.

---

## Diagnostic Commands

### Check ECS Service Status
```bash
aws ecs describe-services \
    --cluster Amesa \
    --services amesa-auth-service \
    --region eu-north-1 \
    --query 'services[0].[status,runningCount,desiredCount,events[0].message]'
```

### Check Task Logs
```bash
aws logs tail /ecs/amesa-auth-service --follow --region eu-north-1
```

### Check ECR Images
```bash
aws ecr list-images \
    --repository-name amesa-auth-service \
    --region eu-north-1
```

### Test Database Connection
```powershell
# PowerShell
$connectionString = "Host=amesadbmain.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com;Port=5432;Database=postgres;Username=dror;Password=<PASSWORD>;"
# Use psql or .NET connection test
```

---

## Getting Help

1. **Run Verification:**
   ```powershell
   .\verify-database-setup.ps1
   ```

2. **Check Logs:**
   - AWS CloudWatch: `/ecs/amesa-*-service`
   - GitHub Actions: Repository → Actions tab

3. **Review Documentation:**
   - `DATABASE_SETUP_GUIDE.md` - Full guide
   - `README.md` - Script overview
   - `HANDOFF.md` - Architecture details

4. **Common Commands:**
   - See `QUICK_REFERENCE.md` for quick commands

---

**Last Updated**: 2025-01-27

