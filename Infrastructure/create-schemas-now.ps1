# Create schemas using .NET Npgsql
$connectionString = "Host=amesadbmain.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com;Port=5432;Database=postgres;Username=dror;Password=u1fwn3s9;"

$csharpScript = @'
using System;
using Npgsql;

var connString = "HOST_PLACEHOLDER";
await using var conn = new NpgsqlConnection(connString);
await conn.OpenAsync();
Console.WriteLine("[OK] Connected to database");

var schemas = new[] {
    "amesa_auth",
    "amesa_payment", 
    "amesa_lottery",
    "amesa_content",
    "amesa_notification",
    "amesa_lottery_results",
    "amesa_analytics"
};

foreach (var schema in schemas) {
    var sql = $"CREATE SCHEMA IF NOT EXISTS {schema};";
    await using var cmd = new NpgsqlCommand(sql, conn);
    await cmd.ExecuteNonQueryAsync();
    Console.WriteLine($"[OK] Created schema: {schema}");
}

await using var verifyCmd = new NpgsqlCommand("SELECT schema_name FROM information_schema.schemata WHERE schema_name LIKE 'amesa_%' ORDER BY schema_name;", conn);
await using var reader = await verifyCmd.ExecuteReaderAsync();
Console.WriteLine("\n[OK] Schemas verified:");
while (await reader.ReadAsync()) {
    Console.WriteLine($"  - {reader.GetString(0)}");
}
'@

$csharpScript = $csharpScript -replace "HOST_PLACEHOLDER", $connectionString

# Try to find a project with Npgsql reference
$projectPath = "..\AmesaBackend.Auth\AmesaBackend.Auth.csproj"
if (Test-Path $projectPath) {
    Write-Output "Using AmesaBackend.Auth project to create schemas..."
    
    # Create a temporary C# file
    $tempFile = [System.IO.Path]::GetTempFileName() + ".cs"
    $csharpScript | Out-File -FilePath $tempFile -Encoding UTF8
    
    # Try to compile and run
    Write-Output "Attempting to create schemas..."
    Write-Output "Note: This requires Npgsql package reference"
    
    # Alternative: Use dotnet-script if available, or provide instructions
    Write-Output ""
    Write-Output "[INFO] Since direct execution requires compilation, please use:"
    Write-Output "1. AWS RDS Query Editor (easiest)"
    Write-Output "2. Or install psql and run setup-database.ps1"
    Write-Output ""
    Write-Output "SQL to execute:"
    Write-Output "CREATE SCHEMA IF NOT EXISTS amesa_auth;"
    Write-Output "CREATE SCHEMA IF NOT EXISTS amesa_payment;"
    Write-Output "CREATE SCHEMA IF NOT EXISTS amesa_lottery;"
    Write-Output "CREATE SCHEMA IF NOT EXISTS amesa_content;"
    Write-Output "CREATE SCHEMA IF NOT EXISTS amesa_notification;"
    Write-Output "CREATE SCHEMA IF NOT EXISTS amesa_lottery_results;"
    Write-Output "CREATE SCHEMA IF NOT EXISTS amesa_analytics;"
    
    Remove-Item $tempFile -ErrorAction SilentlyContinue
} else {
    Write-Output "Project not found. Please use AWS RDS Query Editor or install psql."
}

