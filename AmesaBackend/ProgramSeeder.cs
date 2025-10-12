using Microsoft.EntityFrameworkCore;
using AmesaBackend.Data;
using AmesaBackend.Models;

namespace AmesaBackend
{
    public class ProgramSeeder
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("🌱 Amesa Lottery Database Seeder");
            Console.WriteLine("=================================");

            // Get database connection string from environment variables
            var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") 
                ?? "Data Source=amesa.db"; // Fallback to SQLite for local development

            Console.WriteLine($"🔗 Connecting to database...");
            
            // Determine if using PostgreSQL or SQLite
            if (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"   Using PostgreSQL (connection from environment)");
            }
            else
            {
                Console.WriteLine($"   Using SQLite (local development)");
            }
            Console.WriteLine();

            try
            {
                // Configure DbContext
                var optionsBuilder = new DbContextOptionsBuilder<AmesaDbContext>();
                optionsBuilder.UseNpgsql(connectionString);

                using var context = new AmesaDbContext(optionsBuilder.Options);

                // Test database connection
                Console.WriteLine("🔍 Testing database connection...");
                await context.Database.OpenConnectionAsync();
                Console.WriteLine("✅ Database connection successful!");
                await context.Database.CloseConnectionAsync();
                Console.WriteLine();

                // Run the seeder
                var seeder = new DatabaseSeeder(context);
                await seeder.SeedAsync();

                Console.WriteLine();
                Console.WriteLine("🎉 Database seeding completed successfully!");
                Console.WriteLine();
                Console.WriteLine("📊 Summary of seeded data:");
                Console.WriteLine("   • 5 Languages (English, Hebrew, Arabic, Spanish, French)");
                Console.WriteLine("   • 5 Users with addresses and phone numbers");
                Console.WriteLine("   • 4 Houses with images and lottery details");
                Console.WriteLine("   • Multiple lottery tickets and transactions");
                Console.WriteLine("   • Lottery draws and results");
                Console.WriteLine("   • 18 Translations (3 languages × 6 keys)");
                Console.WriteLine("   • 3 Content categories and articles");
                Console.WriteLine("   • 3 Promotional campaigns");
                Console.WriteLine("   • 8 System settings");
                Console.WriteLine();
                Console.WriteLine("🚀 Your Amesa Lottery database is ready to use!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine();
                Console.WriteLine("🔧 Troubleshooting tips:");
                Console.WriteLine("   1. Check your database connection string");
                Console.WriteLine("   2. Ensure the database server is running");
                Console.WriteLine("   3. Verify your credentials are correct");
                Console.WriteLine("   4. Check if the database 'amesa_lottery' exists");
                Console.WriteLine();
                Console.WriteLine("📝 Full error details:");
                Console.WriteLine(ex.ToString());
                Environment.Exit(1);
            }
        }
    }
}
