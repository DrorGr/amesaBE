# Lottery Component Deep Audit - Implementation Plan

**Generated**: 2024-12-19  
**Component**: AmesaBackend.Lottery  
**Audit Scope**: Security, Integrations, Bugs, Data Consistency, Business Logic

---

## Executive Summary

This plan addresses **75+ critical issues** identified in the lottery component audit (initial + additional recursive audit), including:
- **15 Critical Security Vulnerabilities**
- **12 Critical Bugs**
- **5 Missing Integrations**
- **6 Data Consistency Issues**
- **8 Business Logic Flaws**
- **Multiple Validation & Error Handling Gaps**
- **Performance & Observability Issues**
- **4 Additional TODO Items** (Event publishing, Compensation logic, Password reset, Push notifications)

**Estimated Total Effort**: 6-8 weeks (includes 14 additional hours for new TODO items)  
**Priority**: **CRITICAL** - Immediate action required  
**Implementation Completeness**: ~60% (see Phase 0.5 for missing endpoints)

**Note**: 
- See `LOTTERY_COMPONENT_AUDIT_ADDITIONAL_FINDINGS.md` for 25+ additional issues discovered in recursive audit.
- See `LOTTERY_IMPLEMENTATION_STATUS_REPORT.md` for endpoint completeness check and missing implementations.

---

## Phase 0: Additional Critical Fixes (Week 1 - Immediate)

### 0.1 TicketCreatorProcessor - Complete Rewrite
**Priority**: CRITICAL  
**Effort**: 8 hours  
**Files**: `AmesaBackend.Lottery/Services/Processors/TicketCreatorProcessor.cs`

**Issues Found**:
- No transaction wrapping
- No participant cap check
- No lottery end date validation
- No house status validation
- No user verification check
- Race condition in ticket number generation

**Tasks**:
- [ ] Wrap entire operation in transaction
- [ ] Add participant cap check before ticket creation
- [ ] Add lottery end date validation
- [ ] Add house status validation
- [ ] Add user verification check
- [ ] Fix ticket number generation race condition
- [ ] Add idempotency check for reservation processing

---

### 0.2 RedisInventoryManager - Security & Race Condition Fixes
**Priority**: CRITICAL  
**Effort**: 6 hours  
**Files**: `AmesaBackend.Lottery/Services/RedisInventoryManager.cs`

**Issues Found**:
- Fail-open on participant cap check (line 200)
- Redis can go negative (no bounds checking)
- Reserved count can go negative
- No atomic operations for cap check + add participant

**Tasks**:
- [ ] Fix fail-open to fail-closed for security checks
- [ ] Add bounds checking in Lua scripts
- [ ] Prevent negative values in Redis
- [ ] Make participant cap check + add atomic
- [ ] Add Redis health checks
- [ ] Implement proper fallback strategy

---

### 0.3 ReservationProcessor - Refund Handling
**Priority**: CRITICAL  
**Effort**: 4 hours  
**Files**: `AmesaBackend.Lottery/Services/Processors/ReservationProcessor.cs`

**Issues Found**:
- Refund failures not checked
- No refund status tracking
- No retry for refund failures
- Inventory released even if refund fails

**Tasks**:
- [ ] Check refund result and handle failures
- [ ] Add refund status tracking
- [ ] Implement retry logic for refunds
- [ ] Don't release inventory if refund fails
- [ ] Add audit logging for refunds

---

### 0.4 Ticket Number Generation - Atomic Operations
**Priority**: CRITICAL  
**Effort**: 6 hours  
**Files**: `LotteryService.cs`, `TicketCreatorProcessor.cs`

**Issues Found**:
- Race condition in both services
- Can generate duplicate ticket numbers
- Not thread-safe

**Tasks**:
- [ ] Create database sequences per house
- [ ] OR use Redis atomic increment
- [ ] Add unique constraint on ticket numbers
- [ ] Add retry logic for conflicts
- [ ] Remove duplicate code

---

### 0.5 ReservationCleanupService - Race Condition Fix
**Priority**: High  
**Effort**: 4 hours  
**Files**: `AmesaBackend.Lottery/Services/ReservationCleanupService.cs`

**Issues Found**:
- No transaction wrapping
- Race condition between cleanup processes
- No distributed locking
- Inventory release not atomic

**Tasks**:
- [ ] Add distributed lock (Redis)
- [ ] Wrap operations in transaction
- [ ] Add idempotency checks
- [ ] Implement retry logic

---

### 0.6 InventorySyncService - Data Corruption Prevention
**Priority**: High  
**Effort**: 4 hours  
**Files**: `AmesaBackend.Lottery/Services/InventorySyncService.cs`

**Issues Found**:
- Race condition during sync
- Overwrites active reservations
- No locking during sync
- Calculation doesn't account for processing reservations

**Tasks**:
- [ ] Add distributed lock during sync
- [ ] Exclude processing reservations from calculation
- [ ] Add validation before overwriting Redis
- [ ] Implement conflict resolution

---

### 0.7 LotteryDrawService - Event Publishing
**Priority**: High  
**Effort**: 2 hours  
**Files**: `AmesaBackend.Lottery/Services/LotteryDrawService.cs`

**Issues Found**:
- TODO comment: "Implement IEventPublisher interface" (line 29)
- Events not published after draw completion
- Notification service cannot react to draw completion automatically

**Tasks**:
- [ ] Inject `IEventPublisher` into `LotteryDrawService`
- [ ] Publish `LotteryDrawCompletedEvent` after successful draw
- [ ] Publish `LotteryDrawWinnerSelectedEvent` after winner selection
- [ ] Add error handling for event publishing failures
- [ ] Add logging for event publishing
- [ ] Add unit tests for event publishing

**Implementation**:
```csharp
// Update LotteryDrawService.cs constructor
private readonly IEventPublisher? _eventPublisher;

public LotteryDrawService(
    IServiceProvider serviceProvider,
    ILogger<LotteryDrawService> logger,
    IEventPublisher? eventPublisher = null)
{
    _serviceProvider = serviceProvider;
    _logger = logger;
    _eventPublisher = eventPublisher;
}

// After successful draw (line 53)
await lotteryService.ConductDrawAsync(draw.Id, conductDrawRequest);

// Publish event
if (_eventPublisher != null)
{
    await _eventPublisher.PublishAsync(new LotteryDrawCompletedEvent
    {
        DrawId = draw.Id,
        HouseId = draw.HouseId,
        CompletedAt = DateTime.UtcNow
    });
}
```

---

### 0.8 Promotion Usage Compensation Logic
**Priority**: High  
**Effort**: 6 hours  
**Files**: `AmesaBackend.Lottery/Controllers/TicketsController.cs`, `AmesaBackend.Lottery/Controllers/HousesController.cs`

**Issues Found**:
- TODO comments: "Consider implementing compensation logic" (lines 457, 1146)
- When promotion usage tracking fails after payment, discount is applied but not recorded
- No automatic compensation or audit trail
- Financial inconsistency risk

**Tasks**:
- [ ] Create `PromotionUsageAudit` table/model for failed tracking attempts
- [ ] Create `IPromotionAuditService` interface
- [ ] Implement audit logging when promotion usage tracking fails
- [ ] Add compensation logic options:
  - Option A: Create audit record for manual reconciliation (recommended)
  - Option B: Mark transaction for review
  - Option C: Attempt to reverse discount (complex, requires payment service)
- [ ] Add background job to reconcile audit records
- [ ] Add admin endpoint to view and resolve audit records
- [ ] Add monitoring/alerting for promotion usage failures
- [ ] Add unit tests

**Implementation**:
```csharp
// Create: AmesaBackend.Lottery/Models/PromotionUsageAudit.cs
public class PromotionUsageAudit
{
    public Guid Id { get; set; }
    public Guid TransactionId { get; set; }
    public Guid UserId { get; set; }
    public string PromotionCode { get; set; }
    public decimal DiscountAmount { get; set; }
    public string Status { get; set; } // "Pending", "Resolved", "Reversed"
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
}

// Create: AmesaBackend.Lottery/Services/IPromotionAuditService.cs
public interface IPromotionAuditService
{
    Task CreateAuditRecordAsync(Guid transactionId, Guid userId, string promotionCode, decimal discountAmount);
    Task<List<PromotionUsageAudit>> GetPendingAuditsAsync();
    Task ResolveAuditAsync(Guid auditId, string resolutionNotes);
}

// Update TicketsController.cs and HousesController.cs
catch (Exception promoEx)
{
    _logger.LogError(promoEx, "CRITICAL: Failed to record promotion usage...");
    
    // Create audit record
    await _promotionAuditService.CreateAuditRecordAsync(
        paymentResult.TransactionId,
        userId,
        request.PromotionCode,
        discountAmount);
    
    // Optionally mark transaction for review
    // await _paymentService.MarkTransactionForReviewAsync(paymentResult.TransactionId);
}
```

---

## Phase 0.5: Missing Endpoints & Incomplete Implementations (Week 1 - Critical Blockers)

**Priority**: CRITICAL - Blocks core functionality  
**Status**: Identified in implementation completeness audit  
**Reference**: See `LOTTERY_IMPLEMENTATION_STATUS_REPORT.md` for detailed endpoint mapping

### 0.5.1 Payment Service - Refund Endpoint
**Priority**: CRITICAL  
**Effort**: 8 hours  
**Files**: `AmesaBackend.Payment/Controllers/PaymentController.cs`, `AmesaBackend.Payment/Services/PaymentService.cs`

**Issue**: 
- No refund endpoint exists in Payment Service
- `ReservationProcessor` and `LotteryTicketProductHandler` attempt refunds but endpoint doesn't exist
- Users lose money when ticket creation fails after payment

**Tasks**:
- [ ] Create `POST /api/v1/payments/refund` endpoint in PaymentController
- [ ] Add `RefundPaymentAsync` method to `IPaymentService` interface
- [ ] Implement refund logic in `PaymentService`
- [ ] Add refund request DTO (`RefundRequest`, `RefundResponse`)
- [ ] Add refund transaction tracking in database
- [ ] Update `PaymentProcessor.RefundPaymentAsync` to call new endpoint
- [ ] Add refund status tracking
- [ ] Implement idempotency for refund requests
- [ ] Add audit logging for refunds
- [ ] Add authorization (only user who made payment or admin can refund)
- [ ] Add validation (can't refund already refunded transactions)
- [ ] Add unit tests for refund endpoint
- [ ] Add integration tests for refund flow

**Implementation**:
```csharp
// Add to IPaymentService.cs
Task<RefundResponse> RefundPaymentAsync(Guid userId, RefundRequest request);

// Add to PaymentController.cs
[HttpPost("refund")]
[Authorize]
public async Task<ActionResult<ApiResponse<RefundResponse>>> RefundPayment([FromBody] RefundRequest request)
{
    if (!ControllerHelpers.TryGetUserId(User, out var userId))
    {
        return Unauthorized(new ApiResponse<RefundResponse> 
        { 
            Success = false, 
            Message = "Authentication required" 
        });
    }
    
    var response = await _paymentService.RefundPaymentAsync(userId, request);
    return Ok(new ApiResponse<RefundResponse> { Success = true, Data = response });
}

// RefundRequest DTO
public class RefundRequest
{
    public Guid TransactionId { get; set; }
    public string? Reason { get; set; }
    public decimal? PartialAmount { get; set; } // For partial refunds
}

// RefundResponse DTO
public class RefundResponse
{
    public Guid RefundId { get; set; }
    public Guid TransactionId { get; set; }
    public decimal RefundAmount { get; set; }
    public string Status { get; set; }
    public DateTime ProcessedAt { get; set; }
}
```

---

### 0.5.2 Lottery Service - Draw Participants Endpoint
**Priority**: CRITICAL  
**Effort**: 4 hours  
**Files**: `AmesaBackend.Lottery/Controllers/DrawsController.cs`, `AmesaBackend.Lottery/Services/LotteryService.cs`

**Issue**:
- Notification Service calls `/api/v1/draws/{id}/participants` but endpoint doesn't exist
- `LotteryServiceClient.GetDrawParticipantsAsync` fails
- Cannot notify all participants when draw completes

**Tasks**:
- [ ] Add `GET /api/v1/draws/{id}/participants` endpoint
- [ ] Add `GetDrawParticipantsAsync` method to `ILotteryService`
- [ ] Implement query to get all unique user IDs who have tickets for a draw
- [ ] Return list of `ParticipantDto` (UserId, TicketCount)
- [ ] Add service-to-service authentication
- [ ] Add caching for participant list
- [ ] Add unit tests
- [ ] Update `LotteryServiceClient` to verify endpoint works

**Implementation**:
```csharp
// Add to DrawsController.cs
[HttpGet("{id}/participants")]
[AllowAnonymous] // Service-to-service, protected by middleware
public async Task<ActionResult<ApiResponse<List<ParticipantDto>>>> GetDrawParticipants(Guid id)
{
    try
    {
        var participants = await _lotteryService.GetDrawParticipantsAsync(id);
        return Ok(new ApiResponse<List<ParticipantDto>> 
        { 
            Success = true, 
            Data = participants 
        });
    }
    catch (KeyNotFoundException ex)
    {
        return NotFound(new ApiResponse<List<ParticipantDto>> 
        { 
            Success = false, 
            Message = ex.Message 
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting draw participants");
        return StatusCode(500, new ApiResponse<List<ParticipantDto>> 
        { 
            Success = false, 
            Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred" } 
        });
    }
}

// Add to ILotteryService.cs
Task<List<ParticipantDto>> GetDrawParticipantsAsync(Guid drawId);

// Add to LotteryService.cs
public async Task<List<ParticipantDto>> GetDrawParticipantsAsync(Guid drawId)
{
    var draw = await _context.LotteryDraws
        .Include(d => d.Tickets)
        .FirstOrDefaultAsync(d => d.Id == drawId);
    
    if (draw == null)
        throw new KeyNotFoundException($"Draw {drawId} not found");
    
    var participants = draw.Tickets
        .GroupBy(t => t.UserId)
        .Select(g => new ParticipantDto
        {
            UserId = g.Key,
            TicketCount = g.Count()
        })
        .ToList();
    
    return participants;
}

// ParticipantDto (add to DTOs)
public class ParticipantDto
{
    public Guid UserId { get; set; }
    public int TicketCount { get; set; }
}
```

---

### 0.5.3 Lottery Service - House Favorites List Endpoint
**Priority**: High  
**Effort**: 3 hours  
**Files**: `AmesaBackend.Lottery/Controllers/HousesController.cs`, `AmesaBackend.Lottery/Services/LotteryService.cs`

**Issue**:
- `LotteryServiceClient.GetHouseFavoriteUserIdsAsync` calls `/api/v1/houses/{id}/favorites`
- This endpoint is for adding favorites (POST), not getting list
- Notification Service cannot get list of users who favorited a house

**Tasks**:
- [ ] Add `GET /api/v1/houses/{id}/favorites` endpoint (different from POST/DELETE)
- [ ] Add `GetHouseFavoriteUserIdsAsync` method to `ILotteryService`
- [ ] Return list of user IDs who have favorited the house
- [ ] Add service-to-service authentication
- [ ] Add pagination if needed (for houses with many favorites)
- [ ] Add unit tests
- [ ] Update `LotteryServiceClient` to use correct endpoint

**Implementation**:
```csharp
// Add to HousesController.cs
[HttpGet("{id}/favorites")]
[AllowAnonymous] // Service-to-service, protected by middleware
public async Task<ActionResult<ApiResponse<List<FavoriteUserDto>>>> GetHouseFavorites(Guid id)
{
    try
    {
        var favoriteUserIds = await _lotteryService.GetHouseFavoriteUserIdsAsync(id);
        var favorites = favoriteUserIds.Select(uid => new FavoriteUserDto { UserId = uid }).ToList();
        
        return Ok(new ApiResponse<List<FavoriteUserDto>> 
        { 
            Success = true, 
            Data = favorites 
        });
    }
    catch (KeyNotFoundException ex)
    {
        return NotFound(new ApiResponse<List<FavoriteUserDto>> 
        { 
            Success = false, 
            Message = ex.Message 
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting house favorites");
        return StatusCode(500, new ApiResponse<List<FavoriteUserDto>> 
        { 
            Success = false, 
            Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred" } 
        });
    }
}

// Add to ILotteryService.cs
Task<List<Guid>> GetHouseFavoriteUserIdsAsync(Guid houseId);

// Add to LotteryService.cs
public async Task<List<Guid>> GetHouseFavoriteUserIdsAsync(Guid houseId)
{
    var house = await _context.Houses.FindAsync(houseId);
    if (house == null)
        throw new KeyNotFoundException($"House {houseId} not found");
    
    var favoriteUserIds = await _context.UserWatchlists
        .Where(w => w.HouseId == houseId)
        .Select(w => w.UserId)
        .Distinct()
        .ToListAsync();
    
    return favoriteUserIds;
}

// FavoriteUserDto (add to DTOs)
public class FavoriteUserDto
{
    public Guid UserId { get; set; }
}
```

---

### 0.5.4 Notification Service - Complete EventBridgeEventHandler
**Priority**: High  
**Effort**: 6 hours  
**Files**: `AmesaBackend.Notification/Handlers/EventBridgeEventHandler.cs`

**Issue**:
- `EventBridgeEventHandler` is placeholder code
- Just polls every 30 seconds, doesn't actually consume events
- Events are consumed via webhook endpoint, but background service doesn't work

**Tasks**:
- [ ] Option A: Implement actual EventBridge event consumption (SQS queue as target)
- [ ] Option B: Document that webhook endpoint is used and remove placeholder
- [ ] If Option A: Set up EventBridge rules to send events to SQS queue
- [ ] If Option A: Implement SQS queue consumer in background service
- [ ] If Option A: Add retry logic and dead letter queue
- [ ] If Option B: Remove placeholder code and document webhook usage
- [ ] Add health checks for event consumption
- [ ] Add metrics for events processed

**Implementation (Option A - SQS Consumer)**:
```csharp
// Update EventBridgeEventHandler.cs
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    var queueUrl = _configuration["EventBridge:SqsQueueUrl"];
    if (string.IsNullOrEmpty(queueUrl))
    {
        _logger.LogError("EventBridge SQS queue URL not configured");
        return;
    }
    
    _logger.LogInformation("EventBridge event handler started, consuming from {QueueUrl}", queueUrl);
    
    while (!stoppingToken.IsCancellationRequested)
    {
        try
        {
            var receiveRequest = new ReceiveMessageRequest
            {
                QueueUrl = queueUrl,
                MaxNumberOfMessages = 10,
                WaitTimeSeconds = 20, // Long polling
                VisibilityTimeout = 60
            };
            
            var response = await _sqsClient.ReceiveMessageAsync(receiveRequest, stoppingToken);
            
            if (response.Messages?.Any() == true)
            {
                foreach (var message in response.Messages)
                {
                    await ProcessEventMessageAsync(message, stoppingToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consuming EventBridge events");
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}

private async Task ProcessEventMessageAsync(Message message, CancellationToken cancellationToken)
{
    try
    {
        var eventDetail = JsonSerializer.Deserialize<EventBridgeEventDetail>(message.Body, _jsonOptions);
        
        // Route to appropriate handler based on detail-type
        switch (eventDetail?.DetailType)
        {
            case "LotteryDrawWinnerSelected":
                await HandleLotteryDrawWinnerSelectedEvent(/* parse event */);
                break;
            case "TicketPurchased":
                await HandleTicketPurchasedEvent(/* parse event */);
                break;
            // ... other event types
        }
        
        // Delete message after successful processing
        await _sqsClient.DeleteMessageAsync(new DeleteMessageRequest
        {
            QueueUrl = _configuration["EventBridge:SqsQueueUrl"],
            ReceiptHandle = message.ReceiptHandle
        }, cancellationToken);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing event message {MessageId}", message.MessageId);
        // Message will become visible again after visibility timeout
    }
}
```

---

### 0.5.5 Notification Service - Complete NotificationService Methods
**Priority**: High  
**Effort**: 4 hours  
**Files**: `AmesaBackend.Notification/Services/NotificationService.cs`

**Issue**:
- `SendLotteryWinnerNotificationAsync` gets user data but doesn't parse/send email
- `SendLotteryEndedNotificationAsync` similar issue
- Methods incomplete, notifications don't work properly

**Tasks**:
- [ ] Complete `SendLotteryWinnerNotificationAsync` implementation
- [ ] Parse user data from Auth Service response properly
- [ ] Call `EmailService.SendLotteryWinnerNotificationAsync` with proper parameters
- [ ] Use `NotificationOrchestrator` for multi-channel delivery
- [ ] Complete `SendLotteryEndedNotificationAsync` implementation
- [ ] Add proper error handling
- [ ] Add unit tests
- [ ] Add integration tests

**Implementation**:
```csharp
// Update NotificationService.cs
public async Task SendLotteryWinnerNotificationAsync(Guid userId, string houseTitle, string ticketNumber)
{
    // Get user info from Auth Service
    var authServiceUrl = _configuration["Services:AuthService:Url"] ?? "http://auth-service:8080";
    var userResponse = await _httpRequest.GetRequest<ApiResponse<UserDto>>(
        $"{authServiceUrl}/api/v1/auth/users/{userId}", 
        "");
    
    if (userResponse?.Success == true && userResponse.Data != null)
    {
        var user = userResponse.Data;
        
        // Send email via EmailService
        await _emailService.SendLotteryWinnerNotificationAsync(
            user.Email,
            user.FirstName ?? "User",
            houseTitle,
            ticketNumber);
        
        // Also send via NotificationOrchestrator for multi-channel
        var orchestrator = _serviceProvider.GetRequiredService<INotificationOrchestrator>();
        await orchestrator.SendMultiChannelAsync(
            userId,
            new NotificationRequest
            {
                Type = "lottery_winner",
                Title = "🎉 Congratulations! You Won!",
                Message = $"You won the lottery for {houseTitle} with ticket {ticketNumber}!",
                Channel = "email"
            },
            new List<string> { "email", "webpush", "push" });
    }
    
    // Always create notification record
    await SendNotificationAsync(
        userId,
        "🎉 Congratulations! You Won!",
        $"You won the lottery for {houseTitle} with ticket {ticketNumber}!",
        "winner");
}
```

---

### 0.5.6 Lottery Service - House Participants List Endpoint
**Priority**: Medium  
**Effort**: 3 hours  
**Files**: `AmesaBackend.Lottery/Controllers/HousesController.cs`, `AmesaBackend.Lottery/Services/LotteryService.cs`

**Issue**:
- `LotteryServiceClient.GetHouseParticipantUserIdsAsync` calls `/api/v1/houses/{id}/participants`
- Current endpoint returns stats, not user list
- Notification Service needs list of participant user IDs

**Tasks**:
- [ ] Add query parameter to existing endpoint: `?list=true` OR
- [ ] Create new endpoint: `GET /api/v1/houses/{id}/participants/list`
- [ ] Return list of participant user IDs (not just count)
- [ ] Add service-to-service authentication
- [ ] Add pagination if needed
- [ ] Update `LotteryServiceClient` to use correct endpoint/parameter

**Implementation**:
```csharp
// Update HousesStatsController.cs or create new endpoint
[HttpGet("{id}/participants/list")]
[AllowAnonymous] // Service-to-service
public async Task<ActionResult<ApiResponse<List<ParticipantUserDto>>>> GetHouseParticipantUsers(Guid id)
{
    try
    {
        var participantUserIds = await _lotteryService.GetHouseParticipantUserIdsAsync(id);
        var participants = participantUserIds.Select(uid => new ParticipantUserDto { UserId = uid }).ToList();
        
        return Ok(new ApiResponse<List<ParticipantUserDto>> 
        { 
            Success = true, 
            Data = participants 
        });
    }
    catch (KeyNotFoundException ex)
    {
        return NotFound(new ApiResponse<List<ParticipantUserDto>> 
        { 
            Success = false, 
            Message = ex.Message 
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting house participant users");
        return StatusCode(500, new ApiResponse<List<ParticipantUserDto>> 
        { 
            Success = false, 
            Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred" } 
        });
    }
}

// Add to ILotteryService.cs
Task<List<Guid>> GetHouseParticipantUserIdsAsync(Guid houseId);

// Add to LotteryService.cs
public async Task<List<Guid>> GetHouseParticipantUserIdsAsync(Guid houseId)
{
    var house = await _context.Houses.FindAsync(houseId);
    if (house == null)
        throw new KeyNotFoundException($"House {houseId} not found");
    
    var participantUserIds = await _context.LotteryTickets
        .Where(t => t.HouseId == houseId && t.Status == "active")
        .Select(t => t.UserId)
        .Distinct()
        .ToListAsync();
    
    return participantUserIds;
}
```

---

## Phase 1: Critical Security Fixes (Week 1-2)

### 1.1 SQL Injection Prevention
**Priority**: High  
**Effort**: 2 hours  
**Files**: `AmesaBackend.Lottery/Services/PromotionService.cs`

**Tasks**:
- [ ] Replace raw SQL in `ApplyPromotionAsync` (line 385) with EF Core query
- [ ] Use `FromSqlRaw` with parameters or pure EF Core LINQ
- [ ] Add unit tests for SQL injection attempts
- [ ] Review all `ExecuteSqlRaw`/`ExecuteSqlInterpolated` usage

**Code Change**:
```csharp
// BEFORE (line 385):
await _context.Database.ExecuteSqlRawAsync(
    "SELECT 1 FROM amesa_admin.promotions WHERE UPPER(code) = {0} FOR UPDATE",
    promotionCodeUpper);

// AFTER:
var promotion = await _context.Promotions
    .Where(p => p.Code != null && p.Code.ToUpper() == promotionCodeUpper)
    .FirstOrDefaultAsync();
// Use row-level locking if needed
```

---

### 1.2 Service-to-Service Authentication Hardening
**Priority**: Critical  
**Effort**: 4 hours  
**Files**: `AmesaBackend.Lottery/Controllers/HousesController.cs`, `AmesaBackend.Lottery/Program.cs`

**Tasks**:
- [ ] Add explicit API key validation for `[AllowAnonymous]` endpoints
- [ ] Verify `ServiceToServiceAuthMiddleware` is properly configured
- [ ] Add request signing validation
- [ ] Implement IP whitelist for service-to-service calls
- [ ] Add audit logging for all service-to-service requests

**Endpoints to Secure**:
- `POST /api/v1/houses/{id}/tickets/validate` (line 791)
- `POST /api/v1/houses/{id}/tickets/create-from-payment` (line 836)

---

### 1.3 Error Message Sanitization
**Priority**: High  
**Effort**: 6 hours  
**Files**: All Controllers

**Tasks**:
- [ ] Create `ErrorResponseSanitizer` service
- [ ] Replace all `ex.Message` in error responses with sanitized messages
- [ ] Add environment-based error detail levels (dev vs prod)
- [ ] Log full exceptions server-side, return generic messages to clients
- [ ] Add unit tests for error sanitization

**Implementation**:
```csharp
// Create: AmesaBackend.Lottery/Services/ErrorSanitizer.cs
public class ErrorSanitizer
{
    public ErrorResponse Sanitize(Exception ex, bool isDevelopment)
    {
        if (isDevelopment)
            return new ErrorResponse { Message = ex.Message, Code = ex.GetType().Name };
        
        // Production: Generic messages only
        return ex switch
        {
            KeyNotFoundException => new ErrorResponse { Code = "NOT_FOUND", Message = "Resource not found" },
            UnauthorizedAccessException => new ErrorResponse { Code = "UNAUTHORIZED", Message = "Access denied" },
            _ => new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred" }
        };
    }
}
```

---

### 1.4 Input Validation Enhancement
**Priority**: Critical  
**Effort**: 8 hours  
**Files**: All Controllers, DTOs

**Tasks**:
- [ ] Add server-side validation for all request DTOs
- [ ] Validate `PaymentMethodId` ownership before use
- [ ] Validate `HouseId` existence before processing
- [ ] Add `Quantity` bounds checking (1-100, configurable)
- [ ] Sanitize `PromotionCode` input (trim, uppercase, validate format)
- [ ] Add custom validation attributes for business rules

**Validation Checklist**:
- [ ] `QuickEntryRequest`: HouseId exists, Quantity 1-10, PaymentMethodId owned by user
- [ ] `PurchaseTicketsRequest`: Same as above
- [ ] `CreateReservationRequest`: Quantity 1-100, HouseId exists
- [ ] `ValidatePromotionRequest`: Code format, UserId matches authenticated user
- [ ] `CreateTicketsFromPaymentRequest`: All IDs valid, Quantity reasonable

---

### 1.5 Rate Limiting Implementation
**Priority**: Critical  
**Effort**: 6 hours  
**Files**: `AmesaBackend.Lottery/Program.cs`, Controllers

**Tasks**:
- [ ] Add rate limiting middleware to all purchase endpoints
- [ ] Configure per-user limits (e.g., 10 purchases/hour)
- [ ] Configure per-IP limits (e.g., 50 requests/hour)
- [ ] Add rate limiting for promotion validation (prevent enumeration)
- [ ] Implement sliding window rate limiting
- [ ] Add rate limit headers to responses

**Endpoints to Protect**:
- `POST /api/v1/tickets/quick-entry`
- `POST /api/v1/houses/{id}/tickets/purchase`
- `POST /api/v1/promotions/validate`
- `POST /api/v1/reservations`

**Configuration**:
```csharp
// Add to Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("purchase", opt =>
    {
        opt.Window = TimeSpan.FromHours(1);
        opt.PermitLimit = 10;
        opt.QueueLimit = 0;
    });
});
```

---

### 1.6 Sensitive Data Logging Fix
**Priority**: Medium  
**Effort**: 4 hours  
**Files**: All Services, Controllers

**Tasks**:
- [ ] Implement structured logging with PII masking
- [ ] Create log sanitization utility
- [ ] Replace direct user ID logging with hashed versions
- [ ] Remove stack traces from production logs
- [ ] Add log level configuration per environment

**Implementation**:
```csharp
// Create: AmesaBackend.Lottery/Services/LogSanitizer.cs
public static class LogSanitizer
{
    public static string SanitizeUserId(Guid userId) => 
        $"USER_{userId.GetHashCode():X8}";
    
    public static string SanitizeTransactionId(Guid transactionId) => 
        $"TXN_{transactionId.GetHashCode():X8}";
}
```

---

## Phase 2: Critical Bug Fixes (Week 1-2)

### 2.1 Promotion Usage Tracking Fix
**Priority**: Critical  
**Effort**: 8 hours  
**Files**: `AmesaBackend.Lottery/Controllers/TicketsController.cs`, `AmesaBackend.Lottery/Controllers/HousesController.cs`

**Tasks**:
- [ ] Implement compensation transaction for promotion usage
- [ ] Add audit record when promotion usage fails
- [ ] Create background job to reconcile failed promotion usages
- [ ] Add database constraint to prevent duplicate promotion usage
- [ ] Implement idempotency check for promotion application

**Implementation Strategy**:
1. Wrap promotion application in transaction
2. If promotion application fails after payment:
   - Create audit record
   - Mark transaction for review
   - Send alert to operations team
   - Optionally: Reverse discount (complex, requires payment service integration)

**Code Location**: 
- `TicketsController.cs:430-461`
- `HousesController.cs:1117-1150`

---

### 2.2 Refund Mechanism Implementation
**Priority**: Critical  
**Effort**: 12 hours  
**Files**: `AmesaBackend.Payment/Services/ProductHandlers/LotteryTicketProductHandler.cs`, `AmesaBackend.Lottery/Services/Processors/PaymentProcessor.cs`

**Tasks**:
- [ ] Implement refund endpoint in Payment service
- [ ] Add refund call in `LotteryTicketProductHandler.ProcessPurchaseAsync`
- [ ] Add refund call when ticket creation fails
- [ ] Implement retry logic for refund failures
- [ ] Add refund status tracking
- [ ] Create compensation transaction pattern

**Implementation**:
```csharp
// In LotteryTicketProductHandler.ProcessPurchaseAsync (line 216)
catch (Exception ex)
{
    _logger.LogError(ex, "Error creating lottery tickets for transaction {TransactionId}", transactionId);
    
    // Attempt refund
    try
    {
        var refundService = serviceProvider.GetService<IRefundService>();
        if (refundService != null)
        {
            await refundService.RefundAsync(transactionId, amount, "Ticket creation failed");
            _logger.LogInformation("Refunded transaction {TransactionId} after ticket creation failure", transactionId);
        }
    }
    catch (Exception refundEx)
    {
        _logger.LogError(refundEx, "CRITICAL: Failed to refund transaction {TransactionId}", transactionId);
        // Alert operations team
    }
    
    throw;
}
```

---

### 2.3 Thread-Safe Ticket Number Generation
**Priority**: High  
**Effort**: 4 hours  
**Files**: `AmesaBackend.Lottery/Services/LotteryService.cs`

**Tasks**:
- [ ] Replace max query with database sequence
- [ ] Use `SELECT nextval('ticket_number_seq')` for atomic increment
- [ ] Create database sequence per house
- [ ] Add unique constraint on ticket number
- [ ] Add retry logic for duplicate ticket number conflicts

**Implementation**:
```sql
-- Migration: Create sequence per house
CREATE SEQUENCE IF NOT EXISTS ticket_number_seq_{houseId} START 1;

-- In LotteryService.GetNextTicketNumberAsync:
var sql = $"SELECT nextval('ticket_number_seq_{houseId}')";
var nextNumber = await _context.Database.SqlQueryRaw<int>(sql).FirstOrDefaultAsync();
```

---

### 2.4 Cryptographically Secure Random for Draws
**Priority**: High  
**Effort**: 3 hours  
**Files**: `AmesaBackend.Lottery/Services/LotteryService.cs`

**Tasks**:
- [ ] Replace `Random` with `RandomNumberGenerator` (cryptographically secure)
- [ ] Use proper seed generation (cryptographic hash)
- [ ] Add draw seed to audit log
- [ ] Implement verifiable random selection algorithm

**Implementation**:
```csharp
// Replace line 239-243
private Random CreateSeededRandom(string seed)
{
    // Use cryptographic hash for seed
    using var sha256 = SHA256.Create();
    var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(seed));
    var seedInt = BitConverter.ToInt32(hash, 0);
    return new Random(seedInt);
}

// Better: Use RandomNumberGenerator
private int SelectRandomIndex(int count, string seed)
{
    using var rng = RandomNumberGenerator.Create();
    var bytes = new byte[4];
    rng.GetBytes(bytes);
    var randomValue = BitConverter.ToUInt32(bytes, 0);
    return (int)(randomValue % (uint)count);
}
```

---

### 2.5 Race Condition Fixes
**Priority**: High  
**Effort**: 6 hours  
**Files**: `AmesaBackend.Lottery/Services/LotteryService.cs`

**Tasks**:
- [ ] Review all transaction isolation levels
- [ ] Ensure participant cap check is atomic with ticket creation
- [ ] Add database-level constraints (check constraints, triggers)
- [ ] Implement optimistic concurrency control where needed
- [ ] Add retry logic for concurrency conflicts

**Areas to Fix**:
- `CreateTicketsFromPaymentAsync`: Double-check participant cap
- `CanUserEnterLotteryAsync`: Ensure atomic check
- `CreateReservationAsync`: Inventory reservation race condition

---

## Phase 3: Missing Integrations (Week 2)

### 3.1 Event Handlers for Notifications
**Priority**: Critical  
**Effort**: 12 hours  
**Files**: `AmesaBackend.Notification/Services/`, New Event Handlers

**Tasks**:
- [ ] Create `LotteryDrawWinnerSelectedEventHandler`
- [ ] Create `LotteryDrawCompletedEventHandler`
- [ ] Create `TicketPurchasedEventHandler` for real-time updates
- [ ] Register notification types in database
- [ ] Implement email templates for lottery events
- [ ] Add SignalR hub updates for real-time notifications

**Implementation**:
```csharp
// Create: AmesaBackend.Notification/Handlers/LotteryDrawWinnerSelectedEventHandler.cs
public class LotteryDrawWinnerSelectedEventHandler : IEventHandler<LotteryDrawWinnerSelectedEvent>
{
    private readonly INotificationOrchestrator _orchestrator;
    
    public async Task HandleAsync(LotteryDrawWinnerSelectedEvent evt)
    {
        await _orchestrator.SendMultiChannelAsync(
            evt.WinnerUserId,
            new NotificationRequest
            {
                Type = "lottery_winner",
                Title = "🎉 Congratulations! You Won!",
                Message = $"You won the lottery for {evt.HouseTitle} with ticket #{evt.WinningTicketNumber}!",
                Data = new Dictionary<string, object>
                {
                    ["HouseId"] = evt.HouseId,
                    ["TicketNumber"] = evt.WinningTicketNumber,
                    ["PrizeValue"] = evt.PrizeValue ?? 0
                }
            },
            new List<string> { "email", "push", "webpush" }
        );
    }
}
```

---

### 3.2 Complete Notification Service Implementation
**Priority**: High  
**Effort**: 8 hours  
**Files**: `AmesaBackend.Notification/Services/NotificationService.cs`

**Tasks**:
- [ ] Complete `SendLotteryWinnerNotificationAsync` implementation
- [ ] Complete `SendLotteryEndedNotificationAsync` implementation
- [ ] Add email template rendering
- [ ] Integrate with `NotificationOrchestrator`
- [ ] Add notification type registration script

---

### 3.2.1 Notification Service - Password Reset Email
**Priority**: Medium  
**Effort**: 2 hours  
**Files**: `AmesaBackend.Notification/Services/IEmailService.cs`, `AmesaBackend.Notification/Services/EmailService.cs`, `AmesaBackend.Notification/Handlers/EventBridgeEventHandler.cs`

**Issues Found**:
- TODO comment: "Implement SendPasswordResetEmailAsync method" (line 116)
- `HandlePasswordResetRequestedEvent` doesn't actually send email
- Password reset flow incomplete

**Tasks**:
- [ ] Add `SendPasswordResetEmailAsync` to `IEmailService` interface
- [ ] Implement method in `EmailService`
- [ ] Create password reset email template
- [ ] Update `EventBridgeEventHandler.HandlePasswordResetRequestedEvent` to call new method
- [ ] Add unit tests
- [ ] Test password reset flow end-to-end

**Implementation**:
```csharp
// Add to IEmailService.cs
Task SendPasswordResetEmailAsync(string email, string resetToken);

// Add to EmailService.cs
public async Task SendPasswordResetEmailAsync(string email, string resetToken)
{
    var resetLink = $"{_configuration["FrontendUrl"]}/reset-password?token={resetToken}";
    var subject = "Reset Your Password";
    var body = $@"
        <h2>Password Reset Request</h2>
        <p>You requested to reset your password. Click the link below to reset it:</p>
        <p><a href=""{resetLink}"">Reset Password</a></p>
        <p>This link will expire in 1 hour.</p>
        <p>If you didn't request this, please ignore this email.</p>
    ";
    
    await SendEmailAsync(email, subject, body);
}

// Update EventBridgeEventHandler.cs
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
```

---

### 3.2.2 Notification Service - Push Channel Device Token Management
**Priority**: Medium  
**Effort**: 4 hours  
**Files**: `AmesaBackend.Notification/Services/Channels/PushChannelProvider.cs`

**Issues Found**:
- TODO comment: "Get device tokens from user profile or device registration table" (line 63)
- Push notifications not implemented
- Device token management missing

**Tasks**:
- [ ] Create `DeviceRegistration` model/table
- [ ] Create `IDeviceRegistrationService` interface
- [ ] Implement device registration endpoints (register, unregister, update)
- [ ] Update `PushChannelProvider` to fetch device tokens from database
- [ ] Implement push notification sending (FCM/APNS)
- [ ] Add device token validation
- [ ] Add cleanup job for expired/invalid tokens
- [ ] Add unit tests

**Implementation**:
```csharp
// Create: AmesaBackend.Notification/Models/DeviceRegistration.cs
public class DeviceRegistration
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DeviceToken { get; set; }
    public string Platform { get; set; } // "ios", "android", "web"
    public DateTime RegisteredAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public bool IsActive { get; set; }
}

// Create: AmesaBackend.Notification/Services/IDeviceRegistrationService.cs
public interface IDeviceRegistrationService
{
    Task RegisterDeviceAsync(Guid userId, string deviceToken, string platform);
    Task UnregisterDeviceAsync(Guid userId, string deviceToken);
    Task<List<string>> GetActiveDeviceTokensAsync(Guid userId);
}

// Update PushChannelProvider.cs
public async Task<DeliveryResult> SendAsync(NotificationRequest request)
{
    try
    {
        var deviceService = _serviceProvider.GetRequiredService<IDeviceRegistrationService>();
        var deviceTokens = await deviceService.GetActiveDeviceTokensAsync(request.UserId);
        
        if (deviceTokens == null || !deviceTokens.Any())
        {
            return new DeliveryResult
            {
                Success = false,
                ErrorMessage = "No active device tokens found for user"
            };
        }
        
        // Send push notifications to all devices
        foreach (var token in deviceTokens)
        {
            await SendPushNotificationAsync(token, request);
        }
        
        return new DeliveryResult { Success = true };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error sending push notification");
        return new DeliveryResult { Success = false, ErrorMessage = ex.Message };
    }
}
```

---

### 3.3 Payment Service Integration Improvements
**Priority**: High  
**Effort**: 10 hours  
**Files**: Payment Service, Lottery Service

**Tasks**:
- [ ] Implement refund endpoint in Payment service
- [ ] Add payment status webhook handling
- [ ] Implement transaction reconciliation job
- [ ] Add payment method validation endpoint
- [ ] Implement payment retry mechanism

---

### 3.4 Auth Service Integration Refactoring
**Priority**: High  
**Effort**: 10 hours  
**Files**: `AmesaBackend.Lottery/Services/LotteryService.cs`

**Tasks**:
- [ ] Replace direct `AuthDbContext` access with HTTP calls
- [ ] Create `IAuthServiceClient` interface
- [ ] Implement circuit breaker for Auth service calls
- [ ] Add retry policy with exponential backoff
- [ ] Implement caching for user verification status
- [ ] Add fallback behavior when Auth service unavailable

**Implementation**:
```csharp
// Create: AmesaBackend.Lottery/Services/AuthServiceClient.cs
public interface IAuthServiceClient
{
    Task<bool> IsUserVerifiedAsync(Guid userId);
    Task<bool> UserExistsAsync(Guid userId);
}

// Replace CheckVerificationRequirementAsync to use HTTP client
public async Task CheckVerificationRequirementAsync(Guid userId)
{
    try
    {
        var isVerified = await _authServiceClient.IsUserVerifiedAsync(userId);
        if (!isVerified)
        {
            throw new UnauthorizedAccessException("ID_VERIFICATION_REQUIRED");
        }
    }
    catch (HttpRequestException ex)
    {
        _logger.LogError(ex, "Auth service unavailable");
        throw new UnauthorizedAccessException("ID_VERIFICATION_REQUIRED: Service unavailable");
    }
}
```

---

### 3.5 Promotion Service Schema Alignment
**Priority**: Medium  
**Effort**: 6 hours  
**Files**: `AmesaBackend.Lottery/Services/PromotionService.cs`

**Tasks**:
- [ ] Evaluate moving promotions to Lottery schema
- [ ] OR: Create shared promotion service
- [ ] OR: Implement proper cross-schema transaction handling
- [ ] Add data consistency checks
- [ ] Implement eventual consistency pattern if needed

---

## Phase 4: Data Consistency Fixes (Week 2-3)

### 4.1 Distributed Transaction Coordination
**Priority**: Critical  
**Effort**: 16 hours  
**Files**: Payment Service, Lottery Service

**Tasks**:
- [ ] Implement Saga pattern for payment + ticket creation
- [ ] OR: Implement compensation transaction pattern
- [ ] Add transaction state machine
- [ ] Implement idempotency keys
- [ ] Add transaction reconciliation job
- [ ] Create dead letter queue for failed transactions

**Saga Pattern Implementation**:
1. Payment Initiated → Payment Pending
2. Payment Completed → Create Tickets
3. Tickets Created → Transaction Complete
4. If any step fails → Compensate previous steps

---

### 4.2 Inventory Sync Consistency
**Priority**: High  
**Effort**: 8 hours  
**Files**: `AmesaBackend.Lottery/Services/InventorySyncService.cs`

**Tasks**:
- [ ] Implement periodic inventory reconciliation
- [ ] Add inventory drift detection
- [ ] Implement automatic correction for minor drifts
- [ ] Add alerting for significant drifts
- [ ] Add inventory audit log

---

### 4.3 Participant Cap Atomic Operations
**Priority**: High  
**Effort**: 6 hours  
**Files**: `AmesaBackend.Lottery/Services/LotteryService.cs`

**Tasks**:
- [ ] Add database-level check constraint for participant cap
- [ ] Implement row-level locking for cap checks
- [ ] Add retry logic for concurrency conflicts
- [ ] Add monitoring for cap violations

---

## Phase 5: Business Logic Fixes (Week 3)

### 5.1 Promotion Validation Timing
**Priority**: Medium  
**Effort**: 4 hours  
**Files**: `AmesaBackend.Lottery/Controllers/TicketsController.cs`

**Tasks**:
- [ ] Re-validate promotion after payment succeeds
- [ ] Add promotion lock mechanism
- [ ] Implement promotion reservation system
- [ ] Add promotion expiry check before application

---

### 5.2 House Status Validation
**Priority**: Medium  
**Effort**: 3 hours  
**Files**: All purchase endpoints

**Tasks**:
- [ ] Add house status check in `ValidateTicketsAsync`
- [ ] Add house status check in `CreateTicketsFromPaymentAsync`
- [ ] Add lottery end date validation
- [ ] Add lottery start date validation

---

### 5.3 Business Rule Validations
**Priority**: Medium  
**Effort**: 8 hours  
**Files**: Services, Controllers

**Tasks**:
- [ ] Add minimum ticket purchase validation
- [ ] Add maximum ticket purchase per user validation
- [ ] Add maximum ticket purchase per transaction validation
- [ ] Add user account status check (suspended/banned)
- [ ] Add house availability window validation

---

## Phase 6: Validation & Error Handling (Week 3-4)

### 6.1 Comprehensive Input Validation
**Priority**: High  
**Effort**: 10 hours  
**Files**: All DTOs, Controllers

**Tasks**:
- [ ] Add FluentValidation or similar validation framework
- [ ] Create custom validation attributes
- [ ] Add cross-field validation
- [ ] Add async validation for external service checks
- [ ] Add validation error response formatting

---

### 6.2 Error Handling Improvements
**Priority**: Medium  
**Effort**: 8 hours  
**Files**: All Services, Controllers

**Tasks**:
- [ ] Create specific exception types
- [ ] Implement global exception handler
- [ ] Add retry policies for transient failures
- [ ] Implement circuit breakers
- [ ] Add error correlation IDs

---

### 6.3 Compensation Logic Implementation
**Priority**: Critical  
**Effort**: 12 hours  
**Files**: Payment Service, Lottery Service

**Tasks**:
- [ ] Implement compensation transaction pattern
- [ ] Add compensation state tracking
- [ ] Create compensation reconciliation job
- [ ] Add manual compensation override
- [ ] Implement compensation retry mechanism

---

## Phase 7: Performance & Scalability (Week 4)

### 7.1 Database Indexing
**Priority**: Medium  
**Effort**: 4 hours  
**Files**: Migrations

**Tasks**:
- [ ] Add index on `lottery_tickets(user_id, status)`
- [ ] Add index on `lottery_tickets(house_id, status, purchase_date)`
- [ ] Add index on `ticket_reservations(expires_at, status)`
- [ ] Add index on `lottery_tickets(payment_id)` for idempotency checks
- [ ] Analyze query performance and add missing indexes

**Migration**:
```sql
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_lottery_tickets_user_status 
ON lottery_tickets(user_id, status) WHERE status = 'Active';

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_lottery_tickets_house_status_date 
ON lottery_tickets(house_id, status, purchase_date);

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_ticket_reservations_expires_status 
ON ticket_reservations(expires_at, status) WHERE status = 'pending';
```

---

### 7.2 Query Optimization
**Priority**: Low  
**Effort**: 6 hours  
**Files**: Services

**Tasks**:
- [ ] Fix N+1 query problems
- [ ] Add query result caching
- [ ] Optimize eager loading
- [ ] Add query performance monitoring

---

### 7.3 Cache Optimization
**Priority**: Low  
**Effort**: 4 hours  
**Files**: Controllers, Services

**Tasks**:
- [ ] Replace regex-based cache invalidation with specific keys
- [ ] Implement cache warming
- [ ] Add cache hit/miss monitoring
- [ ] Optimize cache TTL values

---

## Phase 8: Audit & Compliance (Week 4-5)

### 8.1 Audit Trail Implementation
**Priority**: High  
**Effort**: 12 hours  
**Files**: New Audit Service

**Tasks**:
- [ ] Create audit logging service
- [ ] Log all ticket purchases
- [ ] Log all promotion applications
- [ ] Log all draw executions
- [ ] Log all winner selections
- [ ] Add audit log querying API

**Implementation**:
```csharp
// Create: AmesaBackend.Lottery/Services/AuditService.cs
public interface IAuditService
{
    Task LogTicketPurchaseAsync(Guid userId, Guid houseId, int quantity, Guid transactionId);
    Task LogPromotionApplicationAsync(Guid userId, Guid promotionId, Guid transactionId, decimal discount);
    Task LogDrawExecutionAsync(Guid drawId, Guid houseId, Guid? winnerId);
}
```

---

### 8.2 Transaction Logging
**Priority**: High  
**Effort**: 8 hours  
**Files**: Payment Service, Lottery Service

**Tasks**:
- [ ] Log all financial transactions
- [ ] Add transaction reconciliation
- [ ] Implement transaction audit trail
- [ ] Add compliance reporting

---

### 8.3 Data Retention Policy
**Priority**: Medium  
**Effort**: 6 hours  
**Files**: Background Services

**Tasks**:
- [ ] Create data retention background service
- [ ] Archive expired reservations
- [ ] Archive old tickets (after draw completion + retention period)
- [ ] Archive old promotion usage history
- [ ] Implement data deletion policies (GDPR compliance)

---

## Phase 9: Security Hardening (Week 5)

### 9.1 API Security Enhancements
**Priority**: High  
**Effort**: 8 hours  
**Files**: `AmesaBackend.Lottery/Program.cs`, Middleware

**Tasks**:
- [ ] Implement proper CORS configuration
- [ ] Add request size limits
- [ ] Implement request signing for service-to-service calls
- [ ] Add API versioning
- [ ] Implement proper secret management (AWS Secrets Manager)

---

### 9.2 Security Headers
**Priority**: Medium  
**Effort**: 2 hours  
**Files**: Middleware

**Tasks**:
- [ ] Verify CSP headers
- [ ] Verify HSTS headers
- [ ] Add X-Content-Type-Options
- [ ] Add X-Frame-Options
- [ ] Add Referrer-Policy

---

### 9.3 Fraud Detection
**Priority**: Medium  
**Effort**: 12 hours  
**Files**: New Fraud Detection Service

**Tasks**:
- [ ] Implement velocity checks
- [ ] Add device fingerprinting
- [ ] Implement behavioral analysis
- [ ] Add suspicious activity detection
- [ ] Create fraud alert system

---

## Testing Requirements

### Unit Tests
- [ ] All service methods
- [ ] All validation logic
- [ ] All error handling paths
- [ ] All edge cases

### Integration Tests
- [ ] Payment + Ticket creation flow
- [ ] Promotion application flow
- [ ] Draw execution flow
- [ ] Notification delivery
- [ ] Service-to-service communication

### Security Tests
- [ ] SQL injection attempts
- [ ] Authorization bypass attempts
- [ ] Rate limiting effectiveness
- [ ] Input validation bypass attempts
- [ ] XSS attempts

### Performance Tests
- [ ] Load testing for purchase endpoints
- [ ] Concurrent ticket creation
- [ ] Database query performance
- [ ] Cache effectiveness

---

## Monitoring & Alerting

### Metrics to Add
- [ ] Ticket purchase success/failure rate
- [ ] Payment processing latency
- [ ] Promotion application success rate
- [ ] Draw execution time
- [ ] Notification delivery rate
- [ ] Error rates by type
- [ ] Rate limit violations

### Alerts to Configure
- [ ] High error rate (>5%)
- [ ] Payment failures
- [ ] Ticket creation failures
- [ ] Promotion usage tracking failures
- [ ] Refund failures
- [ ] Service-to-service communication failures
- [ ] Database connection issues
- [ ] High latency (>2s for critical endpoints)

---

## Documentation Requirements

- [ ] API documentation updates
- [ ] Architecture diagram updates
- [ ] Security documentation
- [ ] Runbook for critical operations
- [ ] Incident response procedures
- [ ] Data flow diagrams

---

## Rollout Plan

### Week 1 (Immediate - Critical)
- Phase 0: Additional critical fixes (TicketCreatorProcessor, RedisInventoryManager, etc.)
  - **0.7: LotteryDrawService event publishing** (2 hours)
  - **0.8: Promotion usage compensation logic** (6 hours)
- **Phase 0.5: Missing endpoints & incomplete implementations** (Refund endpoint, Draw participants, etc.)
- Critical security fixes
- Critical bug fixes (refund, promotion tracking)

### Week 2
- Missing integrations (event handlers, notifications)
  - **3.2.1: Password reset email** (2 hours)
  - **3.2.2: Push channel device token management** (4 hours)
- Data consistency fixes
- Race condition fixes

### Week 3
- Business logic fixes
- Validation improvements
- Query performance optimizations

### Week 4
- Performance optimizations
- Audit trail implementation
- Monitoring & observability

### Week 5
- Security hardening
- Final testing and documentation
- Integration testing

### Week 6
- Load testing
- Security testing
- Documentation completion

---

## Success Criteria

- [ ] Zero critical security vulnerabilities
- [ ] Zero critical bugs
- [ ] All integrations functional
- [ ] 99.9% transaction success rate
- [ ] <500ms average response time for critical endpoints
- [ ] 100% test coverage for critical paths
- [ ] All audit requirements met
- [ ] All compliance requirements met

---

## Risk Mitigation

### High-Risk Changes
1. **Refund Implementation**: Test thoroughly in staging, have rollback plan
2. **Auth Service Refactoring**: Implement circuit breaker, have fallback
3. **Distributed Transactions**: Test failure scenarios extensively
4. **Database Schema Changes**: Use migrations, test rollback

### Rollback Procedures
- Document rollback steps for each phase
- Maintain feature flags for new functionality
- Keep database migration rollback scripts
- Maintain API versioning for breaking changes

---

## Notes

- All changes should be reviewed by security team
- All database changes require migration scripts
- All API changes require versioning
- All breaking changes require deprecation notices
- All new dependencies require security review

---

**Last Updated**: 2024-12-19  
**Next Review**: After Phase 0.5 completion (Missing Endpoints)  
**Implementation Completeness**: ~60% (see `LOTTERY_IMPLEMENTATION_STATUS_REPORT.md`)

---

## Additional Findings Reference

### Audit Documents

1. **`LOTTERY_COMPONENT_AUDIT_ADDITIONAL_FINDINGS.md`**
   - Detailed analysis of 25+ additional issues discovered in recursive audit
   - Includes: TicketCreatorProcessor, RedisInventoryManager, race conditions, performance issues, etc.

2. **`LOTTERY_IMPLEMENTATION_STATUS_REPORT.md`**
   - Endpoint completeness check
   - Service-to-service integration status
   - Missing endpoints and incomplete implementations
   - Implementation completeness score: ~60%

3. **`LOTTERY_AUDIT_SUMMARY.md`**
   - Executive summary of all findings
   - Quick stats and top 10 critical issues
   - Risk assessment and success metrics

### Key Findings Summary

**Additional Issues (Recursive Audit)**:
- TicketCreatorProcessor missing validations
- RedisInventoryManager fail-open security issues
- ReservationProcessor refund handling gaps
- Ticket number generation race conditions
- Background service race conditions
- Query performance issues
- Missing database constraints
- And 18+ more issues

**Missing Endpoints & Incomplete Implementations**:
- ❌ Payment Service: No refund endpoint (CRITICAL)
- ❌ Lottery Service: Missing draw participants endpoint
- ❌ Lottery Service: Missing house favorites list endpoint
- ⚠️ Notification Service: EventBridgeEventHandler is placeholder
- ⚠️ Notification Service: Incomplete notification methods

**Total Issues Identified**: 75+ across all audits

**Additional TODO Items Added**:
- LotteryDrawService event publishing (Phase 0.7)
- Promotion usage compensation logic (Phase 0.8)
- Password reset email implementation (Phase 3.2.1)
- Push channel device token management (Phase 3.2.2)

**Total Additional Effort**: 14 hours (~2 days)






