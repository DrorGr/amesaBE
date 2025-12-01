using Amazon.SQS;
using Amazon.SQS.Model;
using AmesaBackend.Notification.Data;
using AmesaBackend.Notification.Models;
using AmesaBackend.Notification.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace AmesaBackend.Notification.Services
{
    public class NotificationQueueProcessor : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NotificationQueueProcessor> _logger;
        private readonly IConfiguration _configuration;
        private readonly IAmazonSQS _sqsClient;
        private readonly string? _queueUrl;

        public NotificationQueueProcessor(
            IServiceProvider serviceProvider,
            ILogger<NotificationQueueProcessor> logger,
            IConfiguration configuration,
            IAmazonSQS? sqsClient = null)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
            _sqsClient = sqsClient ?? new AmazonSQSClient(Amazon.RegionEndpoint.GetBySystemName(
                _configuration["NotificationChannels:Email:Region"] ?? "eu-north-1"));
            
            // Get queue URL and trim BOM/whitespace that might be present in secrets
            var rawQueueUrl = _configuration["NotificationQueue:SqsQueueUrl"];
            if (string.IsNullOrEmpty(rawQueueUrl))
            {
                _queueUrl = null;
            }
            else
            {
                // Remove UTF-8 BOM and whitespace
                var cleaned = rawQueueUrl.Trim();
                // Remove UTF-8 BOM (single character U+FEFF)
                cleaned = cleaned.TrimStart('\uFEFF', '\u200B');
                // Remove UTF-8 BOM as three-character sequence (ï»¿ when interpreted as Latin-1)
                if (cleaned.Length >= 3 && cleaned[0] == '\u00EF' && cleaned[1] == '\u00BB' && cleaned[2] == '\u00BF')
                {
                    cleaned = cleaned.Substring(3);
                }
                _queueUrl = cleaned;
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NotificationQueueProcessor started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
                    var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();

                    // Process SQS queue if configured
                    if (!string.IsNullOrEmpty(_queueUrl))
                    {
                        await ProcessSqsQueueAsync(context, orchestrator, stoppingToken);
                    }

                    // Process database queue (fallback or primary)
                    await ProcessDatabaseQueueAsync(context, orchestrator, stoppingToken);

                    // Wait before next poll
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in NotificationQueueProcessor");
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }

            _logger.LogInformation("NotificationQueueProcessor stopped");
        }

        private async Task ProcessSqsQueueAsync(
            NotificationDbContext context,
            INotificationOrchestrator orchestrator,
            CancellationToken cancellationToken)
        {
            try
            {
                var receiveRequest = new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 5, // Long polling
                    VisibilityTimeout = 60 // Give 60 seconds to process
                };

                var response = await _sqsClient.ReceiveMessageAsync(receiveRequest, cancellationToken);

                foreach (var message in response.Messages)
                {
                    try
                    {
                        // Parse notification from SQS message
                        var notificationData = JsonSerializer.Deserialize<Dictionary<string, object>>(message.Body);
                        if (notificationData == null)
                        {
                            _logger.LogWarning("Invalid notification data in SQS message: {MessageId}", message.MessageId);
                            await DeleteSqsMessageAsync(message.ReceiptHandle, cancellationToken);
                            continue;
                        }

                        // Extract notification details
                        var userId = Guid.Parse(notificationData["UserId"]?.ToString() ?? "");
                        var channels = JsonSerializer.Deserialize<List<string>>(notificationData["Channels"]?.ToString() ?? "[]") ?? new List<string>();
                        var notificationRequest = new NotificationRequest
                        {
                            UserId = userId,
                            Type = notificationData["Type"]?.ToString() ?? "",
                            Title = notificationData["Title"]?.ToString() ?? "",
                            Message = notificationData["Message"]?.ToString() ?? "",
                            Language = notificationData["Language"]?.ToString() ?? "en",
                            TemplateName = notificationData["TemplateName"]?.ToString(),
                            TemplateVariables = notificationData.ContainsKey("TemplateVariables")
                                ? JsonSerializer.Deserialize<Dictionary<string, object>>(notificationData["TemplateVariables"]?.ToString() ?? "{}")
                                : null,
                            Data = notificationData.ContainsKey("Data")
                                ? JsonSerializer.Deserialize<Dictionary<string, object>>(notificationData["Data"]?.ToString() ?? "{}")
                                : null
                        };

                        // Process notification
                        var result = await orchestrator.SendMultiChannelAsync(userId, notificationRequest, channels);

                        if (result.SuccessCount > 0)
                        {
                            _logger.LogInformation("Successfully processed notification {NotificationId} from SQS", result.NotificationId);
                            await DeleteSqsMessageAsync(message.ReceiptHandle, cancellationToken);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to process notification from SQS message: {MessageId}", message.MessageId);
                            // Message will become visible again after visibility timeout for retry
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing SQS message {MessageId}", message.MessageId);
                        // Don't delete message on error - let it retry
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing SQS queue");
            }
        }

        private async Task ProcessDatabaseQueueAsync(
            NotificationDbContext context,
            INotificationOrchestrator orchestrator,
            CancellationToken cancellationToken)
        {
            var pendingItems = await context.NotificationQueue
                .Include(q => q.Notification)
                .Where(q => q.Status == "pending" && 
                           (q.ScheduledFor == null || q.ScheduledFor <= DateTime.UtcNow))
                .OrderByDescending(q => q.Priority)
                .ThenBy(q => q.CreatedAt)
                .Take(10)
                .ToListAsync(cancellationToken);

            foreach (var item in pendingItems)
            {
                try
                {
                    item.Status = "processing";
                    item.ProcessedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync(cancellationToken);

                    if (item.Notification == null)
                    {
                        _logger.LogWarning("Queue item {QueueId} has no associated notification", item.Id);
                        item.Status = "failed";
                        item.ErrorMessage = "Notification not found";
                        await context.SaveChangesAsync(cancellationToken);
                        continue;
                    }

                    // Create notification request from queue item
                    var notificationRequest = new NotificationRequest
                    {
                        UserId = item.Notification.UserId,
                        Channel = item.Channel,
                        Type = item.Notification.Type,
                        Title = item.Notification.Title,
                        Message = item.Notification.Message,
                        Language = "en", // Default, can be enhanced
                        Data = item.Notification.Data != null
                            ? JsonSerializer.Deserialize<Dictionary<string, object>>(item.Notification.Data)
                            : null
                    };

                    // Process notification via orchestrator
                    var result = await orchestrator.SendMultiChannelAsync(
                        item.Notification.UserId,
                        notificationRequest,
                        new List<string> { item.Channel });

                    if (result.SuccessCount > 0)
                    {
                        item.Status = "completed";
                        _logger.LogInformation("Successfully processed queue item {QueueId}", item.Id);
                    }
                    else
                    {
                        item.Status = "failed";
                        item.ErrorMessage = "All delivery attempts failed";
                        item.RetryCount++;
                    }

                    await context.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing queue item {QueueId}", item.Id);
                    item.Status = "failed";
                    item.ErrorMessage = ex.Message;
                    item.RetryCount++;

                    if (item.RetryCount >= item.MaxRetries)
                    {
                        item.Status = "failed";
                        _logger.LogWarning("Queue item {QueueId} exceeded max retries", item.Id);
                    }

                    await context.SaveChangesAsync(cancellationToken);
                }
            }
        }

        private async Task DeleteSqsMessageAsync(string receiptHandle, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(_queueUrl))
                    return;

                var deleteRequest = new DeleteMessageRequest
                {
                    QueueUrl = _queueUrl,
                    ReceiptHandle = receiptHandle
                };

                await _sqsClient.DeleteMessageAsync(deleteRequest, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting SQS message");
            }
        }
    }
}

