# Run Database Seeding Now

## Quick Command

Copy and paste this entire block into PowerShell:

```powershell
cd C:\Users\dror0\Curser-Repos\AmesaBase-Monorepo
$taskArn = aws ecs list-tasks --cluster Amesa --service-name amesa-lottery-service --region eu-north-1 --query "taskArns[0]" --output text
$env:Path += ";C:\Program Files\Amazon\SessionManagerPlugin\bin"
aws ecs execute-command --cluster Amesa --task $taskArn --container amesa-lottery-service-container --interactive --region eu-north-1
```

## Once Connected, Run This:

```bash
# Set up connection
export PGPASSWORD='u1fwn3s9'
export DB_HOST='amesa-prod-instance-1.cruuae28ob7m.eu-north-1.rds.amazonaws.com'
export DB_NAME='amesa_prod'
export DB_USER='amesa_admin'
export DB_PORT=5432

# Install psql if needed
which psql || apk add --no-cache postgresql-client

# Run the complete seeding script
psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -f /dev/stdin <<'EOF'
```

Then paste the contents of `BE/Infrastructure/complete-seed-all-data.sql` and end with:

```
EOF
```

## What Gets Seeded

✅ **amesa_content schema:**
- 6 Languages (English, Hebrew, Arabic, Spanish, French, Polish)

✅ **amesa_auth schema:**
- 5 Users (admin + 4 regular users)
- User addresses
- User phones
- Password hashes are correct (Admin123! and Password123!)

## After Seeding

Check the results:

```bash
# Count languages
psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "SELECT COUNT(*) FROM amesa_content.\"Languages\";"

# Count users
psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "SELECT COUNT(*) FROM amesa_auth.\"Users\";"

# List users
psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "SELECT \"Username\", \"Email\", \"Status\" FROM amesa_auth.\"Users\";"
```

## For Complex Data

For houses, comprehensive translations, lottery tickets, etc., use the .NET DatabaseSeeder which handles:
- Complex relationships
- House images
- Comprehensive translations
- Lottery tickets and draws

