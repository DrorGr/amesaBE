using Google.Api.Gax.ResourceNames;
using Google.Cloud.RecaptchaEnterprise.V1;
using Microsoft.AspNetCore.Hosting;

namespace AmesaBackend.Auth.Services
{
    /// <summary>
    /// Service for verifying CAPTCHA tokens using Google reCAPTCHA Enterprise.
    /// </summary>
    public class CaptchaService : ICaptchaService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<CaptchaService> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly RecaptchaEnterpriseServiceClient? _client;
        private readonly string? _projectId;
        private readonly string? _siteKey;
        private readonly double _minScore;
        
        // Metrics tracking
        private static long _totalAttempts = 0;
        private static long _successCount = 0;
        private static long _failureCount = 0;
        private static readonly object _metricsLock = new object();

        public CaptchaService(
            IConfiguration configuration,
            ILogger<CaptchaService> logger,
            IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _logger = logger;
            _environment = environment;

            // Get configuration values
            _projectId = _configuration["Authentication:Google:RecaptchaProjectId"] ?? "amesa-oauth";
            _siteKey = _configuration["Authentication:Google:RecaptchaSiteKey"];
            _minScore = _configuration.GetValue<double>("Authentication:Google:RecaptchaMinScore", 0.5);

            // Initialize Google Cloud reCAPTCHA Enterprise client
            // The client will use Application Default Credentials (ADC) or environment variables
            // For AWS deployment, credentials should be set via GOOGLE_APPLICATION_CREDENTIALS or service account
            try
            {
                _client = RecaptchaEnterpriseServiceClient.Create();
                _logger.LogInformation("reCAPTCHA Enterprise client initialized for project: {ProjectId}", _projectId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize reCAPTCHA Enterprise client. CAPTCHA verification will be skipped.");
                _client = null;
            }
        }

        /// <summary>
        /// Verifies a reCAPTCHA Enterprise token.
        /// </summary>
        /// <param name="token">The reCAPTCHA token provided by the client.</param>
        /// <param name="action">The action name for reCAPTCHA v3 (optional).</param>
        /// <returns>True if the CAPTCHA is successfully verified, false otherwise.</returns>
        public async Task<bool> VerifyCaptchaAsync(string token, string? action = null)
        {
            try
            {
                // If client not initialized or site key not configured, fail open only in development
                if (_client == null || string.IsNullOrWhiteSpace(_siteKey))
                {
                    _logger.LogWarning("reCAPTCHA Enterprise not configured, skipping verification");
                    // Fail open only in development, fail closed in production for security
                    return _environment.IsDevelopment();
                }

                if (string.IsNullOrWhiteSpace(token))
                {
                    _logger.LogWarning("reCAPTCHA token is empty");
                    IncrementFailureMetric();
                    return false;
                }

                // Explicit null check for extra safety (defensive programming)
                // This check is redundant but provides extra safety in case of race conditions
                // or unexpected state changes between the earlier check and this point
                if (_client == null)
                {
                    _logger.LogError("reCAPTCHA client is null - this should not happen after null check");
                    IncrementFailureMetric();
                    return false;
                }

                // Build the assessment request
                var projectName = ProjectName.FromProject(_projectId);
                var createAssessmentRequest = new CreateAssessmentRequest
                {
                    Assessment = new Assessment
                    {
                        Event = new Event
                        {
                            SiteKey = _siteKey,
                            Token = token,
                            ExpectedAction = action ?? "register" // Default action if not specified
                        }
                    },
                    ParentAsProjectName = projectName
                };

                // Create assessment with retry logic for transient failures
                Assessment? response = null;
                const int maxRetries = 3;
                const int baseDelayMs = 100;
                
                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        response = await _client.CreateAssessmentAsync(createAssessmentRequest);
                        break; // Success, exit retry loop
                    }
                    catch (Exception ex) when (IsTransientError(ex) && attempt < maxRetries)
                    {
                        var delayMs = baseDelayMs * (int)Math.Pow(2, attempt - 1); // Exponential backoff
                        _logger.LogWarning(ex, 
                            "Transient error calling reCAPTCHA API (attempt {Attempt}/{MaxRetries}). Retrying in {DelayMs}ms", 
                            attempt, maxRetries, delayMs);
                        await Task.Delay(delayMs);
                    }
                }
                
                if (response == null)
                {
                    _logger.LogError("Failed to create reCAPTCHA assessment after {MaxRetries} attempts", maxRetries);
                    IncrementFailureMetric();
                    return false;
                }

                // Check if the token is valid
                if (!response.TokenProperties.Valid)
                {
                    _logger.LogWarning("reCAPTCHA token validation failed: {Reason}", 
                        response.TokenProperties.InvalidReason);
                    IncrementFailureMetric();
                    return false;
                }

                // Check if the expected action was executed
                if (!string.IsNullOrEmpty(action) && response.TokenProperties.Action != action)
                {
                    _logger.LogWarning("reCAPTCHA action mismatch: expected {Expected}, got {Actual}", 
                        action, response.TokenProperties.Action);
                    IncrementFailureMetric();
                    return false;
                }

                // Get the risk score
                var score = (decimal)response.RiskAnalysis.Score;
                if (score < (decimal)_minScore)
                {
                    _logger.LogWarning("reCAPTCHA score {Score} below minimum {MinScore}", score, _minScore);
                    
                    // Log reasons for low score (helpful for debugging)
                    foreach (var reason in response.RiskAnalysis.Reasons)
                    {
                        _logger.LogDebug("Risk reason: {Reason}", reason);
                    }
                    
                    IncrementFailureMetric();
                    return false;
                }

                _logger.LogDebug("reCAPTCHA verification successful. Score: {Score}, Action: {Action}", 
                    score, response.TokenProperties.Action);

                IncrementSuccessMetric();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying CAPTCHA with reCAPTCHA Enterprise");
                IncrementFailureMetric();
                return false; // Fail closed for security
            }
        }

        /// <summary>
        /// Determines if an exception represents a transient error that should be retried.
        /// </summary>
        private static bool IsTransientError(Exception ex)
        {
            // Check for network-related exceptions that might be transient
            if (ex is System.Net.Http.HttpRequestException ||
                ex is System.Net.Sockets.SocketException ||
                ex is TimeoutException ||
                ex is TaskCanceledException)
            {
                return true;
            }

            // Check for Google API specific transient errors
            if (ex.Message.Contains("deadline exceeded", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("unavailable", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("network", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Increments the success metric counter.
        /// </summary>
        private void IncrementSuccessMetric()
        {
            lock (_metricsLock)
            {
                _totalAttempts++;
                _successCount++;
                
                // Log metrics periodically (every 100 attempts) to avoid log spam
                if (_totalAttempts % 100 == 0)
                {
                    var successRate = _totalAttempts > 0 ? (double)_successCount / _totalAttempts * 100 : 0;
                    _logger.LogInformation(
                        "reCAPTCHA metrics - Total: {Total}, Success: {Success}, Failures: {Failures}, Success Rate: {SuccessRate:F2}%",
                        _totalAttempts, _successCount, _failureCount, successRate);
                    
                    // Alert if failure rate is high (more than 20% failures)
                    var failureRate = _totalAttempts > 0 ? (double)_failureCount / _totalAttempts * 100 : 0;
                    if (failureRate > 20 && _totalAttempts >= 50) // Only alert after meaningful sample size
                    {
                        _logger.LogWarning(
                            "HIGH reCAPTCHA failure rate detected: {FailureRate:F2}% (Threshold: 20%). Total attempts: {Total}",
                            failureRate, _totalAttempts);
                    }
                }
            }
        }

        /// <summary>
        /// Increments the failure metric counter.
        /// </summary>
        private void IncrementFailureMetric()
        {
            lock (_metricsLock)
            {
                _totalAttempts++;
                _failureCount++;
            }
        }

        /// <summary>
        /// Gets current CAPTCHA metrics (for monitoring/health checks).
        /// </summary>
        public static (long Total, long Success, long Failures, double SuccessRate) GetMetrics()
        {
            lock (_metricsLock)
            {
                var successRate = _totalAttempts > 0 ? (double)_successCount / _totalAttempts * 100 : 0;
                return (_totalAttempts, _successCount, _failureCount, successRate);
            }
        }
    }
}
