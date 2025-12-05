using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using AmesaBackend.Lottery.DTOs;

namespace AmesaBackend.Lottery.Services
{
    public class TicketQueueProcessorService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TicketQueueProcessorService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IAmazonSQS _sqsClient;
        private readonly string? _queueUrl;

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
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var reservationProcessor = scope.ServiceProvider.GetRequiredService<IReservationProcessor>();

                        var reservationId = JsonSerializer.Deserialize<QueueMessage>(message.Body)?.ReservationId ?? Guid.Empty;
                        
                        if (reservationId == Guid.Empty)
                        {
                            _logger.LogWarning("Invalid message body: {Body}", message.Body);
                            await DeleteMessageAsync(message.ReceiptHandle, cancellationToken);
                            continue;
                        }

                        var result = await reservationProcessor.ProcessReservationAsync(reservationId, cancellationToken);

                        if (result.Success)
                        {
                            await DeleteMessageAsync(message.ReceiptHandle, cancellationToken);
                            _logger.LogInformation("Successfully processed reservation {ReservationId}", reservationId);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to process reservation {ReservationId}: {Error}", 
                                reservationId, result.ErrorMessage);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing message {MessageId}", message.MessageId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving messages from SQS queue");
            }
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








