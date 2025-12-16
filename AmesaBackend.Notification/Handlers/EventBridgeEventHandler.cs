using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using AmesaBackend.Shared.Events;
using AmesaBackend.Notification.Services;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

namespace AmesaBackend.Notification.Handlers
{
    /// <summary>
    /// Background service to consume EventBridge events
    /// </summary>
    public class EventBridgeEventHandler : BackgroundService
    {
        private readonly IAmazonEventBridge _eventBridge;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EventBridgeEventHandler> _logger;
        private readonly string _eventBusName;

        public EventBridgeEventHandler(
            IAmazonEventBridge eventBridge,
            IConfiguration configuration,
            IServiceProvider serviceProvider,
            ILogger<EventBridgeEventHandler> logger)
        {
            _eventBridge = eventBridge;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _eventBusName = configuration["EventBridge:EventBusName"] ?? "amesa-event-bus";
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // NOTE: This background service is a placeholder.
            // Events are actually consumed via the EventBridgeController webhook endpoint (/api/v1/events/webhook).
            // EventBridge is configured to send events to this webhook endpoint via API destination or custom integration.
            // 
            // If you want to use SQS queue consumption instead, you would:
            // 1. Configure EventBridge rules to send events to an SQS queue
            // 2. Implement SQS queue polling here
            // 3. Process messages from the queue
            //
            // For now, this service just logs that it's running but doesn't actively consume events.
            // All event consumption happens via the webhook endpoint.
            
            _logger.LogInformation(
                "EventBridgeEventHandler background service started. " +
                "Note: Events are consumed via EventBridgeController webhook endpoint at /api/v1/events/webhook. " +
                "This background service is kept for potential future SQS queue consumption.");
            
            // Keep service running but don't actively poll
            // Events come via webhook endpoint
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Just keep the service alive - actual event consumption via webhook
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    _logger.LogDebug("EventBridgeEventHandler background service heartbeat");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in EventBridge event handler background service");
                }
            }
        }

        public async Task HandleUserCreatedEvent(UserCreatedEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            
            try
            {
                await emailService.SendWelcomeEmailAsync(@event.Email, @event.FirstName ?? "");
                _logger.LogInformation("Welcome email sent to {Email}", @event.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending welcome email to {Email}", @event.Email);
            }
        }

        public async Task HandleLotteryDrawWinnerSelectedEvent(LotteryDrawWinnerSelectedEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            
            try
            {
                await notificationService.SendLotteryWinnerNotificationAsync(
                    @event.WinnerUserId,
                    "House", // Would need to get house title from Lottery Service
                    @event.WinningTicketNumber.ToString());
                
                _logger.LogInformation("Winner notification sent to user {UserId}", @event.WinnerUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending winner notification to user {UserId}", @event.WinnerUserId);
            }
        }

        public async Task HandleEmailVerificationRequestedEvent(EmailVerificationRequestedEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            
            try
            {
                await emailService.SendEmailVerificationAsync(@event.Email, @event.VerificationToken);
                _logger.LogInformation("Verification email sent to {Email}", @event.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending verification email to {Email}", @event.Email);
            }
        }

        public async Task HandlePasswordResetRequestedEvent(PasswordResetRequestedEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            
            try
            {
                await emailService.SendPasswordResetEmailAsync(@event.Email, @event.ResetToken);
                _logger.LogInformation("Password reset email sent to {Email}", @event.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset email to {Email}", @event.Email);
            }
        }

        public async Task HandleUserEmailVerifiedEvent(UserEmailVerifiedEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            
            try
            {
                await emailService.SendWelcomeEmailAsync(@event.Email, @event.FirstName ?? "");
                _logger.LogInformation("Welcome email sent to verified user {Email}", @event.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending welcome email to {Email}", @event.Email);
            }
        }

        public async Task HandlePaymentCompletedEvent(PaymentCompletedEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            
            try
            {
                await notificationService.SendNotificationAsync(
                    @event.UserId,
                    "Payment Successful",
                    $"Your payment of ${@event.Amount:F2} has been processed successfully.",
                    "payment_success");
                
                _logger.LogInformation("Payment success notification sent to user {UserId}", @event.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending payment success notification to user {UserId}", @event.UserId);
            }
        }

        public async Task HandleTicketPurchasedEvent(TicketPurchasedEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            
            try
            {
                var ticketText = @event.TicketCount == 1 ? "ticket" : "tickets";
                var message = $"You've successfully purchased {@event.TicketCount} {ticketText} for the lottery!";
                
                if (@event.TicketNumbers != null && @event.TicketNumbers.Any())
                {
                    var ticketNumbers = string.Join(", ", @event.TicketNumbers.Take(5));
                    if (@event.TicketNumbers.Count > 5)
                    {
                        ticketNumbers += $" and {@event.TicketNumbers.Count - 5} more";
                    }
                    message += $" Your ticket numbers: {ticketNumbers}";
                }
                
                await notificationService.SendNotificationAsync(
                    @event.UserId,
                    "Tickets Purchased",
                    message,
                    "ticket_purchased");
                
                _logger.LogInformation("Ticket purchase notification sent to user {UserId} for {TicketCount} tickets", 
                    @event.UserId, @event.TicketCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending ticket purchase notification to user {UserId}", @event.UserId);
            }
        }

        public async Task HandlePaymentFailedEvent(PaymentFailedEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            
            try
            {
                await notificationService.SendNotificationAsync(
                    @event.UserId,
                    "Payment Failed",
                    $"Your payment of ${@event.Amount:F2} could not be processed. Please try again or contact support.",
                    "payment_failed");
                
                _logger.LogInformation("Payment failure notification sent to user {UserId}", @event.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending payment failure notification to user {UserId}", @event.UserId);
            }
        }
    }
}

