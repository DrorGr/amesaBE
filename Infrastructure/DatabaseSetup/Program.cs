using System;
using System.IO;
using System.Threading.Tasks;
using Npgsql;

class Program
{
    static async Task Main(string[] args)
    {
        var connectionString = "Host=amesa-prod-instance-1.cruuae28ob7m.eu-north-1.rds.amazonaws.com;Database=amesa_prod;Username=amesa_admin;Password=u1fwn3s9;Port=5432;";
        var sqlFile = "../CREATE-AND-SEED-DATABASE.sql";
        
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
            Console.WriteLine($"✅ SQL executed successfully!");
            
            Console.WriteLine("🎉 Database setup completed!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            Environment.Exit(1);
        }
    }
}
