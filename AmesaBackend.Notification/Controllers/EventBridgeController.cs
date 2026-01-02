using Microsoft.AspNetCore.Mvc;
using AmesaBackend.Notification.Handlers;
using AmesaBackend.Notification.Services;
using AmesaBackend.Notification.Services.Interfaces;
using AmesaBackend.Notification.Constants;
using AmesaBackend.Notification.DTOs;
using AmesaBackend.Shared.Events;
using AmesaBackend.Shared.Rest;
using AmesaBackend.Shared.Caching;
using AmesaBackend.Auth.Services;
using AmesaBackend.Auth.Services.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace AmesaBackend.Notification.Controllers
{
    /// <summary>
    /// HTTP webhook endpoint for receiving EventBridge events
    /// This replaces the placeholder polling mechanism
    /// </summary>
    [ApiController]
    [Route("api/v1/events")]
    public class EventBridgeController : ControllerBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EventBridgeController> _logger;
        private readonly IConfiguration _configuration;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly IRateLimitService? _rateLimitService;
        private readonly INotificationTypeMappingService? _typeMappingService;
        private readonly ICache? _cache;

        public EventBridgeController(
            IServiceProvider serviceProvider,
            ILogger<EventBridgeController> logger,
            IConfiguration configuration,
            IRateLimitService? rateLimitService = null,
            INotificationTypeMappingService? typeMappingService = null,
            ICache? cache = null)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
            _rateLimitService = rateLimitService;
            _typeMappingService = typeMappingService;
            _cache = cache;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        /// <summary>
        /// Webhook endpoint for EventBridge events
        /// EventBridge will POST events to this endpoint via API destination or custom integration
        /// </summary>
        [HttpPost("webhook")]
        public async Task<IActionResult> ReceiveEvent([FromBody] EventBridgeWebhookRequest? request)
        {
            try
            {
                // Validate request is not null
                if (request == null)
                {
                    _logger.LogWarning("Received null EventBridge webhook request");
                    return BadRequest(new { error = "Request body is required" });
                }

                // Validate event structure
                if (string.IsNullOrEmpty(request.DetailType) || string.IsNullOrEmpty(request.Source))
                {
                    _logger.LogWarning("Invalid event structure: missing DetailType or Source");
                    return BadRequest(new { error = "Invalid event structure: DetailType and Source are required" });
                }

                // Validate Detail is not null
                if (request.Detail == null)
                {
                    _logger.LogWarning("Invalid event structure: Detail is null for event {DetailType} from {Source}", 
                        request.DetailType, request.Source);
                    return BadRequest(new { error = "Invalid event structure: Detail is required" });
                }

                // Validate event source against allowlist
                var allowedSources = _configuration.GetSection("EventBridge:AllowedSources")
                    .Get<string[]>() ?? new[] { "amesa.auth", "amesa.lottery", "amesa.payment", "amesa.content", "amesa.notification-service" };
                
                if (!allowedSources.Contains(request.Source, StringComparer.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Event from untrusted source: {Source} for event {DetailType}", 
                        request.Source, request.DetailType);
                    return BadRequest(new { error = "Untrusted event source" });
                }

                // Rate limiting: Limit webhook requests to prevent DoS
                if (_rateLimitService != null)
                {
                    var rateLimitKey = $"eventbridge_webhook:{request.Source}";
                    var rateLimitAllowed = await _rateLimitService.CheckRateLimitAsync(
                        rateLimitKey, 
                        limit: _configuration.GetValue<int>("EventBridge:RateLimit:MaxRequests", 100),
                        window: TimeSpan.FromMinutes(_configuration.GetValue<int>("EventBridge:RateLimit:WindowMinutes", 1)));
                    
                    if (!rateLimitAllowed)
                    {
                        _logger.LogWarning("Rate limit exceeded for EventBridge webhook from source {Source}", request.Source);
                        return StatusCode(429, new { error = "Rate limit exceeded" });
                    }
                    
                    // Increment rate limit counter
                    await _rateLimitService.IncrementRateLimitAsync(
                        rateLimitKey,
                        TimeSpan.FromMinutes(_configuration.GetValue<int>("EventBridge:RateLimit:WindowMinutes", 1)));
                }

                // Check idempotency - prevent duplicate processing
                var eventId = request.Id ?? Guid.NewGuid().ToString(); // EventBridge provides unique event ID
                var idempotencyKey = $"eventbridge_processed:{eventId}";
                
                try
                {
                    if (_cache != null)
                    {
                        var alreadyProcessed = await _cache.GetRecordAsync<bool>(idempotencyKey);
                        if (alreadyProcessed == true)
                        {
                            _logger.LogInformation("Event {EventId} already processed, skipping duplicate", eventId);
                            return Ok(new { success = true, message = "Event already processed" });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to check idempotency for event {EventId}, proceeding anyway", eventId);
                    // Continue - idempotency check failure shouldn't block processing
                }
                
                _logger.LogInformation("Received EventBridge event: {DetailType} from {Source} (ID: {EventId})", 
                    request.DetailType, request.Source, eventId);

                // Check retry count - prevent infinite retries
                const int maxRetries = 3; // Configurable via EventBridge:MaxRetries
                var retryKey = $"eventbridge_retry:{eventId}";
                int retryCount = 0;
                
                try
                {
                    if (_cache != null)
                    {
                        var cachedRetryCount = await _cache.GetRecordAsync<int?>(retryKey);
                        retryCount = cachedRetryCount ?? 0;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get retry count for event {EventId}", eventId);
                }
                
                if (retryCount >= maxRetries)
                {
                    _logger.LogWarning("Event {EventId} exceeded max retries ({MaxRetries}), marking as processed to prevent further retries", 
                        eventId, maxRetries);
                    
                    // Mark as processed to prevent further retries
                    try
                    {
                        if (_cache != null)
                        {
                            await _cache.SetRecordAsync(idempotencyKey, true, TimeSpan.FromHours(24));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to mark event {EventId} as processed", eventId);
                    }
                    
                    // Return 200 to stop EventBridge retries
                    return Ok(new { success = true, message = "Event processed (max retries reached)" });
                }
                
                // Route event to appropriate handler
                var handled = await RouteEventAsync(request);
                
                // Mark event as processed (24 hour TTL - longer than EventBridge retry window)
                try
                {
                    if (_cache != null)
                    {
                        await _cache.SetRecordAsync(idempotencyKey, true, TimeSpan.FromHours(24));
                        // Clear retry count on success
                        await _cache.RemoveRecordAsync(retryKey);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to mark event {EventId} as processed", eventId);
                    // Non-critical - continue
                }
                
                if (!handled)
                {
                    _logger.LogWarning("Event not handled: {DetailType} from {Source}", 
                        request.DetailType, request.Source);
                    // Return 200 anyway - we don't want EventBridge to retry unhandled events
                }

                return Ok(new { success = true, message = "Event received" });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON deserialization error processing EventBridge event");
                return BadRequest(new { error = "Invalid JSON payload" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing EventBridge event");
                
                // Increment retry count
                try
                {
                    var eventId = request?.Id ?? Guid.NewGuid().ToString();
                    var retryKey = $"eventbridge_retry:{eventId}";
                    if (_cache != null)
                    {
                        var currentRetryCount = await _cache.GetRecordAsync<int?>(retryKey) ?? 0;
                        await _cache.SetRecordAsync(retryKey, currentRetryCount + 1, TimeSpan.FromHours(24));
                    }
                }
                catch (Exception retryEx)
                {
                    _logger.LogWarning(retryEx, "Failed to increment retry count");
                }
                
                // Return 500 to trigger EventBridge retry for transient errors
                // EventBridge will retry up to its configured retry policy
                return StatusCode(500, new { error = "Internal server error processing event" });
            }
        }

        /// <summary>
        /// Route EventBridge event to appropriate handler based on DetailType
        /// </summary>
        /// <summary>
        /// Validates that a UserId is not empty
        /// </summary>
        private bool ValidateUserId(Guid userId, string eventType)
        {
            if (userId == Guid.Empty)
            {
                _logger.LogWarning("Invalid UserId (Guid.Empty) in event {EventType}", eventType);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Safely deserializes JSON with error handling
        /// </summary>
        private T? SafeDeserialize<T>(string json, string eventType) where T : class
        {
            try
            {
                return JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON deserialization error for event {EventType}. JSON: {Json}", eventType, json);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deserializing event {EventType}", eventType);
                return null;
            }
        }

        /// <summary>
        /// Processes bulk notifications in batches to prevent memory issues and timeouts
        /// </summary>
        private async Task ProcessBulkNotificationsAsync(
            List<Guid> userIds,
            Func<Guid, Task> notificationAction,
            int batchSize = 100,
            int maxConcurrency = 10)
        {
            if (userIds == null || userIds.Count == 0)
            {
                return;
            }

            // Limit total users to prevent memory issues
            const int maxUsers = 10000;
            if (userIds.Count > maxUsers)
            {
                _logger.LogWarning("User list exceeds max limit ({MaxUsers}). Processing first {MaxUsers} users.", 
                    maxUsers, maxUsers);
                userIds = userIds.Take(maxUsers).ToList();
            }

            using var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            var tasks = new List<Task>();

            // Process in batches
            for (int i = 0; i < userIds.Count; i += batchSize)
            {
                var batch = userIds.Skip(i).Take(batchSize).ToList();
                
                foreach (var userId in batch)
                {
                    await semaphore.WaitAsync();
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            await notificationAction(userId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing notification for user {UserId}", userId);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }));
                }

                // Wait for current batch to complete before starting next batch
                await Task.WhenAll(tasks);
                tasks.Clear();
            }
        }

        private async Task<bool> RouteEventAsync(EventBridgeWebhookRequest request)
        {
            try
            {
                // Deserialize detail based on DetailType
                var detailJson = JsonSerializer.Serialize(request.Detail);
                var detailType = request.DetailType;

                // Route to appropriate handler based on event type
                switch (detailType)
                {
                    // Authentication Events
                    case "UserCreated":
                        var userCreated = SafeDeserialize<UserCreatedEvent>(detailJson, detailType);
                        if (userCreated != null)
                        {
                            await HandleUserCreatedEvent(userCreated);
                            return true;
                        }
                        break;

                    case "EmailVerificationRequested":
                        var emailVerification = SafeDeserialize<EmailVerificationRequestedEvent>(detailJson, detailType);
                        if (emailVerification != null)
                        {
                            await HandleEmailVerificationRequestedEvent(emailVerification);
                            return true;
                        }
                        break;

                    case "PasswordResetRequested":
                        var passwordReset = SafeDeserialize<PasswordResetRequestedEvent>(detailJson, detailType);
                        if (passwordReset != null)
                        {
                            await HandlePasswordResetRequestedEvent(passwordReset);
                            return true;
                        }
                        break;

                    case "UserEmailVerified":
                        var emailVerified = SafeDeserialize<UserEmailVerifiedEvent>(detailJson, detailType);
                        if (emailVerified != null)
                        {
                            await HandleUserEmailVerifiedEvent(emailVerified);
                            return true;
                        }
                        break;

                    // Lottery Events
                    case "LotteryDrawWinnerSelected":
                        var winnerSelected = SafeDeserialize<LotteryDrawWinnerSelectedEvent>(detailJson, detailType);
                        if (winnerSelected != null)
                        {
                            await HandleLotteryDrawWinnerSelectedEvent(winnerSelected);
                            return true;
                        }
                        break;

                    case "TicketPurchased":
                        var ticketPurchased = SafeDeserialize<TicketPurchasedEvent>(detailJson, detailType);
                        if (ticketPurchased != null)
                        {
                            await HandleTicketPurchasedEvent(ticketPurchased);
                            return true;
                        }
                        break;

                    // Payment Events
                    case "PaymentCompleted":
                        var paymentCompleted = SafeDeserialize<PaymentCompletedEvent>(detailJson, detailType);
                        if (paymentCompleted != null)
                        {
                            await HandlePaymentCompletedEvent(paymentCompleted);
                            return true;
                        }
                        break;

                    case "PaymentFailed":
                        var paymentFailed = SafeDeserialize<PaymentFailedEvent>(detailJson, detailType);
                        if (paymentFailed != null)
                        {
                            await HandlePaymentFailedEvent(paymentFailed);
                            return true;
                        }
                        break;

                    // Authentication/Security Events
                    case "UserLogin":
                        var userLogin = SafeDeserialize<UserLoginEvent>(detailJson, detailType);
                        if (userLogin != null)
                        {
                            await HandleUserLoginEvent(userLogin);
                            return true;
                        }
                        break;

                    // Lottery Events
                    case "LotteryDrawCompleted":
                        var drawCompleted = SafeDeserialize<LotteryDrawCompletedEvent>(detailJson, detailType);
                        if (drawCompleted != null)
                        {
                            await HandleLotteryDrawCompletedEvent(drawCompleted);
                            return true;
                        }
                        break;

                    case "HouseCreated":
                        var houseCreated = SafeDeserialize<HouseCreatedEvent>(detailJson, detailType);
                        if (houseCreated != null)
                        {
                            await HandleHouseCreatedEvent(houseCreated);
                            return true;
                        }
                        break;

                    case "HouseUpdated":
                        var houseUpdated = SafeDeserialize<HouseUpdatedEvent>(detailJson, detailType);
                        if (houseUpdated != null)
                        {
                            await HandleHouseUpdatedEvent(houseUpdated);
                            return true;
                        }
                        break;

                    // Payment Events
                    case "PaymentInitiated":
                        var paymentInitiated = SafeDeserialize<PaymentInitiatedEvent>(detailJson, detailType);
                        if (paymentInitiated != null)
                        {
                            await HandlePaymentInitiatedEvent(paymentInitiated);
                            return true;
                        }
                        break;

                    case "PaymentRefunded":
                        var paymentRefunded = SafeDeserialize<PaymentRefundedEvent>(detailJson, detailType);
                        if (paymentRefunded != null)
                        {
                            await HandlePaymentRefundedEvent(paymentRefunded);
                            return true;
                        }
                        break;

                    // Content/System Events
                    case "ContentPublished":
                        var contentPublished = SafeDeserialize<ContentPublishedEvent>(detailJson, detailType);
                        if (contentPublished != null)
                        {
                            await HandleContentPublishedEvent(contentPublished);
                            return true;
                        }
                        break;

                    case "PromotionCreated":
                        var promotionCreated = SafeDeserialize<PromotionCreatedEvent>(detailJson, detailType);
                        if (promotionCreated != null)
                        {
                            await HandlePromotionCreatedEvent(promotionCreated);
                            return true;
                        }
                        break;

                    // Security Events
                    case "AccountLocked":
                        var accountLocked = SafeDeserialize<AccountLockedEvent>(detailJson, detailType);
                        if (accountLocked != null)
                        {
                            await HandleAccountLockedEvent(accountLocked);
                            return true;
                        }
                        break;

                    case "AccountUnlocked":
                        var accountUnlocked = SafeDeserialize<AccountUnlockedEvent>(detailJson, detailType);
                        if (accountUnlocked != null)
                        {
                            await HandleAccountUnlockedEvent(accountUnlocked);
                            return true;
                        }
                        break;

                    case "FailedLoginAttempts":
                        var failedLoginAttempts = SafeDeserialize<FailedLoginAttemptsEvent>(detailJson, detailType);
                        if (failedLoginAttempts != null)
                        {
                            await HandleFailedLoginAttemptsEvent(failedLoginAttempts);
                            return true;
                        }
                        break;

                    case "NewDeviceLogin":
                        var newDeviceLogin = SafeDeserialize<NewDeviceLoginEvent>(detailJson, detailType);
                        if (newDeviceLogin != null)
                        {
                            await HandleNewDeviceLoginEvent(newDeviceLogin);
                            return true;
                        }
                        break;

                    case "NewLocationLogin":
                        var newLocationLogin = SafeDeserialize<NewLocationLoginEvent>(detailJson, detailType);
                        if (newLocationLogin != null)
                        {
                            await HandleNewLocationLoginEvent(newLocationLogin);
                            return true;
                        }
                        break;

                    case "TwoFactorEnabled":
                        var twoFactorEnabled = SafeDeserialize<TwoFactorEnabledEvent>(detailJson, detailType);
                        if (twoFactorEnabled != null)
                        {
                            await HandleTwoFactorEnabledEvent(twoFactorEnabled);
                            return true;
                        }
                        break;

                    case "TwoFactorDisabled":
                        var twoFactorDisabled = SafeDeserialize<TwoFactorDisabledEvent>(detailJson, detailType);
                        if (twoFactorDisabled != null)
                        {
                            await HandleTwoFactorDisabledEvent(twoFactorDisabled);
                            return true;
                        }
                        break;

                    case "PasswordChanged":
                        var passwordChanged = SafeDeserialize<PasswordChangedEvent>(detailJson, detailType);
                        if (passwordChanged != null)
                        {
                            await HandlePasswordChangedEvent(passwordChanged);
                            return true;
                        }
                        break;

                    case "EmailChanged":
                        var emailChanged = SafeDeserialize<EmailChangedEvent>(detailJson, detailType);
                        if (emailChanged != null)
                        {
                            await HandleEmailChangedEvent(emailChanged);
                            return true;
                        }
                        break;

                    case "PhoneVerified":
                        var phoneVerified = SafeDeserialize<PhoneVerifiedEvent>(detailJson, detailType);
                        if (phoneVerified != null)
                        {
                            await HandlePhoneVerifiedEvent(phoneVerified);
                            return true;
                        }
                        break;

                    case "SuspiciousActivity":
                        var suspiciousActivity = SafeDeserialize<SuspiciousActivityEvent>(detailJson, detailType);
                        if (suspiciousActivity != null)
                        {
                            await HandleSuspiciousActivityEvent(suspiciousActivity);
                            return true;
                        }
                        break;

                    // Additional Lottery Events
                    case "TicketRefunded":
                        var ticketRefunded = SafeDeserialize<TicketRefundedEvent>(detailJson, detailType);
                        if (ticketRefunded != null)
                        {
                            await HandleTicketRefundedEvent(ticketRefunded);
                            return true;
                        }
                        break;

                    case "LotteryDrawStarting":
                        var drawStarting = SafeDeserialize<LotteryDrawStartingEvent>(detailJson, detailType);
                        if (drawStarting != null)
                        {
                            await HandleLotteryDrawStartingEvent(drawStarting);
                            return true;
                        }
                        break;

                    case "LotteryDrawStarted":
                        var drawStarted = SafeDeserialize<LotteryDrawStartedEvent>(detailJson, detailType);
                        if (drawStarted != null)
                        {
                            await HandleLotteryDrawStartedEvent(drawStarted);
                            return true;
                        }
                        break;

                    case "LotteryEnded":
                        var lotteryEnded = SafeDeserialize<LotteryEndedEvent>(detailJson, detailType);
                        if (lotteryEnded != null)
                        {
                            await HandleLotteryEndedEvent(lotteryEnded);
                            return true;
                        }
                        break;

                    case "FavoriteAdded":
                        var favoriteAdded = SafeDeserialize<FavoriteAddedEvent>(detailJson, detailType);
                        if (favoriteAdded != null)
                        {
                            await HandleFavoriteAddedEvent(favoriteAdded);
                            return true;
                        }
                        break;

                    case "FavoriteRemoved":
                        var favoriteRemoved = SafeDeserialize<FavoriteRemovedEvent>(detailJson, detailType);
                        if (favoriteRemoved != null)
                        {
                            await HandleFavoriteRemovedEvent(favoriteRemoved);
                            return true;
                        }
                        break;

                    // Additional Payment Events
                    case "PaymentDisputed":
                        var paymentDisputed = SafeDeserialize<PaymentDisputedEvent>(detailJson, detailType);
                        if (paymentDisputed != null)
                        {
                            await HandlePaymentDisputedEvent(paymentDisputed);
                            return true;
                        }
                        break;

                    // Profile/System Events
                    case "ProfileUpdated":
                        var profileUpdated = SafeDeserialize<ProfileUpdatedEvent>(detailJson, detailType);
                        if (profileUpdated != null)
                        {
                            await HandleProfileUpdatedEvent(profileUpdated);
                            return true;
                        }
                        break;

                    case "PreferencesUpdated":
                        var preferencesUpdated = SafeDeserialize<PreferencesUpdatedEvent>(detailJson, detailType);
                        if (preferencesUpdated != null)
                        {
                            await HandlePreferencesUpdatedEvent(preferencesUpdated);
                            return true;
                        }
                        break;

                    case "SystemAnnouncement":
                        var systemAnnouncement = SafeDeserialize<SystemAnnouncementEvent>(detailJson, detailType);
                        if (systemAnnouncement != null)
                        {
                            await HandleSystemAnnouncementEvent(systemAnnouncement);
                            return true;
                        }
                        break;

                    // Optional Events
                    case "UserUpdated":
                        var userUpdated = SafeDeserialize<UserUpdatedEvent>(detailJson, detailType);
                        if (userUpdated != null)
                        {
                            await HandleUserUpdatedEvent(userUpdated);
                            return true;
                        }
                        break;

                    case "UserVerified":
                        var userVerified = SafeDeserialize<UserVerifiedEvent>(detailJson, detailType);
                        if (userVerified != null)
                        {
                            await HandleUserVerifiedEvent(userVerified);
                            return true;
                        }
                        break;

                    case "PrizeClaimed":
                        var prizeClaimed = SafeDeserialize<PrizeClaimedEvent>(detailJson, detailType);
                        if (prizeClaimed != null)
                        {
                            await HandlePrizeClaimedEvent(prizeClaimed);
                            return true;
                        }
                        break;

                    case "PrizeDelivered":
                        var prizeDelivered = SafeDeserialize<PrizeDeliveredEvent>(detailJson, detailType);
                        if (prizeDelivered != null)
                        {
                            await HandlePrizeDeliveredEvent(prizeDelivered);
                            return true;
                        }
                        break;

                    case "LotteryResultCreated":
                        var lotteryResultCreated = SafeDeserialize<LotteryResultCreatedEvent>(detailJson, detailType);
                        if (lotteryResultCreated != null)
                        {
                            await HandleLotteryResultCreatedEvent(lotteryResultCreated);
                            return true;
                        }
                        break;

                    default:
                        _logger.LogWarning("Unhandled event type: {DetailType}", detailType);
                        return false;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error deserializing event detail for {DetailType}", request.DetailType);
                return false;
            }

            return false;
        }

        // Event handler methods - delegate to EventBridgeEventHandler or implement directly
        private async Task HandleUserCreatedEvent(UserCreatedEvent @event)
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
                throw;
            }
        }

        private async Task HandleEmailVerificationRequestedEvent(EmailVerificationRequestedEvent @event)
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
                throw;
            }
        }

        private async Task HandlePasswordResetRequestedEvent(PasswordResetRequestedEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            
            try
            {
                await emailService.SendPasswordResetAsync(@event.Email, @event.ResetToken);
                _logger.LogInformation("Password reset email sent to {Email}", @event.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset email to {Email}", @event.Email);
                throw; // Critical - password reset emails should not fail silently
            }
        }

        private async Task HandleUserEmailVerifiedEvent(UserEmailVerifiedEvent @event)
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
                throw;
            }
        }

        private async Task HandleLotteryDrawWinnerSelectedEvent(LotteryDrawWinnerSelectedEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            
            try
            {
                await notificationService.SendLotteryWinnerNotificationAsync(
                    @event.WinnerUserId,
                    @event.HouseTitle ?? "House",
                    @event.WinningTicketNumber.ToString());
                
                _logger.LogInformation("Winner notification sent to user {UserId}", @event.WinnerUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending winner notification to user {UserId}", @event.WinnerUserId);
                throw;
            }
        }

        private async Task HandleTicketPurchasedEvent(TicketPurchasedEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();
            
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

                var request = new NotificationRequest
                {
                    UserId = @event.UserId,
                    Type = NotificationTypeConstants.TicketPurchased,
                    Title = "Tickets Purchased",
                    Message = message,
                    Channel = "email",
                    Data = new Dictionary<string, object>
                    {
                        { "houseId", @event.HouseId.ToString() },
                        { "ticketCount", @event.TicketCount },
                        { "ticketNumbers", @event.TicketNumbers ?? new List<string>() }
                    }
                };

                await orchestrator.SendMultiChannelAsync(@event.UserId, request, new List<string> { "email", "webpush" });
                
                _logger.LogInformation("Ticket purchase notification sent to user {UserId} for {TicketCount} tickets", 
                    @event.UserId, @event.TicketCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending ticket purchase notification to user {UserId}", @event.UserId);
                throw;
            }
        }

        private async Task HandlePaymentCompletedEvent(PaymentCompletedEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();
            
            try
            {
                var request = new NotificationRequest
                {
                    UserId = @event.UserId,
                    Type = NotificationTypeConstants.PaymentCompleted,
                    Title = "Payment Successful",
                    Message = $"Your payment of ${@event.Amount:F2} {@event.Currency} has been processed successfully via {@event.PaymentMethod}.",
                    Channel = "email"
                };

                await orchestrator.SendMultiChannelAsync(@event.UserId, request, new List<string> { "email", "webpush" });
                
                _logger.LogInformation("Payment success notification sent to user {UserId}", @event.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending payment success notification to user {UserId}", @event.UserId);
                throw;
            }
        }

        private async Task HandlePaymentFailedEvent(PaymentFailedEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();
            
            try
            {
                var request = new NotificationRequest
                {
                    UserId = @event.UserId,
                    Type = NotificationTypeConstants.PaymentFailed,
                    Title = "Payment Failed",
                    Message = $"Your payment of ${@event.Amount:F2} could not be processed. {(string.IsNullOrEmpty(@event.FailureReason) ? "Please try again or contact support." : $"Reason: {@event.FailureReason}")}",
                    Channel = "email"
                };

                await orchestrator.SendMultiChannelAsync(@event.UserId, request, new List<string> { "email", "webpush" });
                
                _logger.LogInformation("Payment failure notification sent to user {UserId}", @event.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending payment failure notification to user {UserId}", @event.UserId);
                throw;
            }
        }

        private async Task HandleUserLoginEvent(UserLoginEvent @event)
        {
            // Validate UserId
            if (!ValidateUserId(@event.UserId, "UserLogin"))
            {
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();
            
            try
            {
                // Security notification for login - only send via email/SMS, not webpush
                var request = new NotificationRequest
                {
                    UserId = @event.UserId,
                    Type = NotificationTypeConstants.NewDeviceLogin, // Using NewDeviceLogin as closest match
                    Title = "New Login Detected",
                    Message = $"A new login was detected from IP address {@event.IpAddress}. If this wasn't you, please secure your account immediately.",
                    Channel = "email"
                };

                await orchestrator.SendMultiChannelAsync(@event.UserId, request, new List<string> { "email" });
                
                _logger.LogInformation("Login notification sent to user {UserId}", @event.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending login notification to user {UserId}", @event.UserId);
                // Don't throw - login notifications are non-critical
            }
        }

        private async Task HandleLotteryDrawCompletedEvent(LotteryDrawCompletedEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();
            var lotteryClient = scope.ServiceProvider.GetRequiredService<ILotteryServiceClient>();
            
            try
            {
                // Get all participants
                var participantUserIds = await lotteryClient.GetDrawParticipantsAsync(@event.DrawId);
                
                if (participantUserIds == null || participantUserIds.Count == 0)
                {
                    _logger.LogWarning("No participants found for draw {DrawId}", @event.DrawId);
                    return;
                }

                // Filter out invalid user IDs
                participantUserIds = participantUserIds.Where(id => id != Guid.Empty).ToList();
                if (participantUserIds.Count == 0)
                {
                    _logger.LogWarning("No valid participants found for draw {DrawId} after filtering", @event.DrawId);
                    return;
                }

                // Get house info
                var houseInfo = await lotteryClient.GetHouseInfoAsync(@event.HouseId);
                var houseTitle = houseInfo?.Title ?? "House";

                // Process bulk notifications with batching
                await ProcessBulkNotificationsAsync(
                    participantUserIds,
                    async (userId) =>
                    {
                        // Create new request for each user to avoid race condition
                        var request = new NotificationRequest
                        {
                            UserId = userId,
                            Type = NotificationTypeConstants.LotteryDrawCompleted,
                            Title = "Draw Completed",
                            Message = $"The lottery draw for '{houseTitle}' has been completed. Results will be available soon!",
                            Channel = "email"
                        };
                        await orchestrator.SendMultiChannelAsync(userId, request, 
                            new List<string> { "email", "webpush" });
                    },
                    batchSize: 100,
                    maxConcurrency: 10);

                _logger.LogInformation("Notified {Count} participants about draw completion", participantUserIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing lottery draw completed event for DrawId {DrawId}", @event.DrawId);
                throw;
            }
        }

        private async Task HandleHouseCreatedEvent(HouseCreatedEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();
            
            try
            {
                // Notify the creator about the house creation
                var request = new NotificationRequest
                {
                    UserId = @event.CreatedByUserId,
                    Type = NotificationTypeConstants.HouseCreated,
                    Title = "House Created Successfully",
                    Message = $"Your lottery house '{@event.Title}' has been created successfully with a price of ${@event.Price:F2}.",
                    Channel = "email"
                };

                await orchestrator.SendMultiChannelAsync(@event.CreatedByUserId, request, new List<string> { "email", "webpush" });
                
                _logger.LogInformation("House created notification sent to user {UserId}", @event.CreatedByUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending house created notification to user {UserId}", @event.CreatedByUserId);
                throw;
            }
        }

        private async Task HandleHouseUpdatedEvent(HouseUpdatedEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();
            var lotteryClient = scope.ServiceProvider.GetRequiredService<ILotteryServiceClient>();
            
            try
            {
                // Get house creator
                var creatorId = await lotteryClient.GetHouseCreatorIdAsync(@event.HouseId);
                if (!creatorId.HasValue)
                {
                    _logger.LogWarning("Creator not found for house {HouseId}", @event.HouseId);
                    return;
                }

                // Get house info
                var houseInfo = await lotteryClient.GetHouseInfoAsync(@event.HouseId);
                var houseTitle = houseInfo?.Title ?? "House";

                // Get favorite users
                var favoriteUserIds = await lotteryClient.GetHouseFavoriteUserIdsAsync(@event.HouseId);
                
                // Notify creator
                var creatorRequest = new NotificationRequest
                {
                    UserId = creatorId.Value,
                    Type = NotificationTypeConstants.HouseUpdated,
                    Title = "House Updated",
                    Message = $"Your lottery house '{houseTitle}' has been updated.",
                    Channel = "email"
                };

                await orchestrator.SendMultiChannelAsync(creatorId.Value, creatorRequest, 
                    new List<string> { "email", "webpush" });

                // Notify favorite users
                if (favoriteUserIds.Any())
                {
                    var favoriteRequest = new NotificationRequest
                    {
                        Type = NotificationTypeConstants.HouseUpdated,
                        Title = "Favorite House Updated",
                        Message = $"'{houseTitle}' has been updated.",
                        Channel = "email"
                    };

                    var tasks = favoriteUserIds.Select(async userId =>
                    {
                        try
                        {
                            favoriteRequest.UserId = userId;
                            await orchestrator.SendMultiChannelAsync(userId, favoriteRequest, 
                                new List<string> { "email", "webpush" });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error notifying favorite user {UserId} about house update", userId);
                        }
                    });

                    await Task.WhenAll(tasks);
                }
                
                _logger.LogInformation("House updated notifications sent for house {HouseId}", @event.HouseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing house updated event for HouseId {HouseId}", @event.HouseId);
                // Don't throw - house updates are non-critical notifications
            }
        }

        private async Task HandlePaymentInitiatedEvent(PaymentInitiatedEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();
            
            try
            {
                var request = new NotificationRequest
                {
                    UserId = @event.UserId,
                    Type = NotificationTypeConstants.PaymentInitiated,
                    Title = "Payment Initiated",
                    Message = $"A payment of ${@event.Amount:F2} {@event.Currency} has been initiated via {@event.PaymentMethod}. Processing...",
                    Channel = "email"
                };

                await orchestrator.SendMultiChannelAsync(@event.UserId, request, new List<string> { "email", "webpush" });
                
                _logger.LogInformation("Payment initiated notification sent to user {UserId}", @event.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending payment initiated notification to user {UserId}", @event.UserId);
                throw;
            }
        }

        private async Task HandlePaymentRefundedEvent(PaymentRefundedEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();
            
            try
            {
                var request = new NotificationRequest
                {
                    UserId = @event.UserId,
                    Type = NotificationTypeConstants.PaymentRefunded,
                    Title = "Payment Refunded",
                    Message = $"A refund of ${@event.RefundAmount:F2} has been processed. {(string.IsNullOrEmpty(@event.RefundReason) ? "" : $"Reason: {@event.RefundReason}")} The refund should appear in your account within 5-10 business days.",
                    Channel = "email"
                };

                await orchestrator.SendMultiChannelAsync(@event.UserId, request, new List<string> { "email", "webpush" });
                
                _logger.LogInformation("Payment refunded notification sent to user {UserId}", @event.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending payment refunded notification to user {UserId}", @event.UserId);
                throw;
            }
        }

        private async Task HandleContentPublishedEvent(ContentPublishedEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();
            var authClient = scope.ServiceProvider.GetRequiredService<IAuthServiceClient>();
            
            try
            {
                // For now, notify all active users
                // In production, you'd implement targeting logic based on content type, tags, etc.
                var targetUserIds = await authClient.GetActiveUserIdsAsync();
                
                if (targetUserIds.Count == 0)
                {
                    _logger.LogWarning("No active users found for content published notification");
                    return;
                }

                var tasks = targetUserIds.Select(async userId =>
                {
                    try
                    {
                        // Create new request for each user to avoid race condition
                        var request = new NotificationRequest
                        {
                            UserId = userId,
                            Type = NotificationTypeConstants.ContentPublished,
                            Title = "New Content Available",
                            Message = $"New content: {@event.Title}",
                            Channel = "email"
                        };
                        await orchestrator.SendMultiChannelAsync(userId, request, 
                            new List<string> { "email", "webpush" });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error notifying user {UserId} about content published", userId);
                    }
                });

                await Task.WhenAll(tasks);
                _logger.LogInformation("Notified {Count} users about content published: {Title}", 
                    targetUserIds.Count, @event.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing content published event for ContentId {ContentId}", @event.ContentId);
                // Don't throw - content publishing notifications are non-critical
            }
        }

        private async Task HandlePromotionCreatedEvent(PromotionCreatedEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();
            var authClient = scope.ServiceProvider.GetRequiredService<IAuthServiceClient>();
            
            try
            {
                // For now, notify all active users
                // In production, you'd implement targeting logic (e.g., users who haven't purchased in X days)
                var targetUserIds = await authClient.GetActiveUserIdsAsync();
                
                if (targetUserIds.Count == 0)
                {
                    _logger.LogWarning("No active users found for promotion created notification");
                    return;
                }

                var tasks = targetUserIds.Select(async userId =>
                {
                    try
                    {
                        // Create new request for each user to avoid race condition
                        var request = new NotificationRequest
                        {
                            UserId = userId,
                            Type = NotificationTypeConstants.PromotionCreated,
                            Title = "New Promotion Available",
                            Message = $"New promotion code: {@event.Code} - Save ${@event.DiscountAmount:F2}!",
                            Channel = "email"
                        };
                        await orchestrator.SendMultiChannelAsync(userId, request, 
                            new List<string> { "email", "webpush" });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error notifying user {UserId} about promotion created", userId);
                    }
                });

                await Task.WhenAll(tasks);
                _logger.LogInformation("Notified {Count} users about promotion created: {Code}", 
                    targetUserIds.Count, @event.Code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing promotion created event for PromotionId {PromotionId}", @event.PromotionId);
                // Don't throw - promotion notifications are non-critical
            }
        }

        // ========== Security Event Handlers ==========

        private async Task HandleAccountLockedEvent(AccountLockedEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();
            
            try
            {
                var request = new NotificationRequest
                {
                    UserId = @event.UserId,
                    Type = NotificationTypeConstants.AccountLocked,
                    Title = "Account Locked",
                    Message = $"Your account has been locked. Reason: {@event.LockReason}. " +
                              (@event.LockedUntil.HasValue 
                                  ? $"It will be unlocked on {@event.LockedUntil.Value:yyyy-MM-dd HH:mm} UTC." 
                                  : "Please contact support to unlock your account."),
                    Channel = "email"
                };

                // Critical security notification - send via all channels, bypass quiet hours
                await orchestrator.SendMultiChannelAsync(@event.UserId, request, 
                    new List<string> { "email", "sms", "webpush" });
                
                _logger.LogInformation("Account locked notification sent to user {UserId}", @event.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending account locked notification to user {UserId}", @event.UserId);
                throw; // Security notifications should not fail silently
            }
        }

        private async Task HandleAccountUnlockedEvent(AccountUnlockedEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();
            
            try
            {
                var request = new NotificationRequest
                {
                    UserId = @event.UserId,
                    Type = NotificationTypeConstants.AccountUnlocked,
                    Title = "Account Unlocked",
                    Message = $"Your account has been unlocked. {@event.UnlockReason}",
                    Channel = "email"
                };

                await orchestrator.SendMultiChannelAsync(@event.UserId, request, 
                    new List<string> { "email", "webpush" });
                
                _logger.LogInformation("Account unlocked notification sent to user {UserId}", @event.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending account unlocked notification to user {UserId}", @event.UserId);
                // Don't throw - unlock notifications are non-critical
            }
        }

        private async Task HandleFailedLoginAttemptsEvent(FailedLoginAttemptsEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();
            
            try
            {
                var request = new NotificationRequest
                {
                    UserId = @event.UserId,
                    Type = NotificationTypeConstants.FailedLoginAttempts,
                    Title = "Multiple Failed Login Attempts",
                    Message = $"We detected {@event.AttemptCount} failed login attempts from IP {@event.IpAddress} on {@event.LastAttemptAt:yyyy-MM-dd HH:mm} UTC. If this wasn't you, please secure your account immediately.",
                    Channel = "email"
                };

                // Critical security notification
                await orchestrator.SendMultiChannelAsync(@event.UserId, request, 
                    new List<string> { "email", "sms" });
                
                _logger.LogInformation("Failed login attempts notification sent to user {UserId}", @event.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending failed login attempts notification to user {UserId}", @event.UserId);
                throw; // Security notifications should not fail silently
            }
        }

        private async Task HandleNewDeviceLoginEvent(NewDeviceLoginEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();
            
            try
            {
                var locationText = !string.IsNullOrEmpty(@event.Location) ? $" from {@event.Location}" : "";
                var request = new NotificationRequest
                {
                    UserId = @event.UserId,
                    Type = NotificationTypeConstants.NewDeviceLogin,
                    Title = "New Device Login Detected",
                    Message = $"A login was detected from a new device{locationText} ({@event.DeviceName}) from IP {@event.IpAddress}. If this wasn't you, please secure your account immediately.",
                    Channel = "email"
                };

                await orchestrator.SendMultiChannelAsync(@event.UserId, request, 
                    new List<string> { "email", "sms" });
                
                _logger.LogInformation("New device login notification sent to user {UserId}", @event.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending new device login notification to user {UserId}", @event.UserId);
                // Don't throw - device login notifications are non-critical
            }
        }

        private async Task HandleNewLocationLoginEvent(NewLocationLoginEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();
            
            try
            {
                var locationText = !string.IsNullOrEmpty(@event.City) && !string.IsNullOrEmpty(@event.Country)
                    ? $"{@event.City}, {@event.Country}"
                    : @event.Location;
                
                var request = new NotificationRequest
                {
                    UserId = @event.UserId,
                    Type = NotificationTypeConstants.NewLocationLogin,
                    Title = "New Location Login Detected",
                    Message = $"A login was detected from a new location: {locationText} (IP: {@event.IpAddress}). If this wasn't you, please secure your account immediately.",
                    Channel = "email"
                };

                await orchestrator.SendMultiChannelAsync(@event.UserId, request, 
                    new List<string> { "email", "sms" });
                
                _logger.LogInformation("New location login notification sent to user {UserId}", @event.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending new location login notification to user {UserId}", @event.UserId);
                // Don't throw - location login notifications are non-critical
            }
        }

        private async Task HandleTwoFactorEnabledEvent(TwoFactorEnabledEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();
            
            try
            {
                var request = new NotificationRequest
                {
                    UserId = @event.UserId,
                    Type = NotificationTypeConstants.TwoFactorEnabled,
                    Title = "Two-Factor Authentication Enabled",
                    Message = $"Two-factor authentication has been enabled on your account using {@event.TwoFactorMethod}.",
                    Channel = "email"
                };

                await orchestrator.SendMultiChannelAsync(@event.UserId, request, 
                    new List<string> { "email", "webpush" });
                
                _logger.LogInformation("Two-factor enabled notification sent to user {UserId}", @event.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending two-factor enabled notification to user {UserId}", @event.UserId);
                // Don't throw - 2FA notifications are non-critical
            }
        }

        private async Task HandleTwoFactorDisabledEvent(TwoFactorDisabledEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();
            
            try
            {
                var request = new NotificationRequest
                {
                    UserId = @event.UserId,
                    Type = NotificationTypeConstants.TwoFactorDisabled,
                    Title = "Two-Factor Authentication Disabled",
                    Message = $"Two-factor authentication has been disabled on your account. Reason: {@event.DisabledReason}. If this wasn't you, please secure your account immediately.",
                    Channel = "email"
                };

                // Security notification - send via email and SMS
                await orchestrator.SendMultiChannelAsync(@event.UserId, request, 
                    new List<string> { "email", "sms" });
                
                _logger.LogInformation("Two-factor disabled notification sent to user {UserId}", @event.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending two-factor disabled notification to user {UserId}", @event.UserId);
                throw; // Security notifications should not fail silently
            }
        }

        private async Task HandlePasswordChangedEvent(PasswordChangedEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();
            
            try
            {
                var changedBy = @event.ChangedByUser ? "you" : "an administrator";
                var request = new NotificationRequest
                {
                    UserId = @event.UserId,
                    Type = NotificationTypeConstants.PasswordChanged,
                    Title = "Password Changed",
                    Message = $"Your password has been changed by {changedBy} from IP {@event.IpAddress}. If this wasn't you, please secure your account immediately.",
                    Channel = "email"
                };

                // Security notification
                await orchestrator.SendMultiChannelAsync(@event.UserId, request, 
                    new List<string> { "email", "sms" });
                
                _logger.LogInformation("Password changed notification sent to user {UserId}", @event.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password changed notification to user {UserId}", @event.UserId);
                throw; // Security notifications should not fail silently
            }
        }

        private async Task HandleEmailChangedEvent(EmailChangedEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();
            
            try
            {
                // Send notification to old email
                var oldEmailRequest = new NotificationRequest
                {
                    UserId = @event.UserId,
                    Type = NotificationTypeConstants.EmailChanged,
                    Title = "Email Address Changed",
                    Message = $"Your email address has been changed from {@event.OldEmail} to {@event.NewEmail} from IP {@event.IpAddress}. If this wasn't you, please contact support immediately.",
                    Channel = "email"
                };

                // Send notification to new email
                var newEmailRequest = new NotificationRequest
                {
                    UserId = @event.UserId,
                    Type = NotificationTypeConstants.EmailChanged,
                    Title = "Email Address Changed",
                    Message = $"Your email address has been changed to this address from IP {@event.IpAddress}. If this wasn't you, please contact support immediately.",
                    Channel = "email"
                };

                // Note: In production, you'd need to send to both emails separately
                // For now, we'll send to the user ID (which will use current email)
                await orchestrator.SendMultiChannelAsync(@event.UserId, newEmailRequest, 
                    new List<string> { "email", "sms" });
                
                _logger.LogInformation("Email changed notification sent to user {UserId}", @event.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email changed notification to user {UserId}", @event.UserId);
                throw; // Security notifications should not fail silently
            }
        }

        private async Task HandlePhoneVerifiedEvent(PhoneVerifiedEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();
            
            try
            {
                var request = new NotificationRequest
                {
                    UserId = @event.UserId,
                    Type = NotificationTypeConstants.PhoneVerified,
                    Title = "Phone Number Verified",
                    Message = $"Your phone number {@event.PhoneNumber} has been verified successfully.",
                    Channel = "email"
                };

                await orchestrator.SendMultiChannelAsync(@event.UserId, request, 
                    new List<string> { "email", "webpush" });
                
                _logger.LogInformation("Phone verified notification sent to user {UserId}", @event.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending phone verified notification to user {UserId}", @event.UserId);
                // Don't throw - phone verification notifications are non-critical
            }
        }

        private async Task HandleSuspiciousActivityEvent(SuspiciousActivityEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();
            
            try
            {
                var severityText = @event.Severity.ToUpper();
                var request = new NotificationRequest
                {
                    UserId = @event.UserId,
                    Type = NotificationTypeConstants.SuspiciousActivity,
                    Title = $"{severityText} Suspicious Activity Detected",
                    Message = $"We detected suspicious activity on your account: {@event.Description} from IP {@event.IpAddress}. Please review your account security immediately.",
                    Channel = "email"
                };

                // Critical security notification - send via all channels
                var channels = new List<string> { "email", "sms" };
                if (@event.Severity == "critical" || @event.Severity == "high")
                {
                    channels.Add("webpush");
                }
                
                await orchestrator.SendMultiChannelAsync(@event.UserId, request, channels);
                
                _logger.LogInformation("Suspicious activity notification sent to user {UserId} (severity: {Severity})", 
                    @event.UserId, @event.Severity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending suspicious activity notification to user {UserId}", @event.UserId);
                throw; // Security notifications should not fail silently
            }
        }

        // ========== Lottery Event Handlers ==========

        private async Task HandleTicketRefundedEvent(TicketRefundedEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();
            
            try
            {
                var request = new NotificationRequest
                {
                    UserId = @event.UserId,
                    Type = NotificationTypeConstants.TicketRefunded,
                    Title = "Ticket Refunded",
                    Message = $"Your ticket #{@event.TicketNumber} has been refunded. Amount: ${@event.RefundAmount:F2}. Reason: {@event.RefundReason}",
                    Channel = "email"
                };

                await orchestrator.SendMultiChannelAsync(@event.UserId, request, 
                    new List<string> { "email", "webpush" });
                
                _logger.LogInformation("Ticket refunded notification sent to user {UserId}", @event.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending ticket refunded notification to user {UserId}", @event.UserId);
                throw;
            }
        }

        private async Task HandleLotteryDrawStartingEvent(LotteryDrawStartingEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();
            var lotteryClient = scope.ServiceProvider.GetRequiredService<ILotteryServiceClient>();
            
            try
            {
                // Get all participants who have tickets for this draw
                var participantUserIds = await lotteryClient.GetDrawParticipantsAsync(@event.DrawId);
                
                if (participantUserIds.Count == 0)
                {
                    _logger.LogWarning("No participants found for draw {DrawId}", @event.DrawId);
                    return;
                }

                // Notify all participants
                var tasks = participantUserIds.Select(async userId =>
                {
                    try
                    {
                        // Create new request for each user to avoid race condition
                        var request = new NotificationRequest
                        {
                            UserId = userId,
                            Type = NotificationTypeConstants.LotteryDrawStarting,
                            Title = "Draw Starting Soon",
                            Message = $"The lottery draw for '{@event.HouseTitle}' is starting in {@event.MinutesUntilStart} minutes!",
                            Channel = "email"
                        };
                        await orchestrator.SendMultiChannelAsync(userId, request, 
                            new List<string> { "email", "webpush" });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error notifying participant {UserId} about draw starting", userId);
                    }
                });

                await Task.WhenAll(tasks);
                _logger.LogInformation("Notified {Count} participants about draw starting", participantUserIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing lottery draw starting event for DrawId {DrawId}", @event.DrawId);
                throw;
            }
        }

        private async Task HandleLotteryDrawStartedEvent(LotteryDrawStartedEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();
            var lotteryClient = scope.ServiceProvider.GetRequiredService<ILotteryServiceClient>();
            
            try
            {
                var participantUserIds = await lotteryClient.GetDrawParticipantsAsync(@event.DrawId);
                
                if (participantUserIds.Count == 0)
                {
                    _logger.LogWarning("No participants found for draw {DrawId}", @event.DrawId);
                    return;
                }

                var tasks = participantUserIds.Select(async userId =>
                {
                    try
                    {
                        // Create new request for each user to avoid race condition
                        var request = new NotificationRequest
                        {
                            UserId = userId,
                            Type = NotificationTypeConstants.LotteryDrawStarted,
                            Title = "Draw Started",
                            Message = $"The lottery draw for '{@event.HouseTitle}' has started! Results will be available soon.",
                            Channel = "email"
                        };
                        await orchestrator.SendMultiChannelAsync(userId, request, 
                            new List<string> { "email", "webpush" });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error notifying participant {UserId} about draw started", userId);
                    }
                });

                await Task.WhenAll(tasks);
                _logger.LogInformation("Notified {Count} participants about draw started", participantUserIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing lottery draw started event for DrawId {DrawId}", @event.DrawId);
                throw;
            }
        }

        private async Task HandleLotteryEndedEvent(LotteryEndedEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();
            var lotteryClient = scope.ServiceProvider.GetRequiredService<ILotteryServiceClient>();
            
            try
            {
                var participantUserIds = await lotteryClient.GetDrawParticipantsAsync(@event.DrawId);
                
                if (participantUserIds.Count == 0)
                {
                    _logger.LogWarning("No participants found for draw {DrawId}", @event.DrawId);
                    return;
                }

                var message = @event.WasCancelled
                    ? $"The lottery draw for '{@event.HouseTitle}' has been cancelled. Reason: {@event.CancellationReason}"
                    : @event.WinnerUserId.HasValue
                        ? $"The lottery draw for '{@event.HouseTitle}' has ended. Winner: {@event.WinnerName ?? "Selected"}"
                        : $"The lottery draw for '{@event.HouseTitle}' has ended.";

                var tasks = participantUserIds.Select(async userId =>
                {
                    try
                    {
                        // Create new request for each user to avoid race condition
                        var request = new NotificationRequest
                        {
                            UserId = userId,
                            Type = NotificationTypeConstants.LotteryEnded,
                            Title = "Draw Ended",
                            Message = message,
                            Channel = "email"
                        };
                        await orchestrator.SendMultiChannelAsync(userId, request, 
                            new List<string> { "email", "webpush" });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error notifying participant {UserId} about draw ended", userId);
                    }
                });

                await Task.WhenAll(tasks);
                _logger.LogInformation("Notified {Count} participants about draw ended", participantUserIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing lottery ended event for DrawId {DrawId}", @event.DrawId);
                throw;
            }
        }

        private async Task HandleFavoriteAddedEvent(FavoriteAddedEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();
            
            try
            {
                var request = new NotificationRequest
                {
                    UserId = @event.UserId,
                    Type = NotificationTypeConstants.FavoriteAdded,
                    Title = "House Added to Favorites",
                    Message = $"'{@event.HouseTitle}' has been added to your favorites.",
                    Channel = "email"
                };

                await orchestrator.SendMultiChannelAsync(@event.UserId, request, 
                    new List<string> { "email", "webpush" });
                
                _logger.LogInformation("Favorite added notification sent to user {UserId}", @event.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending favorite added notification to user {UserId}", @event.UserId);
                // Don't throw - favorite notifications are non-critical
            }
        }

        private async Task HandleFavoriteRemovedEvent(FavoriteRemovedEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();
            
            try
            {
                var request = new NotificationRequest
                {
                    UserId = @event.UserId,
                    Type = NotificationTypeConstants.FavoriteRemoved,
                    Title = "House Removed from Favorites",
                    Message = $"'{@event.HouseTitle}' has been removed from your favorites.",
                    Channel = "email"
                };

                await orchestrator.SendMultiChannelAsync(@event.UserId, request, 
                    new List<string> { "email", "webpush" });
                
                _logger.LogInformation("Favorite removed notification sent to user {UserId}", @event.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending favorite removed notification to user {UserId}", @event.UserId);
                // Don't throw - favorite notifications are non-critical
            }
        }

        // ========== Payment/Profile/System Event Handlers ==========

        private async Task HandlePaymentDisputedEvent(PaymentDisputedEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();
            
            try
            {
                var request = new NotificationRequest
                {
                    UserId = @event.UserId,
                    Type = NotificationTypeConstants.PaymentDisputed,
                    Title = "Payment Dispute",
                    Message = $"A dispute has been filed for your payment of ${@event.Amount:F2} {@event.Currency}. Type: {@event.DisputeType}. Status: {@event.Status}. Reason: {@event.DisputeReason}",
                    Channel = "email"
                };

                // Critical payment notification
                await orchestrator.SendMultiChannelAsync(@event.UserId, request, 
                    new List<string> { "email", "sms" });
                
                _logger.LogInformation("Payment dispute notification sent to user {UserId}", @event.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending payment dispute notification to user {UserId}", @event.UserId);
                throw;
            }
        }

        private async Task HandleProfileUpdatedEvent(ProfileUpdatedEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();
            
            try
            {
                var changedFieldsText = string.Join(", ", @event.ChangedFields);
                var request = new NotificationRequest
                {
                    UserId = @event.UserId,
                    Type = NotificationTypeConstants.ProfileUpdated,
                    Title = "Profile Updated",
                    Message = $"Your profile has been updated. Changed fields: {changedFieldsText}.",
                    Channel = "email"
                };

                await orchestrator.SendMultiChannelAsync(@event.UserId, request, 
                    new List<string> { "email", "webpush" });
                
                _logger.LogInformation("Profile updated notification sent to user {UserId}", @event.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending profile updated notification to user {UserId}", @event.UserId);
                // Don't throw - profile update notifications are non-critical
            }
        }

        private async Task HandlePreferencesUpdatedEvent(PreferencesUpdatedEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();
            
            try
            {
                var request = new NotificationRequest
                {
                    UserId = @event.UserId,
                    Type = NotificationTypeConstants.PreferencesUpdated,
                    Title = "Preferences Updated",
                    Message = $"Your {@event.PreferenceCategory} preferences have been updated successfully.",
                    Channel = "email"
                };

                await orchestrator.SendMultiChannelAsync(@event.UserId, request, 
                    new List<string> { "email", "webpush" });
                
                _logger.LogInformation("Preferences updated notification sent to user {UserId}", @event.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending preferences updated notification to user {UserId}", @event.UserId);
                // Don't throw - preferences update notifications are non-critical
            }
        }

        private async Task HandleSystemAnnouncementEvent(SystemAnnouncementEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();
            var authClient = scope.ServiceProvider.GetRequiredService<IAuthServiceClient>();
            
            try
            {
                List<Guid> targetUserIds;
                
                if (@event.TargetUserIds != null && @event.TargetUserIds.Any())
                {
                    // Specific users targeted
                    targetUserIds = @event.TargetUserIds;
                }
                else if (!string.IsNullOrEmpty(@event.TargetUserSegment))
                {
                    // User segment targeted
                    targetUserIds = await authClient.GetUserIdsBySegmentAsync(@event.TargetUserSegment);
                }
                else
                {
                    // All users
                    targetUserIds = await authClient.GetActiveUserIdsAsync();
                }

                if (targetUserIds.Count == 0)
                {
                    _logger.LogWarning("No target users found for system announcement {AnnouncementId}", @event.AnnouncementId);
                    return;
                }

                // Notify all target users
                var tasks = targetUserIds.Select(async userId =>
                {
                    try
                    {
                        // Create new request for each user to avoid race condition
                        var request = new NotificationRequest
                        {
                            UserId = userId,
                            Type = NotificationTypeConstants.SystemAnnouncement,
                            Title = @event.Title,
                            Message = @event.Message,
                            Channel = "email"
                        };
                        await orchestrator.SendMultiChannelAsync(userId, request, 
                            new List<string> { "email", "webpush" });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error notifying user {UserId} about system announcement", userId);
                    }
                });

                await Task.WhenAll(tasks);
                _logger.LogInformation("Notified {Count} users about system announcement {AnnouncementId}", 
                    targetUserIds.Count, @event.AnnouncementId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing system announcement event for AnnouncementId {AnnouncementId}", 
                    @event.AnnouncementId);
                throw;
            }
        }

        // ========== Optional Event Handlers ==========

        private async Task HandleUserUpdatedEvent(UserUpdatedEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();
            
            try
            {
                var request = new NotificationRequest
                {
                    UserId = @event.UserId,
                    Type = NotificationTypeConstants.ProfileUpdated, // Using closest match
                    Title = "Account Updated",
                    Message = "Your account information has been updated successfully.",
                    Channel = "email"
                };

                await orchestrator.SendMultiChannelAsync(@event.UserId, request, 
                    new List<string> { "email", "webpush" });
                
                _logger.LogInformation("User updated notification sent to user {UserId}", @event.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending user updated notification to user {UserId}", @event.UserId);
                // Don't throw - user update notifications are non-critical
            }
        }

        private async Task HandleUserVerifiedEvent(UserVerifiedEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();
            
            try
            {
                var request = new NotificationRequest
                {
                    UserId = @event.UserId,
                    Type = NotificationTypeConstants.EmailVerified, // Using closest match
                    Title = "Verification Complete",
                    Message = $"Your {@event.VerificationType} has been verified successfully.",
                    Channel = "email"
                };

                await orchestrator.SendMultiChannelAsync(@event.UserId, request, 
                    new List<string> { "email", "webpush" });
                
                _logger.LogInformation("User verified notification sent to user {UserId}", @event.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending user verified notification to user {UserId}", @event.UserId);
                // Don't throw - verification notifications are non-critical
            }
        }

        private async Task HandlePrizeClaimedEvent(PrizeClaimedEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();
            
            try
            {
                var request = new NotificationRequest
                {
                    UserId = @event.UserId,
                    Type = NotificationTypeConstants.LotteryWinnerSelected, // Using closest match
                    Title = "Prize Claimed",
                    Message = $"Your prize has been claimed successfully on {@event.ClaimedAt:yyyy-MM-dd HH:mm} UTC.",
                    Channel = "email"
                };

                await orchestrator.SendMultiChannelAsync(@event.UserId, request, 
                    new List<string> { "email", "webpush" });
                
                _logger.LogInformation("Prize claimed notification sent to user {UserId}", @event.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending prize claimed notification to user {UserId}", @event.UserId);
                // Don't throw - prize claim notifications are non-critical
            }
        }

        private async Task HandlePrizeDeliveredEvent(PrizeDeliveredEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();
            
            try
            {
                var request = new NotificationRequest
                {
                    UserId = @event.UserId,
                    Type = NotificationTypeConstants.LotteryWinnerSelected, // Using closest match
                    Title = "Prize Delivered",
                    Message = $"Your prize has been delivered successfully on {@event.DeliveredAt:yyyy-MM-dd HH:mm} UTC.",
                    Channel = "email"
                };

                await orchestrator.SendMultiChannelAsync(@event.UserId, request, 
                    new List<string> { "email", "webpush" });
                
                _logger.LogInformation("Prize delivered notification sent to user {UserId}", @event.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending prize delivered notification to user {UserId}", @event.UserId);
                // Don't throw - prize delivery notifications are non-critical
            }
        }

        private async Task HandleLotteryResultCreatedEvent(LotteryResultCreatedEvent @event)
        {
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();
            
            try
            {
                var request = new NotificationRequest
                {
                    UserId = @event.WinnerUserId,
                    Type = NotificationTypeConstants.LotteryWinnerSelected,
                    Title = "Lottery Result Created",
                    Message = "A lottery result has been created for your winning ticket. Check your account for details.",
                    Channel = "email"
                };

                await orchestrator.SendMultiChannelAsync(@event.WinnerUserId, request, 
                    new List<string> { "email", "webpush" });
                
                _logger.LogInformation("Lottery result created notification sent to user {UserId}", @event.WinnerUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending lottery result created notification to user {UserId}", @event.WinnerUserId);
                throw;
            }
        }

        // ========== Validation Helper Methods ==========

        /// <summary>
        /// Validates that a notification type exists in the system
        /// </summary>
        private async Task<bool> ValidateNotificationTypeAsync(string notificationTypeCode)
        {
            if (_typeMappingService == null)
            {
                // If service not available, log warning but allow (fail-open)
                _logger.LogWarning("NotificationTypeMappingService not available, skipping type validation for {Type}", notificationTypeCode);
                return true;
            }

            try
            {
                var exists = await _typeMappingService.TypeExistsAsync(notificationTypeCode);
                if (!exists)
                {
                    _logger.LogWarning("Notification type {Type} does not exist in system", notificationTypeCode);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating notification type {Type}", notificationTypeCode);
                // Fail-open: allow notification if validation fails
                return true;
            }
        }

        // ========== Error Handling Helper Methods ==========

        /// <summary>
        /// Executes a critical handler (security-related) that should fail fast on errors
        /// </summary>
        private async Task ExecuteCriticalHandlerAsync(Func<Task> handler, string handlerName, object? context = null)
        {
            try
            {
                await handler();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error in {HandlerName}. Context: {@Context}", handlerName, context);
                throw; // Security notifications should not fail silently
            }
        }

        /// <summary>
        /// Executes a non-critical handler (business-related) that should log and continue on errors
        /// </summary>
        private async Task ExecuteNonCriticalHandlerAsync(Func<Task> handler, string handlerName, object? context = null)
        {
            try
            {
                await handler();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Non-critical error in {HandlerName}. Context: {@Context}", handlerName, context);
                // Don't throw - non-critical notifications should not block event processing
            }
        }

        /// <summary>
        /// Executes a bulk notification handler with per-user error handling
        /// </summary>
        private async Task ExecuteBulkNotificationHandlerAsync(
            List<Guid> userIds,
            Func<Guid, Task> perUserHandler,
            string handlerName,
            object? context = null)
        {
            var tasks = userIds.Select(async userId =>
            {
                try
                {
                    await perUserHandler(userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in {HandlerName} for user {UserId}. Context: {@Context}", 
                        handlerName, userId, context);
                    // Continue with other users - don't fail entire batch
                }
            });

            await Task.WhenAll(tasks);
            _logger.LogInformation("Completed {HandlerName} for {Count} users. Context: {@Context}", 
                handlerName, userIds.Count, context);
        }
    }

    /// <summary>
    /// EventBridge webhook request format
    /// EventBridge sends events in this format when using API destinations
    /// </summary>
    public class EventBridgeWebhookRequest
    {
        public string Version { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        [JsonPropertyName("detail-type")]
        public string DetailType { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Account { get; set; } = string.Empty;
        public DateTime Time { get; set; }
        public string Region { get; set; } = string.Empty;
        public Dictionary<string, object> Detail { get; set; } = new();
    }
}

