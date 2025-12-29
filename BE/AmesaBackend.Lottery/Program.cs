using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.Services;
// using AmesaBackend.Lottery.Services.Processors; // Processors namespace doesn't exist
// using AmesaBackend.Lottery.Hubs; // Hubs namespace doesn't exist
// using AmesaBackend.Lottery.Configuration; // Configuration namespace doesn't exist
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using AmesaBackend.Shared.Extensions;
using AmesaBackend.Shared.Middleware.Extensions;
using AmesaBackend.Auth.Data;
using AmesaBackend.Auth.Services;
using AmesaBackend.Data;
using Amazon.SQS;
using Serilog;
using Npgsql;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// JSON support is enabled by default in Npgsql 7.0+
// No need for GlobalTypeMapper.EnableDynamicJson() (obsolete)

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

// Register AmesaDbContext for Promotions access (shared database, same connection string)
builder.Services.AddDbContext<AmesaDbContext>(options =>
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
// Register CircuitBreakerService FIRST (required by RateLimitService, but doesn't depend on Redis)
// #region agent log
Log.Information("[DEBUG] Registering ICircuitBreakerService - hypothesis: A");
// #endregion
builder.Services.AddSingleton<AmesaBackend.Auth.Services.ICircuitBreakerService, AmesaBackend.Auth.Services.CircuitBreakerService>();
// #region agent log
Log.Information("[DEBUG] ICircuitBreakerService registered successfully - hypothesis: A");
// #endregion

// AddAmesaBackendShared registers Redis services (IDistributedCache, IConnectionMultiplexer) which RateLimitService needs
// #region agent log
Log.Information("[DEBUG] Calling AddAmesaBackendShared - hypothesis: D");
// #endregion
builder.Services.AddAmesaBackendShared(builder.Configuration, builder.Environment, requireRedis: true);
// #region agent log
Log.Information("[DEBUG] AddAmesaBackendShared completed - checking Redis services - hypothesis: D");
var redisCacheDescriptor = builder.Services.FirstOrDefault(s => s.ServiceType == typeof(Microsoft.Extensions.Caching.Distributed.IDistributedCache));
var redisConnectionDescriptor = builder.Services.FirstOrDefault(s => s.ServiceType == typeof(StackExchange.Redis.IConnectionMultiplexer));
Log.Information("[DEBUG] Redis services registered - IDistributedCache: {HasCache}, IConnectionMultiplexer: {HasConnection} - hypothesis: D", 
    redisCacheDescriptor != null, redisConnectionDescriptor != null);
// #endregion

// Register RateLimitService AFTER AddAmesaBackendShared so Redis services are available
// #region agent log
Log.Information("[DEBUG] Registering IRateLimitService - hypothesis: A, D");
// #endregion
builder.Services.AddScoped<AmesaBackend.Auth.Services.IRateLimitService, AmesaBackend.Auth.Services.RateLimitService>();
// #region agent log
Log.Information("[DEBUG] IRateLimitService registered successfully - hypothesis: A, D");
// #endregion

// Configure Lottery settings
builder.Services.Configure<LotterySettings>(builder.Configuration.GetSection("Lottery"));

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
// #region agent log
Log.Information("[DEBUG] Registering UserPreferencesService - checking dependencies");
try
{
    // Check if AuthDbContext is registered
    var authDbContextDescriptor = builder.Services.FirstOrDefault(s => s.ServiceType == typeof(AuthDbContext));
    Log.Information("[DEBUG] AuthDbContext registration: {IsRegistered}", authDbContextDescriptor != null);
    
    // Check if IHttpRequest is registered (optional dependency)
    var httpRequestDescriptor = builder.Services.FirstOrDefault(s => s.ServiceType.Name == "IHttpRequest");
    Log.Information("[DEBUG] IHttpRequest registration: {IsRegistered}", httpRequestDescriptor != null);
}
catch (Exception ex)
{
    Log.Error(ex, "[DEBUG] Error checking UserPreferencesService dependencies");
}
// #endregion
builder.Services.AddScoped<IUserPreferencesService, UserPreferencesService>();
// #region agent log
Log.Information("[DEBUG] UserPreferencesService registered successfully");
// #endregion
builder.Services.AddScoped<ILotteryService, LotteryService>();
// PromotionService is excluded from compilation (see .csproj), so registration is commented out
// Controller handles null IPromotionService gracefully with ServiceUnavailable responses
// builder.Services.AddScoped<IPromotionService, PromotionService>();
builder.Services.AddScoped<IPromotionAuditService, PromotionAuditService>();
builder.Services.AddScoped<IErrorSanitizer, ErrorSanitizer>();
// Register GamificationService
builder.Services.AddScoped<IGamificationService, GamificationService>();
// IFileService and IHouseCacheService are available but not currently used
// Uncomment if file upload or house cache invalidation functionality is needed
// builder.Services.AddScoped<IFileService, FileService>();
// builder.Services.AddScoped<IHouseCacheService, HouseCacheService>();
builder.Services.AddScoped<AmesaBackend.Shared.Configuration.IConfigurationService, AmesaBackend.Lottery.Services.ConfigurationService>();

// Reservation system services
builder.Services.AddScoped<IRedisInventoryManager, RedisInventoryManager>();
builder.Services.AddScoped<ITicketReservationService, TicketReservationService>();
// Processor services are not implemented yet - commented out to prevent startup errors
// builder.Services.AddScoped<IReservationProcessor, ReservationProcessor>();
// builder.Services.AddScoped<IPaymentProcessor, PaymentProcessor>();
// builder.Services.AddScoped<ITicketCreatorProcessor, TicketCreatorProcessor>();

// IRateLimitService registered after AddAmesaBackendShared to ensure Redis services (IDistributedCache, IConnectionMultiplexer) are available

// Register HttpClient for payment service with timeout, retry, and circuit breaker policies
// PaymentProcessor is not implemented yet - commented out to prevent startup errors
// builder.Services.AddHttpClient<IPaymentProcessor, PaymentProcessor>(client =>
// {
//     var paymentServiceUrl = builder.Configuration["PaymentService:BaseUrl"] 
//         ?? "http://amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com/api/v1";
//     client.BaseAddress = new Uri(paymentServiceUrl);
//     var lotterySettings = builder.Configuration.GetSection("Lottery").Get<LotterySettings>() ?? new LotterySettings();
//     client.Timeout = TimeSpan.FromSeconds(lotterySettings.Payment.TimeoutSeconds);
// })
// .AddPolicyHandler(GetRetryPolicy())
// .AddPolicyHandler(GetCircuitBreakerPolicy());

// Retry policy: Exponential backoff for transient HTTP errors
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                // Log retry attempt (will be logged by PaymentProcessor)
            });
}

// Circuit breaker policy: Open circuit after 5 consecutive failures, break for 30 seconds
static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30));
}

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

// Add CORS policy for frontend access
builder.Services.AddAmesaCors(builder.Configuration);

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Enable X-Ray tracing if configured
if (builder.Configuration.GetValue<bool>("XRay:Enabled", false))
{
    // X-Ray tracing removed for microservices
}

app.UseAmesaSecurityHeaders(); // Security headers (before other middleware)
app.UseAmesaMiddleware();
app.UseAmesaLogging();

// Add CORS early in pipeline (before routing)
app.UseCors("AllowFrontend");

app.UseResponseCaching(); // Must be before UseRouting for VaryByQueryKeys to work
app.UseRouting();
// Debug routing middleware - log all API requests (production and development)
    app.Use(async (context, next) =>
    {
        var method = context.Request.Method;
        var path = context.Request.Path;
    var query = context.Request.QueryString.ToString();
        
    // #region agent log
    Log.Information("[DEBUG] Incoming request: {Method} {Path}{Query}", method, path, query);
    // #endregion
        
        await next();
        
    var statusCode = context.Response.StatusCode;
    // #region agent log
    Log.Information("[DEBUG] Request completed: {Method} {Path}{Query} - Status: {StatusCode}", method, path, query, statusCode);
    // #endregion
    
    if (statusCode == 405)
        {
            Log.Warning("405 Method Not Allowed: {Method} {Path}", method, path);
        }
    if (statusCode >= 400 && statusCode < 500)
    {
        // #region agent log
        Log.Warning("[DEBUG] Client error: {Method} {Path}{Query} - Status: {StatusCode}", method, path, query, statusCode);
        // #endregion
    }
    if (statusCode >= 500)
    {
        // #region agent log
        Log.Error("[DEBUG] Server error: {Method} {Path}{Query} - Status: {StatusCode}", method, path, query, statusCode);
        // #endregion
    }
    });
app.UseAuthentication();

// Service-to-service authentication middleware
app.UseMiddleware<AmesaBackend.Shared.Middleware.ServiceToServiceAuthMiddleware>();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

// #region agent log
// Validate DI resolution for UserPreferencesService
try
{
    using (var scope = app.Services.CreateScope())
    {
        Log.Information("[DEBUG] Attempting to resolve IUserPreferencesService...");
        var userPrefsService = scope.ServiceProvider.GetService<IUserPreferencesService>();
        if (userPrefsService == null)
        {
            Log.Error("[DEBUG] FAILED: IUserPreferencesService could not be resolved from DI container");
        }
        else
        {
            Log.Information("[DEBUG] SUCCESS: IUserPreferencesService resolved successfully - Type: {Type}", userPrefsService.GetType().Name);
        }
        
        // Also try to resolve AuthDbContext
        Log.Information("[DEBUG] Attempting to resolve AuthDbContext...");
        var authDbContext = scope.ServiceProvider.GetService<AuthDbContext>();
        if (authDbContext == null)
        {
            Log.Error("[DEBUG] FAILED: AuthDbContext could not be resolved from DI container");
        }
        else
        {
            Log.Information("[DEBUG] SUCCESS: AuthDbContext resolved successfully");
        }
        
        // Test RateLimitService resolution (hypothesis: A, D)
        Log.Information("[DEBUG] Attempting to resolve IRateLimitService... - hypothesis: A, D");
        try
        {
            var rateLimitService = scope.ServiceProvider.GetService<AmesaBackend.Auth.Services.IRateLimitService>();
            if (rateLimitService == null)
            {
                Log.Error("[DEBUG] FAILED: IRateLimitService could not be resolved from DI container - hypothesis: A, D");
            }
            else
            {
                Log.Information("[DEBUG] SUCCESS: IRateLimitService resolved successfully - Type: {Type} - hypothesis: A, D", rateLimitService.GetType().Name);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[DEBUG] ERROR resolving IRateLimitService - Type: {Type}, Message: {Message} - hypothesis: A, D", 
                ex.GetType().Name, ex.Message);
        }
        
        // Test ICircuitBreakerService resolution (hypothesis: A)
        Log.Information("[DEBUG] Attempting to resolve ICircuitBreakerService... - hypothesis: A");
        try
        {
            var circuitBreakerService = scope.ServiceProvider.GetService<AmesaBackend.Auth.Services.ICircuitBreakerService>();
            if (circuitBreakerService == null)
            {
                Log.Error("[DEBUG] FAILED: ICircuitBreakerService could not be resolved from DI container - hypothesis: A");
            }
            else
            {
                Log.Information("[DEBUG] SUCCESS: ICircuitBreakerService resolved successfully - Type: {Type} - hypothesis: A", circuitBreakerService.GetType().Name);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[DEBUG] ERROR resolving ICircuitBreakerService - Type: {Type}, Message: {Message} - hypothesis: A", 
                ex.GetType().Name, ex.Message);
        }
    }
}
catch (Exception ex)
{
    Log.Error(ex, "[DEBUG] ERROR resolving IUserPreferencesService or dependencies - Type: {Type}, Message: {Message}, StackTrace: {StackTrace}", 
        ex.GetType().Name, ex.Message, ex.StackTrace?.Substring(0, Math.Min(1000, ex.StackTrace?.Length ?? 0)));
}
// #endregion

// Map SignalR hubs
// LotteryHub is not implemented yet - commented out to prevent startup errors
// app.MapHub<LotteryHub>("/ws/lottery");

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

