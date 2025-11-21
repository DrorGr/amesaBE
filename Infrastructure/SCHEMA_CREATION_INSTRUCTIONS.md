# Database Schema Creation - Quick Instructions

**Password**: Already configured in appsettings.json files ✅

## Option 1: AWS RDS Query Editor (Easiest - Recommended)

1. **Go to AWS Console** → **RDS** → **Databases**
2. **Select cluster**: `amesadbmain`
3. **Click "Query Editor"** (or go to RDS → Query Editor directly)
4. **Connect to database**:
   - Database: `postgres`
   - Username: `dror`
   - Password: `u1fwn3s9`
5. **Run this SQL**:

```sql
CREATE SCHEMA IF NOT EXISTS amesa_auth;
CREATE SCHEMA IF NOT EXISTS amesa_payment;
CREATE SCHEMA IF NOT EXISTS amesa_lottery;
CREATE SCHEMA IF NOT EXISTS amesa_content;
CREATE SCHEMA IF NOT EXISTS amesa_notification;
CREATE SCHEMA IF NOT EXISTS amesa_lottery_results;
CREATE SCHEMA IF NOT EXISTS amesa_analytics;

-- Verify schemas were created
SELECT schema_name 
FROM information_schema.schemata 
WHERE schema_name LIKE 'amesa_%'
ORDER BY schema_name;
```

6. **Verify**: You should see 7 schemas listed

## Option 2: Install psql and Run Script

1. **Install PostgreSQL client tools** (if not installed)
2. **Run**:
```bash
psql -h amesadbmain.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com \
     -U dror \
     -d postgres \
     -f BE/Infrastructure/create-database-schemas.sql
```

## Option 3: Use Existing Script

```powershell
cd BE/Infrastructure
.\setup-database.ps1
```

(Will prompt for password - use: `u1fwn3s9`)

---

**After schemas are created**, proceed with:
1. Building projects: `dotnet build`
2. Creating migrations: `.\scripts\database-migrations.ps1`
3. Applying migrations: `.\scripts\apply-database-migrations.ps1`

