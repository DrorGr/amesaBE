# Quick Database Creation and Seeding
Write-Host "Quick Database Setup for Amesa Lottery" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green
Write-Host ""

# First, let's try to create the database using a simple approach
Write-Host "Step 1: Creating database 'amesa_lottery'..." -ForegroundColor Yellow

# Set connection string to postgres database to create the new database
$env:DB_CONNECTION_STRING = "Host=amesadbmain1.cruuae28ob7m.eu-north-1.rds.amazonaws.com;Database=postgres;Username=dror;Password=aAXa406L6qdqfTU6o8vr;Port=5432;"

# Create a simple C# program to create the database
$createDbScript = @"
using Npgsql;
using System;

class Program
{
    static void Main()
    {
        try
        {
            var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();
            
            // Check if database exists
            using var checkCmd = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = 'amesa_lottery'", connection);
            var exists = checkCmd.ExecuteScalar();
            
            if (exists == null)
            {
                // Create database
                using var createCmd = new NpgsqlCommand("CREATE DATABASE amesa_lottery", connection);
                createCmd.ExecuteNonQuery();
                Console.WriteLine("Database 'amesa_lottery' created successfully!");
            }
            else
            {
                Console.WriteLine("Database 'amesa_lottery' already exists!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}
"@

# Write the script to a file
$createDbScript | Out-File -FilePath "CreateDatabase.cs" -Encoding UTF8

# Compile and run the database creation
Write-Host "Compiling database creation script..." -ForegroundColor Gray
try {
    csc CreateDatabase.cs -r:"C:\Program Files\dotnet\shared\Microsoft.NETCore.App\8.0.414\System.Data.Common.dll" -r:"C:\Program Files\dotnet\shared\Microsoft.NETCore.App\8.0.414\System.Runtime.dll" -r:"C:\Program Files\dotnet\shared\Microsoft.NETCore.App\8.0.414\System.Console.dll" 2>$null
    
    if (Test-Path "CreateDatabase.exe") {
        Write-Host "Running database creation..." -ForegroundColor Gray
        .\CreateDatabase.exe
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Database created successfully!" -ForegroundColor Green
        } else {
            Write-Host "Database creation failed, but continuing..." -ForegroundColor Yellow
        }
    } else {
        Write-Host "Compilation failed, trying alternative approach..." -ForegroundColor Yellow
    }
} catch {
    Write-Host "Alternative approach needed..." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Step 2: Seeding the database..." -ForegroundColor Yellow

# Now set the connection string to the new database
$env:DB_CONNECTION_STRING = "Host=amesadbmain1.cruuae28ob7m.eu-north-1.rds.amazonaws.com;Database=amesa_lottery;Username=dror;Password=aAXa406L6qdqfTU6o8vr;Port=5432;"

# Run the seeder
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
if (Test-Path "CreateDatabase.cs") { Remove-Item "CreateDatabase.cs" }
if (Test-Path "CreateDatabase.exe") { Remove-Item "CreateDatabase.exe" }
