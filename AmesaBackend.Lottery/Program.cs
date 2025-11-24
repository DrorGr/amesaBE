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

builder.Services.AddControllers();
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
builder.Services.AddScoped<IFileService, FileService>();

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

