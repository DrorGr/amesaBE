using AmesaBackend.Admin.Configuration;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog logging
builder.Host.UseAdminSerilog(builder.Configuration);

// Add Blazor Server services and controllers
builder.Services.AddAdminControllers();

// Configure Entity Framework with PostgreSQL for all schemas
builder.Services.AddAdminDatabase(builder.Configuration, builder.Environment);

// Configure Redis for session storage with fallback to in-memory cache
builder.Services.AddAdminSessionStorage(builder.Configuration, builder.Environment);

// Register all application services
builder.Services.AddAdminServices(builder.Configuration, builder.Environment);

var app = builder.Build();

// Configure middleware pipeline
app.UseAdminMiddleware(builder.Configuration, builder.Environment);

// Configure graceful shutdown
app.ConfigureGracefulShutdown();

try
{
    Log.Information("Starting Amesa Admin Service");
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
