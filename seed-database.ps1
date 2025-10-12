# Amesa Lottery Database Seeding Script
Write-Host "🌱 Amesa Lottery Database Seeder" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green
Write-Host ""

# Set environment variables for database connection
# SECURITY: Use environment variable or prompt for connection string
if (-not $env:DB_CONNECTION_STRING) {
    Write-Host "⚠️  DB_CONNECTION_STRING environment variable not set!" -ForegroundColor Yellow
    Write-Host "Please set the environment variable or run:" -ForegroundColor Yellow
    Write-Host "   `$env:DB_CONNECTION_STRING = 'Host=your-host;Database=amesa_lottery;Username=your-user;Password=your-password;Port=5432;'" -ForegroundColor Gray
    Write-Host ""
    exit 1
}

Write-Host "🔗 Using database connection from environment variable" -ForegroundColor Yellow
Write-Host ""

# Check if we're in the correct directory
if (-not (Test-Path "AmesaBackend.csproj")) {
    Write-Host "❌ Error: AmesaBackend.csproj not found. Please run this script from the backend directory." -ForegroundColor Red
    exit 1
}

Write-Host "🔍 Checking .NET installation..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host "✅ .NET version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ Error: .NET is not installed or not in PATH" -ForegroundColor Red
    Write-Host "Please install .NET 8.0 SDK from: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "🔧 Building the project..." -ForegroundColor Yellow
try {
    dotnet build --configuration Release
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Build failed. Please check the errors above." -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ Build successful!" -ForegroundColor Green
} catch {
    Write-Host "❌ Error during build: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "🌱 Starting database seeding..." -ForegroundColor Yellow
Write-Host "This will populate your database with sample data including:" -ForegroundColor Gray
Write-Host "   • Users with different verification statuses" -ForegroundColor Gray
Write-Host "   • Houses with lottery details and images" -ForegroundColor Gray
Write-Host "   • Lottery tickets and transactions" -ForegroundColor Gray
Write-Host "   • Lottery results and winners" -ForegroundColor Gray
Write-Host "   • Translations in multiple languages" -ForegroundColor Gray
Write-Host "   • Content and promotional campaigns" -ForegroundColor Gray
Write-Host ""

# Ask for confirmation
$confirmation = Read-Host "Do you want to proceed with seeding? (y/N)"
if ($confirmation -ne "y" -and $confirmation -ne "Y") {
    Write-Host "❌ Seeding cancelled by user." -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "🚀 Running database seeder..." -ForegroundColor Yellow

try {
    # Run the seeder
    dotnet run --project AmesaBackend.csproj --configuration Release -- --seeder
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "🎉 Database seeding completed successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "📊 Your database now contains:" -ForegroundColor Cyan
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
        Write-Host ""
        Write-Host "💡 Next steps:" -ForegroundColor Yellow
        Write-Host "   1. Start your backend application" -ForegroundColor Gray
        Write-Host "   2. Test the API endpoints" -ForegroundColor Gray
        Write-Host "   3. Access the frontend to see the seeded data" -ForegroundColor Gray
    } else {
        Write-Host "❌ Seeding failed. Please check the errors above." -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "❌ Error during seeding: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "🔧 Troubleshooting tips:" -ForegroundColor Yellow
    Write-Host "   1. Check your database connection string" -ForegroundColor Gray
    Write-Host "   2. Ensure the database server is running" -ForegroundColor Gray
    Write-Host "   3. Verify your credentials are correct" -ForegroundColor Gray
    Write-Host "   4. Check if the database 'amesa_lottery' exists" -ForegroundColor Gray
    Write-Host "   5. Ensure you have the required database permissions" -ForegroundColor Gray
    exit 1
}

Write-Host ""
Write-Host "✨ Seeding process completed!" -ForegroundColor Green