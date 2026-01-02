using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AmesaBackend.Auth.Services.Interfaces;

namespace AmesaBackend.Auth.Services
{
    /// <summary>
    /// Service for checking if passwords have been compromised in data breaches using Have I Been Pwned API.
    /// Implements k-anonymity model: only sends first 5 characters of SHA-1 hash to protect privacy.
    /// </summary>
    public class PasswordBreachService : IPasswordBreachService
    {
        private const string HIBPApiUrl = "https://api.pwnedpasswords.com/range/";
        private const int DefaultTimeoutSeconds = 5;
        private const int DefaultRetryCount = 3;

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PasswordBreachService> _logger;
        private readonly bool _enabled;
        private readonly bool _blockBreachedPasswords;
        private readonly int _timeoutSeconds;
        private readonly int _retryCount;

        public PasswordBreachService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<PasswordBreachService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            // Configuration
            _enabled = _configuration.GetValue<bool>("SecuritySettings:PasswordBreachCheck:Enabled", true);
            _blockBreachedPasswords = _configuration.GetValue<bool>("SecuritySettings:PasswordBreachCheck:BlockBreachedPasswords", false);
            _timeoutSeconds = _configuration.GetValue<int>("SecuritySettings:PasswordBreachCheck:TimeoutSeconds", DefaultTimeoutSeconds);
            _retryCount = _configuration.GetValue<int>("SecuritySettings:PasswordBreachCheck:RetryCount", DefaultRetryCount);

            // Configure HTTP client
            _httpClient.Timeout = TimeSpan.FromSeconds(_timeoutSeconds);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Amesa-Auth-Service/1.0");
        }

        public async Task<bool> IsPasswordBreachedAsync(string password)
        {
            if (!_enabled)
            {
                return false; // Service disabled, assume password is safe
            }

            if (string.IsNullOrEmpty(password))
            {
                return false;
            }

            try
            {
                // Compute SHA-1 hash of password
                var hashBytes = SHA1.HashData(Encoding.UTF8.GetBytes(password));
                var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToUpperInvariant();

                // Extract first 5 characters (prefix) and remaining 35 characters (suffix)
                var prefix = hashString.Substring(0, 5);
                var suffix = hashString.Substring(5);

                // Call HIBP API with retry logic
                var response = await CallHIBPApiWithRetryAsync(prefix);

                if (string.IsNullOrEmpty(response))
                {
                    // API call failed - fail open (don't block user)
                    _logger.LogWarning("HIBP API call failed, allowing password (fail-open)");
                    return false;
                }

                // Check if suffix is in the response
                // Response format: SUFFIX:COUNT (one per line)
                var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var parts = line.Split(':');
                    if (parts.Length >= 1 && parts[0].Equals(suffix, StringComparison.OrdinalIgnoreCase))
                    {
                        var count = parts.Length >= 2 && int.TryParse(parts[1], out var breachCount) ? breachCount : 1;
                        _logger.LogWarning("Password found in HIBP database with {Count} breaches", count);
                        return true;
                    }
                }

                return false; // Password not found in breaches
            }
            catch (Exception ex)
            {
                // Fail open - don't block user if API call fails
                _logger.LogError(ex, "Error checking password breach status");
                return false;
            }
        }

        public async Task<int> GetBreachCountAsync(string password)
        {
            if (!_enabled || string.IsNullOrEmpty(password))
            {
                return 0;
            }

            try
            {
                // Compute SHA-1 hash of password
                var hashBytes = SHA1.HashData(Encoding.UTF8.GetBytes(password));
                var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToUpperInvariant();

                // Extract first 5 characters (prefix) and remaining 35 characters (suffix)
                var prefix = hashString.Substring(0, 5);
                var suffix = hashString.Substring(5);

                // Call HIBP API with retry logic
                var response = await CallHIBPApiWithRetryAsync(prefix);

                if (string.IsNullOrEmpty(response))
                {
                    return 0;
                }

                // Check if suffix is in the response and return count
                var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var parts = line.Split(':');
                    if (parts.Length >= 1 && parts[0].Equals(suffix, StringComparison.OrdinalIgnoreCase))
                    {
                        if (parts.Length >= 2 && int.TryParse(parts[1], out var breachCount))
                        {
                            return breachCount;
                        }
                        return 1; // Found but count not available
                    }
                }

                return 0; // Password not found in breaches
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting password breach count");
                return 0;
            }
        }

        private async Task<string> CallHIBPApiWithRetryAsync(string prefix)
        {
            var lastException = (Exception?)null;

            for (int attempt = 1; attempt <= _retryCount; attempt++)
            {
                try
                {
                    var url = $"{HIBPApiUrl}{prefix}";
                    var response = await _httpClient.GetStringAsync(url);
                    return response;
                }
                catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
                {
                    lastException = ex;
                    _logger.LogWarning("HIBP API timeout on attempt {Attempt}/{RetryCount}", attempt, _retryCount);
                    if (attempt < _retryCount)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt)); // Exponential backoff
                    }
                }
                catch (HttpRequestException ex)
                {
                    lastException = ex;
                    _logger.LogWarning(ex, "HIBP API request failed on attempt {Attempt}/{RetryCount}", attempt, _retryCount);
                    if (attempt < _retryCount)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt)); // Exponential backoff
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogError(ex, "Unexpected error calling HIBP API on attempt {Attempt}/{RetryCount}", attempt, _retryCount);
                    if (attempt < _retryCount)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt)); // Exponential backoff
                    }
                }
            }

            _logger.LogError(lastException, "HIBP API call failed after {RetryCount} attempts", _retryCount);
            return string.Empty; // Return empty string to indicate failure
        }
    }
}



