using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Linq;
using AmesaBackend.Data;
using AmesaBackend.Services;
using AmesaBackend.Middleware;
using AmesaBackend.Configuration;
using AmesaBackend.Models;
using System.Security.Claims;
using System.Security.Cryptography;
using Serilog;
using Npgsql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;

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

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/amesa-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger/OpenAPI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Amesa Lottery API", 
        Version = "v1",
        Description = "API for Amesa Lottery Platform",
        Contact = new OpenApiContact
        {
            Name = "Amesa Support",
            Email = "support@amesa.com"
        }
    });

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure Entity Framework with database provider based on environment
builder.Services.AddDbContext<AmesaDbContext>(options =>
{
    // Get connection string - prioritize environment variable, then Development config, then default
    var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
    
    if (string.IsNullOrEmpty(connectionString))
    {
        // In Development, explicitly load from appsettings.Development.json
        if (builder.Environment.IsDevelopment())
        {
            var devConfig = new ConfigurationBuilder()
                .SetBasePath(builder.Environment.ContentRootPath)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .Build();
            
            var devConnectionString = devConfig.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrEmpty(devConnectionString))
            {
                connectionString = devConnectionString;
                Console.WriteLine("[DB Config] ✅ Loaded connection string from appsettings.Development.json");
            }
        }
        
        // Fallback to base config if not found in Development config
        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        }
    }
    
    // Debug output to console
    var connectionPreview = connectionString != null && connectionString.Length > 50 
        ? connectionString.Substring(0, 50) + "..." 
        : connectionString ?? "NULL";
    Console.WriteLine($"[DB Config] Connection string preview: {connectionPreview}");
    
    // Use PostgreSQL if connection string contains PostgreSQL format, otherwise SQLite
    if (connectionString != null && (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) || 
                                     connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase)))
    {
        // PostgreSQL connection string detected
        Console.WriteLine("[DB Config] ✅ Using PostgreSQL database provider");
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);
        });
    }
    else
    {
        // SQLite connection string (fallback only - not recommended for production)
        // Only use SQLite if explicitly requested via connection string format
        if (builder.Environment.IsDevelopment() && string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("[DB Config] ⚠️ WARNING: No connection string found. Using SQLite fallback.");
            Console.WriteLine("[DB Config] ⚠️ For PostgreSQL, ensure appsettings.Development.json has correct connection string.");
            connectionString = "Data Source=AmesaDB.db";
        }
        Console.WriteLine("[DB Config] ⚠️ Using SQLite database provider (PostgreSQL connection string not detected)");
        options.UseSqlite(connectionString ?? "Data Source=AmesaDB.db");
    }
    
    // Enable sensitive data logging in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Load OAuth credentials from AWS Secrets Manager (Production) or optionally from secrets in Development
var awsRegion = builder.Configuration["Aws:Region"] ?? Environment.GetEnvironmentVariable("AWS_REGION") ?? "eu-north-1";

// Load Google OAuth credentials
var googleSecretId = builder.Configuration["Authentication:Google:SecretId"] ?? "amesa-google_people_API";
if (builder.Environment.IsProduction())
{
    // Load from AWS Secrets Manager in Production
    AwsSecretLoader.TryLoadJsonSecret(
        builder.Configuration,
        googleSecretId,
        awsRegion,
        Log.Logger,
        ("ClientId", "Authentication:Google:ClientId"),
        ("ClientSecret", "Authentication:Google:ClientSecret"));
    Console.WriteLine("[OAuth] Loaded Google credentials from AWS Secrets Manager");
}
else
{
    // Use hardcoded values from appsettings in Development (will be replaced by secrets when deployed)
    var devGoogleClientId = builder.Configuration["Authentication:Google:ClientId"];
    var devGoogleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    if (!string.IsNullOrWhiteSpace(devGoogleClientId) && !string.IsNullOrWhiteSpace(devGoogleClientSecret))
    {
        Console.WriteLine("[OAuth] Development mode - using Google credentials from appsettings.Development.json");
    }
    else
    {
        Console.WriteLine("[OAuth] Development mode - Google OAuth not configured (add ClientId and ClientSecret to appsettings.Development.json)");
    }
}

// Load Meta OAuth credentials
var metaSecretId = builder.Configuration["Authentication:Meta:SecretId"];
if (builder.Environment.IsProduction() && !string.IsNullOrWhiteSpace(metaSecretId))
{
    // Load from AWS Secrets Manager in Production
    AwsSecretLoader.TryLoadJsonSecret(
        builder.Configuration,
        metaSecretId,
        awsRegion,
        Log.Logger,
        ("AppId", "Authentication:Meta:AppId"),
        ("AppSecret", "Authentication:Meta:AppSecret"));
    Console.WriteLine("[OAuth] Loaded Meta credentials from AWS Secrets Manager");
}
else
{
    // Use hardcoded values from appsettings in Development (will be replaced by secrets when deployed)
    var metaAppId = builder.Configuration["Authentication:Meta:AppId"];
    var metaAppSecret = builder.Configuration["Authentication:Meta:AppSecret"];
    if (!string.IsNullOrWhiteSpace(metaAppId) && !string.IsNullOrWhiteSpace(metaAppSecret))
    {
        Console.WriteLine("[OAuth] Development mode - using Meta credentials from appsettings.Development.json");
    }
    else
    {
        Console.WriteLine("[OAuth] Development mode - Meta OAuth not configured (add AppId and AppSecret to appsettings.Development.json)");
    }
}

// Get frontend URL for OAuth redirects
var frontendUrl = builder.Configuration["FrontendUrl"] ?? 
                  builder.Configuration["AllowedOrigins:0"] ?? 
                  "https://dpqbvdgnenckf.cloudfront.net";

// Configure JWT Authentication
// In Production, JWT SecretKey is loaded from AWS SSM Parameter Store via ECS task definition secrets
// (environment variable: JwtSettings__SecretKey -> config: JwtSettings:SecretKey)
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

// Validate JWT SecretKey exists and is not a placeholder
if (string.IsNullOrWhiteSpace(secretKey))
{
    throw new InvalidOperationException("JWT SecretKey is not configured. Set JwtSettings__SecretKey environment variable or configure JwtSettings:SecretKey in appsettings.");
}

// In Production, ensure we're not using placeholder values
if (builder.Environment.IsProduction())
{
    var placeholderValues = new[] 
    { 
        "your-super-secret-key-for-jwt-tokens-min-32-chars",
        "your-super-secret-key-that-is-at-least-32-characters-long"
    };
    
    if (placeholderValues.Any(p => secretKey.Contains(p, StringComparison.OrdinalIgnoreCase)))
    {
        throw new InvalidOperationException("JWT SecretKey appears to be a placeholder. Ensure JwtSettings__SecretKey environment variable is set from AWS SSM Parameter Store.");
    }
    
    Console.WriteLine("[JWT] Using SecretKey from environment variable (SSM Parameter Store)");
}
else
{
    Console.WriteLine("[JWT] Development mode - using SecretKey from appsettings.Development.json");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    // Don't set DefaultChallengeScheme to JwtBearer - OAuth challenges need to use their specific scheme
    // When Challenge() is called with a specific scheme (like Google), it will use that scheme
    options.DefaultSignInScheme = "Cookies"; // Required for OAuth sign-in
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddCookie("Cookies", options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
        // Use Lax for localhost to allow cookies on redirects
        // In production with HTTPS, this should be None with Secure
        if (builder.Environment.IsDevelopment())
        {
            options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
        }
        else
        {
            options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
            options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
        }
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };

        // Handle token from query string for WebSocket connections
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/ws"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

// Conditionally add OAuth providers only if credentials are available
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    // Get the existing authentication builder and chain OAuth providers
    var authBuilder = builder.Services.AddAuthentication();
    authBuilder.AddGoogle(options =>
    {

        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.CallbackPath = "/api/v1/oauth/google-callback";
        options.SignInScheme = "Cookies"; // Use cookies for OAuth sign-in
        options.SaveTokens = true;

        // Log configuration for debugging
        Console.WriteLine($"[OAuth] Google ClientId: {googleClientId?.Substring(0, Math.Min(30, googleClientId?.Length ?? 0))}...");
        Console.WriteLine($"[OAuth] Google ClientSecret: {(string.IsNullOrWhiteSpace(googleClientSecret) ? "MISSING" : googleClientSecret.Substring(0, Math.Min(10, googleClientSecret.Length)) + "...")}");
        Console.WriteLine($"[OAuth] Google CallbackPath: {options.CallbackPath}");
        Console.WriteLine($"[OAuth] Frontend URL: {frontendUrl}");

        // Configure OAuth cookie for state management
        options.CorrelationCookie.Name = ".Amesa.Google.Correlation";
        options.CorrelationCookie.HttpOnly = true;
        options.CorrelationCookie.Path = "/";
        options.CorrelationCookie.MaxAge = TimeSpan.FromMinutes(10);
        
        // For development (HTTP), use Lax. For production (HTTPS), use None with Secure
        if (builder.Environment.IsDevelopment())
        {
            options.CorrelationCookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
            options.CorrelationCookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
        }
        else
        {
            options.CorrelationCookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
            options.CorrelationCookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
        }
        
        // Removed OnRedirectToAuthorizationEndpoint handler - let default redirect behavior work
        // The default behavior will redirect to Google's OAuth page with a 302 status
        
        // Handle user creation when OAuth ticket is being created
        // This event fires when the OAuth response is received and ticket is being constructed
        // We create the user, generate JWT tokens, and set the redirect URI to frontend with temp token
        options.Events.OnCreatingTicket = async context =>
        {
            try
            {
                // Get services from HttpContext
                var serviceProvider = context.HttpContext.RequestServices;
                var authService = serviceProvider.GetRequiredService<IAuthService>();
                var memoryCache = serviceProvider.GetRequiredService<IMemoryCache>();
                var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
                
                // Get user info from claims (these are populated from Google's OAuth response)
                var claims = context.Principal?.Claims.ToList() ?? new List<Claim>();
                var email = claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
                var googleId = claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var firstName = claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.GivenName)?.Value;
                var lastName = claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Surname)?.Value;

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(googleId))
                {
                    logger.LogWarning("Google OAuth ticket creation: missing email or ID");
                    context.Fail("Missing email or Google ID");
                    return;
                }

                logger.LogInformation("Google OAuth ticket being created for: {Email}", email);

                // Create/update user and get JWT tokens
                var (authResponse, isNewUser) = await authService.CreateOrUpdateOAuthUserAsync(
                    email: email,
                    providerId: googleId,
                    provider: AuthProvider.Google,
                    firstName: firstName,
                    lastName: lastName
                );

                // Generate a temporary one-time token for token exchange with frontend
                var tempToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                
                // Store JWT tokens in memory cache with 5 minute expiration, keyed by temp token
                var cacheKey = $"oauth_token_{tempToken}";
                memoryCache.Set(cacheKey, new OAuthTokenCache
                {
                    AccessToken = authResponse.AccessToken,
                    RefreshToken = authResponse.RefreshToken,
                    ExpiresAt = authResponse.ExpiresAt,
                    IsNewUser = isNewUser,
                    UserAlreadyExists = !isNewUser
                }, TimeSpan.FromMinutes(5));

                // Store temp token keyed by email so we can retrieve it after redirect
                // Also store in properties as backup
                var emailCacheKey = $"oauth_temp_token_{email}";
                memoryCache.Set(emailCacheKey, tempToken, TimeSpan.FromMinutes(5));
                context.Properties.Items["temp_token"] = tempToken;
                
                // Modify the RedirectUri to include the temp token
                // This ensures it's available after the middleware redirects
                var frontendUrl = builder.Configuration["FrontendUrl"] ?? "http://localhost:4200";
                context.Properties.RedirectUri = $"{frontendUrl}/auth/callback?code={Uri.EscapeDataString(tempToken)}";
                
                logger.LogInformation("User created/updated and tokens cached for: {Email}, temp_token: {TempToken}", email, tempToken);
            }
            catch (Exception ex)
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Error in OnCreatingTicket for Google OAuth");
                context.Fail("Error processing authentication");
            }
        };
        
        options.Events.OnRemoteFailure = context =>
        {
            var errorMessage = context.Failure?.Message ?? "Unknown error";
            var innerException = context.Failure?.InnerException?.Message ?? "";
            Log.Error("Google OAuth remote failure: {Error}, Inner: {Inner}", errorMessage, innerException);
            Console.WriteLine($"[OAuth Error] {errorMessage}");
            Console.WriteLine($"[OAuth Error Inner] {innerException}");
            
            // Redirect to frontend with error
            var frontendUrl = builder.Configuration["FrontendUrl"] ?? "http://localhost:4200";
            context.Response.Redirect($"{frontendUrl}/auth/callback?error={Uri.EscapeDataString(errorMessage)}");
            context.HandleResponse();
            
            return Task.CompletedTask;
        };
        
        options.Events.OnAccessDenied = context =>
        {
            Log.Warning("Google OAuth access denied");
            Console.WriteLine("[OAuth] Access denied");
            
            // Redirect to frontend with error
            var frontendUrl = builder.Configuration["FrontendUrl"] ?? "http://localhost:4200";
            context.Response.Redirect($"{frontendUrl}/auth/callback?error={Uri.EscapeDataString("Access denied")}");
            context.HandleResponse();
            
            return Task.CompletedTask;
        };
    });
    
    // Add Meta OAuth if credentials are available
    var metaAppId = builder.Configuration["Authentication:Meta:AppId"];
    var metaAppSecret = builder.Configuration["Authentication:Meta:AppSecret"];
    
    if (!string.IsNullOrWhiteSpace(metaAppId) && !string.IsNullOrWhiteSpace(metaAppSecret))
    {
        authBuilder.AddFacebook(options =>
        {
            options.AppId = metaAppId;
            options.AppSecret = metaAppSecret;
            options.CallbackPath = "/api/v1/oauth/meta-callback";
            options.SaveTokens = true;

            // Configure OAuth cookie for state management
            options.CorrelationCookie.Name = ".Amesa.Meta.Correlation";
            options.CorrelationCookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
            options.CorrelationCookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
        });
    }
    else
    {
        Log.Warning("Meta OAuth credentials not configured. Meta login will not work.");
    }
}
else
{
    Log.Warning("Google OAuth credentials not configured. Google login will not work.");
}

// Configure Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserOrAdmin", policy => policy.RequireRole("User", "Admin"));
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:4200" };
        Log.Information("CORS allowed origins: {Origins}", string.Join(", ", allowedOrigins));
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .SetPreflightMaxAge(TimeSpan.FromHours(24));
    });
});

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Add Application Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ILotteryService, LotteryService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IContentService, ContentService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IQRCodeService, QRCodeService>();

// Add Admin Panel Services
builder.Services.AddScoped<IAdminAuthService, AdminAuthService>();
builder.Services.AddScoped<IAdminDatabaseService, AdminDatabaseService>();
builder.Services.AddHttpContextAccessor();

// Add Session for Admin Panel
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "AmesaAdmin.Session";
});

// Add Blazor Server for Admin Panel
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add Background Services
builder.Services.AddHostedService<LotteryDrawService>();
builder.Services.AddHostedService<NotificationBackgroundService>();

// Add SignalR for real-time updates
builder.Services.AddSignalR();

// Add Memory Cache
builder.Services.AddMemoryCache();

// Add Distributed Cache (Redis in production) - Disabled for local development
// if (builder.Environment.IsProduction())
// {
//     builder.Services.AddStackExchangeRedisCache(options =>
//     {
//         options.Configuration = builder.Configuration.GetConnectionString("Redis");
//     });
// }

// Add Health Checks
builder.Services.AddHealthChecks();

// Rate limiting will be added later when needed

// Add Response Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// Configure Kestrel to use HTTPS in development (required for OAuth)
if (builder.Environment.IsDevelopment())
{
    builder.WebHost.UseKestrel(options =>
    {
        // Listen on HTTPS port 5001
        options.ListenLocalhost(5001, listenOptions =>
        {
            listenOptions.UseHttps();
        });
        // Also listen on HTTP port 5000 for backwards compatibility
        options.ListenLocalhost(5000);
    });
}

var app = builder.Build();

// Configure the HTTP request pipeline
// Enable Swagger in all environments for testing
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Amesa Lottery API V1");
    c.RoutePrefix = "swagger";
});

// Add CORS early in pipeline (before other middleware)
// Must be before UseRouting() for preflight requests to work
// CORS must be before error handling to ensure headers are sent on errors
app.UseCors("AllowFrontend");

// Add custom middleware
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

// Add response compression
app.UseResponseCompression();

// Rate limiting will be added later when needed

// Add routing first (required for Blazor Server)
app.UseRouting();

// Add authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// Add Session (after routing for Blazor Server compatibility)
app.UseSession();

// Serve static files for Blazor
app.UseStaticFiles();

// Add health checks endpoint
app.MapHealthChecks("/health");

// Map controllers
app.MapControllers();

// Map SignalR hubs
// app.MapHub<LotteryHub>("/ws/lottery");
// app.MapHub<NotificationHub>("/ws/notifications");

// Map Blazor Admin Panel
app.MapBlazorHub();
app.MapRazorPages(); // This is needed for Razor Pages
app.MapFallbackToPage("/admin", "/Admin/App");
app.MapFallbackToPage("/admin/{*path:nonfile}", "/Admin/App");

// Ensure database is created (migrations should be run manually)
// NOTE: Database seeding is DISABLED - all seeding must be done manually
// Use the standalone AmesaBackend.DatabaseSeeder project for seeding
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AmesaDbContext>();
    try
    {
        // Only ensure database schema exists - NO automatic seeding
        // Database migrations should be run manually: dotnet ef database update
        // Database seeding should be done manually using AmesaBackend.DatabaseSeeder project
        Log.Information("Ensuring database schema exists (no automatic seeding)...");
        await context.Database.EnsureCreatedAsync();
        Log.Information("Database schema check completed. No data was seeded automatically.");
        Log.Information("⚠️  To seed data manually, use: dotnet run --project AmesaBackend.DatabaseSeeder");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while setting up the database");
    }
}

// Configure graceful shutdown
app.Lifetime.ApplicationStopping.Register(() =>
{
    Log.Information("Application is shutting down...");
    Log.CloseAndFlush();
});

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
