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

                // Load Stripe secrets
                LoadSecret(client, "amesa/payment/stripe-keys", (secretValue) =>
                {
                    var secrets = JsonSerializer.Deserialize<Dictionary<string, string>>(secretValue);
                    if (secrets != null)
                    {
                        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["Stripe:PublishableKey"] = secrets.GetValueOrDefault("PublishableKey"),
                            ["Stripe:ApiKey"] = secrets.GetValueOrDefault("SecretKey"), // Map SecretKey to ApiKey for StripeService
                            ["Stripe:SecretKey"] = secrets.GetValueOrDefault("SecretKey"), // Keep for backward compatibility
                            ["Stripe:WebhookSecret"] = secrets.GetValueOrDefault("WebhookSecret")
                        });
                    }
                });

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
                configureAction(response.SecretString);
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

