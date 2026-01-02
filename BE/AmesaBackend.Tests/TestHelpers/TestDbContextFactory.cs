using Microsoft.EntityFrameworkCore;
using AmesaBackend.Lottery.Data;

namespace AmesaBackend.Tests.TestHelpers;

public static class TestDbContextFactory
{
    public static LotteryDbContext CreateInMemoryDbContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<LotteryDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new LotteryDbContext(options);
    }
}
