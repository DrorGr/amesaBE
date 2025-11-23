using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AmesaBackend.Shared.Events
{
    /// <summary>
    /// EventBridge implementation of IEventPublisher
    /// </summary>
    public class EventBridgePublisher : IEventPublisher
    {
        private readonly IAmazonEventBridge _eventBridge;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EventBridgePublisher> _logger;
        private readonly string _eventBusName;
        private readonly string _source;

        public EventBridgePublisher(
            IAmazonEventBridge eventBridge,
            IConfiguration configuration,
            ILogger<EventBridgePublisher> logger)
        {
            _eventBridge = eventBridge;
            _configuration = configuration;
            _logger = logger;
            _eventBusName = configuration["EventBridge:EventBusName"] ?? "amesa-event-bus";
            _source = configuration["EventBridge:Source"] ?? "amesa.unknown-service";
        }

        public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : DomainEvent
        {
            try
            {
                @event.Source = _source;
                @event.DetailType = @event.GetType().Name;

                var request = new PutEventsRequest
                {
                    Entries = new List<PutEventsRequestEntry>
                    {
                        new PutEventsRequestEntry
                        {
                            Source = @event.Source,
                            DetailType = @event.DetailType,
                            Detail = JsonSerializer.Serialize(@event, new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            }),
                            EventBusName = _eventBusName
                        }
                    }
                };

                var response = await _eventBridge.PutEventsAsync(request, cancellationToken);

                if (response.FailedEntryCount > 0)
                {
                    _logger.LogError("Failed to publish event {EventType} to EventBridge. Failed entries: {FailedCount}",
                        @event.GetType().Name, response.FailedEntryCount);
                    
                    foreach (var entry in response.Entries)
                    {
                        if (!string.IsNullOrEmpty(entry.ErrorMessage))
                        {
                            _logger.LogError("EventBridge error: {ErrorMessage}, ErrorCode: {ErrorCode}",
                                entry.ErrorMessage, entry.ErrorCode);
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("Successfully published event {EventType} with ID {EventId}",
                        @event.GetType().Name, @event.EventId);
                }
            }
            catch (Amazon.EventBridge.AmazonEventBridgeException ex)
            {
                // EventBridge exceptions are non-fatal - don't break the application flow
                // This is especially important for OAuth flows where EventBridge failures should not prevent authentication
                _logger.LogWarning(ex, "EventBridge error publishing event {EventType} (non-fatal, continuing): {Message}", 
                    @event.GetType().Name, ex.Message);
                // Don't rethrow - EventBridge failures should not break the application
            }
            catch (Exception ex)
            {
                // For non-EventBridge exceptions, log and rethrow
                // These might be critical errors that need to propagate
                _logger.LogError(ex, "Unexpected error publishing event {EventType} to EventBridge", @event.GetType().Name);
                throw;
            }
        }
    }
}

