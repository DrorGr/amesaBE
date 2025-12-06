using AmesaBackend.Configuration;
using AmesaBackend.Data;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Debug: Log environment and configuration sources
Console.WriteLine($"[Config] Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"[Config] Content Root: {builder.Environment.ContentRootPath}");
Console.WriteLine($"[Config] IsDevelopment: {builder.Environment.IsDevelopment()}");

// Note: Npgsql 7+ enables dynamic JSON support by default via data source mapping
// No need for GlobalTypeMapper.EnableDynamicJson() - it's obsolete

// Check if we're running in seeder mode
// Note: Database seeding is handled by the standalone AmesaBackend.DatabaseSeeder project
// Use that project instead of running seeder from here
if (args.Length > 0 && args[0] == "--seeder")
{
    Console.WriteLine("⚠️  Database seeding is now handled by the standalone AmesaBackend.DatabaseSeeder project.");
    Console.WriteLine("    Please use that project to seed the database.");
    return;
}

// Configure Serilog logging
builder.Host.UseMainSerilog(builder.Configuration);

// Add Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = false;
    });
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger/OpenAPI with Bearer token security
builder.Services.AddMainSwagger();

// Configure Entity Framework with dual database provider (PostgreSQL or SQLite)
builder.Services.AddMainDatabase(builder.Configuration, builder.Environment);

// Load OAuth credentials from AWS Secrets Manager (production only)
// Note: ConfigurationManager implements both IConfiguration and IConfigurationBuilder
builder.Configuration.LoadOAuthSecretsFromAws(builder.Configuration, builder.Environment);

// Configure JWT Authentication (returns AuthenticationBuilder for OAuth configuration)
var authBuilder = builder.Services.AddJwtAuthentication(builder.Configuration, builder.Environment);

// Configure Google OAuth (if credentials are configured)
// Note: Main backend creates a new authBuilder for OAuth providers
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    // Get the existing authentication builder and chain OAuth providers
    var oauthAuthBuilder = builder.Services.AddAuthentication();
    oauthAuthBuilder.AddGoogleOAuth(builder.Configuration, builder.Environment);
    oauthAuthBuilder.AddMetaOAuth(builder.Configuration);
}

// Configure Authorization policies
builder.Services.AddMainAuthorization();

// Add CORS policy
builder.Services.AddMainCors(builder.Configuration);

// Register all application services (AutoMapper, Blazor Server, Session, background services, infrastructure)
builder.Services.AddMainServices(builder.Configuration, builder.Environment);

// Configure Kestrel HTTPS (development only)
builder.WebHost.UseMainKestrel(builder.Environment);

var app = builder.Build();

// Configure middleware pipeline (Swagger, CORS, custom middleware, response compression, 
// routing, authentication, authorization, session, static files, health checks, controllers, Blazor Admin Panel)
app.UseMainMiddleware(builder.Configuration, builder.Environment);

// Ensure database is created (development only)
// NOTE: Database seeding is DISABLED - all seeding must be done manually
await app.EnsureDatabaseCreatedAsync(builder.Environment);

// Configure graceful shutdown
app.ConfigureGracefulShutdown();

try
{
    Log.Information("Starting Amesa Lottery API");
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
