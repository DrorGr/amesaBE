# Quick Database Seeding Guide

## Status ✅

- ✅ Session Manager Plugin: Installed
- ✅ ECS Exec: Enabled
- ✅ Database Password: `u1fwn3s9`

## Quick Method: Interactive ECS Exec

### Step 1: Get Task ARN

```powershell
$taskArn = aws ecs list-tasks --cluster Amesa --service-name amesa-lottery-service --region eu-north-1 --query "taskArns[0]" --output text
```

### Step 2: Connect to Container

```powershell
aws ecs execute-command `
    --cluster Amesa `
    --task $taskArn `
    --container amesa-lottery-service-container `
    --interactive `
    --region eu-north-1
```

### Step 3: Once Connected, Run These Commands

```bash
# Set environment variables
export PGPASSWORD='u1fwn3s9'
export DB_HOST='amesa-prod-instance-1.cruuae28ob7m.eu-north-1.rds.amazonaws.com'
export DB_NAME='amesa_prod'
export DB_USER='amesa_admin'
export DB_PORT=5432

# Install PostgreSQL client if needed
which psql || apk add --no-cache postgresql-client
# OR for Debian/Ubuntu:
# which psql || (apt-get update && apt-get install -y postgresql-client)

# Test connection
psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "SELECT 1;"

# Create all schemas
for schema in amesa_auth amesa_content amesa_lottery amesa_lottery_results amesa_payment amesa_notification amesa_analytics; do
    echo "Creating schema: $schema"
    psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "CREATE SCHEMA IF NOT EXISTS $schema;"
    COUNT=$(psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -tAc "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='$schema';" 2>/dev/null || echo "0")
    echo "  Tables in $schema: $COUNT"
done

echo "✅ All schemas created!"
```

## Alternative: One-Line Command

If you want to run it as a single command:

```powershell
$taskArn = aws ecs list-tasks --cluster Amesa --service-name amesa-lottery-service --region eu-north-1 --query "taskArns[0]" --output text

aws ecs execute-command `
    --cluster Amesa `
    --task $taskArn `
    --container amesa-lottery-service-container `
    --interactive `
    --command "bash" `
    --region eu-north-1
```

Then paste the bash commands from Step 3 above.

## What Gets Created

This will create/verify these 7 schemas:
- `amesa_auth`
- `amesa_content`
- `amesa_lottery`
- `amesa_lottery_results`
- `amesa_payment`
- `amesa_notification`
- `amesa_analytics`

## Next Steps After Schema Creation

1. **Run migrations** for each service to create tables
2. **Run .NET seeder** to populate data
3. **Or use SQL scripts** to seed data directly

## Troubleshooting

### "psql: command not found"
Install PostgreSQL client:
- Alpine: `apk add --no-cache postgresql-client`
- Debian/Ubuntu: `apt-get update && apt-get install -y postgresql-client`

### "Connection refused" or "Cannot connect"
- Check security groups allow connections from ECS tasks
- Verify database endpoint is correct
- Check credentials

### "Schema already exists"
That's fine! The script uses `CREATE SCHEMA IF NOT EXISTS`, so it won't fail if schemas already exist.

