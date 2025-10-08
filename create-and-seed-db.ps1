# Create Database and Seed Data Script
Write-Host "Creating database and seeding data..." -ForegroundColor Green

# Set environment variables
$env:DB_CONNECTION_STRING = "Host=amesadbmain1.cruuae28ob7m.eu-north-1.rds.amazonaws.com;Database=postgres;Username=dror;Password=aAXa406L6qdqfTU6o8vr;Port=5432;"

Write-Host "Step 1: Creating database 'amesa_lottery'..." -ForegroundColor Yellow

# Create the database using psql if available, or use a simple approach
try {
    # Try to create database using a simple SQL command
    $createDbScript = @"
CREATE DATABASE amesa_lottery;
"@
    
    # Write the SQL to a temporary file
    $createDbScript | Out-File -FilePath "create_db.sql" -Encoding UTF8
    
    Write-Host "Database creation script created. Please run this manually:" -ForegroundColor Cyan
    Write-Host "psql -h amesadbmain1.cruuae28ob7m.eu-north-1.rds.amazonaws.com -U dror -d postgres -f create_db.sql" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Or create the database manually in your PostgreSQL client with:" -ForegroundColor Cyan
    Write-Host "CREATE DATABASE amesa_lottery;" -ForegroundColor Gray
    Write-Host ""
    
    # Ask user to confirm they've created the database
    $confirmation = Read-Host "Have you created the 'amesa_lottery' database? (y/N)"
    if ($confirmation -ne "y" -and $confirmation -ne "Y") {
        Write-Host "Please create the database first and run this script again." -ForegroundColor Yellow
        exit 0
    }
    
    Write-Host "Step 2: Seeding the database..." -ForegroundColor Yellow
    
    # Now run the seeder
    $env:DB_CONNECTION_STRING = "Host=amesadbmain1.cruuae28ob7m.eu-north-1.rds.amazonaws.com;Database=amesa_lottery;Username=dror;Password=aAXa406L6qdqfTU6o8vr;Port=5432;"
    
    dotnet run --project AmesaBackend/AmesaBackend.csproj --configuration Release -- --seeder
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "Database seeding completed successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Your database now contains:" -ForegroundColor Cyan
        Write-Host "   - 5 Languages (English, Hebrew, Arabic, Spanish, French)" -ForegroundColor Gray
        Write-Host "   - 5 Users with addresses and phone numbers" -ForegroundColor Gray
        Write-Host "   - 4 Houses with images and lottery details" -ForegroundColor Gray
        Write-Host "   - Multiple lottery tickets and transactions" -ForegroundColor Gray
        Write-Host "   - Lottery draws and results" -ForegroundColor Gray
        Write-Host "   - 18 Translations (3 languages x 6 keys)" -ForegroundColor Gray
        Write-Host "   - 3 Content categories and articles" -ForegroundColor Gray
        Write-Host "   - 3 Promotional campaigns" -ForegroundColor Gray
        Write-Host "   - 8 System settings" -ForegroundColor Gray
        Write-Host ""
        Write-Host "Your Amesa Lottery database is ready to use!" -ForegroundColor Green
    } else {
        Write-Host "Seeding failed. Please check the errors above." -ForegroundColor Red
    }
    
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
} finally {
    # Clean up
    if (Test-Path "create_db.sql") {
        Remove-Item "create_db.sql"
    }
}
