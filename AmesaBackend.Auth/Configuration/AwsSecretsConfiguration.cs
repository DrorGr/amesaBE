using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace AmesaBackend.Auth.Configuration;

public static class AwsSecretsConfiguration
{
    /// <summary>
    /// Loads OAuth credentials from AWS Secrets Manager (production only).
    /// Loads ClientId, ClientSecret, RecaptchaSiteKey, RecaptchaProjectId, and RecaptchaMinScore.
    /// Adds the secrets to the configuration builder's in-memory collection.
    /// </summary>
    public static IConfigurationBuilder LoadOAuthSecretsFromAws(this IConfigurationBuilder configurationBuilder, IHostEnvironment environment)
    {
        if (environment.IsProduction())
        {
            // Build a temporary configuration to read existing values
            var tempConfig = configurationBuilder.Build();
            var oauthAwsRegion = tempConfig["Aws:Region"] ?? Environment.GetEnvironmentVariable("AWS_REGION") ?? "eu-north-1";
            var googleSecretId = tempConfig["Authentication:Google:SecretId"] ?? "amesa-google_people_API";
            
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
                        // Add to configuration builder's in-memory collection
                        configurationBuilder.AddInMemoryCollection(configValues);
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

        return configurationBuilder;
    }
}

