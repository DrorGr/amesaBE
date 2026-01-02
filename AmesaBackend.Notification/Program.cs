using AmesaBackend.Notification.Configuration;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Load notification secrets from AWS Secrets Manager (production only)
builder.Configuration.LoadNotificationSecretsFromAws(builder.Environment);

// Configure Serilog logging
builder.Host.UseNotificationSerilog(builder.Configuration);

// Add Controllers with camelCase JSON serialization
builder.Services.AddNotificationControllers();

// Configure Swagger/OpenAPI with Bearer token security
builder.Services.AddNotificationSwagger();

// Configure Entity Framework with PostgreSQL
builder.Services.AddNotificationDatabase(builder.Configuration, builder.Environment);

// Configure JWT Authentication
builder.Services.AddNotificationJwtAuthentication(builder.Configuration, builder.Environment);

// Register all application services
builder.Services.AddNotificationServices(builder.Configuration, builder.Environment);

var app = builder.Build();

// Configure middleware pipeline
app.UseNotificationMiddleware(builder.Configuration, builder.Environment);

// Ensure database is created (development only)
await app.EnsureDatabaseCreatedAsync(builder.Environment);

// Configure graceful shutdown
app.ConfigureGracefulShutdown();

try
{
    Log.Information("Starting Amesa Notification Service");
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
