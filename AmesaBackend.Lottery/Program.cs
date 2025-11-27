using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.Tasks;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Lottery.Hubs;
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

builder.Services.AddAmesaBackendShared(builder.Configuration);

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
        ValidIssuer = jwtSettings["Issuer"] ?? "AmesaBackend",
        ValidAudience = jwtSettings["Audience"] ?? "AmesaFrontend",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!)),
        ClockSkew = TimeSpan.Zero
    };

    // Extract JWT token from query string for SignalR WebSocket connections and HTTP negotiate requests
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
            var allQueryParams = string.Join(", ", context.Request.Query.Select(q => $"{q.Key}={q.Value}"));
            Log.Information("[DEBUG] OnMessageReceived: path={Path} hasToken={HasToken} isWsPath={IsWsPath} tokenLength={TokenLength} queryString={QueryString} allParams={AllParams}", 
                path, hasToken, isWsPath, accessToken.ToString().Length, queryString, allQueryParams);
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
                Log.Warning("[DEBUG] OnMessageReceived: token NOT set - hasToken={HasToken} isWsPath={IsWsPath} path={Path}", hasToken, isWsPath, path);
                // #endregion
            }
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            // #region agent log
            var path = context.HttpContext.Request.Path;
            var queryString = context.HttpContext.Request.QueryString.ToString();
            var principal = context.Principal;
            var hasPrincipal = principal != null;
            var userId = principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "null";
            Log.Information("[DEBUG] OnTokenValidated: path={Path} hasPrincipal={HasPrincipal} userId={UserId} queryString={QueryString}", path, hasPrincipal, userId, queryString);
            // #endregion
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            // #region agent log
            var path = context.HttpContext.Request.Path;
            var queryString = context.Request.QueryString.ToString();
            Log.Warning("[DEBUG] OnAuthenticationFailed: path={Path} queryString={QueryString} exception={Exception}", 
                path, queryString, context.Exception?.Message ?? "null");
            // #endregion
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            // #region agent log
            var path = context.HttpContext.Request.Path;
            var queryString = context.Request.QueryString.ToString();
            Log.Warning("[DEBUG] OnChallenge: path={Path} queryString={QueryString}", path, queryString);
            // #endregion
            return Task.CompletedTask;
        }
    };
});

// Add Services
// Note: IUserPreferencesService is optional for ILotteryService
// If you want favorites functionality, add project reference to AmesaBackend.Auth
// and register: builder.Services.AddScoped<IUserPreferencesService, UserPreferencesService>();
builder.Services.AddScoped<ILotteryService, LotteryService>();
builder.Services.AddScoped<IWatchlistService, WatchlistService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<AmesaBackend.Shared.Configuration.IConfigurationService, AmesaBackend.Lottery.Services.ConfigurationService>();

// Add Background Service for lottery draws
builder.Services.AddHostedService<LotteryDrawService>();

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
    var rawQueryString = context.Request.QueryString.ToString();
    var fullUrl = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{rawQueryString}";
    var accessTokenQuery = context.Request.Query["access_token"];
    var accessToken = accessTokenQuery.ToString();
    var hasToken = !string.IsNullOrWhiteSpace(accessToken);
    var existingAuthHeader = context.Request.Headers["Authorization"].ToString();
    var hasAuthHeader = !string.IsNullOrWhiteSpace(existingAuthHeader);
    var allQueryKeys = string.Join(", ", context.Request.Query.Keys);
    var allHeaders = string.Join("; ", context.Request.Headers.Select(h => $"{h.Key}={h.Value}"));
    Log.Information("[DEBUG] SignalRTokenExtractor:entry path={Path} method={Method} isWsPath={IsWsPath} hasToken={HasToken} hasAuthHeader={HasAuthHeader} tokenLength={TokenLength} rawQueryString={RawQueryString} fullUrl={FullUrl} allQueryKeys={AllQueryKeys} allHeaders={AllHeaders}", 
        path, method, isWsPath, hasToken, hasAuthHeader, accessToken.Length, rawQueryString, fullUrl, allQueryKeys, allHeaders);
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

