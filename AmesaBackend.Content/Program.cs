using AmesaBackend.Content.Configuration;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// JSON support is enabled by default in Npgsql 7.0+
// No need for GlobalTypeMapper.EnableDynamicJson() (obsolete)

// Configure Serilog logging
builder.Host.UseContentSerilog(builder.Configuration);

// Add Controllers with camelCase JSON serialization
builder.Services.AddContentControllers();

// Configure Swagger/OpenAPI with Bearer token security
builder.Services.AddContentSwagger();

// Configure Entity Framework with PostgreSQL
builder.Services.AddContentDatabase(builder.Configuration, builder.Environment);

// Register all application services
builder.Services.AddContentServices(builder.Configuration, builder.Environment);

var app = builder.Build();

// Configure middleware pipeline
app.UseContentMiddleware(builder.Configuration, builder.Environment);

// Ensure database is created (development only)
await app.EnsureDatabaseCreatedAsync(builder.Environment);

// Configure graceful shutdown
app.ConfigureGracefulShutdown();

try
{
    Log.Information("Starting Amesa Content Service");
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
