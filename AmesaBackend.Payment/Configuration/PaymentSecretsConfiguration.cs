using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System.Text.Json;

namespace AmesaBackend.Payment.Configuration;

public static class PaymentSecretsConfiguration
{
    /// <summary>
    /// Loads payment secrets from AWS Secrets Manager (production only).
    /// Secrets are loaded and merged into the configuration.
    /// </summary>
    public static IConfigurationBuilder LoadPaymentSecretsFromAws(
        this IConfigurationBuilder configurationBuilder,
        IHostEnvironment environment)
    {
        // Only load from AWS Secrets Manager in production
        if (environment.IsProduction())
        {
            try
            {
                var awsRegion = Environment.GetEnvironmentVariable("AWS_REGION") ?? "eu-north-1";
                var client = new AmazonSecretsManagerClient(Amazon.RegionEndpoint.GetBySystemName(awsRegion));

                // Load Stripe secrets from AWS Secrets Manager (only if not already set via ECS environment variables)
                // ECS environment variables (Stripe__ApiKey) are automatically mapped to Stripe:ApiKey by .NET Core
                // Only load from Secrets Manager if environment variables are not present
                var existingApiKey = Environment.GetEnvironmentVariable("Stripe__ApiKey") 
                    ?? Environment.GetEnvironmentVariable("STRIPE_API_KEY");
                
                if (string.IsNullOrEmpty(existingApiKey))
                {
                    // No environment variable set, load from Secrets Manager
                    LoadSecret(client, "amesa/payment/stripe-keys", (secretValue) =>
                    {
                        // Strip any leading whitespace or BOM
                        secretValue = secretValue.TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
                        
                        var secrets = JsonSerializer.Deserialize<Dictionary<string, string>>(secretValue);
                        if (secrets != null)
                        {
                            var secretKey = secrets.GetValueOrDefault("SecretKey");
                            var publishableKey = secrets.GetValueOrDefault("PublishableKey");
                            var webhookSecret = secrets.GetValueOrDefault("WebhookSecret");
                            
                            // #region agent log
                            Console.WriteLine($"[DEBUG] Stripe secrets loaded from Secrets Manager - SecretKey present: {!string.IsNullOrEmpty(secretKey)}, PublishableKey present: {!string.IsNullOrEmpty(publishableKey)}, WebhookSecret present: {!string.IsNullOrEmpty(webhookSecret)}");
                            // #endregion
                            
                            // Add in-memory collection - this should override appsettings.json values
                            // Note: AddInMemoryCollection is added last, so it has highest priority
                            var stripeConfig = new Dictionary<string, string?>
                            {
                                ["Stripe:PublishableKey"] = publishableKey,
                                ["Stripe:ApiKey"] = secretKey, // Map SecretKey to ApiKey for StripeService
                                ["Stripe:SecretKey"] = secretKey, // Keep for backward compatibility
                                ["Stripe:WebhookSecret"] = webhookSecret
                            };
                            
                            configurationBuilder.AddInMemoryCollection(stripeConfig);
                            
                            // #region agent log
                            Console.WriteLine($"[DEBUG] Stripe configuration keys set from Secrets Manager - ApiKey: {(!string.IsNullOrEmpty(secretKey) ? "SET" : "EMPTY")}, PublishableKey: {(!string.IsNullOrEmpty(publishableKey) ? "SET" : "EMPTY")}, WebhookSecret: {(!string.IsNullOrEmpty(webhookSecret) ? "SET" : "EMPTY")}");
                            Console.WriteLine($"[DEBUG] Stripe ApiKey value (first 20 chars): {(string.IsNullOrEmpty(secretKey) ? "EMPTY" : secretKey.Substring(0, Math.Min(20, secretKey.Length)) + "...")}");
                            // #endregion
                        }
                    });
                }
                else
                {
                    Console.WriteLine($"[DEBUG] Stripe API key already set via environment variable, skipping Secrets Manager load");
                }

                // Load Coinbase Commerce secrets
                LoadSecret(client, "amesa/payment/coinbase-keys", (secretValue) =>
                {
                    var secrets = JsonSerializer.Deserialize<Dictionary<string, string>>(secretValue);
                    if (secrets != null)
                    {
                        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["CoinbaseCommerce:ApiKey"] = secrets.GetValueOrDefault("ApiKey"),
                            ["CoinbaseCommerce:WebhookSecret"] = secrets.GetValueOrDefault("WebhookSecret")
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                // Log error but don't fail startup - secrets may not be configured yet
                Console.WriteLine($"Warning: Failed to load payment secrets from AWS Secrets Manager: {ex.Message}");
            }
        }

        return configurationBuilder;
    }

    private static void LoadSecret(
        AmazonSecretsManagerClient client,
        string secretName,
        Action<string> configureAction,
        bool required = true)
    {
        try
        {
            var request = new GetSecretValueRequest
            {
                SecretId = secretName
            };

            // ConfigureAwait(false) prevents deadlock when called from synchronous context
            var response = client.GetSecretValueAsync(request)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
            
            if (response.SecretString != null)
            {
                // Strip UTF-8 BOM if present (0xEF 0xBB 0xBF)
                var secretValue = response.SecretString;
                if (secretValue.Length > 0 && secretValue[0] == '\uFEFF')
                {
                    secretValue = secretValue.Substring(1);
                }
                
                configureAction(secretValue);
                Console.WriteLine($"Successfully loaded secret: {secretName}");
            }
        }
        catch (ResourceNotFoundException)
        {
            if (required)
            {
                Console.WriteLine($"Warning: Required secret not found: {secretName}");
            }
            // Optional secrets are silently ignored
        }
        catch (Exception ex)
        {
            if (required)
            {
                Console.WriteLine($"Error loading secret {secretName}: {ex.Message}");
                throw;
            }
            // Optional secrets are silently ignored
        }
    }
}

