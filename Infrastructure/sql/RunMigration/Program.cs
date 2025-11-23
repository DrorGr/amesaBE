using System;
using System.IO;
using System.Threading.Tasks;
using Npgsql;

class Program
{
    static async Task Main(string[] args)
    {
        // Production database connection string (using cluster endpoint)
        var connectionString = "Host=amesadbmain.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com;Port=5432;Database=amesadb;Username=amesa_admin;Password=u1fwn3s9;SSL Mode=Require;";
        
        var migrationFile = Path.Combine(Directory.GetCurrentDirectory(), "lottery-favorites-migration.sql");
        
        if (!File.Exists(migrationFile))
        {
            Console.WriteLine($"❌ Migration file not found: {migrationFile}");
            return;
        }
        
        var sql = await File.ReadAllTextAsync(migrationFile);
        
        Console.WriteLine("🚀 Running Lottery Favorites Migration...");
        Console.WriteLine($"📄 File: {migrationFile}");
        Console.WriteLine($"🗄️  Database: amesa_lottery");
        
        try
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();
            Console.WriteLine("✅ Connected to database");
            
            await using var cmd = new NpgsqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
            
            Console.WriteLine("✅ Migration completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Migration failed: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }
}

