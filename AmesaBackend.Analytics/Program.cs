using Microsoft.EntityFrameworkCore;
using AmesaBackend.Analytics.Data;
using AmesaBackend.Analytics.Services;
using AmesaBackend.Shared.Extensions;
using AmesaBackend.Shared.Middleware.Extensions;
using Serilog;
using Npgsql;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// JSON support is enabled by default in Npgsql 7.0+
// No need for GlobalTypeMapper.EnableDynamicJson() (obsolete)

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/analytics-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = false;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Amesa Analytics API",
        Version = "v1",
        Description = "Analytics service endpoints for sessions and activity."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT Bearer token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

// Configure Entity Framework
builder.Services.AddDbContext<AnalyticsDbContext>(options =>
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

builder.Services.AddAmesaBackendShared(builder.Configuration, builder.Environment);

// Add CORS policy for frontend access
builder.Services.AddAmesaCors(builder.Configuration);

// Add Services
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();

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

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();
    try
    {
        if (builder.Environment.IsDevelopment())
        {
            Log.Information("Development mode: Ensuring Analytics database tables are created...");
            await context.Database.EnsureCreatedAsync();
            Log.Information("Analytics database setup completed successfully");
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

Log.Information("Starting Amesa Analytics Service");
await app.RunAsync();

public partial class Program { }

