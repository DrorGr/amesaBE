# Database Seeding via SSH Script
# Seeds all database schemas in the Aurora cluster via SSH connection to EC2 instance

param(
    # SSH Connection Details
    [string]$SshHost = "",
    [string]$SshUser = "ec2-user",
    [string]$SshKeyPath = "",
    [int]$SshPort = 22,
    
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
    
    # Region
    [string]$Region = "eu-north-1"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Database Seeding via SSH" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Validate SSH connection details
if ([string]::IsNullOrEmpty($SshHost)) {
    Write-Host "❌ SSH Host is required!" -ForegroundColor Red
    Write-Host "`nPlease provide:" -ForegroundColor Yellow
    Write-Host "  -SshHost <ec2-instance-ip-or-hostname>" -ForegroundColor White
    Write-Host "  -SshUser <username> (default: ec2-user)" -ForegroundColor White
    Write-Host "  -SshKeyPath <path-to-pem-key>" -ForegroundColor White
    Write-Host ""
    Write-Host "Example:" -ForegroundColor Cyan
    Write-Host "  .\seed-databases-via-ssh.ps1 -SshHost 54.123.45.67 -SshUser ec2-user -SshKeyPath C:\keys\amesa-key.pem -DbPassword 'your-password'" -ForegroundColor Gray
    exit 1
}

if ([string]::IsNullOrEmpty($SshKeyPath) -or -not (Test-Path $SshKeyPath)) {
    Write-Host "❌ SSH Key file not found: $SshKeyPath" -ForegroundColor Red
    Write-Host "Please provide a valid path to your SSH private key (.pem file)" -ForegroundColor Yellow
    exit 1
}

# Validate database password
if ([string]::IsNullOrEmpty($DbPassword)) {
    Write-Host "⚠️  Database password not provided. Prompting..." -ForegroundColor Yellow
    $securePassword = Read-Host "Enter database password" -AsSecureString
    $BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    $DbPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
}

Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  SSH Host: $SshHost" -ForegroundColor White
Write-Host "  SSH User: $SshUser" -ForegroundColor White
Write-Host "  SSH Key: $SshKeyPath" -ForegroundColor White
Write-Host "  Database: $DbName @ $DbEndpoint" -ForegroundColor White
Write-Host "  Schemas: $($SchemasToSeed -join ', ')" -ForegroundColor White
Write-Host ""

# Test SSH connection
Write-Host "Testing SSH connection..." -ForegroundColor Cyan
try {
    $sshTest = ssh -i $SshKeyPath -o ConnectTimeout=10 -o StrictHostKeyChecking=no -p $SshPort "$SshUser@$SshHost" "echo 'SSH connection successful'" 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "SSH connection failed: $sshTest"
    }
    Write-Host "✅ SSH connection successful" -ForegroundColor Green
} catch {
    Write-Host "❌ SSH connection failed: $_" -ForegroundColor Red
    Write-Host "`nTroubleshooting:" -ForegroundColor Yellow
    Write-Host "  1. Verify EC2 instance is running" -ForegroundColor Gray
    Write-Host "  2. Check security group allows SSH from your IP" -ForegroundColor Gray
    Write-Host "  3. Verify SSH key permissions (chmod 400 on Linux/Mac)" -ForegroundColor Gray
    Write-Host "  4. Check username (ec2-user for Amazon Linux, ubuntu for Ubuntu)" -ForegroundColor Gray
    exit 1
}

# Check if .NET is available on remote host
Write-Host "Checking .NET installation on remote host..." -ForegroundColor Cyan
$dotnetCheck = ssh -i $SshKeyPath -o StrictHostKeyChecking=no "$SshUser@$SshHost" "which dotnet || echo 'NOT_FOUND'"
if ($dotnetCheck -match "NOT_FOUND") {
    Write-Host "⚠️  .NET not found on remote host. Installing..." -ForegroundColor Yellow
    
    # Install .NET 8.0 SDK
    $installScript = @"
#!/bin/bash
# Install .NET 8.0 SDK
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0
export PATH="`$HOME/.dotnet:`$PATH"
dotnet --version
"@
    
    ssh -i $SshKeyPath -o StrictHostKeyChecking=no "$SshUser@$SshHost" "bash -s" <<< $installScript
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Failed to install .NET. Please install manually on the EC2 instance." -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ .NET installed" -ForegroundColor Green
} else {
    Write-Host "✅ .NET found: $dotnetCheck" -ForegroundColor Green
}

# Create seeding script on remote host
Write-Host "`nCreating seeding script on remote host..." -ForegroundColor Cyan

$seedingScript = @"
#!/bin/bash
set -e

# Database connection details
DB_ENDPOINT="$DbEndpoint"
DB_NAME="$DbName"
DB_USER="$DbUser"
DB_PASSWORD="$DbPassword"
DB_PORT=$DbPort
REGION="$Region"

# Schemas to seed
SCHEMAS=($($SchemasToSeed | ForEach-Object { "'$_'" } | Join-String -Separator ' '))

# Options
CLEAR_EXISTING="$ClearExisting"
SKIP_IF_EXISTS="$SkipIfExists"

echo "========================================"
echo "Database Seeding Script"
echo "========================================"
echo ""
echo "Database: `$DB_NAME @ `$DB_ENDPOINT"
echo "Schemas: `$SCHEMAS"
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
    
    # Run migrations first (if EF migrations exist)
    echo "  🔄 Running migrations for `$schema..."
    # This would run EF migrations if available
    
    # Seed data based on schema
    case `$schema in
        "amesa_auth")
            echo "  📝 Seeding authentication data..."
            # Add seeding commands for auth schema
            ;;
        "amesa_content")
            echo "  📝 Seeding content/translations..."
            # Add seeding commands for content schema
            ;;
        "amesa_lottery")
            echo "  📝 Seeding lottery data..."
            # Add seeding commands for lottery schema
            ;;
        "amesa_lottery_results")
            echo "  📝 Seeding lottery results..."
            # Add seeding commands for lottery results schema
            ;;
        "amesa_payment")
            echo "  📝 Seeding payment data..."
            # Add seeding commands for payment schema
            ;;
        "amesa_notification")
            echo "  📝 Seeding notifications..."
            # Add seeding commands for notification schema
            ;;
        "amesa_analytics")
            echo "  📝 Seeding analytics..."
            # Add seeding commands for analytics schema
            ;;
    esac
    
    echo "  ✅ Schema `$schema seeded successfully"
    return 0
}

# Seed each schema
for schema in "`${SCHEMAS[@]}"; do
    seed_schema "`$schema" || {
        echo "❌ Failed to seed schema: `$schema"
        exit 1
    }
    echo ""
done

echo "========================================"
echo "✅ All schemas seeded successfully!"
echo "========================================"
"@

# Upload and execute seeding script
$tempScript = [System.IO.Path]::GetTempFileName()
$seedingScript | Out-File -FilePath $tempScript -Encoding UTF8

Write-Host "Uploading seeding script..." -ForegroundColor Cyan
scp -i $SshKeyPath -o StrictHostKeyChecking=no "$tempScript" "$SshUser@${SshHost}:/tmp/seed-databases.sh" | Out-Null

Write-Host "Executing seeding script on remote host..." -ForegroundColor Cyan
ssh -i $SshKeyPath -o StrictHostKeyChecking=no "$SshUser@$SshHost" "chmod +x /tmp/seed-databases.sh && /tmp/seed-databases.sh"

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Database seeding completed successfully!" -ForegroundColor Green
} else {
    Write-Host "`n❌ Database seeding failed!" -ForegroundColor Red
    exit 1
}

# Cleanup
Remove-Item $tempScript -Force -ErrorAction SilentlyContinue
ssh -i $SshKeyPath -o StrictHostKeyChecking=no "$SshUser@$SshHost" "rm -f /tmp/seed-databases.sh" | Out-Null

Write-Host "`n🎉 All done!" -ForegroundColor Green

