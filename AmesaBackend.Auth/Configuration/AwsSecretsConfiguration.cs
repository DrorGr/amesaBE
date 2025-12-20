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
    /// Loads Google OAuth (ClientId, ClientSecret, RecaptchaSiteKey, RecaptchaProjectId, RecaptchaMinScore)
    /// and Meta OAuth (AppId, AppSecret).
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
            var metaSecretId = tempConfig["Authentication:Meta:SecretId"] ?? "amesa-meta-oauth";
            var appleSecretId = tempConfig["Authentication:Apple:SecretId"] ?? "amesa-apple-oauth";
            
            var configValues = new Dictionary<string, string?>();
            var client = new AmazonSecretsManagerClient(Amazon.RegionEndpoint.GetBySystemName(oauthAwsRegion));

            // Load Google OAuth credentials
            try
            {
                var googleRequest = new GetSecretValueRequest
                {
                    SecretId = googleSecretId
                };

                var googleResponse = client.GetSecretValueAsync(googleRequest).GetAwaiter().GetResult();

                if (!string.IsNullOrWhiteSpace(googleResponse.SecretString))
                {
                    var secretJson = JsonDocument.Parse(googleResponse.SecretString);

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
                }
            }
            catch (ResourceNotFoundException)
            {
                Log.Warning("AWS Secrets Manager secret {SecretId} not found. Google OAuth may not work correctly.", googleSecretId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading Google OAuth secret {SecretId} from AWS Secrets Manager", googleSecretId);
            }

            // Load Meta OAuth credentials
            try
            {
                var metaRequest = new GetSecretValueRequest
                {
                    SecretId = metaSecretId
                };

                var metaResponse = client.GetSecretValueAsync(metaRequest).GetAwaiter().GetResult();

                if (!string.IsNullOrWhiteSpace(metaResponse.SecretString))
                {
                    var secretJson = JsonDocument.Parse(metaResponse.SecretString);

                    if (secretJson.RootElement.TryGetProperty("AppId", out var appIdValue))
                    {
                        var appId = appIdValue.GetString();
                        if (!string.IsNullOrWhiteSpace(appId))
                        {
                            configValues["Authentication:Meta:AppId"] = appId;
                            Log.Information("Loaded Meta AppId from AWS Secrets Manager secret {SecretId}", metaSecretId);
                        }
                    }

                    if (secretJson.RootElement.TryGetProperty("AppSecret", out var appSecretValue))
                    {
                        var appSecret = appSecretValue.GetString();
                        if (!string.IsNullOrWhiteSpace(appSecret))
                        {
                            configValues["Authentication:Meta:AppSecret"] = appSecret;
                            Log.Information("Loaded Meta AppSecret from AWS Secrets Manager secret {SecretId}", metaSecretId);
                        }
                    }
                }
            }
            catch (ResourceNotFoundException)
            {
                Log.Warning("AWS Secrets Manager secret {SecretId} not found. Meta OAuth may not work correctly.", metaSecretId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading Meta OAuth secret {SecretId} from AWS Secrets Manager", metaSecretId);
            }

            // Load Apple OAuth credentials
            try
            {
                var appleRequest = new GetSecretValueRequest
                {
                    SecretId = appleSecretId
                };

                var appleResponse = client.GetSecretValueAsync(appleRequest).GetAwaiter().GetResult();

                if (!string.IsNullOrWhiteSpace(appleResponse.SecretString))
                {
                    var secretJson = JsonDocument.Parse(appleResponse.SecretString);

                    if (secretJson.RootElement.TryGetProperty("ClientId", out var clientIdValue))
                    {
                        var clientId = clientIdValue.GetString();
                        if (!string.IsNullOrWhiteSpace(clientId))
                        {
                            configValues["Authentication:Apple:ClientId"] = clientId;
                            Log.Information("Loaded Apple ClientId from AWS Secrets Manager secret {SecretId}", appleSecretId);
                        }
                    }

                    if (secretJson.RootElement.TryGetProperty("TeamId", out var teamIdValue))
                    {
                        var teamId = teamIdValue.GetString();
                        if (!string.IsNullOrWhiteSpace(teamId))
                        {
                            configValues["Authentication:Apple:TeamId"] = teamId;
                            Log.Information("Loaded Apple TeamId from AWS Secrets Manager secret {SecretId}", appleSecretId);
                        }
                    }

                    if (secretJson.RootElement.TryGetProperty("KeyId", out var keyIdValue))
                    {
                        var keyId = keyIdValue.GetString();
                        if (!string.IsNullOrWhiteSpace(keyId))
                        {
                            configValues["Authentication:Apple:KeyId"] = keyId;
                            Log.Information("Loaded Apple KeyId from AWS Secrets Manager secret {SecretId}", appleSecretId);
                        }
                    }

                    if (secretJson.RootElement.TryGetProperty("PrivateKey", out var privateKeyValue))
                    {
                        var privateKey = privateKeyValue.GetString();
                        if (!string.IsNullOrWhiteSpace(privateKey))
                        {
                            configValues["Authentication:Apple:PrivateKey"] = privateKey;
                            Log.Information("Loaded Apple PrivateKey from AWS Secrets Manager secret {SecretId}", appleSecretId);
                        }
                    }
                }
            }
            catch (ResourceNotFoundException)
            {
                Log.Warning("AWS Secrets Manager secret {SecretId} not found. Apple OAuth may not work correctly.", appleSecretId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading Apple OAuth secret {SecretId} from AWS Secrets Manager", appleSecretId);
            }

            // Add all loaded values to configuration
            if (configValues.Count > 0)
            {
                configurationBuilder.AddInMemoryCollection(configValues);
                Console.WriteLine("[OAuth] Loaded OAuth credentials from AWS Secrets Manager");
                
                // Log previews for verification (first 10 chars for security)
                if (configValues.TryGetValue("Authentication:Google:ClientId", out var googleClientId) && !string.IsNullOrWhiteSpace(googleClientId))
                {
                    var clientIdPreview = googleClientId.Length > 10 ? googleClientId.Substring(0, 10) + "..." : googleClientId;
                    Log.Information("Google OAuth ClientId loaded (preview): {ClientIdPreview}", clientIdPreview);
                }
                
                if (configValues.TryGetValue("Authentication:Meta:AppId", out var metaAppId) && !string.IsNullOrWhiteSpace(metaAppId))
                {
                    var appIdPreview = metaAppId.Length > 10 ? metaAppId.Substring(0, 10) + "..." : metaAppId;
                    Log.Information("Meta OAuth AppId loaded (preview): {AppIdPreview}", appIdPreview);
                }
                
                if (configValues.TryGetValue("Authentication:Apple:ClientId", out var appleClientId) && !string.IsNullOrWhiteSpace(appleClientId))
                {
                    var clientIdPreview = appleClientId.Length > 10 ? appleClientId.Substring(0, 10) + "..." : appleClientId;
                    Log.Information("Apple OAuth ClientId loaded (preview): {ClientIdPreview}", clientIdPreview);
                }
            }
            else
            {
                Log.Warning("No OAuth credentials were loaded from AWS Secrets Manager");
            }
        }

        return configurationBuilder;
    }
}

