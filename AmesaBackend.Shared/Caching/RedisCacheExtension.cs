using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace AmesaBackend.Shared.Caching
{
    public static class RedisCacheExtension
    {
        private static string _caPath = string.Empty;

        public static void UseRedisCache(this IServiceCollection services, IConfiguration configuration, string? redisConnectionString = null)
        {
            var pfxFilePath = configuration.GetValue<string>("CacheConfig:PfxFilePath");
            _caPath = configuration.GetValue<string>("CacheConfig:CaPath") ?? string.Empty;
            var isTlsOn = configuration.GetValue<bool>("CacheConfig:UseSSL", false);
            var pfxPassword = configuration.GetValue<string>("CacheConfig:PfxPassword");
            var cacheConfig = configuration.GetSection("CacheConfig").Get<CacheConfig>() 
                ?? new CacheConfig();

            // Get Redis connection string - use passed value first, then fallback to configuration sources
            var finalRedisConnectionString = redisConnectionString  // Use passed parameter first
                ?? cacheConfig.RedisConnection 
                ?? configuration.GetConnectionString("Redis") 
                ?? configuration["CacheConfig:RedisConnection"]
                ?? Environment.GetEnvironmentVariable("ConnectionStrings__Redis")  // Also check env var directly as fallback
                ?? throw new InvalidOperationException("Redis connection string is not configured");

            // Validate connection string is not empty or whitespace
            if (string.IsNullOrWhiteSpace(finalRedisConnectionString))
            {
                throw new InvalidOperationException("Redis connection string is empty or whitespace. Set ConnectionStrings__Redis environment variable or configure CacheConfig:RedisConnection in appsettings.");
            }

            var connection = finalRedisConnectionString.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var configurationOptions = new ConfigurationOptions
            {
                AbortOnConnectFail = false,
            };

            for (var i = connection.Length - 1; i >= 0; i--)
            {
                if (string.IsNullOrWhiteSpace(connection[i]))
                    continue;  // Skip empty parts (defensive check, though TrimEntries should handle this)
                
                if (connection[i].StartsWith("password", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = connection[i].Split("=", 2);
                    if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[1]))
                    {
                        configurationOptions.Password = parts[1];
                    }
                    continue;
                }
                
                if (connection[i].StartsWith("serviceName", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = connection[i].Split("=", 2);
                    if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[1]))
                    {
                        configurationOptions.ServiceName = parts[1];
                    }
                    continue;
                }
                
                // Must be an endpoint
                configurationOptions.EndPoints.Add(connection[i]);
            }

            // Validate that at least one endpoint was added
            if (configurationOptions.EndPoints.Count == 0)
            {
                throw new InvalidOperationException("Redis connection string must contain at least one endpoint (host:port). No valid endpoints found in connection string.");
            }

            if (isTlsOn)
                ConfigureTls(configurationOptions, configuration, pfxFilePath, pfxPassword);

            services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(configurationOptions));

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = finalRedisConnectionString;
                options.InstanceName = cacheConfig.InstanceName;
            });

            // Configure CacheConfig
            services.Configure<CacheConfig>(configuration.GetSection("CacheConfig"));
            cacheConfig.RedisConnection = finalRedisConnectionString;
            services.AddSingleton(cacheConfig);

            services.AddSingleton<ICache, StackRedisCache>();
        }

        public static void ConfigureTls(ConfigurationOptions sentinelConfig, IConfiguration configuration, string? pfxFilePath, string? pfxPassword)
        {
            sentinelConfig.Ssl = true;
            sentinelConfig.SslHost = configuration.GetValue<string>("CacheConfig:SslHostName");
            sentinelConfig.CheckCertificateRevocation = false;
            sentinelConfig.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;

            if (!string.IsNullOrEmpty(pfxFilePath))
            {
                sentinelConfig.CertificateSelection += delegate
                {
                    try
                    {
                        Console.WriteLine($"Searching for SSL Certificate at: {pfxFilePath}");
                        if (File.Exists(pfxFilePath))
                        {
                            var cert = new X509Certificate2(pfxFilePath, pfxPassword);
                            return cert;
                        }
                        else
                        {
                            Console.Error.WriteLine($"Certificate file not found at: {pfxFilePath}");
                            return null!;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error selecting certificate: {ex.Message}");
                        return null!;
                    }
                };
            }

            sentinelConfig.CertificateValidation += ValidateServerCertificate;
        }

        private static bool ValidateServerCertificate(object sender, X509Certificate? certificate, X509Chain? chain,
            SslPolicyErrors sslPolicyErrors)
        {
            try
            {
                Console.WriteLine($"Start certificate validation process.");
                if (certificate == null)
                {
                    Console.WriteLine($"Certificate is null. Return false.");
                    return false;
                }

                // Creating X509Certificate2 from CA for validation purpose
                if (!string.IsNullOrEmpty(_caPath) && File.Exists(_caPath))
                {
                    try
                    {
                        Console.WriteLine($"Trying to construct X509Certificate2 object from CaPath. CaPath: {_caPath}");
                        var ca = new X509Certificate2(_caPath);
                        Console.WriteLine($"Certificate   Issuer: {certificate.Issuer} | Subject: {certificate.Subject}");
                        Console.WriteLine($"CA   Issuer: {ca.Issuer} | Subject: {ca.Subject}");
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error constructing X509Certificate2 object from CaPath. Error message:  {ex.Message}");
                        return false;
                    }
                }

                Console.Error.WriteLine("Certificate warning: {0}", sslPolicyErrors);

                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("The validation has been failed with next message: {0}", ex.Message);
                return false;
            }
        }
    }
}

