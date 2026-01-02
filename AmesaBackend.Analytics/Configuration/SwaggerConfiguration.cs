using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace AmesaBackend.Analytics.Configuration;

public static class SwaggerConfiguration
{
    /// <summary>
    /// Configures Swagger/OpenAPI for the Analytics service with Bearer token security.
    /// </summary>
    public static IServiceCollection AddAnalyticsSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
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

        return services;
    }
}
