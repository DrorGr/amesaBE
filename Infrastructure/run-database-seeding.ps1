# Database Seeding via ECS Exec - Complete Script
# Seeds all database schemas using ECS Exec

param(
    [string]$Cluster = "Amesa",
    [string]$Region = "eu-north-1",
    [string]$ServiceName = "amesa-lottery-service",
    [string]$DbEndpoint = "amesa-prod-instance-1.cruuae28ob7m.eu-north-1.rds.amazonaws.com",
    [string]$DbName = "amesa_prod",
    [string]$DbUser = "amesa_admin",
    [string]$DbPassword = "u1fwn3s9",
    [int]$DbPort = 5432
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Database Seeding via ECS Exec" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  Cluster: $Cluster" -ForegroundColor White
Write-Host "  Service: $ServiceName" -ForegroundColor White
Write-Host "  Database: $DbName @ $DbEndpoint" -ForegroundColor White
Write-Host ""

# Get running task
Write-Host "Finding running task..." -ForegroundColor Cyan
$taskArn = aws ecs list-tasks --cluster $Cluster --service-name $ServiceName --region $Region --desired-status RUNNING --query "taskArns[0]" --output text 2>&1

if ([string]::IsNullOrEmpty($taskArn) -or $taskArn -eq "None") {
    Write-Host "❌ No running tasks found for service: $ServiceName" -ForegroundColor Red
    exit 1
}

$taskArn = $taskArn.Trim()
Write-Host "✅ Found task: $($taskArn.Split('/')[-1])" -ForegroundColor Green

# Get container name
$taskDetails = aws ecs describe-tasks --cluster $Cluster --tasks $taskArn --region $Region --query "tasks[0].containers[0].name" --output text 2>&1
$containerName = $taskDetails.Trim()

if ([string]::IsNullOrEmpty($containerName)) {
    $containerName = $ServiceName
}

Write-Host "✅ Container: $containerName" -ForegroundColor Green
Write-Host ""

# Create comprehensive seeding script
$seedingScript = @"
#!/bin/bash
set -e

DB_ENDPOINT='$DbEndpoint'
DB_NAME='$DbName'
DB_USER='$DbUser'
DB_PASSWORD='$DbPassword'
DB_PORT=$DbPort

echo "========================================"
echo "Database Seeding Script"
echo "========================================"
echo ""
echo "Database: `$DB_NAME @ `$DB_ENDPOINT"
echo ""

# Export password for psql
export PGPASSWORD=`$DB_PASSWORD

# Function to check if command exists
command_exists() {
    command -v `"`$1`" >/dev/null 2>&1
}

# Check for psql
if ! command_exists psql; then
    echo "❌ psql not found. Installing PostgreSQL client..."
    # Try to install (works on Alpine/Debian/Ubuntu)
    if command_exists apk; then
        apk add --no-cache postgresql-client
    elif command_exists apt-get; then
        apt-get update && apt-get install -y postgresql-client
    elif command_exists yum; then
        yum install -y postgresql
    else
        echo "❌ Cannot install psql automatically. Please ensure PostgreSQL client is in the container."
        exit 1
    fi
fi

echo "✅ PostgreSQL client ready"
echo ""

# Test database connection
echo "Testing database connection..."
if ! psql -h `$DB_ENDPOINT -p `$DB_PORT -U `$DB_USER -d `$DB_NAME -c "SELECT 1;" >/dev/null 2>&1; then
    echo "❌ Cannot connect to database"
    echo "Check:"
    echo "  - Database endpoint: `$DB_ENDPOINT"
    echo "  - Credentials"
    echo "  - Security groups"
    exit 1
fi

echo "✅ Database connection successful"
echo ""

# Schemas to seed
SCHEMAS=("amesa_auth" "amesa_content" "amesa_lottery" "amesa_lottery_results" "amesa_payment" "amesa_notification" "amesa_analytics")

# Function to seed a schema
seed_schema() {
    local schema=`$1
    echo "🌱 Processing schema: `$schema"
    
    # Check if schema exists, create if not
    schema_exists=`$(psql -h `$DB_ENDPOINT -p `$DB_PORT -U `$DB_USER -d `$DB_NAME -tAc "SELECT 1 FROM information_schema.schemata WHERE schema_name='`$schema'" 2>/dev/null || echo "0")
    
    if [ "`$schema_exists" != "1" ]; then
        echo "  📝 Creating schema `$schema..."
        psql -h `$DB_ENDPOINT -p `$DB_PORT -U `$DB_USER -d `$DB_NAME -c "CREATE SCHEMA IF NOT EXISTS `$schema;" || {
            echo "  ❌ Failed to create schema `$schema"
            return 1
        }
        echo "  ✅ Schema `$schema created"
    else
        echo "  ✅ Schema `$schema already exists"
    fi
    
    # Check if tables exist
    table_count=`$(psql -h `$DB_ENDPOINT -p `$DB_PORT -U `$DB_USER -d `$DB_NAME -tAc "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='`$schema'" 2>/dev/null || echo "0")
    
    if [ "`$table_count" -eq "0" ]; then
        echo "  ⚠️  Schema `$schema has no tables yet"
        echo "  💡 Run migrations to create tables, then seed data"
    else
        echo "  ✅ Schema `$schema has `$table_count table(s)"
    fi
    
    return 0
}

# Process each schema
for schema in "`${SCHEMAS[@]}"; do
    seed_schema "`$schema" || {
        echo "❌ Failed to process schema: `$schema"
        exit 1
    }
    echo ""
done

echo "========================================"
echo "✅ Schema setup completed!"
echo "========================================"
echo ""
echo "Next steps:"
echo "  1. Ensure migrations are run for each schema"
echo "  2. Run the .NET seeder to populate data"
echo "  3. Or use SQL scripts to seed data directly"
echo ""
"@

Write-Host "Executing seeding script in container..." -ForegroundColor Cyan
Write-Host ""

# Save script to temp file for upload
$tempScript = [System.IO.Path]::GetTempFileName()
$seedingScript | Out-File -FilePath $tempScript -Encoding UTF8 -NoNewline

Write-Host "Running command via ECS Exec..." -ForegroundColor Yellow
Write-Host "(This will open an interactive session)" -ForegroundColor Gray
Write-Host ""

# Create a command that reads and executes the script
$commandParts = @(
    "bash"
    "-c"
    "'$(Get-Content $tempScript -Raw -Encoding UTF8 | ForEach-Object { $_ -replace "'", "'\''" })'"
)

Write-Host "To execute the seeding, run this command:" -ForegroundColor Cyan
Write-Host "  aws ecs execute-command --cluster $Cluster --task $taskArn --container $containerName --interactive --command `"bash -c '$(Get-Content $tempScript -Raw)'`" --region $Region" -ForegroundColor Gray
Write-Host ""

# Alternative: Use a simpler approach - create script in container and run it
Write-Host "Creating and executing seeding script in container..." -ForegroundColor Cyan

# Base64 encode the script to avoid escaping issues
$scriptBytes = [System.Text.Encoding]::UTF8.GetBytes($seedingScript)
$scriptBase64 = [Convert]::ToBase64String($scriptBytes)

# Command to create and run the script
$execCommand = @"
bash -c 'echo "$scriptBase64" | base64 -d > /tmp/seed.sh && chmod +x /tmp/seed.sh && /tmp/seed.sh'
"@

Write-Host "Executing via ECS Exec..." -ForegroundColor Yellow
Write-Host "Command: $execCommand" -ForegroundColor Gray
Write-Host ""

# Execute command
aws ecs execute-command `
    --cluster $Cluster `
    --task $taskArn `
    --container $containerName `
    --interactive `
    --command $execCommand `
    --region $Region

$result = $LASTEXITCODE

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Seeding script executed successfully!" -ForegroundColor Green
} else {
    Write-Host "`n⚠️  Command execution completed. Check output above." -ForegroundColor Yellow
    Write-Host "`nIf you see connection issues, you may need to run this interactively:" -ForegroundColor Cyan
    Write-Host "  aws ecs execute-command --cluster $Cluster --task $taskArn --container $containerName --interactive --region $Region" -ForegroundColor Gray
}

Write-Host "`n🎉 Done!" -ForegroundColor Green

