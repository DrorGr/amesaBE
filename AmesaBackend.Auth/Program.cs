using AmesaBackend.Auth.Configuration;
using AmesaBackend.Auth.Data;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Setup Google service account credentials (BOM handling, base64 decoding, JSON validation)
builder.Configuration.SetupGoogleCredentials(builder.Environment);

// Configure Serilog logging
builder.Host.UseAuthSerilog(builder.Configuration);

// Configure forwarded headers for load balancer/proxy
builder.Services.AddAuthForwardedHeaders();

// Add Controllers with camelCase JSON serialization
builder.Services.AddAuthControllers();

// Configure Swagger/OpenAPI with Bearer token security
builder.Services.AddAuthSwagger();

// Configure Entity Framework with PostgreSQL
builder.Services.AddAuthDatabase(builder.Configuration, builder.Environment);

// Load OAuth credentials from AWS Secrets Manager (production only)
// Note: ConfigurationManager implements both IConfiguration and IConfigurationBuilder
builder.Configuration.LoadOAuthSecretsFromAws(builder.Environment);

// Configure JWT Authentication (returns AuthenticationBuilder for OAuth configuration)
var authBuilder = builder.Services.AddJwtAuthentication(builder.Configuration, builder.Environment);

// Configure Google OAuth (if credentials are configured)
authBuilder.AddGoogleOAuth(builder.Configuration, builder.Environment);

// Configure Meta/Facebook OAuth (if credentials are configured)
authBuilder.AddMetaOAuth(builder.Configuration);

// Configure Authorization policies
builder.Services.AddAuthAuthorization();

// Add CORS policy
builder.Services.AddAuthCors(builder.Configuration);

// Register all application services (security services, auth services, AWS services, infrastructure)
builder.Services.AddAuthServices(builder.Configuration, builder.Environment);

// Configure Kestrel HTTPS (development only)
builder.WebHost.UseAuthKestrel(builder.Environment);

var app = builder.Build();

// Configure middleware pipeline (forwarded headers, HTTPS enforcement, Swagger, CORS, 
// shared middleware, security middleware, routing, authentication, authorization, health checks, CAPTCHA endpoint, controllers)
app.UseAuthMiddleware(builder.Configuration, builder.Environment);

// Ensure database is created (development only)
await app.EnsureDatabaseCreatedAsync(builder.Environment);

// Configure graceful shutdown
app.ConfigureGracefulShutdown();

try
{
    Log.Information("Starting Amesa Auth Service");
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
