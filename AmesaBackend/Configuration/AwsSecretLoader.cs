using System.Text.Json;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace AmesaBackend.Configuration;

public static class AwsSecretLoader
{
    public static void TryLoadJsonSecret(
        ConfigurationManager configuration,
        string? secretId,
        string? region,
        Serilog.ILogger? logger,
        params (string SecretField, string ConfigKey)[] mappings)
    {
        if (string.IsNullOrWhiteSpace(secretId) || mappings.Length == 0)
        {
            return;
        }

        try
        {
            var clientConfig = new AmazonSecretsManagerConfig();
            if (!string.IsNullOrWhiteSpace(region))
            {
                clientConfig.RegionEndpoint = RegionEndpoint.GetBySystemName(region);
            }

            using var client = new AmazonSecretsManagerClient(clientConfig);
            var request = new GetSecretValueRequest
            {
                SecretId = secretId,
                VersionStage = "AWSCURRENT"
            };

            var response = client.GetSecretValueAsync(request).GetAwaiter().GetResult();
            var secretString = response.SecretString;

            if (string.IsNullOrWhiteSpace(secretString))
            {
                logger?.Warning("AWS secret {SecretId} returned an empty payload.", secretId);
                return;
            }

            var secretValues = JsonSerializer.Deserialize<Dictionary<string, string>>(secretString) ?? new Dictionary<string, string>();
            if (secretValues.Count == 0)
            {
                logger?.Warning("AWS secret {SecretId} did not contain any key/value pairs.", secretId);
                return;
            }

            var updates = new List<KeyValuePair<string, string?>>();

            foreach (var (secretField, configKey) in mappings)
            {
                if (TryGetValueCaseInsensitive(secretValues, secretField, out var value) && !string.IsNullOrWhiteSpace(value))
                {
                    updates.Add(new KeyValuePair<string, string?>(configKey, value));
                }
            }

            if (updates.Count == 0)
            {
                logger?.Warning("AWS secret {SecretId} did not provide any mapped values.", secretId);
                return;
            }

            configuration.AddInMemoryCollection(updates);
            logger?.Information("Loaded {Count} values from AWS secret {SecretId}.", updates.Count, secretId);
        }
        catch (Exception ex)
        {
            logger?.Error(ex, "Failed to load AWS secret {SecretId}.", secretId);
        }
    }

    private static bool TryGetValueCaseInsensitive(IDictionary<string, string> source, string key, out string value)
    {
        if (source.TryGetValue(key, out value!))
        {
            return true;
        }

        foreach (var kvp in source)
        {
            if (string.Equals(kvp.Key?.Replace(" ", string.Empty), key.Replace(" ", string.Empty), StringComparison.OrdinalIgnoreCase))
            {
                value = kvp.Value;
                return true;
            }
        }

        value = string.Empty;
        return false;
    }
}

