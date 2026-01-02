using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace AmesaBackend.Notification.Configuration;

public static class SwaggerConfiguration
{
    /// <summary>
    /// Configures Swagger/OpenAPI for the Notification service with Bearer token security.
    /// </summary>
    public static IServiceCollection AddNotificationSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Amesa Notification API",
                Version = "v1",
                Description = "Notification service APIs including Telegram link/status endpoints."
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

        return services;
    }
}
