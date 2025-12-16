using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using AmesaBackend.Lottery.DTOs;
using StackExchange.Redis;

namespace AmesaBackend.Lottery.Services
{
    public class TicketQueueProcessorService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TicketQueueProcessorService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IAmazonSQS _sqsClient;
        private readonly string? _queueUrl;
        private const int MaxRetries = 3;
        private const int RetryTrackingTTLHours = 24; // Keep retry count for 24 hours
        
        // Cached Redis instance for efficiency (lazy initialization, thread-safe)
        private IConnectionMultiplexer? _redis;
        private readonly object _redisLock = new object();

        public TicketQueueProcessorService(
            IServiceProvider serviceProvider,
            ILogger<TicketQueueProcessorService> logger,
            IConfiguration configuration,
            IAmazonSQS? sqsClient = null)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
            _sqsClient = sqsClient ?? new AmazonSQSClient(Amazon.RegionEndpoint.GetBySystemName("eu-north-1"));
            
            var rawQueueUrl = _configuration["SQS:LotteryTicketQueueUrl"];
            _queueUrl = rawQueueUrl?.Trim().TrimStart('\uFEFF', '\u200B');
            
            if (!string.IsNullOrEmpty(_queueUrl) && _queueUrl.Length >= 3 && 
                _queueUrl[0] == '\u00EF' && _queueUrl[1] == '\u00BB' && _queueUrl[2] == '\u00BF')
            {
                _queueUrl = _queueUrl.Substring(3);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (string.IsNullOrEmpty(_queueUrl))
            {
                _logger.LogWarning("SQS queue URL not configured. Queue processor will not run.");
                return;
            }

            _logger.LogInformation("TicketQueueProcessorService started. Queue: {QueueUrl}", _queueUrl);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessQueueAsync(stoppingToken);
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in TicketQueueProcessorService");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }

            _logger.LogInformation("TicketQueueProcessorService stopped");
        }

        private async Task ProcessQueueAsync(CancellationToken cancellationToken)
        {
            try
            {
                var receiveRequest = new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = 1,
                    WaitTimeSeconds = 20,
                    VisibilityTimeout = 60,
                    MessageAttributeNames = new List<string> { "All" }
                };

                var response = await _sqsClient.ReceiveMessageAsync(receiveRequest, cancellationToken);

                foreach (var message in response.Messages)
                {
                    Guid reservationId = Guid.Empty;
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var reservationProcessor = scope.ServiceProvider.GetRequiredService<IReservationProcessor>();

                        reservationId = JsonSerializer.Deserialize<QueueMessage>(message.Body)?.ReservationId ?? Guid.Empty;
                        
                        if (reservationId == Guid.Empty)
                        {
                            _logger.LogWarning("Invalid message body: {Body}", message.Body);
                            await DeleteMessageAsync(message.ReceiptHandle, cancellationToken);
                            continue;
                        }

                        // Get retry count from Redis (or default to 0)
                        var retryCount = await GetRetryCountAsync(reservationId);

                        if (retryCount >= MaxRetries)
                        {
                            _logger.LogError(
                                "Reservation {ReservationId} exceeded max retries ({MaxRetries}), moving to dead letter handling. Manual intervention required.",
                                reservationId, MaxRetries);
                            
                            // Log for manual review (DLQ equivalent)
                            await LogDeadLetterMessageAsync(reservationId, "Max retries exceeded");
                            
                            // Delete message to prevent infinite retry loop
                            await DeleteMessageAsync(message.ReceiptHandle, cancellationToken);
                            
                            // Clear retry count from Redis
                            await ClearRetryCountAsync(reservationId);
                            continue;
                        }

                        var result = await reservationProcessor.ProcessReservationAsync(reservationId, cancellationToken);

                        if (result.Success)
                        {
                            await DeleteMessageAsync(message.ReceiptHandle, cancellationToken);
                            await ClearRetryCountAsync(reservationId); // Clear retry count on success
                            _logger.LogInformation("Successfully processed reservation {ReservationId}", reservationId);
                        }
                        else
                        {
                            // Increment retry count in Redis
                            var newRetryCount = await IncrementRetryCountAsync(reservationId);
                            
                            _logger.LogWarning(
                                "Failed to process reservation {ReservationId}: {Error} (Retry {RetryCount}/{MaxRetries})", 
                                reservationId, result.ErrorMessage, newRetryCount, MaxRetries);
                            
                            // Don't delete message - let it become visible again after visibility timeout
                            // This allows for automatic retries
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing message {MessageId}", message.MessageId);
                        
                        // Only track retries if we have a valid reservation ID
                        if (reservationId != Guid.Empty)
                        {
                            // Increment retry count on exception
                            var retryCount = await IncrementRetryCountAsync(reservationId);
                            
                            if (retryCount >= MaxRetries)
                            {
                                _logger.LogError(
                                    "Reservation {ReservationId} exceeded max retries ({MaxRetries}) due to exception. Moving to dead letter handling.",
                                    reservationId, MaxRetries);
                                
                                await LogDeadLetterMessageAsync(reservationId, $"Exception: {ex.Message}");
                                await DeleteMessageAsync(message.ReceiptHandle, cancellationToken);
                                await ClearRetryCountAsync(reservationId);
                            }
                            // Otherwise, don't delete message - let it become visible again for retry
                        }
                        else
                        {
                            // Invalid message - delete it to prevent infinite retries
                            _logger.LogWarning("Invalid message body, deleting message {MessageId}", message.MessageId);
                            await DeleteMessageAsync(message.ReceiptHandle, cancellationToken);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving messages from SQS queue");
            }
        }

        private IConnectionMultiplexer? GetRedis()
        {
            // Return cached instance if available (double-check locking pattern)
            if (_redis != null)
            {
                return _redis;
            }
            
            lock (_redisLock)
            {
                // Double-check after acquiring lock
                if (_redis != null)
                {
                    return _redis;
                }
                
                try
                {
                    // Get Redis from service provider (registered as Singleton by AddAmesaBackendShared)
                    // No need for scope since it's a Singleton - get it directly from root service provider
                    _redis = _serviceProvider.GetService<IConnectionMultiplexer>();
                    return _redis;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Redis not available for retry tracking");
                    return null;
                }
            }
        }

        private async Task<int> GetRetryCountAsync(Guid reservationId)
        {
            var redis = GetRedis();
            if (redis == null)
            {
                return 0; // No Redis, assume first attempt
            }

            try
            {
                var db = redis.GetDatabase();
                var key = $"sqs:retry:{reservationId}";
                var value = await db.StringGetAsync(key);
                
                if (value.HasValue && int.TryParse(value, out var count))
                {
                    return count;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting retry count for reservation {ReservationId}", reservationId);
            }
            
            return 0;
        }

        private async Task<int> IncrementRetryCountAsync(Guid reservationId)
        {
            var redis = GetRedis();
            if (redis == null)
            {
                return 1; // No Redis, return 1 as first retry
            }

            try
            {
                var db = redis.GetDatabase();
                var key = $"sqs:retry:{reservationId}";
                var newCount = await db.StringIncrementAsync(key);
                await db.KeyExpireAsync(key, TimeSpan.FromHours(RetryTrackingTTLHours));
                return (int)newCount;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error incrementing retry count for reservation {ReservationId}", reservationId);
                return 1; // Fallback to 1
            }
        }

        private async Task ClearRetryCountAsync(Guid reservationId)
        {
            var redis = GetRedis();
            if (redis == null)
            {
                return;
            }

            try
            {
                var db = redis.GetDatabase();
                var key = $"sqs:retry:{reservationId}";
                await db.KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error clearing retry count for reservation {ReservationId}", reservationId);
            }
        }

        private async Task LogDeadLetterMessageAsync(Guid reservationId, string errorMessage)
        {
            // Log for manual review (DLQ equivalent)
            // In production, you could send to CloudWatch Logs, SNS topic, or a dedicated DLQ table
            _logger.LogError(
                "DEAD LETTER: Reservation {ReservationId} failed after {MaxRetries} retries. " +
                "Error: {Error}. Timestamp: {Timestamp}. Manual intervention required.",
                reservationId, MaxRetries, errorMessage, DateTime.UtcNow);
            
            // Optionally: Store in database for manual review
            // await _deadLetterService.LogFailedReservationAsync(reservationId, errorMessage);
        }

        private async Task DeleteMessageAsync(string receiptHandle, CancellationToken cancellationToken)
        {
            try
            {
                await _sqsClient.DeleteMessageAsync(new DeleteMessageRequest
                {
                    QueueUrl = _queueUrl,
                    ReceiptHandle = receiptHandle
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message from queue");
            }
        }
    }

    public class QueueMessage
    {
        public Guid ReservationId { get; set; }
    }
}












