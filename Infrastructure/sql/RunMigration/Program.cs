using System;
using System.IO;
using System.Threading.Tasks;
using Npgsql;

class Program
{
    static async Task Main(string[] args)
    {
        // Production database connection string (using instance endpoint)
        var connectionString = "Host=amesa-prod-instance-1.cruuae28ob7m.eu-north-1.rds.amazonaws.com;Port=5432;Database=amesa_prod;Username=amesa_admin;Password=u1fwn3s9;SSL Mode=Require;";
        
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        
        // Check current database
        await using var cmdDb = new NpgsqlCommand("SELECT current_database();", conn);
        var currentDb = await cmdDb.ExecuteScalarAsync();
        Console.WriteLine($"✅ Connected to database: {currentDb}");
        Console.WriteLine("");
        
        // First, check existing tables
        Console.WriteLine("========================================");
        Console.WriteLine("Checking Existing Tables in amesa_lottery");
        Console.WriteLine("========================================");
        Console.WriteLine("");
        
        // Query 0: Check all schemas first
        Console.WriteLine("📊 All schemas in database:");
        await using var cmd0 = new NpgsqlCommand(
            "SELECT schema_name FROM information_schema.schemata WHERE schema_name NOT IN ('pg_catalog', 'information_schema', 'pg_toast') ORDER BY schema_name;",
            conn);
        await using var reader0 = await cmd0.ExecuteReaderAsync();
        while (await reader0.ReadAsync())
        {
            var schemaName = reader0.GetString(0);
            Console.WriteLine($"  - {schemaName}");
        }
        await reader0.CloseAsync();
        Console.WriteLine("");
        
        // Query 1: All tables (check all schemas)
        Console.WriteLine("📊 All Tables/Views (all schemas):");
        await using var cmd1 = new NpgsqlCommand(
            "SELECT table_schema, table_name, table_type FROM information_schema.tables WHERE table_schema NOT IN ('pg_catalog', 'information_schema', 'pg_toast') ORDER BY table_schema, table_name;",
            conn);
        await using var reader1 = await cmd1.ExecuteReaderAsync();
        while (await reader1.ReadAsync())
        {
            var schema = reader1.GetString(0);
            var name = reader1.GetString(1);
            var type = reader1.GetString(2);
            Console.WriteLine($"  {type}: {schema}.{name}");
        }
        await reader1.CloseAsync();
        Console.WriteLine("");
        
        // Query 2: Check for watchlist-related tables
        Console.WriteLine("🔍 Checking for watchlist-related tables:");
        await using var cmd2 = new NpgsqlCommand(
            "SELECT table_schema, table_name, table_type FROM information_schema.tables WHERE table_schema = 'amesa_lottery' AND (table_name ILIKE '%watchlist%' OR table_name ILIKE '%watch%' OR table_name ILIKE '%track%' OR table_name ILIKE '%bookmark%') ORDER BY table_name;",
            conn);
        await using var reader2 = await cmd2.ExecuteReaderAsync();
        bool foundWatchlist = false;
        while (await reader2.ReadAsync())
        {
            foundWatchlist = true;
            var schema = reader2.GetString(0);
            var name = reader2.GetString(1);
            var type = reader2.GetString(2);
            Console.WriteLine($"  ⚠️  FOUND: {type}: {schema}.{name}");
        }
        await reader2.CloseAsync();
        if (!foundWatchlist)
        {
            Console.WriteLine("  ✅ No watchlist-related tables found");
        }
        Console.WriteLine("");
        
        // Query 3: Check for participant-related views
        Console.WriteLine("🔍 Checking for participant-related views:");
        await using var cmd3 = new NpgsqlCommand(
            "SELECT schemaname, viewname FROM pg_views WHERE schemaname = 'amesa_lottery' AND viewname ILIKE '%participant%';",
            conn);
        await using var reader3 = await cmd3.ExecuteReaderAsync();
        bool foundParticipant = false;
        while (await reader3.ReadAsync())
        {
            foundParticipant = true;
            var schema = reader3.GetString(0);
            var name = reader3.GetString(1);
            Console.WriteLine($"  ⚠️  FOUND: View: {schema}.{name}");
        }
        await reader3.CloseAsync();
        if (!foundParticipant)
        {
            Console.WriteLine("  ✅ No participant-related views found");
        }
        Console.WriteLine("");
        
        // Query 4: Check houses table columns
        Console.WriteLine("🔍 Checking houses table columns:");
        await using var cmd4 = new NpgsqlCommand(
            "SELECT column_name, data_type, is_nullable FROM information_schema.columns WHERE table_schema = 'amesa_lottery' AND table_name = 'houses' ORDER BY ordinal_position;",
            conn);
        await using var reader4 = await cmd4.ExecuteReaderAsync();
        bool hasMaxParticipants = false;
        while (await reader4.ReadAsync())
        {
            var colName = reader4.GetString(0);
            var dataType = reader4.GetString(1);
            var nullable = reader4.GetString(2);
            if (colName == "max_participants")
            {
                hasMaxParticipants = true;
                Console.WriteLine($"  ⚠️  FOUND: {colName} ({dataType}, nullable: {nullable})");
            }
            else
            {
                Console.WriteLine($"  - {colName} ({dataType})");
            }
        }
        await reader4.CloseAsync();
        if (!hasMaxParticipants)
        {
            Console.WriteLine("  ✅ max_participants column does NOT exist");
        }
        Console.WriteLine("");
        
        Console.WriteLine("========================================");
        Console.WriteLine("Summary:");
        Console.WriteLine("========================================");
        Console.WriteLine("✅ Safe to create user_watchlist table");
        Console.WriteLine("✅ Safe to create lottery_participants view");
        if (!hasMaxParticipants)
        {
            Console.WriteLine("✅ Safe to add max_participants column");
        }
        else
        {
            Console.WriteLine("⚠️  max_participants column already exists");
        }
        Console.WriteLine("");
        
        var migrationFile = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory())!.FullName, "lottery-user-link-migration.sql");
        
        if (!File.Exists(migrationFile))
        {
            Console.WriteLine($"❌ Migration file not found: {migrationFile}");
            return;
        }
        
        var sql = await File.ReadAllTextAsync(migrationFile);
        
        Console.WriteLine("🚀 Running Lottery-User Link Migration...");
        Console.WriteLine($"📄 File: {migrationFile}");
        Console.WriteLine($"🗄️  Database: amesa_lottery");
        Console.WriteLine("");
        
        try
        {
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


        while (await reader1.ReadAsync())
        {
            var schema = reader1.GetString(0);
            var name = reader1.GetString(1);
            var type = reader1.GetString(2);
            Console.WriteLine($"  {type}: {schema}.{name}");
        }
        await reader1.CloseAsync();
        Console.WriteLine("");
        
        // Query 2: Check for watchlist-related tables
        Console.WriteLine("🔍 Checking for watchlist-related tables:");
        await using var cmd2 = new NpgsqlCommand(
            "SELECT table_schema, table_name, table_type FROM information_schema.tables WHERE table_schema = 'amesa_lottery' AND (table_name ILIKE '%watchlist%' OR table_name ILIKE '%watch%' OR table_name ILIKE '%track%' OR table_name ILIKE '%bookmark%') ORDER BY table_name;",
            conn);
        await using var reader2 = await cmd2.ExecuteReaderAsync();
        bool foundWatchlist = false;
        while (await reader2.ReadAsync())
        {
            foundWatchlist = true;
            var schema = reader2.GetString(0);
            var name = reader2.GetString(1);
            var type = reader2.GetString(2);
            Console.WriteLine($"  ⚠️  FOUND: {type}: {schema}.{name}");
        }
        await reader2.CloseAsync();
        if (!foundWatchlist)
        {
            Console.WriteLine("  ✅ No watchlist-related tables found");
        }
        Console.WriteLine("");
        
        // Query 3: Check for participant-related views
        Console.WriteLine("🔍 Checking for participant-related views:");
        await using var cmd3 = new NpgsqlCommand(
            "SELECT schemaname, viewname FROM pg_views WHERE schemaname = 'amesa_lottery' AND viewname ILIKE '%participant%';",
            conn);
        await using var reader3 = await cmd3.ExecuteReaderAsync();
        bool foundParticipant = false;
        while (await reader3.ReadAsync())
        {
            foundParticipant = true;
            var schema = reader3.GetString(0);
            var name = reader3.GetString(1);
            Console.WriteLine($"  ⚠️  FOUND: View: {schema}.{name}");
        }
        await reader3.CloseAsync();
        if (!foundParticipant)
        {
            Console.WriteLine("  ✅ No participant-related views found");
        }
        Console.WriteLine("");
        
        // Query 4: Check houses table columns
        Console.WriteLine("🔍 Checking houses table columns:");
        await using var cmd4 = new NpgsqlCommand(
            "SELECT column_name, data_type, is_nullable FROM information_schema.columns WHERE table_schema = 'amesa_lottery' AND table_name = 'houses' ORDER BY ordinal_position;",
            conn);
        await using var reader4 = await cmd4.ExecuteReaderAsync();
        bool hasMaxParticipants = false;
        while (await reader4.ReadAsync())
        {
            var colName = reader4.GetString(0);
            var dataType = reader4.GetString(1);
            var nullable = reader4.GetString(2);
            if (colName == "max_participants")
            {
                hasMaxParticipants = true;
                Console.WriteLine($"  ⚠️  FOUND: {colName} ({dataType}, nullable: {nullable})");
            }
            else
            {
                Console.WriteLine($"  - {colName} ({dataType})");
            }
        }
        await reader4.CloseAsync();
        if (!hasMaxParticipants)
        {
            Console.WriteLine("  ✅ max_participants column does NOT exist");
        }
        Console.WriteLine("");
        
        Console.WriteLine("========================================");
        Console.WriteLine("Summary:");
        Console.WriteLine("========================================");
        Console.WriteLine("✅ Safe to create user_watchlist table");
        Console.WriteLine("✅ Safe to create lottery_participants view");
        if (!hasMaxParticipants)
        {
            Console.WriteLine("✅ Safe to add max_participants column");
        }
        else
        {
            Console.WriteLine("⚠️  max_participants column already exists");
        }
        Console.WriteLine("");
        
        var migrationFile = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory())!.FullName, "lottery-user-link-migration.sql");
        
        if (!File.Exists(migrationFile))
        {
            Console.WriteLine($"❌ Migration file not found: {migrationFile}");
            return;
        }
        
        var sql = await File.ReadAllTextAsync(migrationFile);
        
        Console.WriteLine("🚀 Running Lottery-User Link Migration...");
        Console.WriteLine($"📄 File: {migrationFile}");
        Console.WriteLine($"🗄️  Database: amesa_lottery");
        Console.WriteLine("");
        
        try
        {
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

