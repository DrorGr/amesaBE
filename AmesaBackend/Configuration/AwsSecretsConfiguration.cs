using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using AmesaBackend.Configuration;
using Serilog;

namespace AmesaBackend.Configuration;

public static class AwsSecretsConfiguration
{
    /// <summary>
    /// Loads OAuth credentials from AWS Secrets Manager using AwsSecretLoader helper (production only).
    /// Loads Google and Meta OAuth credentials.
    /// </summary>
    public static IConfigurationBuilder LoadOAuthSecretsFromAws(this IConfigurationBuilder configurationBuilder, IConfiguration existingConfiguration, IHostEnvironment environment)
    {
        var awsRegion = existingConfiguration["Aws:Region"] ?? Environment.GetEnvironmentVariable("AWS_REGION") ?? "eu-north-1";

        // Load Google OAuth credentials
        var googleSecretId = existingConfiguration["Authentication:Google:SecretId"] ?? "amesa-google_people_API";
        if (environment.IsProduction())
        {
            // Load from AWS Secrets Manager in Production
            AwsSecretLoader.TryLoadJsonSecret(
                configurationBuilder,
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
            var devGoogleClientId = existingConfiguration["Authentication:Google:ClientId"];
            var devGoogleClientSecret = existingConfiguration["Authentication:Google:ClientSecret"];
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
        var metaSecretId = existingConfiguration["Authentication:Meta:SecretId"];
        if (environment.IsProduction() && !string.IsNullOrWhiteSpace(metaSecretId))
        {
            // Load from AWS Secrets Manager in Production
            AwsSecretLoader.TryLoadJsonSecret(
                configurationBuilder,
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
            var metaAppId = existingConfiguration["Authentication:Meta:AppId"];
            var metaAppSecret = existingConfiguration["Authentication:Meta:AppSecret"];
            if (!string.IsNullOrWhiteSpace(metaAppId) && !string.IsNullOrWhiteSpace(metaAppSecret))
            {
                Console.WriteLine("[OAuth] Development mode - using Meta credentials from appsettings.Development.json");
            }
            else
            {
                Console.WriteLine("[OAuth] Development mode - Meta OAuth not configured (add AppId and AppSecret to appsettings.Development.json)");
            }
        }

        return configurationBuilder;
    }
}






