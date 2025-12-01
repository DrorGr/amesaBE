# How to Run the Lottery Favorites SQL Scripts

## Database Connection Details
- **Host**: amesa-prod-instance-1.cruuae28ob7m.eu-north-1.rds.amazonaws.com
- **Username**: amesa_admin
- **Password**: u1fwn3s9
- **Database**: postgres (or your actual database name)

## Option 1: Using psql Command Line (if PostgreSQL client installed)

### Step 1: Navigate to scripts directory
```powershell
cd C:\Users\dror0\Curser-Repos\AmesaBase-Monorepo\BE\Infrastructure\sql
```

### Step 2: Set password and run migration
```powershell
$env:PGPASSWORD='u1fwn3s9'
psql -h amesa-prod-instance-1.cruuae28ob7m.eu-north-1.rds.amazonaws.com -U amesa_admin -d postgres -f lottery-favorites-migration.sql
```

### Step 3: Run translations script
```powershell
psql -h amesa-prod-instance-1.cruuae28ob7m.eu-north-1.rds.amazonaws.com -U amesa_admin -d postgres -f lottery-favorites-translations.sql
```

## Option 2: Using PowerShell Script

Run the provided script:
```powershell
cd C:\Users\dror0\Curser-Repos\AmesaBase-Monorepo\BE\Infrastructure\sql
.\run-migration.ps1
```

## Option 3: Using pgAdmin or DBeaver

1. Connect to your database using the credentials above
2. Open the SQL script file: `lottery-favorites-migration.sql`
3. Execute the script
4. Open the SQL script file: `lottery-favorites-translations.sql`
5. Execute the script

## Option 4: Using Azure Data Studio / VS Code with PostgreSQL Extension

1. Connect to your database
2. Open the SQL files
3. Execute them

## Verification Queries

After running both scripts, verify with these queries:

### Check indexes were created:
```sql
SELECT 
    schemaname,
    tablename,
    indexname,
    indexdef 
FROM pg_indexes 
WHERE schemaname IN ('amesa_lottery', 'amesa_auth')
AND indexname LIKE 'idx_%'
ORDER BY schemaname, tablename, indexname;
```

### Check view was created:
```sql
SELECT 
    table_schema,
    table_name
FROM information_schema.views
WHERE table_schema = 'amesa_auth'
AND table_name = 'user_lottery_dashboard';
```

### Check translations were inserted:
```sql
SELECT 
    "Category",
    COUNT(DISTINCT "Key") as key_count,
    COUNT(*) as total_translations
FROM amesa_content.translations
WHERE "Category" = 'Lottery'
GROUP BY "Category";
```

## Important Notes

1. **Run migration script FIRST** (creates indexes and view)
2. **Then run translations script** (adds translation keys)
3. Both scripts use `IF NOT EXISTS` and `ON CONFLICT DO NOTHING` so they're safe to re-run
4. Make sure you're connected to the correct database (check database name)

## Troubleshooting

If you get "psql: command not found":
- Install PostgreSQL client tools from https://www.postgresql.org/download/
- Or use Option 3 (pgAdmin/DBeaver) instead

If connection fails:
- Check your network/VPN connection
- Verify RDS security group allows your IP
- Verify database name is correct















