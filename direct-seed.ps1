# Direct Database Creation and Seeding
Write-Host "Direct Database Setup for Amesa Lottery" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green
Write-Host ""

# Let's try to create the database using a direct approach
Write-Host "Attempting to create database 'amesa_lottery'..." -ForegroundColor Yellow

# Set connection string to postgres database first
$env:DB_CONNECTION_STRING = "Host=amesadbmain1.cruuae28ob7m.eu-north-1.rds.amazonaws.com;Database=postgres;Username=dror;Password=aAXa406L6qdqfTU6o8vr;Port=5432;"

# Create a simple C# program to create the database
$createDbProgram = @"
using Npgsql;
using System;

class CreateDatabase
{
    static void Main()
    {
        try
        {
            var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
            Console.WriteLine("Connecting to PostgreSQL...");
            
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();
            Console.WriteLine("Connected successfully!");
            
            // Check if database exists
            using var checkCmd = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = 'amesa_lottery'", connection);
            var exists = checkCmd.ExecuteScalar();
            
            if (exists == null)
            {
                Console.WriteLine("Creating database 'amesa_lottery'...");
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

# Write the program to a file
$createDbProgram | Out-File -FilePath "CreateDatabase.cs" -Encoding UTF8

# Try to compile and run it
Write-Host "Compiling database creation program..." -ForegroundColor Gray
try {
    # Try to find the Npgsql assembly
    $npgsqlPath = Get-ChildItem -Path "AmesaBackend\bin" -Recurse -Name "Npgsql.dll" | Select-Object -First 1
    if ($npgsqlPath) {
        $fullNpgsqlPath = Join-Path (Get-Location) "AmesaBackend\bin\$npgsqlPath"
        csc CreateDatabase.cs -r:"$fullNpgsqlPath" -r:"C:\Program Files\dotnet\shared\Microsoft.NETCore.App\8.0.414\System.Runtime.dll" -r:"C:\Program Files\dotnet\shared\Microsoft.NETCore.App\8.0.414\System.Console.dll" 2>$null
        
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
    } else {
        Write-Host "Npgsql.dll not found, trying alternative approach..." -ForegroundColor Yellow
    }
} catch {
    Write-Host "Alternative approach needed..." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Step 2: Seeding the database..." -ForegroundColor Yellow

# Now set the connection string to the new database
$env:DB_CONNECTION_STRING = "Host=amesadbmain1.cruuae28ob7m.eu-north-1.rds.amazonaws.com;Database=amesa_lottery;Username=dror;Password=aAXa406L6qdqfTU6o8vr;Port=5432;"

# Build the project
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
if (Test-Path "CreateDatabase.cs") { Remove-Item "CreateDatabase.cs" }
if (Test-Path "CreateDatabase.exe") { Remove-Item "CreateDatabase.exe" }
