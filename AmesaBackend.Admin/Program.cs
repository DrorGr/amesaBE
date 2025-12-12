using AmesaBackend.Admin.Services;
using AmesaBackend.Admin.Data;
using AmesaBackend.Admin.Hubs;
using AmesaBackend.Auth.Services;
using AmesaBackend.Auth.Data;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Payment.Data;
using AmesaBackend.Content.Data;
using AmesaBackend.LotteryResults.Data;
using AmesaBackend.Notification.Data;
using AmesaBackend.Shared.Extensions;
using AmesaBackend.Shared.Middleware.Extensions;
using AmesaBackend.Admin.Middleware;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Serilog;
using StackExchange.Redis;
using Microsoft.AspNetCore.Http;
using Amazon.S3;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatch;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/admin-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Configure database connections for all schemas
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") 
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (!string.IsNullOrEmpty(connectionString))
{
    // Configure all DbContexts with schema-specific search paths
    builder.Services.AddDbContext<AuthDbContext>(options =>
        ConfigureDbContext(options, connectionString, "amesa_auth", builder.Environment));
    
    builder.Services.AddDbContext<LotteryDbContext>(options =>
        ConfigureDbContext(options, connectionString, "amesa_lottery", builder.Environment));
    
    builder.Services.AddDbContext<PaymentDbContext>(options =>
        ConfigureDbContext(options, connectionString, "amesa_payment", builder.Environment));
    
    builder.Services.AddDbContext<ContentDbContext>(options =>
        ConfigureDbContext(options, connectionString, "amesa_content", builder.Environment));
    
    builder.Services.AddDbContext<LotteryResultsDbContext>(options =>
        ConfigureDbContext(options, connectionString, "amesa_lottery_results", builder.Environment));
    
    builder.Services.AddDbContext<NotificationDbContext>(options =>
        ConfigureDbContext(options, connectionString, "amesa_notification", builder.Environment));
    
    builder.Services.AddDbContext<AdminDbContext>(options =>
        ConfigureDbContext(options, connectionString, "amesa_admin", builder.Environment));
}

// Configure Redis for session storage
var redisConnection = builder.Configuration.GetConnectionString("Redis") 
    ?? builder.Configuration["CacheConfig:RedisConnection"]
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__Redis");

if (!string.IsNullOrWhiteSpace(redisConnection))
{
    // Add Redis connection
    builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        ConnectionMultiplexer.Connect(redisConnection.Trim()));
    
    // Configure distributed cache (used by session)
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection.Trim();
        options.InstanceName = builder.Configuration["CacheConfig:InstanceName"] ?? "amesa-admin";
    });
    
    Log.Information("Session storage configured to use Redis");
}
else
{
    // Fallback to in-memory distributed cache if Redis is not configured
    builder.Services.AddDistributedMemoryCache();
    Log.Warning("Redis connection not configured. Using in-memory session storage (sessions will not persist across restarts)");
}

// Configure session (always register, uses Redis if available, otherwise in-memory)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(120); // 2 hours
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

builder.Services.AddAmesaBackendShared(builder.Configuration, builder.Environment);

// Configure AWS Services
var awsRegion = Amazon.RegionEndpoint.GetBySystemName(builder.Configuration["AWS:Region"] ?? "eu-north-1");
builder.Services.AddSingleton<IAmazonS3>(sp => new AmazonS3Client(awsRegion));
builder.Services.AddSingleton<IAmazonCloudWatchLogs>(sp => new AmazonCloudWatchLogsClient(awsRegion));
builder.Services.AddSingleton<IAmazonCloudWatch>(sp => new AmazonCloudWatchClient(awsRegion));

// Add Admin Services
builder.Services.AddScoped<IAdminAuthService, AdminAuthService>();
builder.Services.AddScoped<IAdminDatabaseService, AdminDatabaseService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IHousesService, HousesService>();
builder.Services.AddScoped<IUsersService, UsersService>();
builder.Services.AddScoped<ITranslationsService, TranslationsService>();
builder.Services.AddScoped<ITicketsService, TicketsService>();
builder.Services.AddScoped<IDrawsService, DrawsService>();
builder.Services.AddScoped<IPaymentsService, PaymentsService>();
builder.Services.AddScoped<IS3ImageService, S3ImageService>();
builder.Services.AddScoped<ICloudWatchLoggingService, CloudWatchLoggingService>();
builder.Services.AddScoped<IRealTimeNotificationService, RealTimeNotificationService>();

// Configure health checks (simple endpoint that returns healthy if service is running)
builder.Services.AddHealthChecks();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Enable X-Ray tracing if configured
if (builder.Configuration.GetValue<bool>("XRay:Enabled", false))
{
    // X-Ray tracing removed for microservices
}

app.UseAmesaSecurityHeaders(); // Security headers (before other middleware)
app.UseGlobalExceptionHandler(); // Global error handling
app.UseAmesaMiddleware();
app.UseAmesaLogging();

app.UseRouting();

// Add session middleware (before authentication)
app.UseSession();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Health check endpoint (before other routes)
app.MapHealthChecks("/health");

app.MapRazorPages();
app.MapBlazorHub();
app.MapHub<AdminHub>("/admin/hub");
app.MapFallbackToPage("/_Host");

Log.Information("Starting Amesa Admin Service");
await app.RunAsync();

// Helper method to configure DbContext with schema search path
static void ConfigureDbContext(DbContextOptionsBuilder options, string connectionString, string schema, IHostEnvironment environment)
{
    var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString)
    {
        MaxPoolSize = 100,
        MinPoolSize = 10,
        ConnectionLifetime = 300, // 5 minutes
        CommandTimeout = 30, // 30 seconds
        Timeout = 15, // Connection timeout in seconds
        SearchPath = schema // Set schema search path
    };
    
    options.UseNpgsql(connectionStringBuilder.ConnectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
        
        npgsqlOptions.CommandTimeout(30);
    });
    
    if (environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
    
    // Disable concurrency detection for read operations
    options.EnableThreadSafetyChecks(false);
}

public partial class Program { }
