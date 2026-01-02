using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace AmesaBackend.LotteryResults.Configuration;

public static class SwaggerConfiguration
{
    /// <summary>
    /// Configures Swagger/OpenAPI for the LotteryResults service with Bearer token security.
    /// </summary>
    public static IServiceCollection AddLotteryResultsSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo 
            { 
                Title = "Amesa Lottery Results API", 
                Version = "v1",
                Description = "Lottery Results API for Amesa Platform",
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

        return services;
    }
}
