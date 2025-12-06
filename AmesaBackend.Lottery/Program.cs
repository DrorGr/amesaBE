using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.Tasks;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Lottery.Services.Processors;
using AmesaBackend.Lottery.Hubs;
using AmesaBackend.Shared.Extensions;
using AmesaBackend.Shared.Middleware.Extensions;
using AmesaBackend.Auth.Data;
using AmesaBackend.Auth.Services;
using Amazon.SQS;
using Serilog;
using Npgsql;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/lottery-.txt", rollingInterval: RollingInterval.Day)
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

// Add Response Caching for [ResponseCache] attribute support
builder.Services.AddResponseCaching();

// Configure Entity Framework
builder.Services.AddDbContext<LotteryDbContext>(options =>
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

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Register AuthDbContext for UserPreferencesService (shared database, same connection string)
builder.Services.AddDbContext<AuthDbContext>(options =>
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

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Lottery service requires Redis for house list caching and cache invalidation
builder.Services.AddAmesaBackendShared(builder.Configuration, builder.Environment, requireRedis: true);

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] 
    ?? Environment.GetEnvironmentVariable("JwtSettings__SecretKey");

if (string.IsNullOrWhiteSpace(secretKey))
{
    throw new InvalidOperationException("JWT SecretKey is not configured. Set JwtSettings__SecretKey environment variable or configure JwtSettings:SecretKey in appsettings.");
}

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

    // Extract JWT token from query string for SignalR WebSocket connections
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/ws"))
            {
                context.Token = accessToken;
                
                // Only log in development for debugging
                if (builder.Environment.IsDevelopment())
                {
                    Log.Debug("SignalR token extracted from query string for path: {Path}", path);
                }
            }
            else if (path.StartsWithSegments("/ws"))
            {
                // Log missing token only in development
                if (builder.Environment.IsDevelopment())
                {
                    Log.Warning("SignalR connection attempt without token on path: {Path}", path);
                }
            }
            
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            var path = context.HttpContext.Request.Path;
            
            // Log authentication failures with sanitized information
            if (builder.Environment.IsDevelopment())
            {
                Log.Warning("SignalR authentication failed for path: {Path}, Error: {Error}",
                    path, context.Exception?.Message ?? "Unknown error");
            }
            else
            {
                // Production: Only log error type, not details
                Log.Warning("SignalR authentication failed for WebSocket path");
            }
            
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            // Minimal logging in production
            if (builder.Environment.IsDevelopment())
            {
                var path = context.HttpContext.Request.Path;
                Log.Debug("SignalR authentication challenge for path: {Path}", path);
            }
            
            return Task.CompletedTask;
        }
    };
});

// Add Services
// Register UserPreferencesService for favorites functionality
builder.Services.AddScoped<IUserPreferencesService, UserPreferencesService>();
builder.Services.AddScoped<ILotteryService, LotteryService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<AmesaBackend.Shared.Configuration.IConfigurationService, AmesaBackend.Lottery.Services.ConfigurationService>();

// Reservation system services
builder.Services.AddScoped<IRedisInventoryManager, RedisInventoryManager>();
builder.Services.AddScoped<ITicketReservationService, TicketReservationService>();
builder.Services.AddScoped<IReservationProcessor, ReservationProcessor>();
builder.Services.AddScoped<IPaymentProcessor, PaymentProcessor>();
builder.Services.AddScoped<ITicketCreatorProcessor, TicketCreatorProcessor>();

// Register IRateLimitService from Auth service (optional)
var rateLimitServiceType = typeof(IRateLimitService);
if (rateLimitServiceType != null)
{
    builder.Services.AddScoped(typeof(IRateLimitService), typeof(RateLimitService));
}

// Register HttpClient for payment service with timeout configuration
builder.Services.AddHttpClient<IPaymentProcessor, PaymentProcessor>(client =>
{
    var paymentServiceUrl = builder.Configuration["PaymentService:BaseUrl"] 
        ?? "http://amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com/api/v1";
    client.BaseAddress = new Uri(paymentServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30); // 30 second timeout for payment requests
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Register SQS client
builder.Services.AddSingleton<IAmazonSQS>(sp =>
{
    var region = builder.Configuration["AWS:Region"] ?? "eu-north-1";
    return new AmazonSQSClient(Amazon.RegionEndpoint.GetBySystemName(region));
});

// Add Background Services
builder.Services.AddHostedService<LotteryDrawService>();
builder.Services.AddHostedService<TicketQueueProcessorService>();
builder.Services.AddHostedService<ReservationCleanupService>();
builder.Services.AddHostedService<InventorySyncService>();
builder.Services.AddHostedService<LotteryCountdownService>();

// Add SignalR for real-time updates
builder.Services.AddSignalR();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Enable X-Ray tracing if configured
if (builder.Configuration.GetValue<bool>("XRay:Enabled", false))
{
    // X-Ray tracing removed for microservices
}

app.UseAmesaMiddleware();
app.UseAmesaLogging();
app.UseResponseCaching(); // Must be before UseRouting for VaryByQueryKeys to work
app.UseRouting();
// Debug routing middleware (development only)
if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        var method = context.Request.Method;
        var path = context.Request.Path;
        
        Log.Debug("Request: {Method} {Path}", method, path);
        
        await next();
        
        if (context.Response.StatusCode == 405)
        {
            Log.Warning("405 Method Not Allowed: {Method} {Path}", method, path);
        }
    });
}
app.UseAuthentication();

// Service-to-service authentication middleware
app.UseMiddleware<AmesaBackend.Shared.Middleware.ServiceToServiceAuthMiddleware>();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

// Map SignalR hubs
app.MapHub<LotteryHub>("/ws/lottery");

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<LotteryDbContext>();
    try
    {
        if (builder.Environment.IsDevelopment())
        {
            Log.Information("Development mode: Ensuring Lottery database tables are created...");
            await context.Database.EnsureCreatedAsync();
            Log.Information("Lottery database setup completed successfully");
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

Log.Information("Starting Amesa Lottery Service");
await app.RunAsync();

public partial class Program { }

