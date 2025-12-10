using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;

namespace AmesaBackend.Auth.Services
{
    /// <summary>
    /// Service for managing circuit breakers to prevent cascading failures.
    /// Wraps Polly circuit breaker with configuration support.
    /// </summary>
    public class CircuitBreakerService : ICircuitBreakerService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<CircuitBreakerService> _logger;
        private readonly Dictionary<string, AsyncCircuitBreakerPolicy> _circuitBreakers = new();

        // Default circuit breaker configuration
        private const int DefaultFailureThreshold = 5;
        private const int DefaultDurationOfBreakSeconds = 30;
        private const int DefaultSamplingDurationSeconds = 30;

        public CircuitBreakerService(
            IConfiguration configuration,
            ILogger<CircuitBreakerService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Gets or creates a circuit breaker policy for the specified operation.
        /// </summary>
        public AsyncCircuitBreakerPolicy GetCircuitBreakerPolicy(string operationName)
        {
            if (_circuitBreakers.TryGetValue(operationName, out var existingPolicy))
            {
                return existingPolicy;
            }

            var failureThreshold = _configuration.GetValue<int>(
                $"CircuitBreaker:{operationName}:FailureThreshold",
                _configuration.GetValue<int>("CircuitBreaker:FailureThreshold", DefaultFailureThreshold));

            var durationOfBreak = TimeSpan.FromSeconds(_configuration.GetValue<int>(
                $"CircuitBreaker:{operationName}:DurationOfBreak",
                _configuration.GetValue<int>("CircuitBreaker:DurationOfBreak", DefaultDurationOfBreakSeconds)));

            var samplingDuration = TimeSpan.FromSeconds(_configuration.GetValue<int>(
                $"CircuitBreaker:{operationName}:SamplingDuration",
                _configuration.GetValue<int>("CircuitBreaker:SamplingDuration", DefaultSamplingDurationSeconds)));

            // Create circuit breaker policy using Polly v8 API
            // Note: Polly v8 uses a different API structure
            var policy = Policy
                .Handle<Exception>()
                .AdvancedCircuitBreakerAsync(
                    failureThreshold: failureThreshold / (double)samplingDuration.TotalSeconds * 100, // Convert to percentage
                    samplingDuration: samplingDuration,
                    minimumThroughput: failureThreshold,
                    durationOfBreak: durationOfBreak,
                    onBreak: (exception, duration) =>
                    {
                        _logger.LogWarning(
                            "Circuit breaker opened for {OperationName}. Duration: {Duration}. Exception: {Exception}",
                            operationName, duration, exception.Message);
                    },
                    onReset: () =>
                    {
                        _logger.LogInformation("Circuit breaker reset for {OperationName}", operationName);
                    },
                    onHalfOpen: () =>
                    {
                        _logger.LogInformation("Circuit breaker half-open for {OperationName}", operationName);
                    });

            _circuitBreakers[operationName] = policy;
            return policy;
        }

        /// <summary>
        /// Executes an operation with circuit breaker protection.
        /// </summary>
        public async Task<T> ExecuteAsync<T>(string operationName, Func<Task<T>> operation)
        {
            var policy = GetCircuitBreakerPolicy(operationName);
            return await policy.ExecuteAsync(operation);
        }

        /// <summary>
        /// Executes an operation with circuit breaker protection (void return).
        /// </summary>
        public async Task ExecuteAsync(string operationName, Func<Task> operation)
        {
            var policy = GetCircuitBreakerPolicy(operationName);
            await policy.ExecuteAsync(operation);
        }
    }

    /// <summary>
    /// Interface for circuit breaker service.
    /// </summary>
    public interface ICircuitBreakerService
    {
        AsyncCircuitBreakerPolicy GetCircuitBreakerPolicy(string operationName);
        Task<T> ExecuteAsync<T>(string operationName, Func<Task<T>> operation);
        Task ExecuteAsync(string operationName, Func<Task> operation);
    }
}

