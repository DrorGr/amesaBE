# Create database schemas using AWS RDS Data API or direct connection
$AuroraHost = "amesadbmain.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com"
$Database = "postgres"
$Username = "dror"
$Password = "u1fwn3s9"
$Region = "eu-north-1"

Write-Output "Creating database schemas in Aurora..."
Write-Output "Host: $AuroraHost"
Write-Output ""

# SQL to create schemas
$schemaSQL = @"
CREATE SCHEMA IF NOT EXISTS amesa_auth;
CREATE SCHEMA IF NOT EXISTS amesa_payment;
CREATE SCHEMA IF NOT EXISTS amesa_lottery;
CREATE SCHEMA IF NOT EXISTS amesa_content;
CREATE SCHEMA IF NOT EXISTS amesa_notification;
CREATE SCHEMA IF NOT EXISTS amesa_lottery_results;
CREATE SCHEMA IF NOT EXISTS amesa_analytics;
SELECT schema_name FROM information_schema.schemata WHERE schema_name LIKE 'amesa_%' ORDER BY schema_name;
"@

# Try using psql if available
$psql = Get-Command psql -ErrorAction SilentlyContinue
if ($psql) {
    Write-Output "Using psql to create schemas..."
    $env:PGPASSWORD = $Password
    $schemaSQL | & psql -h $AuroraHost -U $Username -d $Database 2>&1
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
} else {
    Write-Output "[INFO] psql not available. Please use one of these methods:"
    Write-Output ""
    Write-Output "Method 1: AWS RDS Query Editor"
    Write-Output "  1. Go to AWS Console -> RDS -> Query Editor"
    Write-Output "  2. Connect to cluster: amesadbmain"
    Write-Output "  3. Run the SQL from create-database-schemas.sql"
    Write-Output ""
    Write-Output "Method 2: Install psql and run:"
    Write-Output "  psql -h $AuroraHost -U $Username -d $Database -f create-database-schemas.sql"
    Write-Output ""
    Write-Output "SQL to execute:"
    Write-Output $schemaSQL
}

