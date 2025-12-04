using AmesaBackend.Shared.Events;
using AmesaBackend.LotteryResults.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AmesaBackend.LotteryResults.Handlers
{
    /// <summary>
    /// Background service to handle LotteryDrawWinnerSelectedEvent and create lottery results
    /// </summary>
    public class LotteryDrawWinnerEventHandler : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LotteryDrawWinnerEventHandler> _logger;
        private readonly IEventPublisher _eventPublisher;

        public LotteryDrawWinnerEventHandler(
            IServiceProvider serviceProvider,
            ILogger<LotteryDrawWinnerEventHandler> logger,
            IEventPublisher eventPublisher)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _eventPublisher = eventPublisher;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // This service listens to EventBridge events via AWS EventBridge rules
            // In production, EventBridge rules route LotteryDrawWinnerSelectedEvent to this service
            // For now, this is a placeholder that can be extended with actual EventBridge polling
            // or configured to receive events via HTTP endpoint (EventBridge target)
            
            _logger.LogInformation("Lottery Draw Winner Event Handler started");
            
            // In a real implementation, this would:
            // 1. Poll EventBridge for events matching LotteryDrawWinnerSelectedEvent
            // 2. Or receive events via HTTP endpoint configured as EventBridge target
            // 3. Or use AWS Lambda as intermediary to call this service
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Poll interval - in production, this would be replaced with actual event consumption
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Lottery Draw Winner Event Handler");
                }
            }
        }

        /// <summary>
        /// Handles LotteryDrawWinnerSelectedEvent and creates lottery result
        /// This method is called when EventBridge delivers the event to this service
        /// </summary>
        public async Task HandleLotteryDrawWinnerSelectedEvent(LotteryDrawWinnerSelectedEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var resultsService = scope.ServiceProvider.GetRequiredService<ILotteryResultsService>();
            
            try
            {
                // Convert WinningTicketNumber (int) to string format
                // The ticket number format is: {HouseId}-{Number:D6}
                var ticketNumberString = $"{@event.HouseId:N}-{@event.WinningTicketNumber:D6}";
                
                await resultsService.CreateResultFromWinnerEventAsync(
                    @event.DrawId,
                    @event.HouseId,
                    @event.WinnerTicketId,
                    @event.WinnerUserId,
                    ticketNumberString,
                    @event.HouseTitle,
                    @event.PrizeValue,
                    @event.PrizeDescription);
                
                _logger.LogInformation(
                    "Lottery result created for draw {DrawId}, winner {WinnerUserId}",
                    @event.DrawId, @event.WinnerUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error creating lottery result for draw {DrawId}, winner {WinnerUserId}",
                    @event.DrawId, @event.WinnerUserId);
            }
        }
    }
}

