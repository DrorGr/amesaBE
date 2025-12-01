using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System.Text.Json;

namespace AmesaBackend.Notification.Configuration;

public static class AwsSecretsConfiguration
{
    /// <summary>
    /// Loads notification secrets from AWS Secrets Manager (production only).
    /// Secrets are loaded and merged into the configuration.
    /// </summary>
    public static IConfigurationBuilder LoadNotificationSecretsFromAws(
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

                // Load WebPush VAPID keys
                LoadSecret(client, "amesa/notification/webpush-vapid-keys", (secretValue) =>
                {
                    var secrets = JsonSerializer.Deserialize<Dictionary<string, string>>(secretValue);
                    if (secrets != null)
                    {
                        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["NotificationChannels:WebPush:VapidPublicKey"] = secrets.GetValueOrDefault("VapidPublicKey"),
                            ["NotificationChannels:WebPush:VapidPrivateKey"] = secrets.GetValueOrDefault("VapidPrivateKey")
                        });
                    }
                });

                // Load Telegram bot token
                LoadSecret(client, "amesa/notification/telegram-bot-token", (secretValue) =>
                {
                    configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["NotificationChannels:Telegram:BotToken"] = secretValue
                    });
                });

                // Load SQS queue URL (optional)
                LoadSecret(client, "amesa/notification/sqs-queue-url", (secretValue) =>
                {
                    configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["NotificationQueue:SqsQueueUrl"] = secretValue
                    });
                }, required: false);
            }
            catch (Exception ex)
            {
                // Log error but don't fail startup - secrets may not be configured yet
                Console.WriteLine($"Warning: Failed to load secrets from AWS Secrets Manager: {ex.Message}");
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

            var response = client.GetSecretValueAsync(request).GetAwaiter().GetResult();
            
            if (response.SecretString != null)
            {
                // Trim BOM and whitespace that might be present in secrets
                // Handle both single U+FEFF character and three-byte UTF-8 BOM sequence (EF BB BF)
                var cleanedSecret = response.SecretString.Trim();
                // Remove UTF-8 BOM (single character U+FEFF)
                cleanedSecret = cleanedSecret.TrimStart('\uFEFF', '\u200B');
                // Remove UTF-8 BOM as three-character sequence (ï»¿ when interpreted as Latin-1)
                if (cleanedSecret.Length >= 3 && cleanedSecret[0] == '\u00EF' && cleanedSecret[1] == '\u00BB' && cleanedSecret[2] == '\u00BF')
                {
                    cleanedSecret = cleanedSecret.Substring(3);
                }
                configureAction(cleanedSecret);
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

