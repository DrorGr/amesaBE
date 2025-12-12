using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AmesaBackend.Admin.Services
{
    public interface ICloudWatchLoggingService
    {
        Task LogAsync(string message, LogLevel level = LogLevel.Information);
        Task LogErrorAsync(string message, Exception? exception = null);
        Task LogMetricAsync(string metricName, double value, Dictionary<string, string>? dimensions = null);
    }

    public class CloudWatchLoggingService : ICloudWatchLoggingService
    {
        private readonly IAmazonCloudWatchLogs _cloudWatchLogs;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CloudWatchLoggingService> _logger;
        private readonly string _logGroupName;
        private readonly string _logStreamName;
        private string? _sequenceToken;

        public CloudWatchLoggingService(
            IAmazonCloudWatchLogs cloudWatchLogs,
            IConfiguration configuration,
            ILogger<CloudWatchLoggingService> logger)
        {
            _cloudWatchLogs = cloudWatchLogs;
            _configuration = configuration;
            _logger = logger;
            _logGroupName = _configuration["AWS:CloudWatch:LogGroup"] ?? "/ecs/amesa-admin-service";
            _logStreamName = _configuration["AWS:CloudWatch:LogStream"] ?? $"admin-service-{Environment.MachineName}";
            
            // Initialize log group and stream
            _ = InitializeLogGroupAsync();
        }

        private async Task InitializeLogGroupAsync()
        {
            try
            {
                // Check if log group exists
                try
                {
                    await _cloudWatchLogs.DescribeLogGroupsAsync(new DescribeLogGroupsRequest
                    {
                        LogGroupNamePrefix = _logGroupName
                    });
                }
                catch
                {
                    // Log group doesn't exist, create it
                    await _cloudWatchLogs.CreateLogGroupAsync(new CreateLogGroupRequest
                    {
                        LogGroupName = _logGroupName
                    });
                }

                // Check if log stream exists
                try
                {
                    await _cloudWatchLogs.DescribeLogStreamsAsync(new DescribeLogStreamsRequest
                    {
                        LogGroupName = _logGroupName,
                        LogStreamNamePrefix = _logStreamName
                    });
                }
                catch
                {
                    // Log stream doesn't exist, create it
                    await _cloudWatchLogs.CreateLogStreamAsync(new CreateLogStreamRequest
                    {
                        LogGroupName = _logGroupName,
                        LogStreamName = _logStreamName
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize CloudWatch log group/stream. Logging will continue to console.");
            }
        }

        public async Task LogAsync(string message, LogLevel level = LogLevel.Information)
        {
            try
            {
                var logEvent = new InputLogEvent
                {
                    Message = $"[{level}] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - {message}",
                    Timestamp = DateTime.UtcNow
                };

                var request = new PutLogEventsRequest
                {
                    LogGroupName = _logGroupName,
                    LogStreamName = _logStreamName,
                    LogEvents = new List<InputLogEvent> { logEvent }
                };

                if (!string.IsNullOrEmpty(_sequenceToken))
                {
                    request.SequenceToken = _sequenceToken;
                }

                var response = await _cloudWatchLogs.PutLogEventsAsync(request);
                _sequenceToken = response.NextSequenceToken;
            }
            catch (Exception ex)
            {
                // Fallback to console logging if CloudWatch fails
                _logger.LogWarning(ex, "Failed to log to CloudWatch: {Message}", message);
            }
        }

        public async Task LogErrorAsync(string message, Exception? exception = null)
        {
            var errorMessage = exception != null
                ? $"{message}\nException: {exception.GetType().Name}\nMessage: {exception.Message}\nStack Trace: {exception.StackTrace}"
                : message;

            await LogAsync(errorMessage, LogLevel.Error);
        }

        public async Task LogMetricAsync(string metricName, double value, Dictionary<string, string>? dimensions = null)
        {
            // CloudWatch Logs doesn't support metrics directly
            // This would need to use CloudWatch Metrics API (separate service)
            // For now, log as structured log entry
            var metricMessage = $"METRIC: {metricName} = {value}";
            if (dimensions != null && dimensions.Any())
            {
                metricMessage += $" | Dimensions: {string.Join(", ", dimensions.Select(d => $"{d.Key}={d.Value}"))}";
            }

            await LogAsync(metricMessage, LogLevel.Information);
        }
    }
}

