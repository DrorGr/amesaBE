using Npgsql;

var connectionString = "Host=amesadbmain.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com;Port=5432;Database=postgres;Username=dror;Password=u1fwn3s9;";

await using var conn = new NpgsqlConnection(connectionString);
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

Console.WriteLine("\n[SUCCESS] All schemas created successfully!");

