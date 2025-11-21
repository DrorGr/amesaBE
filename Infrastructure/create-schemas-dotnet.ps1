# Create schemas using .NET Npgsql connection
$connectionString = "Host=amesadbmain.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com;Port=5432;Database=postgres;Username=dror;Password=u1fwn3s9;"

$sql = @"
CREATE SCHEMA IF NOT EXISTS amesa_auth;
CREATE SCHEMA IF NOT EXISTS amesa_payment;
CREATE SCHEMA IF NOT EXISTS amesa_lottery;
CREATE SCHEMA IF NOT EXISTS amesa_content;
CREATE SCHEMA IF NOT EXISTS amesa_notification;
CREATE SCHEMA IF NOT EXISTS amesa_lottery_results;
CREATE SCHEMA IF NOT EXISTS amesa_analytics;
"@

$csharpCode = @"
using Npgsql;
using System;

var connString = "$connectionString";
await using var conn = new NpgsqlConnection(connString);
await conn.OpenAsync();

var commands = @"$sql".Split(';', StringSplitOptions.RemoveEmptyEntries);
foreach (var cmd in commands) {
    if (!string.IsNullOrWhiteSpace(cmd)) {
        await using var command = new NpgsqlCommand(cmd.Trim(), conn);
        await command.ExecuteNonQueryAsync();
        Console.WriteLine($"[OK] Executed: {cmd.Trim().Substring(0, Math.Min(30, cmd.Trim().Length))}...");
    }
}

await using var verifyCmd = new NpgsqlCommand("SELECT schema_name FROM information_schema.schemata WHERE schema_name LIKE 'amesa_%' ORDER BY schema_name;", conn);
await using var reader = await verifyCmd.ExecuteReaderAsync();
Console.WriteLine("\nSchemas created:");
while (await reader.ReadAsync()) {
    Console.WriteLine($"  - {reader.GetString(0)}");
}
"@

# Try to find a project with Npgsql to use as reference
$tempScript = [System.IO.Path]::GetTempFileName() + ".cs"
$csharpCode | Out-File -FilePath $tempScript -Encoding UTF8

Write-Output "Created temporary script. To execute, you need:"
Write-Output "1. Npgsql NuGet package"
Write-Output "2. Or use AWS RDS Query Editor (recommended)"
Write-Output ""
Write-Output "SQL to run in RDS Query Editor:"
Write-Output $sql

Remove-Item $tempScript -ErrorAction SilentlyContinue

