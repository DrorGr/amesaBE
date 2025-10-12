using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using AmesaBackend.Data;
using AmesaBackend.Services;
using AmesaBackend.Middleware;
using Serilog;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Configure Npgsql for dynamic JSON support
NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson();

// Check if we're running in seeder mode
if (args.Length > 0 && args[0] == "--seeder")
{
    // Run the database seeder
    await RunDatabaseSeeder();
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
    // Get connection string from environment variables first, then configuration
    var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ?? 
                          builder.Configuration.GetConnectionString("DefaultConnection");
    
    // Use PostgreSQL in production, SQLite in development
    if (builder.Environment.IsProduction())
    {
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
        options.UseSqlite(connectionString);
    }
    
    // Enable sensitive data logging in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
        policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:4201" })
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
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

var app = builder.Build();

// Configure the HTTP request pipeline
// Enable Swagger in all environments for testing
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Amesa Lottery API V1");
    c.RoutePrefix = "swagger";
});

// Add custom middleware
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

// Add response compression
app.UseResponseCompression();

// Rate limiting will be added later when needed

// Add CORS
app.UseCors("AllowFrontend");

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

// Ensure database is created and migrated (only for SQLite local development)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (connectionString != null && connectionString.Contains("Data Source="))
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<AmesaDbContext>();
        try
        {
            await context.Database.EnsureCreatedAsync();
            await DataSeedingService.SeedDatabaseAsync(context);
            await TranslationSeedingService.SeedTranslationsAsync(context);
            
            // Seed lottery results with sample data
            var qrCodeService = scope.ServiceProvider.GetRequiredService<IQRCodeService>();
            await LotteryResultsSeedingService.SeedLotteryResultsAsync(context, qrCodeService);
            
            Log.Information("Database setup and seeding completed successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while setting up the database");
        }
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

// Database seeder function
static async Task RunDatabaseSeeder()
{
    Console.WriteLine("🌱 Amesa Lottery Database Seeder");
    Console.WriteLine("=================================");

    // Get database connection string from environment variables
    var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") 
        ?? "Data Source=amesa.db"; // Fallback to SQLite for local development

    Console.WriteLine($"🔗 Connecting to database...");
    
    // Determine if using PostgreSQL or SQLite
    if (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine($"   Using PostgreSQL (connection from environment)");
    }
    else
    {
        Console.WriteLine($"   Using SQLite (local development)");
    }
    Console.WriteLine();

    try
    {
        // Configure Npgsql for dynamic JSON support
        NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson();
        
        // Configure DbContext
        var optionsBuilder = new DbContextOptionsBuilder<AmesaDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        using var context = new AmesaDbContext(optionsBuilder.Options);

        // Test database connection
        Console.WriteLine("🔍 Testing database connection...");
        await context.Database.OpenConnectionAsync();
        Console.WriteLine("✅ Database connection successful!");
        await context.Database.CloseConnectionAsync();
        Console.WriteLine();

        // Run the seeder
        var seeder = new DatabaseSeeder(context);
        await seeder.SeedAsync();

        Console.WriteLine();
        Console.WriteLine("🎉 Database seeding completed successfully!");
        Console.WriteLine();
        Console.WriteLine("📊 Summary of seeded data:");
        Console.WriteLine("   • 5 Languages (English, Hebrew, Arabic, Spanish, French)");
        Console.WriteLine("   • 5 Users with addresses and phone numbers");
        Console.WriteLine("   • 4 Houses with images and lottery details");
        Console.WriteLine("   • Multiple lottery tickets and transactions");
        Console.WriteLine("   • Lottery draws and results");
        Console.WriteLine("   • 18 Translations (3 languages × 6 keys)");
        Console.WriteLine("   • 3 Content categories and articles");
        Console.WriteLine("   • 3 Promotional campaigns");
        Console.WriteLine("   • 8 System settings");
        Console.WriteLine();
        Console.WriteLine("🚀 Your Amesa Lottery database is ready to use!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error: {ex.Message}");
        Console.WriteLine();
        Console.WriteLine("🔧 Troubleshooting tips:");
        Console.WriteLine("   1. Check your database connection string");
        Console.WriteLine("   2. Ensure the database server is running");
        Console.WriteLine("   3. Verify your credentials are correct");
        Console.WriteLine("   4. Check if the database 'amesa_lottery' exists");
        Console.WriteLine();
        Console.WriteLine("📝 Full error details:");
        Console.WriteLine(ex.ToString());
        Environment.Exit(1);
    }
}

// Make Program class accessible to test projects
public partial class Program { }
