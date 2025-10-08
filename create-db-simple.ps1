# Simple Database Creation and Seeding
Write-Host "Creating Amesa Lottery Database..." -ForegroundColor Green

# First, let's create the database using a simple approach
Write-Host "Step 1: Creating database 'amesa_lottery'..." -ForegroundColor Yellow

# Create a simple SQL script to create the database
$createDbSQL = @"
-- Create the database if it doesn't exist
SELECT 'CREATE DATABASE amesa_lottery' 
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'amesa_lottery')\gexec
"@

# Write the SQL to a file
$createDbSQL | Out-File -FilePath "create_db.sql" -Encoding UTF8

Write-Host "Database creation SQL script created." -ForegroundColor Gray
Write-Host ""
Write-Host "To create the database, run this command:" -ForegroundColor Cyan
Write-Host "psql -h amesadbmain1.cruuae28ob7m.eu-north-1.rds.amazonaws.com -U dror -d postgres -f create_db.sql" -ForegroundColor White
Write-Host ""
Write-Host "Or manually create the database with:" -ForegroundColor Cyan
Write-Host "CREATE DATABASE amesa_lottery;" -ForegroundColor White
Write-Host ""

# Ask user to confirm
$confirmation = Read-Host "Have you created the 'amesa_lottery' database? (y/N)"
if ($confirmation -ne "y" -and $confirmation -ne "Y") {
    Write-Host "Please create the database first and run this script again." -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "Step 2: Seeding the database..." -ForegroundColor Yellow

# Set the connection string to the new database
$env:DB_CONNECTION_STRING = "Host=amesadbmain1.cruuae28ob7m.eu-north-1.rds.amazonaws.com;Database=amesa_lottery;Username=dror;Password=aAXa406L6qdqfTU6o8vr;Port=5432;"

# Build the project first
Write-Host "Building project..." -ForegroundColor Gray
dotnet build AmesaBackend/AmesaBackend.csproj --configuration Release --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed. Please check the errors above." -ForegroundColor Red
    exit 1
}

# Run the seeder
Write-Host "Running database seeder..." -ForegroundColor Gray
dotnet run --project AmesaBackend/AmesaBackend.csproj --configuration Release -- --seeder

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "🎉 Database seeding completed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Your Amesa Lottery database now contains:" -ForegroundColor Cyan
    Write-Host "   • 5 Languages (English, Hebrew, Arabic, Spanish, French)" -ForegroundColor Gray
    Write-Host "   • 5 Users with addresses and phone numbers" -ForegroundColor Gray
    Write-Host "   • 4 Houses with images and lottery details" -ForegroundColor Gray
    Write-Host "   • Multiple lottery tickets and transactions" -ForegroundColor Gray
    Write-Host "   • Lottery draws and results" -ForegroundColor Gray
    Write-Host "   • 18 Translations (3 languages × 6 keys)" -ForegroundColor Gray
    Write-Host "   • 3 Content categories and articles" -ForegroundColor Gray
    Write-Host "   • 3 Promotional campaigns" -ForegroundColor Gray
    Write-Host "   • 8 System settings" -ForegroundColor Gray
    Write-Host ""
    Write-Host "🚀 Your Amesa Lottery database is ready to use!" -ForegroundColor Green
} else {
    Write-Host "❌ Seeding failed. Please check the errors above." -ForegroundColor Red
}

# Clean up
if (Test-Path "create_db.sql") { Remove-Item "create_db.sql" }
