using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AmesaBackend.Lottery.Data
{
    public class DesignTimeLotteryDbContextFactory : IDesignTimeDbContextFactory<LotteryDbContext>
    {
        public LotteryDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<LotteryDbContext>();

            // Prefer environment variable (used by tooling/scripts). Fallback to a local default for design-time only.
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                                   ?? "Host=localhost;Port=5432;Database=amesa_dev;Username=postgres;Password=postgres;";

            optionsBuilder.UseNpgsql(connectionString, npgsql =>
            {
                // Ensures EF tooling can generate proper migrations even if the DB isn't reachable.
                npgsql.EnableRetryOnFailure();
            });

            return new LotteryDbContext(optionsBuilder.Options);
        }
    }
}


