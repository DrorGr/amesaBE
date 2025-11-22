using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using AmesaBackend.Auth.Data;
using AmesaBackend.Auth.Services;
using AmesaBackend.Auth.Models;
using AmesaBackend.Shared.Extensions;
using AmesaBackend.Shared.Middleware.Extensions;
using AmesaBackend.Shared.Logging;
using Serilog;
using Npgsql;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// Configure Npgsql for dynamic JSON support
NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson();

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/auth-.txt", rollingInterval: RollingInterval.Day)
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
        Title = "Amesa Auth API", 
        Version = "v1",
        Description = "Authentication and User Management API for Amesa Lottery Platform",
        Contact = new OpenApiContact
        {
            Name = "Amesa Support",
            Email = "support@amesa.com"
        }
    });

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

// Configure Entity Framework with PostgreSQL
builder.Services.AddDbContext<AuthDbContext>(options =>
{
    var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") 
        ?? builder.Configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrEmpty(connectionString))
    {
        if (builder.Environment.IsDevelopment())
        {
            var devConfig = new ConfigurationBuilder()
                .SetBasePath(builder.Environment.ContentRootPath)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .Build();
            
            connectionString = devConfig.GetConnectionString("DefaultConnection");
        }
    }

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

// Load OAuth credentials from AWS Secrets Manager (only in Production)
if (builder.Environment.IsProduction())
{
    var awsRegion = builder.Configuration["Aws:Region"] ?? Environment.GetEnvironmentVariable("AWS_REGION") ?? "eu-north-1";
    // OAuth secrets loading would go here if needed
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
if (builder.Environment.IsProduction() || builder.Environment.IsStaging())
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

var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = "Cookies";
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddCookie("Cookies", options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!)),
            ClockSkew = TimeSpan.Zero
        };

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

// Conditionally add Google OAuth only if credentials are configured
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authBuilder.AddGoogle(options =>
    {

        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.CallbackPath = "/api/v1/oauth/google-callback";
        options.SignInScheme = "Cookies";
        options.SaveTokens = true;

        options.CorrelationCookie.Name = ".Amesa.Google.Correlation";
        options.CorrelationCookie.HttpOnly = true;
        options.CorrelationCookie.Path = "/";
        options.CorrelationCookie.MaxAge = TimeSpan.FromMinutes(10);
        
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

        options.Events.OnCreatingTicket = async context =>
        {
            try
            {
                var serviceProvider = context.HttpContext.RequestServices;
                var authService = serviceProvider.GetRequiredService<IAuthService>();
                var memoryCache = serviceProvider.GetRequiredService<IMemoryCache>();
                var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
                
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

                var (authResponse, isNewUser) = await authService.CreateOrUpdateOAuthUserAsync(
                    email: email,
                    providerId: googleId,
                    provider: AuthProvider.Google,
                    firstName: firstName,
                    lastName: lastName
                );

                var tempToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                
                var cacheKey = $"oauth_token_{tempToken}";
                memoryCache.Set(cacheKey, new OAuthTokenCache
                {
                    AccessToken = authResponse.AccessToken,
                    RefreshToken = authResponse.RefreshToken,
                    ExpiresAt = authResponse.ExpiresAt,
                    IsNewUser = isNewUser,
                    UserAlreadyExists = !isNewUser
                }, TimeSpan.FromMinutes(5));

                var emailCacheKey = $"oauth_temp_token_{email}";
                memoryCache.Set(emailCacheKey, tempToken, TimeSpan.FromMinutes(5));
                context.Properties.Items["temp_token"] = tempToken;
                
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
            Log.Error("Google OAuth remote failure: {Error}", errorMessage);
            var frontendUrl = builder.Configuration["FrontendUrl"] ?? "http://localhost:4200";
            context.Response.Redirect($"{frontendUrl}/auth/callback?error={Uri.EscapeDataString(errorMessage)}");
            context.HandleResponse();
            return Task.CompletedTask;
        };
        
        options.Events.OnAccessDenied = context =>
        {
            Log.Warning("Google OAuth access denied");
            var frontendUrl = builder.Configuration["FrontendUrl"] ?? "http://localhost:4200";
            context.Response.Redirect($"{frontendUrl}/auth/callback?error={Uri.EscapeDataString("Access denied")}");
            context.HandleResponse();
            return Task.CompletedTask;
        };
    });
}
else
{
    Log.Warning("Google OAuth credentials not configured. Google login will not work.");
}

// Conditionally add Meta/Facebook OAuth only if credentials are configured
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

        options.CorrelationCookie.Name = ".Amesa.Meta.Correlation";
        options.CorrelationCookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
        options.CorrelationCookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
    });
}
else
{
    Log.Warning("Meta OAuth credentials not configured. Meta login will not work.");
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

// Add Shared Library Services
builder.Services.AddAmesaBackendShared(builder.Configuration);

// Add Application Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAdminAuthService, AdminAuthService>();
builder.Services.AddScoped<IUserPreferencesService, UserPreferencesService>();
builder.Services.AddHttpContextAccessor();

// Add Memory Cache
builder.Services.AddMemoryCache();

// Add Health Checks
builder.Services.AddHealthChecks();

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
        options.ListenLocalhost(5001, listenOptions =>
        {
            listenOptions.UseHttps();
        });
        options.ListenLocalhost(5000);
    });
}

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Amesa Auth API V1");
    c.RoutePrefix = "swagger";
});

// Add CORS early in pipeline
app.UseCors("AllowFrontend");

// Add shared middleware
// Enable X-Ray tracing if configured
if (builder.Configuration.GetValue<bool>("XRay:Enabled", false))
{
    // X-Ray tracing removed for microservices
}

app.UseAmesaMiddleware();
app.UseAmesaLogging();

// Add response compression
app.UseResponseCompression();

// Add routing
app.UseRouting();

// Add authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// Add health checks endpoint
app.MapHealthChecks("/health");

// Map controllers
app.MapControllers();

// Ensure database is created and migrated
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    try
    {
        if (builder.Environment.IsDevelopment())
        {
            Log.Information("Development mode: Ensuring Auth database tables are created...");
            await context.Database.EnsureCreatedAsync();
            Log.Information("Auth database setup completed successfully");
        }
        else
        {
            Log.Information("Production mode: Skipping EnsureCreated (use migrations)");
        }
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

