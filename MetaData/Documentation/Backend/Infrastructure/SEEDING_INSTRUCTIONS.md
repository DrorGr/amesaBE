# Database Seeding Instructions

## Prerequisites

### 1. Install AWS Session Manager Plugin

ECS Exec requires the AWS Session Manager Plugin. Install it:

**Windows:**
```powershell
cd BE/Infrastructure
.\install-session-manager-plugin.ps1
```

**Or manually:**
1. Download: https://s3.amazonaws.com/session-manager-downloads/plugin/latest/windows/SessionManagerPluginSetup.exe
2. Run the installer
3. Restart your terminal

**Linux/Mac:**
```bash
curl "https://s3.amazonaws.com/session-manager-downloads/plugin/latest/mac/sessionmanager-bundle.zip" -o "sessionmanager-bundle.zip"
unzip sessionmanager-bundle.zip
sudo ./sessionmanager-bundle/install -i /usr/local/sessionmanagerplugin -b /usr/local/bin/session-manager-plugin
```

### 2. Verify Installation

```powershell
session-manager-plugin
# Should show version information, not "command not found"
```

## Seeding Methods

### Method 1: Interactive ECS Exec (Recommended)

1. **Get a running task:**
```powershell
$taskArn = aws ecs list-tasks --cluster Amesa --service-name amesa-lottery-service --region eu-north-1 --query "taskArns[0]" --output text
```

2. **Start interactive session:**
```powershell
aws ecs execute-command `
    --cluster Amesa `
    --task $taskArn `
    --container amesa-lottery-service-container `
    --interactive `
    --region eu-north-1
```

3. **Once in the container, run:**
```bash
# Set database connection
export DB_ENDPOINT='amesa-prod-instance-1.cruuae28ob7m.eu-north-1.rds.amazonaws.com'
export DB_NAME='amesa_prod'
export DB_USER='amesa_admin'
export DB_PASSWORD='u1fwn3s9'
export DB_PORT=5432
export PGPASSWORD='u1fwn3s9'

# Check if psql is available
which psql || apk add --no-cache postgresql-client  # Alpine
# OR
which psql || apt-get update && apt-get install -y postgresql-client  # Debian/Ubuntu

# Test connection
psql -h $DB_ENDPOINT -p $DB_PORT -U $DB_USER -d $DB_NAME -c "SELECT 1;"

# Create/verify schemas
for schema in amesa_auth amesa_content amesa_lottery amesa_lottery_results amesa_payment amesa_notification amesa_analytics; do
    echo "Processing schema: $schema"
    psql -h $DB_ENDPOINT -p $DB_PORT -U $DB_USER -d $DB_NAME -c "CREATE SCHEMA IF NOT EXISTS $schema;"
    psql -h $DB_ENDPOINT -p $DB_PORT -U $DB_USER -d $DB_NAME -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='$schema';"
done
```

### Method 2: Automated Script (After Plugin Installation)

Once the Session Manager Plugin is installed, run:

```powershell
cd BE/Infrastructure
.\run-database-seeding.ps1 -DbPassword "u1fwn3s9"
```

### Method 3: Use .NET Seeder in Container

If the container has .NET and your seeder code:

1. **Get into container** (Method 1 above)
2. **Upload seeder code** or use existing code
3. **Run seeder:**
```bash
export DB_CONNECTION_STRING="Host=amesa-prod-instance-1.cruuae28ob7m.eu-north-1.rds.amazonaws.com;Port=5432;Database=amesa_prod;Username=amesa_admin;Password=u1fwn3s9;SearchPath=amesa_lottery;"
dotnet run -- --seeder
```

## Current Status

- ✅ ECS Exec is enabled on `amesa-lottery-service`
- ✅ Database password: `u1fwn3s9`
- ✅ Database endpoint: `amesa-prod-instance-1.cruuae28ob7m.eu-north-1.rds.amazonaws.com`
- ⚠️  Session Manager Plugin needs to be installed

## Next Steps

1. **Install Session Manager Plugin** (see above)
2. **Restart terminal/PowerShell**
3. **Run the seeding script** or use interactive method

## Troubleshooting

### "SessionManagerPlugin is not found"
- Install the plugin (see Prerequisites above)
- Restart your terminal
- Verify: `session-manager-plugin --version`

### "Cannot connect to database"
- Check security groups allow connections from ECS tasks
- Verify database endpoint is correct
- Check credentials

### "psql not found"
- Install PostgreSQL client in container:
  - Alpine: `apk add --no-cache postgresql-client`
  - Debian/Ubuntu: `apt-get update && apt-get install -y postgresql-client`

### "No running tasks"
- Ensure service has at least one running task
- Check service status in AWS Console

