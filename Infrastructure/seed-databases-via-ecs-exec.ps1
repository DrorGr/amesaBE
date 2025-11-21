# Database Seeding via ECS Exec
# Seeds all database schemas using ECS Exec to run commands in Fargate containers

param(
    # ECS Details
    [string]$Cluster = "Amesa",
    [string]$Region = "eu-north-1",
    
    # Database Connection Details
    [string]$DbEndpoint = "amesa-prod-instance-1.cruuae28ob7m.eu-north-1.rds.amazonaws.com",
    [string]$DbName = "amesa_prod",
    [string]$DbUser = "amesa_admin",
    [string]$DbPassword = "",
    [int]$DbPort = 5432,
    
    # Seeding Options
    [string[]]$SchemasToSeed = @("amesa_auth", "amesa_content", "amesa_lottery", "amesa_lottery_results", "amesa_payment", "amesa_notification", "amesa_analytics"),
    [switch]$ClearExisting = $false,
    [switch]$SkipIfExists = $false,
    
    # Service to use for seeding (must have .NET and database access)
    [string]$ServiceName = "amesa-lottery-service"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Database Seeding via ECS Exec" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Validate database password
if ([string]::IsNullOrEmpty($DbPassword)) {
    Write-Host "⚠️  Database password not provided. Prompting..." -ForegroundColor Yellow
    $securePassword = Read-Host "Enter database password" -AsSecureString
    $BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    $DbPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
}

Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  Cluster: $Cluster" -ForegroundColor White
Write-Host "  Service: $ServiceName" -ForegroundColor White
Write-Host "  Region: $Region" -ForegroundColor White
Write-Host "  Database: $DbName @ $DbEndpoint" -ForegroundColor White
Write-Host "  Schemas: $($SchemasToSeed -join ', ')" -ForegroundColor White
Write-Host ""

# Get running task for the service
Write-Host "Finding running task for $ServiceName..." -ForegroundColor Cyan
$tasks = aws ecs list-tasks --cluster $Cluster --service-name $ServiceName --region $Region --desired-status RUNNING --query "taskArns[0]" --output text 2>&1

if ([string]::IsNullOrEmpty($tasks) -or $tasks -eq "None") {
    Write-Host "❌ No running tasks found for service: $ServiceName" -ForegroundColor Red
    Write-Host "`nPlease ensure the service has at least one running task." -ForegroundColor Yellow
    Write-Host "You can check with:" -ForegroundColor Cyan
    Write-Host "  aws ecs describe-services --cluster $Cluster --services $ServiceName --region $Region" -ForegroundColor Gray
    exit 1
}

$taskArn = $tasks.Trim()
Write-Host "✅ Found task: $taskArn" -ForegroundColor Green

# Get container name
Write-Host "Getting container name..." -ForegroundColor Cyan
$taskDetails = aws ecs describe-tasks --cluster $Cluster --tasks $taskArn --region $Region --query "tasks[0].containers[0].name" --output text 2>&1
$containerName = $taskDetails.Trim()

if ([string]::IsNullOrEmpty($containerName)) {
    Write-Host "⚠️  Could not get container name, using default..." -ForegroundColor Yellow
    $containerName = $ServiceName
}

Write-Host "✅ Container: $containerName" -ForegroundColor Green
Write-Host ""

# Check if ECS Exec is enabled
Write-Host "Checking if ECS Exec is enabled..." -ForegroundColor Cyan
$taskDefArn = aws ecs describe-tasks --cluster $Cluster --tasks $taskArn --region $Region --query "tasks[0].taskDefinitionArn" --output text
$taskDef = aws ecs describe-task-definition --task-definition $taskDefArn --region $Region --query "taskDefinition" | ConvertFrom-Json

$enableExecuteCommand = $taskDef.enableExecuteCommand
if (-not $enableExecuteCommand) {
    Write-Host "⚠️  ECS Exec is not enabled for this task definition." -ForegroundColor Yellow
    Write-Host "`nTo enable ECS Exec, you need to:" -ForegroundColor Cyan
    Write-Host "1. Update the service to enable execute command" -ForegroundColor White
    Write-Host "2. Or update the task definition" -ForegroundColor White
    Write-Host ""
    Write-Host "Run this command:" -ForegroundColor Yellow
    Write-Host "  aws ecs update-service --cluster $Cluster --service $ServiceName --enable-execute-command --region $Region" -ForegroundColor Gray
    Write-Host ""
    $continue = Read-Host "Do you want to enable ECS Exec now? (y/N)"
    if ($continue -eq "y" -or $continue -eq "Y") {
        Write-Host "Enabling ECS Exec..." -ForegroundColor Cyan
        aws ecs update-service --cluster $Cluster --service $ServiceName --enable-execute-command --region $Region | Out-Null
        Write-Host "✅ ECS Exec enabled. Waiting for service update..." -ForegroundColor Green
        Start-Sleep -Seconds 10
        Write-Host "Please wait for the service to stabilize, then run this script again." -ForegroundColor Yellow
        exit 0
    } else {
        Write-Host "❌ ECS Exec is required. Exiting." -ForegroundColor Red
        exit 1
    }
}

Write-Host "✅ ECS Exec is enabled" -ForegroundColor Green
Write-Host ""

# Create seeding command
$seedingCommand = @"
export DB_ENDPOINT='$DbEndpoint'
export DB_NAME='$DbName'
export DB_USER='$DbUser'
export DB_PASSWORD='$DbPassword'
export DB_PORT=$DbPort
export SCHEMAS='$($SchemasToSeed -join ' ')'
export CLEAR_EXISTING='$ClearExisting'
export SKIP_IF_EXISTS='$SkipIfExists'

echo "========================================"
echo "Database Seeding via ECS Exec"
echo "========================================"
echo ""
echo "Database: `$DB_NAME @ `$DB_ENDPOINT"
echo "Schemas: `$SCHEMAS"
echo ""

# Check if .NET is available
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET not found in container"
    echo "The container needs .NET 8.0 SDK installed"
    exit 1
fi

echo "✅ .NET found: `$(dotnet --version)"
echo ""

# Function to seed a schema
seed_schema() {
    local schema=`$1
    echo "🌱 Seeding schema: `$schema"
    
    # Build connection string
    local conn_string="Host=`$DB_ENDPOINT;Port=`$DB_PORT;Database=`$DB_NAME;Username=`$DB_USER;Password=`$DB_PASSWORD;SearchPath=`$schema;"
    
    # Check if schema exists
    export PGPASSWORD=`$DB_PASSWORD
    schema_exists=`$(psql -h `$DB_ENDPOINT -p `$DB_PORT -U `$DB_USER -d `$DB_NAME -tAc "SELECT 1 FROM information_schema.schemata WHERE schema_name='`$schema'" 2>/dev/null || echo "0")
    
    if [ "`$schema_exists" != "1" ]; then
        echo "  ⚠️  Schema `$schema does not exist. Creating..."
        psql -h `$DB_ENDPOINT -p `$DB_PORT -U `$DB_USER -d `$DB_NAME -c "CREATE SCHEMA IF NOT EXISTS `$schema;" || {
            echo "  ❌ Failed to create schema `$schema"
            return 1
        }
    fi
    
    # Check if data exists
    if [ "`$SKIP_IF_EXISTS" == "True" ]; then
        table_count=`$(psql -h `$DB_ENDPOINT -p `$DB_PORT -U `$DB_USER -d `$DB_NAME -tAc "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='`$schema'" 2>/dev/null || echo "0")
        if [ "`$table_count" -gt "0" ]; then
            echo "  ⏭️  Schema `$schema already has data. Skipping..."
            return 0
        fi
    fi
    
    # Clear existing data if requested
    if [ "`$CLEAR_EXISTING" == "True" ]; then
        echo "  🗑️  Clearing existing data in `$schema..."
        psql -h `$DB_ENDPOINT -p `$DB_PORT -U `$DB_USER -d `$DB_NAME -c "DROP SCHEMA IF EXISTS `$schema CASCADE; CREATE SCHEMA `$schema;" || {
            echo "  ❌ Failed to clear schema `$schema"
            return 1
        }
    fi
    
    echo "  ✅ Schema `$schema ready for seeding"
    return 0
}

# Seed each schema
for schema in `$SCHEMAS; do
    seed_schema "`$schema" || {
        echo "❌ Failed to seed schema: `$schema"
        exit 1
    }
    echo ""
done

echo "========================================"
echo "✅ Schema preparation completed!"
echo "========================================"
echo ""
echo "Note: This script only prepares schemas."
echo "To seed actual data, you'll need to run the .NET seeder."
echo "The seeder code needs to be in the container or uploaded."
"@

Write-Host "Executing seeding command in container..." -ForegroundColor Cyan
Write-Host ""

# Execute command via ECS Exec
$commandBase64 = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($seedingCommand))
$commandEscaped = $seedingCommand -replace "'", "'\''"

# Use aws ecs execute-command
Write-Host "Running command via ECS Exec..." -ForegroundColor Cyan
Write-Host "(This will open an interactive session)" -ForegroundColor Yellow
Write-Host ""

$fullCommand = "bash -c `"$commandEscaped`""

Write-Host "To run the seeding, execute this command:" -ForegroundColor Cyan
Write-Host "  aws ecs execute-command --cluster $Cluster --task $taskArn --container $containerName --interactive --command `"$fullCommand`" --region $Region" -ForegroundColor Gray
Write-Host ""

# Alternative: Create a script file and execute it
Write-Host "Alternative: Creating a seeding script in the container..." -ForegroundColor Cyan

$scriptCommand = @"
cat > /tmp/seed-databases.sh << 'SEEDSCRIPT'
$seedingCommand
SEEDSCRIPT
chmod +x /tmp/seed-databases.sh
/tmp/seed-databases.sh
"@

Write-Host "`nRun this command to execute seeding:" -ForegroundColor Yellow
Write-Host "  aws ecs execute-command --cluster $Cluster --task $taskArn --container $containerName --interactive --command `"bash -c '$scriptCommand'`" --region $Region" -ForegroundColor Gray
Write-Host ""

Write-Host "Or use the interactive session:" -ForegroundColor Cyan
Write-Host "  aws ecs execute-command --cluster $Cluster --task $taskArn --container $containerName --interactive --region $Region" -ForegroundColor Gray
Write-Host ""

Write-Host "⚠️  Note: ECS Exec requires:" -ForegroundColor Yellow
Write-Host "  - IAM permissions for ecs:ExecuteCommand" -ForegroundColor Gray
Write-Host "  - SSM agent in the container (usually pre-installed)" -ForegroundColor Gray
Write-Host "  - Network connectivity to SSM endpoints" -ForegroundColor Gray

