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
using AmesaBackend.Auth.Middleware;
using AmesaBackend.Auth.BackgroundServices;
using AmesaBackend.Shared.Extensions;
using AmesaBackend.Shared.Middleware.Extensions;
using AmesaBackend.Shared.Logging;
using Serilog;
using Npgsql;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Linq;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Amazon.EventBridge;
using Amazon.Rekognition;
using System.Text.Json;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);
var isProduction = builder.Environment.IsProduction();

// Write Google service account JSON from environment variable to file (for ECS deployment)
var googleCredentialsPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
var googleServiceAccountJson = Environment.GetEnvironmentVariable("GOOGLE_SERVICE_ACCOUNT_JSON");
if (!string.IsNullOrEmpty(googleCredentialsPath) && !string.IsNullOrEmpty(googleServiceAccountJson) && !File.Exists(googleCredentialsPath))
{
    try
    {
        // Process the JSON string - handle base64 encoding, BOM, and whitespace
        string processedJson = googleServiceAccountJson.Trim();
        
        // Remove UTF-8 BOM if present (0xEF 0xBB 0xBF)
        if (processedJson.Length > 0 && processedJson[0] == '\uFEFF')
        {
            processedJson = processedJson.Substring(1);
        }
        
        // Try base64 decode if it doesn't start with '{' (JSON object start)
        if (!processedJson.TrimStart().StartsWith("{"))
        {
            try
            {
                byte[] decodedBytes = Convert.FromBase64String(processedJson);
                processedJson = System.Text.Encoding.UTF8.GetString(decodedBytes);
                Console.WriteLine("Google service account JSON was base64 encoded, decoded successfully");
            }
            catch (FormatException)
            {
                // Not base64, continue with original string
                Console.WriteLine("Google service account JSON is not base64 encoded, using as-is");
            }
        }
        
        // Validate JSON before writing
        using (JsonDocument.Parse(processedJson))
        {
            // JSON is valid, continue
        }

        var directory = Path.GetDirectoryName(googleCredentialsPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        // Write with UTF-8 encoding (no BOM)
        File.WriteAllText(googleCredentialsPath, processedJson, System.Text.Encoding.UTF8);
        
        // Set restrictive file permissions on Linux/Unix
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            try
            {
                File.SetUnixFileMode(googleCredentialsPath, 
                    System.IO.UnixFileMode.UserRead | System.IO.UnixFileMode.UserWrite);
            }
            catch (PlatformNotSupportedException)
            {
                // Not on Unix, ignore
            }
        }
        
        Console.WriteLine($"Google service account credentials written to: {googleCredentialsPath}");
    }
    catch (JsonException ex)
    {
        var errorMsg = $"Invalid Google service account JSON: {ex.Message}. " +
                      $"First 100 chars: {googleServiceAccountJson?.Substring(0, Math.Min(100, googleServiceAccountJson.Length ?? 0))}";
        Console.WriteLine($"ERROR: {errorMsg}");
        if (isProduction)
        {
            throw new InvalidOperationException(errorMsg, ex);
        }
    }
    catch (Exception ex)
    {
        var errorMsg = $"Failed to write Google service account credentials file: {ex.Message}";
        Console.WriteLine($"ERROR: {errorMsg}");
        if (isProduction)
        {
            throw new InvalidOperationException(errorMsg, ex);
        }
    }
}

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/auth-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Configure forwarded headers for load balancer/proxy (required for HTTPS detection)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    // Trust all proxies in production (ALB/CloudFront)
    // In a more secure setup, you'd whitelist specific proxy IPs
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configure JSON serialization to use camelCase for property names
        // This ensures frontend receives properties in camelCase (e.g., "success" instead of "Success")
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = false;
    });
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
    
    // #region agent log - Enable SQL logging to diagnose deleted_at column error
    // Log all EF Core SQL queries to diagnose the deleted_at column error
    options.LogTo((message) => {
        // Log all SQL queries and commands
        if (message.Contains("Executing") || message.Contains("Executed") || message.Contains("Failed"))
        {
            Log.Information("[SQL DEBUG] {Message}", message);
        }
    }, new[] { 
        Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.CommandExecuting,
        Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.CommandExecuted,
        Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.CommandError
    });
    // #endregion
});

// Load OAuth credentials from AWS Secrets Manager (only in Production)
if (builder.Environment.IsProduction())
{
    var oauthAwsRegion = builder.Configuration["Aws:Region"] ?? Environment.GetEnvironmentVariable("AWS_REGION") ?? "eu-north-1";
    var googleSecretId = builder.Configuration["Authentication:Google:SecretId"] ?? "amesa-google_people_API";
    
    try
    {
        var client = new AmazonSecretsManagerClient(Amazon.RegionEndpoint.GetBySystemName(oauthAwsRegion));
        var request = new GetSecretValueRequest
        {
            SecretId = googleSecretId
        };

        var response = client.GetSecretValueAsync(request).GetAwaiter().GetResult();

        if (!string.IsNullOrWhiteSpace(response.SecretString))
        {
            var secretJson = JsonDocument.Parse(response.SecretString);
            var configValues = new Dictionary<string, string?>();

            if (secretJson.RootElement.TryGetProperty("ClientId", out var clientIdValue))
            {
                var clientId = clientIdValue.GetString();
                if (!string.IsNullOrWhiteSpace(clientId))
                {
                    configValues["Authentication:Google:ClientId"] = clientId;
                    Log.Information("Loaded Google ClientId from AWS Secrets Manager secret {SecretId}", googleSecretId);
                }
            }

            if (secretJson.RootElement.TryGetProperty("ClientSecret", out var clientSecretValue))
            {
                var clientSecret = clientSecretValue.GetString();
                if (!string.IsNullOrWhiteSpace(clientSecret))
                {
                    configValues["Authentication:Google:ClientSecret"] = clientSecret;
                    Log.Information("Loaded Google ClientSecret from AWS Secrets Manager secret {SecretId}", googleSecretId);
                }
            }

            // Load reCAPTCHA Enterprise Site Key if present
            if (secretJson.RootElement.TryGetProperty("RecaptchaSiteKey", out var recaptchaSiteKeyValue))
            {
                var recaptchaSiteKey = recaptchaSiteKeyValue.GetString();
                if (!string.IsNullOrWhiteSpace(recaptchaSiteKey))
                {
                    configValues["Authentication:Google:RecaptchaSiteKey"] = recaptchaSiteKey;
                    Log.Information("Loaded Google reCAPTCHA Enterprise Site Key from AWS Secrets Manager secret {SecretId}", googleSecretId);
                }
            }

            // Load reCAPTCHA Enterprise Project ID if present
            if (secretJson.RootElement.TryGetProperty("RecaptchaProjectId", out var recaptchaProjectIdValue))
            {
                var recaptchaProjectId = recaptchaProjectIdValue.GetString();
                if (!string.IsNullOrWhiteSpace(recaptchaProjectId))
                {
                    configValues["Authentication:Google:RecaptchaProjectId"] = recaptchaProjectId;
                    Log.Information("Loaded Google reCAPTCHA Enterprise Project ID from AWS Secrets Manager secret {SecretId}", googleSecretId);
                }
            }

            // Load reCAPTCHA min score if present
            if (secretJson.RootElement.TryGetProperty("RecaptchaMinScore", out var recaptchaScoreValue))
            {
                var recaptchaScore = recaptchaScoreValue.GetString();
                if (!string.IsNullOrWhiteSpace(recaptchaScore))
                {
                    configValues["Authentication:Google:RecaptchaMinScore"] = recaptchaScore;
                    Log.Information("Loaded Google reCAPTCHA Min Score from AWS Secrets Manager secret {SecretId}", googleSecretId);
                }
            }

            if (configValues.Count > 0)
            {
                builder.Configuration.AddInMemoryCollection(configValues);
                Console.WriteLine("[OAuth] Loaded Google credentials from AWS Secrets Manager");
                
                // Log ClientId preview for verification (first 10 chars for security)
                var clientId = configValues["Authentication:Google:ClientId"];
                if (!string.IsNullOrWhiteSpace(clientId))
                {
                    var clientIdPreview = clientId.Length > 10 ? clientId.Substring(0, 10) + "..." : clientId;
                    Log.Information("OAuth ClientId loaded (preview): {ClientIdPreview}", clientIdPreview);
                }
            }
            else
            {
                Log.Warning("No OAuth credentials were loaded from AWS Secrets Manager secret {SecretId}", googleSecretId);
            }
        }
    }
    catch (ResourceNotFoundException)
    {
        Log.Warning("AWS Secrets Manager secret {SecretId} not found. OAuth may not work correctly.", googleSecretId);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error loading secret {SecretId} from AWS Secrets Manager", googleSecretId);
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
                
                // #region agent log
                var redirectUriBefore = context.Properties.RedirectUri ?? "NULL";
                logger.LogInformation("[DEBUG] OnCreatingTicket:entry hypothesisId=A,B redirectUriBefore={RedirectUriBefore}", redirectUriBefore);
                // #endregion
                
                var claims = context.Principal?.Claims.ToList() ?? new List<Claim>();
                var email = claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
                var googleId = claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var firstName = claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.GivenName)?.Value;
                var lastName = claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Surname)?.Value;
                
                // Extract additional Google claims
                var birthdateClaim = claims.FirstOrDefault(c => c.Type == "birthdate")?.Value;
                var genderClaim = claims.FirstOrDefault(c => c.Type == "gender")?.Value;
                var pictureClaim = claims.FirstOrDefault(c => c.Type == "picture")?.Value;
                
                DateTime? dateOfBirth = null;
                if (!string.IsNullOrEmpty(birthdateClaim) && DateTime.TryParse(birthdateClaim, out var parsedDate))
                {
                    dateOfBirth = parsedDate;
                }
                
                string? gender = null;
                if (!string.IsNullOrEmpty(genderClaim))
                {
                    // Map Google gender to our enum (male/female/other)
                    gender = genderClaim.ToLower() switch
                    {
                        "male" => "Male",
                        "female" => "Female",
                        _ => "Other"
                    };
                }

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(googleId))
                {
                    logger.LogWarning("Google OAuth ticket creation: missing email or ID");
                    context.Fail("Missing email or Google ID");
                    return;
                }

                logger.LogInformation("Google OAuth ticket being created for: {Email}", email);

                // #region agent log
                logger.LogInformation("[DEBUG] OnCreatingTicket:before-CreateOrUpdateOAuthUserAsync hypothesisId=E email={Email}", email);
                // #endregion

                var (authResponse, isNewUser) = await authService.CreateOrUpdateOAuthUserAsync(
                    email: email,
                    providerId: googleId,
                    provider: AuthProvider.Google,
                    firstName: firstName,
                    lastName: lastName,
                    dateOfBirth: dateOfBirth,
                    gender: gender,
                    profileImageUrl: pictureClaim
                );

                // #region agent log
                logger.LogInformation("[DEBUG] OnCreatingTicket:after-CreateOrUpdateOAuthUserAsync hypothesisId=E hasAuthResponse={HasAuthResponse} isNewUser={IsNewUser}", authResponse != null, isNewUser);
                logger.LogInformation("[DEBUG] OnCreatingTicket:checking-authResponse hypothesisId=E authResponseNull={AuthResponseNull} hasAccessToken={HasAccessToken} hasRefreshToken={HasRefreshToken}", 
                    authResponse == null, authResponse?.AccessToken != null, authResponse?.RefreshToken != null);
                // #endregion

                if (authResponse == null)
                {
                    logger.LogError("[DEBUG] OnCreatingTicket:authResponse-is-null hypothesisId=E");
                    context.Fail("Failed to create authentication response");
                    return;
                }

                if (string.IsNullOrEmpty(authResponse.AccessToken))
                {
                    logger.LogError("[DEBUG] OnCreatingTicket:accessToken-is-null-or-empty hypothesisId=E");
                    context.Fail("Failed to generate access token");
                    return;
                }

                // #region agent log
                logger.LogInformation("[DEBUG] OnCreatingTicket:before-tempToken-generation hypothesisId=E");
                // #endregion

                var tempToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                
                // #region agent log
                logger.LogInformation("[DEBUG] OnCreatingTicket:after-tempToken-generation hypothesisId=E tempTokenLength={TempTokenLength}", tempToken?.Length ?? 0);
                // #endregion
                
                var cacheKey = $"oauth_token_{tempToken}";
                
                // #region agent log
                logger.LogInformation("[DEBUG] OnCreatingTicket:before-cache-set hypothesisId=E cacheKey={CacheKey}", cacheKey);
                // #endregion
                
                memoryCache.Set(cacheKey, new OAuthTokenCache
                {
                    AccessToken = authResponse.AccessToken,
                    RefreshToken = authResponse.RefreshToken,
                    ExpiresAt = authResponse.ExpiresAt,
                    IsNewUser = isNewUser,
                    UserAlreadyExists = !isNewUser
                }, TimeSpan.FromMinutes(5));

                // #region agent log
                logger.LogInformation("[DEBUG] OnCreatingTicket:after-cache-set hypothesisId=E");
                // #endregion

                var emailCacheKey = $"oauth_temp_token_{email}";
                memoryCache.Set(emailCacheKey, tempToken, TimeSpan.FromMinutes(5));
                context.Properties.Items["temp_token"] = tempToken;
                
                // #region agent log
                var baseRedirectUri = context.Properties.RedirectUri ?? $"{frontendUrl}/auth/callback";
                var tempTokenPreview = tempToken?.Substring(0, Math.Min(10, tempToken?.Length ?? 0)) + "...";
                logger.LogInformation("[DEBUG] OnCreatingTicket:before-modify hypothesisId=A,B,C baseRedirectUri={BaseRedirectUri} tempTokenPreview={TempTokenPreview}", baseRedirectUri, tempTokenPreview);
                // #endregion
                
                var modifiedRedirectUri = $"{frontendUrl}/auth/callback?code={Uri.EscapeDataString(tempToken ?? string.Empty)}";
                context.Properties.RedirectUri = modifiedRedirectUri;
                
                // #region agent log
                logger.LogInformation("[DEBUG] OnCreatingTicket:after-modify hypothesisId=A,B,C redirectUriAfter={RedirectUriAfter} hasCode={HasCode}", modifiedRedirectUri, modifiedRedirectUri.Contains("code="));
                // #endregion
                
                logger.LogInformation("User created/updated and tokens cached for: {Email}, temp_token: {TempToken}", email, tempToken);
            }
            catch (Exception ex)
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                
                // #region agent log
                var exType = ex.GetType().FullName;
                var exMessage = ex.Message;
                var isEventBridge = ex is Amazon.EventBridge.AmazonEventBridgeException;
                var innerIsEventBridge = ex.InnerException is Amazon.EventBridge.AmazonEventBridgeException;
                logger.LogError(ex, "[DEBUG] OnCreatingTicket:catch exType={ExType} exMessage={ExMessage} isEventBridge={IsEventBridge} innerIsEventBridge={InnerIsEventBridge} hypothesisId=E", exType, exMessage, isEventBridge, innerIsEventBridge);
                // #endregion
                
                // Check if this is an EventBridge exception - these are non-fatal and should not fail OAuth
                var isEventBridgeException = ex is Amazon.EventBridge.AmazonEventBridgeException ||
                                           ex.InnerException is Amazon.EventBridge.AmazonEventBridgeException;
                
                if (isEventBridgeException)
                {
                    logger.LogWarning(ex, "[DEBUG] OnCreatingTicket:EventBridge-detected (non-fatal, OAuth flow continues) hypothesisId=E");
                    logger.LogWarning(ex, "EventBridge error in OnCreatingTicket (non-fatal, OAuth flow continues)");
                    // Don't call context.Fail() for EventBridge errors - they're non-fatal
                    // The OAuth flow should continue even if EventBridge publishing fails
                    return;
                }
                
                logger.LogError(ex, "[DEBUG] OnCreatingTicket:non-EventBridge-exception calling context.Fail() hypothesisId=E");
                logger.LogError(ex, "Error in OnCreatingTicket for Google OAuth");
                context.Fail("Error processing authentication");
            }
        };
        
        options.Events.OnRemoteFailure = context =>
        {
            var errorMessage = context.Failure?.Message ?? "Unknown error";
            var errorDescription = context.Failure?.ToString() ?? "No additional details";
            
            Log.Error("Google OAuth remote failure: {Error}", errorMessage);
            Log.Error("Google OAuth failure details: {Details}", errorDescription);
            
            // Log the ClientId (first 10 chars for security) to verify it's being used
            var clientId = builder.Configuration["Authentication:Google:ClientId"];
            if (!string.IsNullOrWhiteSpace(clientId))
            {
                var clientIdPreview = clientId.Length > 10 ? clientId.Substring(0, 10) + "..." : clientId;
                Log.Information("OAuth ClientId being used: {ClientIdPreview}", clientIdPreview);
            }
            else
            {
                Log.Warning("OAuth ClientId is null or empty!");
            }
            
            // Check if it's an invalid_client error
            if (errorMessage.Contains("invalid_client", StringComparison.OrdinalIgnoreCase))
            {
                Log.Error("INVALID_CLIENT ERROR: The ClientId or ClientSecret is incorrect or doesn't match Google Cloud Console");
                Log.Error("Please verify:");
                Log.Error("1. AWS Secrets Manager secret 'amesa-google_people_API' contains correct ClientId and ClientSecret");
                Log.Error("2. The ClientId matches the OAuth 2.0 Client ID in Google Cloud Console");
                Log.Error("3. The ClientSecret matches the OAuth 2.0 Client Secret in Google Cloud Console");
            }
            
            var frontendUrl = builder.Configuration["FrontendUrl"] ?? "https://dpqbvdgnenckf.cloudfront.net";
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

// Add AWS Services
var awsRegion = builder.Configuration["Aws:Region"] ?? Environment.GetEnvironmentVariable("AWS_REGION") ?? "eu-north-1";
builder.Services.AddSingleton<IAmazonRekognition>(sp =>
{
    var region = Amazon.RegionEndpoint.GetBySystemName(awsRegion);
    return new AmazonRekognitionClient(region);
});

// Add Application Services
// Security services
builder.Services.AddScoped<IRateLimitService, RateLimitService>();
builder.Services.AddScoped<IAccountLockoutService, AccountLockoutService>();
builder.Services.AddScoped<IPasswordValidatorService, PasswordValidatorService>();
builder.Services.AddScoped<ICaptchaService, CaptchaService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

// Auth services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAdminAuthService, AdminAuthService>();
// TODO: UserPreferencesService implementation missing
// builder.Services.AddScoped<IUserPreferencesService, UserPreferencesService>();
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
builder.Services.AddScoped<IAwsRekognitionService, AwsRekognitionService>();
builder.Services.AddScoped<IIdentityVerificationService, IdentityVerificationService>();
builder.Services.AddHttpContextAccessor();

// Note: reCAPTCHA Enterprise uses Google Cloud API client, not HttpClient
// Google Cloud credentials should be configured via GOOGLE_APPLICATION_CREDENTIALS environment variable
// or Application Default Credentials (ADC) in AWS

// Add Memory Cache
builder.Services.AddMemoryCache();

// Add Health Checks
builder.Services.AddHealthChecks();

// Add Background Services
builder.Services.AddHostedService<SessionCleanupService>();

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
// Use forwarded headers BEFORE other middleware to ensure correct scheme detection
app.UseForwardedHeaders();

// Force HTTPS scheme in production for OAuth redirects
// This ensures the OAuth redirect URI uses HTTPS even if the request comes as HTTP from the load balancer
if (app.Environment.IsProduction())
{
    app.Use(async (context, next) =>
    {
        // If request came through CloudFront/ALB, ensure scheme is HTTPS
        if (context.Request.Headers.ContainsKey("X-Forwarded-Proto"))
        {
            var proto = context.Request.Headers["X-Forwarded-Proto"].ToString();
            if (proto.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                context.Request.Scheme = "https";
            }
        }
        // Also check CloudFront-specific headers
        else if (context.Request.Headers.ContainsKey("CloudFront-Forwarded-Proto"))
        {
            var proto = context.Request.Headers["CloudFront-Forwarded-Proto"].ToString();
            if (proto.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                context.Request.Scheme = "https";
            }
        }
        // For CloudFront domains, always use HTTPS
        else if (context.Request.Host.Host.Contains("cloudfront.net", StringComparison.OrdinalIgnoreCase))
        {
            context.Request.Scheme = "https";
        }
        
        await next();
    });
}

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

// Add custom security middleware (early in pipeline)
app.UseMiddleware<IpTrackingMiddleware>();
app.UseMiddleware<EmailVerificationMiddleware>();
app.UseMiddleware<SecurityAuditMiddleware>();

// Add response compression
app.UseResponseCompression();

// Add routing
app.UseRouting();

// Add authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// Add health checks endpoint
app.MapHealthChecks("/health");

// Add CAPTCHA metrics endpoint (for monitoring)
app.MapGet("/health/captcha", () =>
{
    var metrics = AmesaBackend.Auth.Services.CaptchaService.GetMetrics();
    return Results.Json(new
    {
        totalAttempts = metrics.Total,
        successCount = metrics.Success,
        failureCount = metrics.Failures,
        successRate = metrics.SuccessRate,
        failureRate = metrics.Total > 0 ? 100 - metrics.SuccessRate : 0,
        status = metrics.SuccessRate >= 80 || metrics.Total < 10 ? "healthy" : "degraded"
    });
});

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

