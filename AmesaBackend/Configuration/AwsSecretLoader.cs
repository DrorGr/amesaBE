using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Text.Json;
using ILogger = Serilog.ILogger;

namespace AmesaBackend.Configuration
{
    public static class AwsSecretLoader
    {
        /// <summary>
        /// Loads a JSON secret from AWS Secrets Manager and maps its values to configuration keys
        /// </summary>
        /// <param name="configuration">The configuration builder to update</param>
        /// <param name="secretId">The AWS Secrets Manager secret ID</param>
        /// <param name="awsRegion">The AWS region (e.g., "eu-north-1")</param>
        /// <param name="logger">Logger instance</param>
        /// <param name="mappings">Key-value pairs mapping JSON keys to configuration keys</param>
        public static void TryLoadJsonSecret(
            IConfigurationBuilder configuration,
            string? secretId,
            string? awsRegion,
            ILogger logger,
            params (string JsonKey, string ConfigKey)[] mappings)
        {
            if (string.IsNullOrWhiteSpace(secretId))
            {
                logger.Warning("Secret ID is not configured. Skipping AWS Secrets Manager load.");
                return;
            }

            if (string.IsNullOrWhiteSpace(awsRegion))
            {
                logger.Warning("AWS Region is not configured. Skipping AWS Secrets Manager load.");
                return;
            }

            try
            {
                var client = new AmazonSecretsManagerClient(Amazon.RegionEndpoint.GetBySystemName(awsRegion));
                var request = new GetSecretValueRequest
                {
                    SecretId = secretId
                };

                var response = client.GetSecretValueAsync(request).GetAwaiter().GetResult();

                if (string.IsNullOrWhiteSpace(response.SecretString))
                {
                    logger.Warning("Secret {SecretId} exists but contains no value.", secretId);
                    return;
                }

                var secretJson = JsonDocument.Parse(response.SecretString);
                var configValues = new Dictionary<string, string?>();

                foreach (var (jsonKey, configKey) in mappings)
                {
                    if (secretJson.RootElement.TryGetProperty(jsonKey, out var value))
                    {
                        var stringValue = value.GetString();
                        if (!string.IsNullOrWhiteSpace(stringValue))
                        {
                            configValues[configKey] = stringValue;
                            logger.Information("Loaded {ConfigKey} from AWS Secrets Manager secret {SecretId}", configKey, secretId);
                        }
                        else
                        {
                            logger.Warning("Secret {SecretId} has empty value for key {JsonKey}", secretId, jsonKey);
                        }
                    }
                    else
                    {
                        logger.Warning("Secret {SecretId} does not contain key {JsonKey}", secretId, jsonKey);
                    }
                }

                if (configValues.Count > 0)
                {
                    configuration.AddInMemoryCollection(configValues);
                }
            }
            catch (ResourceNotFoundException)
            {
                logger.Warning("AWS Secrets Manager secret {SecretId} not found. OAuth may not work correctly.", secretId);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error loading secret {SecretId} from AWS Secrets Manager", secretId);
            }
        }
    }
}

