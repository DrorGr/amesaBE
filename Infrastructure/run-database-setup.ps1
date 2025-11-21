# PowerShell script to run the database setup SQL file
# This script connects to PostgreSQL and runs the CREATE-AND-SEED-DATABASE.sql file

param(
    [string]$ConnectionString = "Host=amesa-prod-instance-1.cruuae28ob7m.eu-north-1.rds.amazonaws.com;Database=amesa_prod;Username=amesa_admin;Password=u1fwn3s9;Port=5432;"
)

Write-Host "🚀 Running Database Setup Script..." -ForegroundColor Green
Write-Host "Connection: $($ConnectionString.Replace('Password=u1fwn3s9', 'Password=***'))" -ForegroundColor Yellow

# Check if the SQL file exists
$sqlFile = "CREATE-AND-SEED-DATABASE.sql"
if (-not (Test-Path $sqlFile)) {
    Write-Error "SQL file not found: $sqlFile"
    exit 1
}

Write-Host "📄 Found SQL file: $sqlFile" -ForegroundColor Green

# Create a simple .NET console app to run the SQL
$csharpCode = @"
using System;
using System.IO;
using System.Threading.Tasks;
using Npgsql;

class Program
{
    static async Task Main(string[] args)
    {
        var connectionString = args[0];
        var sqlFile = args[1];
        
        try
        {
            Console.WriteLine("📡 Connecting to database...");
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            Console.WriteLine("✅ Connected successfully!");
            
            Console.WriteLine("📖 Reading SQL file...");
            var sql = await File.ReadAllTextAsync(sqlFile);
            Console.WriteLine($"📄 SQL file size: {sql.Length} characters");
            
            Console.WriteLine("⚡ Executing SQL commands...");
            using var command = new NpgsqlCommand(sql, connection);
            command.CommandTimeout = 300; // 5 minutes timeout
            
            var result = await command.ExecuteNonQueryAsync();
            Console.WriteLine($"✅ SQL executed successfully! Rows affected: {result}");
            
            Console.WriteLine("🎉 Database setup completed!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}
"@

# Write the C# code to a temporary file
$tempDir = [System.IO.Path]::GetTempPath()
$projectDir = Join-Path $tempDir "DatabaseSetup"
$programFile = Join-Path $projectDir "Program.cs"
$projectFile = Join-Path $projectDir "DatabaseSetup.csproj"

# Create directory
if (Test-Path $projectDir) {
    Remove-Item $projectDir -Recurse -Force
}
New-Item -ItemType Directory -Path $projectDir -Force | Out-Null

# Write Program.cs
$csharpCode | Out-File -FilePath $programFile -Encoding UTF8

# Write project file
$projectContent = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Npgsql" Version="8.0.0" />
  </ItemGroup>
</Project>
"@

$projectContent | Out-File -FilePath $projectFile -Encoding UTF8

Write-Host "🔨 Building temporary .NET app..." -ForegroundColor Yellow
Push-Location $projectDir
try {
    dotnet build --configuration Release --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to build .NET app"
        exit 1
    }
    
    Write-Host "🚀 Running database setup..." -ForegroundColor Green
    dotnet run --configuration Release -- $ConnectionString (Resolve-Path $sqlFile).Path
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Database setup completed successfully!" -ForegroundColor Green
    } else {
        Write-Error "Database setup failed"
        exit 1
    }
} finally {
    Pop-Location
    # Clean up
    Remove-Item $projectDir -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "Ready to run the seeder!" -ForegroundColor Cyan
