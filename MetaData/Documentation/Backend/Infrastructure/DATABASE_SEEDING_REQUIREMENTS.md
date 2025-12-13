# Database Seeding Requirements

## Database Architecture

Based on the infrastructure, you have an **Aurora PostgreSQL cluster** with:
- **Database**: `amesa_prod` (or `postgres` as default)
- **Schemas** (not separate databases):
  1. `amesa_auth` - Authentication service
  2. `amesa_content` - Content/Translations service
  3. `amesa_notification` - Notifications service
  4. `amesa_payment` - Payment service
  5. `amesa_lottery` - Lottery service
  6. `amesa_lottery_results` - Lottery results service
  7. `amesa_analytics` - Analytics service

## What I Need From You

### 1. Database Connection Details

**Option A: If you have direct access from your machine**
- [ ] Aurora **cluster endpoint** (e.g., `amesadbmain.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com`)
- [ ] Database **username** (e.g., `dror` or `amesa_admin`)
- [ ] Database **password**
- [ ] Database **name** (likely `amesa_prod` or `postgres`)

**Option B: If you need to connect via EC2/Bastion**
- [ ] EC2 instance IP or hostname
- [ ] SSH key file path
- [ ] Username for SSH (e.g., `ec2-user`, `ubuntu`)
- [ ] Database connection details (same as Option A)

### 2. Access Method

How can I access the database?
- [ ] **Direct from localhost** (if security groups allow)
- [ ] **Via EC2 instance** (SSH tunnel)
- [ ] **Via AWS Systems Manager Session Manager** (if configured)
- [ ] **Via VPN/Bastion host**
- [ ] **Other** (please specify)

### 3. Which Schemas Need Seeding?

Please confirm which schemas need data:
- [ ] `amesa_auth` - Users, authentication data
- [ ] `amesa_content` - Translations, content
- [ ] `amesa_notification` - Notification templates
- [ ] `amesa_payment` - Payment methods, transactions
- [ ] `amesa_lottery` - Houses, lottery tickets, draws
- [ ] `amesa_lottery_results` - Lottery results, winners
- [ ] `amesa_analytics` - Analytics data

### 4. Seeding Strategy

**Option A: Seed all schemas with full data**
- Users, houses, tickets, translations, etc. in all schemas

**Option B: Seed specific schemas only**
- Which schemas? (specify above)

**Option C: Seed with minimal test data**
- Just enough to test functionality

### 5. Existing Data

- [ ] **Clear existing data** before seeding? (⚠️ DESTRUCTIVE)
- [ ] **Keep existing data** and add new records
- [ ] **Skip if data exists** (only seed empty tables)

## Connection String Format

Once I have the details, I'll use this format:
```
Host=<endpoint>;Port=5432;Database=<db_name>;Username=<username>;Password=<password>;SearchPath=<schema_name>;
```

## What I'll Create

Once you provide the information, I'll create:

1. **Multi-Schema Seeder Script** (`seed-all-schemas.ps1`)
   - Connects to each schema
   - Seeds appropriate data for each service
   - Provides progress feedback
   - Handles errors gracefully

2. **Schema-Specific Seeders**
   - Individual scripts for each schema if needed
   - Can run independently

3. **Verification Script**
   - Checks what data was seeded
   - Reports counts per schema

## Example Response

Please provide information like this:

```
Connection Method: Direct from localhost
Endpoint: amesadbmain.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com
Database: amesa_prod
Username: dror
Password: [your password]
Port: 5432

Schemas to seed: All (amesa_auth, amesa_content, amesa_lottery, amesa_lottery_results, amesa_payment, amesa_notification, amesa_analytics)

Strategy: Seed all with full data
Existing data: Skip if data exists
```

## Security Note

⚠️ **Never share passwords in chat!** Instead:
- Provide password when I ask (I'll use it only in the script)
- Or set it as an environment variable
- Or use AWS Secrets Manager (if configured)

