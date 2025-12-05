using System;
using System.Threading.Tasks;
using Npgsql;

class Program
{
    static async Task Main(string[] args)
    {
        // Production database connection string
        var connectionString = "Host=amesa-prod-instance-1.cruuae28ob7m.eu-north-1.rds.amazonaws.com;Port=5432;Database=postgres;Username=amesa_admin;Password=u1fwn3s9;SSL Mode=Require;";
        
        Console.WriteLine("🔌 Connecting to database...");
        Console.WriteLine("   Host: amesa-prod-instance-1.cruuae28ob7m.eu-north-1.rds.amazonaws.com");
        Console.WriteLine("   Database: postgres");
        Console.WriteLine("");
        
        try
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();
            
            // Test connection
            await using var cmd = new NpgsqlCommand("SELECT current_database(), current_user, version();", conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                Console.WriteLine($"✅ Connected successfully!");
                Console.WriteLine($"   Database: {reader.GetString(0)}");
                Console.WriteLine($"   User: {reader.GetString(1)}");
                Console.WriteLine($"   Version: {reader.GetString(2).Substring(0, Math.Min(50, reader.GetString(2).Length))}...");
            }
            await reader.CloseAsync();
            
            Console.WriteLine("");
            Console.WriteLine("📊 Available schemas:");
            await using var cmdSchemas = new NpgsqlCommand(
                "SELECT schema_name FROM information_schema.schemata WHERE schema_name NOT IN ('pg_catalog', 'information_schema', 'pg_toast') ORDER BY schema_name;",
                conn);
            await using var readerSchemas = await cmdSchemas.ExecuteReaderAsync();
            while (await readerSchemas.ReadAsync())
            {
                Console.WriteLine($"   - {readerSchemas.GetString(0)}");
            }
            await readerSchemas.CloseAsync();
            
            Console.WriteLine("");
            Console.WriteLine("💡 Connection successful! You can now run queries.");
            Console.WriteLine("   To run a custom query, modify this tool or use psql directly.");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Connection failed: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Inner exception: {ex.InnerException.Message}");
            }
            Environment.Exit(1);
        }
    }
}

