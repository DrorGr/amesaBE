using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AmesaBackend.Payment.Data;
using AmesaBackend.Payment.Services;
using AmesaBackend.Payment.Configuration;
using AmesaBackend.Payment.Middleware;
using AmesaBackend.Shared.Extensions;
using AmesaBackend.Shared.Middleware.Extensions;
using AmesaBackend.Auth.Services;
using Serilog;
using Npgsql;
using ProductHandlers = AmesaBackend.Payment.Services.ProductHandlers;

var builder = WebApplication.CreateBuilder(args);

// Load payment secrets from AWS Secrets Manager (production only)
// This must be called AFTER builder is created but configuration is built later
// AddInMemoryCollection will override appsettings.json values
builder.Configuration.LoadPaymentSecretsFromAws(builder.Environment);

NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/payment-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = false;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Entity Framework
builder.Services.AddDbContext<PaymentDbContext>(options =>
{
    var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") 
        ?? builder.Configuration.GetConnectionString("DefaultConnection");

    if (!string.IsNullOrEmpty(connectionString))
    {
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);
        });
    }

    // Never enable sensitive data logging in production
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Add shared services with Redis required for rate limiting
builder.Services.AddAmesaBackendShared(builder.Configuration, builder.Environment, requireRedis: true);

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] 
    ?? Environment.GetEnvironmentVariable("JwtSettings__SecretKey");

// Track if authentication was configured
bool authenticationConfigured = false;

if (!string.IsNullOrWhiteSpace(secretKey))
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"] ?? "AmesaAuthService",
            ValidAudience = jwtSettings["Audience"] ?? "AmesaFrontend",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!)),
            ClockSkew = TimeSpan.FromMinutes(5) // Allow 5 minute clock difference for reliability
        };
    });
    authenticationConfigured = true;
    Log.Information("JWT Authentication configured successfully");
}
else
{
    Log.Warning("JWT SecretKey is not configured. Authentication will not work. Set JwtSettings__SecretKey environment variable or configure JwtSettings:SecretKey in appsettings.");
}

// Add Rate Limit Service (required by PaymentRateLimitService)
builder.Services.AddScoped<IRateLimitService, RateLimitService>();

// Add Services
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPaymentRateLimitService, PaymentRateLimitService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IStripeService, StripeService>();
builder.Services.AddScoped<ICoinbaseCommerceService, CoinbaseCommerceService>();
builder.Services.AddScoped<IPaymentAuditService, PaymentAuditService>();

// Product Handlers
builder.Services.AddScoped<ProductHandlers.IProductHandler, ProductHandlers.LotteryTicketProductHandler>();
builder.Services.AddSingleton<ProductHandlers.IProductHandlerRegistry>(serviceProvider =>
{
    var registry = new ProductHandlers.ProductHandlerRegistry();
    var lotteryHandler = serviceProvider.GetRequiredService<ProductHandlers.IProductHandler>();
    registry.RegisterHandler(lotteryHandler);
    return registry;
});

builder.Services.AddHealthChecks();

var app = builder.Build();

// Security middleware (before other middleware)
app.UseMiddleware<SecurityHeadersMiddleware>();

// HTTPS redirection and HSTS (production only)
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseSwagger();
app.UseSwaggerUI();

// Enable X-Ray tracing if configured
if (builder.Configuration.GetValue<bool>("XRay:Enabled", false))
{
    // X-Ray tracing removed for microservices
}

app.UseAmesaMiddleware();
app.UseAmesaLogging();
app.UseRouting();

// Only use authentication if it was configured
// Check if JWT secret is available at runtime
var jwtSecretAtRuntime = app.Configuration["JwtSettings:SecretKey"] 
    ?? Environment.GetEnvironmentVariable("JwtSettings__SecretKey");
    
if (!string.IsNullOrWhiteSpace(jwtSecretAtRuntime))
{
    app.UseAuthentication();
    app.UseAuthorization();
    Log.Information("Authentication middleware enabled - JWT secret found");
}
else
{
    Log.Warning("Skipping UseAuthentication() and UseAuthorization() - JWT secret not configured");
}

app.MapHealthChecks("/health");
app.MapControllers();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    try
    {
        if (builder.Environment.IsDevelopment())
        {
            Log.Information("Development mode: Ensuring Payment database tables are created...");
            await context.Database.EnsureCreatedAsync();
            Log.Information("Payment database setup completed successfully");
        }
        else
        {
            Log.Information("Production mode: Skipping EnsureCreated (use migrations)");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error setting up database");
    }
}

Log.Information("Starting Amesa Payment Service");
await app.RunAsync();

public partial class Program { }

