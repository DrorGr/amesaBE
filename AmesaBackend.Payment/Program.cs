using AmesaBackend.Payment.Configuration;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Load payment secrets from AWS Secrets Manager (production only)
builder.Configuration.LoadPaymentSecretsFromAws(builder.Environment);

// Configure Serilog logging
builder.Host.UsePaymentSerilog(builder.Configuration);

// Add Controllers with camelCase JSON serialization
builder.Services.AddPaymentControllers();

// Configure Swagger/OpenAPI with Bearer token security
builder.Services.AddPaymentSwagger();

// Configure Entity Framework with PostgreSQL
builder.Services.AddPaymentDatabase(builder.Configuration, builder.Environment);

// Configure JWT Authentication
builder.Services.AddPaymentJwtAuthentication(builder.Configuration, builder.Environment);

// Register all application services
builder.Services.AddPaymentServices(builder.Configuration, builder.Environment);

var app = builder.Build();

// Configure middleware pipeline
app.UsePaymentMiddleware(builder.Configuration, builder.Environment);

// Ensure database is created (development only)
await app.EnsureDatabaseCreatedAsync(builder.Environment);

// Configure graceful shutdown
app.ConfigureGracefulShutdown();

try
{
    Log.Information("Starting Amesa Payment Service");
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
