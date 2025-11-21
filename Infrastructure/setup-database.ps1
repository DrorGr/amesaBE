# PowerShell script to create database schemas in Aurora
# Prerequisites: psql must be installed and in PATH

$AuroraHost = "amesadbmain.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com"
$Database = "postgres"
$Username = "dror"
$SchemaScript = "BE/Infrastructure/create-database-schemas.sql"

Write-Output "=== Database Schema Setup ==="
Write-Output ""
Write-Output "This script will create schemas in Aurora PostgreSQL"
Write-Output "Host: $AuroraHost"
Write-Output "Database: $Database"
Write-Output "Username: $Username"
Write-Output ""

# Check if psql is available
$psqlPath = Get-Command psql -ErrorAction SilentlyContinue
if (-not $psqlPath) {
    Write-Output "❌ Error: psql is not installed or not in PATH"
    Write-Output ""
    Write-Output "To install psql:"
    Write-Output "  - Windows: Install PostgreSQL client tools"
    Write-Output "  - Or use AWS RDS Query Editor in Console"
    Write-Output ""
    Write-Output "Alternative: Connect manually and run:"
    Write-Output "  psql -h $AuroraHost -U $Username -d $Database -f $SchemaScript"
    exit 1
}

# Prompt for password
$securePassword = Read-Host "Enter database password for user '$Username'" -AsSecureString
$password = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword))

# Set PGPASSWORD environment variable
$env:PGPASSWORD = $password

Write-Output ""
Write-Output "Creating schemas..."

try {
    # Execute the SQL script
    $result = & psql -h $AuroraHost -U $Username -d $Database -f $SchemaScript 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Output "✅ Schemas created successfully!"
        Write-Output ""
        Write-Output "Verifying schemas..."
        
        # Verify schemas were created
        $verifyQuery = "SELECT schema_name FROM information_schema.schemata WHERE schema_name LIKE 'amesa_%' ORDER BY schema_name;"
        $verifyResult = & psql -h $AuroraHost -U $Username -d $Database -c $verifyQuery 2>&1
        
        Write-Output $verifyResult
    } else {
        Write-Output "❌ Error creating schemas:"
        Write-Output $result
        exit 1
    }
} catch {
    Write-Output "❌ Error: $_"
    exit 1
} finally {
    # Clear password from environment
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
}

Write-Output ""
Write-Output "✅ Database schema setup complete!"

