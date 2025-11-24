using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.Tasks;
using AmesaBackend.Notification.Data;
using AmesaBackend.Notification.Services;
using AmesaBackend.Notification.Handlers;
using AmesaBackend.Notification.Hubs;
using AmesaBackend.Shared.Extensions;
using AmesaBackend.Shared.Middleware.Extensions;
using Serilog;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/notification-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Entity Framework
builder.Services.AddDbContext<NotificationDbContext>(options =>
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

builder.Services.AddAmesaBackendShared(builder.Configuration);

// Configure JWT Authentication (required for SignalR [Authorize] attribute)
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] 
    ?? Environment.GetEnvironmentVariable("JwtSettings__SecretKey");

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
            ValidIssuer = jwtSettings["Issuer"] ?? "AmesaBackend",
            ValidAudience = jwtSettings["Audience"] ?? "AmesaFrontend",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!)),
            ClockSkew = TimeSpan.Zero
        };

        // Extract JWT token from query string for SignalR WebSocket connections
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // #region agent log
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                var hasToken = !string.IsNullOrEmpty(accessToken);
                var isWsPath = path.StartsWithSegments("/ws");
                var queryString = context.Request.QueryString.ToString();
                Log.Information("[DEBUG] OnMessageReceived: path={Path} hasToken={HasToken} isWsPath={IsWsPath} queryString={QueryString}", 
                    path, hasToken, isWsPath, queryString);
                // #endregion
                
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/ws"))
                {
                    context.Token = accessToken;
                    // #region agent log
                    Log.Information("[DEBUG] OnMessageReceived: token set in context");
                    // #endregion
                }
                else
                {
                    // #region agent log
                    Log.Warning("[DEBUG] OnMessageReceived: token NOT set - hasToken={HasToken} isWsPath={IsWsPath}", hasToken, isWsPath);
                    // #endregion
                }
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                // #region agent log
                var path = context.HttpContext.Request.Path;
                var queryString = context.HttpContext.Request.QueryString.ToString();
                Log.Warning("[DEBUG] OnAuthenticationFailed: path={Path} queryString={QueryString} exception={Exception}", 
                    path, queryString, context.Exception?.Message ?? "null");
                // #endregion
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                // #region agent log
                var path = context.HttpContext.Request.Path;
                var queryString = context.HttpContext.Request.QueryString.ToString();
                Log.Warning("[DEBUG] OnChallenge: path={Path} queryString={QueryString}", path, queryString);
                // #endregion
                return Task.CompletedTask;
            }
        };
    });
}
else
{
    // JWT not configured - SignalR will not work with [Authorize] until JWT is configured
    // This allows the service to start, but SignalR authentication will fail
    Log.Warning("JWT SecretKey is not configured. SignalR authentication will not work. Set JwtSettings__SecretKey environment variable or configure JwtSettings:SecretKey in appsettings.");
}

// Add Services
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Add Background Service for EventBridge events
builder.Services.AddHostedService<EventBridgeEventHandler>();

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
app.UseRouting();

// Extract JWT token from query string for SignalR HTTP requests (negotiate endpoint)
app.Use(async (context, next) =>
{
    // #region agent log
    var path = context.Request.Path;
    var method = context.Request.Method;
    var isWsPath = path.StartsWithSegments("/ws");
    var accessTokenQuery = context.Request.Query["access_token"];
    var accessToken = accessTokenQuery.ToString();
    var hasToken = !string.IsNullOrWhiteSpace(accessToken);
    var existingAuthHeader = context.Request.Headers["Authorization"].ToString();
    var hasAuthHeader = !string.IsNullOrWhiteSpace(existingAuthHeader);
    Log.Information("[DEBUG] SignalRTokenExtractor:entry path={Path} method={Method} isWsPath={IsWsPath} hasToken={HasToken} hasAuthHeader={HasAuthHeader} tokenLength={TokenLength}", 
        path, method, isWsPath, hasToken, hasAuthHeader, accessToken.Length);
    // #endregion

    // For SignalR negotiate requests, extract token from query string if not in header
    if (isWsPath && hasToken)
    {
        if (!hasAuthHeader)
        {
            // #region agent log
            Log.Information("[DEBUG] SignalRTokenExtractor:extracting path={Path} method={Method} tokenLength={TokenLength}", path, method, accessToken.Length);
            // #endregion
            // Add token to Authorization header for JWT middleware
            context.Request.Headers["Authorization"] = $"Bearer {accessToken}";
            // #region agent log
            Log.Information("[DEBUG] SignalRTokenExtractor:extracted path={Path} method={Method} headerSet={HeaderSet}", path, method, context.Request.Headers.ContainsKey("Authorization"));
            // #endregion
        }
        else
        {
            // #region agent log
            Log.Information("[DEBUG] SignalRTokenExtractor:skipped path={Path} method={Method} existingHeader={ExistingHeader}", path, method, existingAuthHeader);
            // #endregion
        }
    }
    
    await next();
    
    // #region agent log
    Log.Information("[DEBUG] SignalRTokenExtractor:exit path={Path} method={Method} statusCode={StatusCode}", path, method, context.Response.StatusCode);
    // #endregion
});

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

// Map SignalR hubs
app.MapHub<NotificationHub>("/ws/notifications");

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    try
    {
        if (builder.Environment.IsDevelopment())
        {
            Log.Information("Development mode: Ensuring Notification database tables are created...");
            await context.Database.EnsureCreatedAsync();
            Log.Information("Notification database setup completed successfully");
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

Log.Information("Starting Amesa Notification Service");
await app.RunAsync();

public partial class Program { }

