using AmesaBackend.Lottery.Configuration;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// JSON support is enabled by default in Npgsql 7.0+
// No need for GlobalTypeMapper.EnableDynamicJson() (obsolete)

// Configure Serilog logging
builder.Host.UseLotterySerilog(builder.Configuration);

// Add Controllers with camelCase JSON serialization
builder.Services.AddLotteryControllers();

// Configure Swagger/OpenAPI with Bearer token security
builder.Services.AddLotterySwagger();

// Configure Entity Framework with PostgreSQL
builder.Services.AddLotteryDatabase(builder.Configuration, builder.Environment);

// Configure JWT Authentication
builder.Services.AddLotteryJwtAuthentication(builder.Configuration, builder.Environment);

// Register all application services
builder.Services.AddLotteryServices(builder.Configuration, builder.Environment);

var app = builder.Build();

// Configure middleware pipeline
app.UseLotteryMiddleware(builder.Configuration, builder.Environment);

// Ensure database is created (development only)
await app.EnsureDatabaseCreatedAsync(builder.Environment);

// Configure graceful shutdown
app.ConfigureGracefulShutdown();

try
{
    Log.Information("Starting Amesa Lottery Service");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible to test projects
public partial class Program { }
