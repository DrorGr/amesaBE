#!/usr/bin/env pwsh

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "AmesaBase SQL Database Seeder" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

# Connection details
$connectionString = "Host=amesa-prod-instance-1.cruuae28ob7m.eu-north-1.rds.amazonaws.com;Database=amesa_prod;Username=amesa_admin;Password=u1fwn3s9;Port=5432;"

Write-Host "Environment: Production" -ForegroundColor Yellow
Write-Host "Database: amesa_prod" -ForegroundColor Yellow
Write-Host ""

# Confirmation
Write-Host "⚠️  WARNING: You are about to seed the PRODUCTION database!" -ForegroundColor Red
Write-Host "This will TRUNCATE existing data and replace it with demo data." -ForegroundColor Red
Write-Host ""
$confirmation = Read-Host "Are you sure you want to continue? (type 'YES' to confirm)"

if ($confirmation -ne "YES") {
    Write-Host "Operation cancelled by user" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "Reading SQL script..." -ForegroundColor Blue

# Read the SQL script
$sqlScript = Get-Content -Path "Infrastructure/PUBLIC-SCHEMA-SEEDER.sql" -Raw

Write-Host "Executing SQL script..." -ForegroundColor Blue
Write-Host ""

try {
    # Load Npgsql assembly
    Add-Type -Path "AmesaBackend.DatabaseSeeder/bin/Debug/net8.0/Npgsql.dll"
    
    # Create connection
    $connection = New-Object Npgsql.NpgsqlConnection($connectionString)
    $connection.Open()
    
    Write-Host "✅ Connected to database successfully" -ForegroundColor Green
    
    # Create command
    $command = $connection.CreateCommand()
    $command.CommandText = $sqlScript
    $command.CommandTimeout = 300  # 5 minutes
    
    # Execute the script
    $result = $command.ExecuteNonQuery()
    
    Write-Host "✅ SQL script executed successfully!" -ForegroundColor Green
    Write-Host "Rows affected: $result" -ForegroundColor Green
    
} catch {
    Write-Host "❌ Error executing SQL script: $_" -ForegroundColor Red
    exit 1
} finally {
    if ($connection) {
        $connection.Close()
        Write-Host "Database connection closed" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "🎉 Database seeding completed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Demo users created:" -ForegroundColor Cyan
Write-Host "- admin@amesa.com / Admin123!" -ForegroundColor White
Write-Host "- john.doe@example.com / Password123!" -ForegroundColor White
Write-Host "- sarah.wilson@example.com / Password123!" -ForegroundColor White
Write-Host "- ahmed.hassan@example.com / Password123!" -ForegroundColor White
Write-Host "- maria.garcia@example.com / Password123!" -ForegroundColor White
Write-Host ""
Write-Host "Press any key to continue..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
