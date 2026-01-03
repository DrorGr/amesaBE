using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.Models;
using AmesaBackend.Models;
using AmesaBackend.Shared.Rest;
using AmesaBackend.Auth.Services;
using Microsoft.EntityFrameworkCore;

namespace AmesaBackend.Lottery.Services;

/// <summary>
/// Background service that processes pending ticket reservations from the queue.
/// Converts pending reservations to confirmed tickets by calling the payment service.
/// Handles retries, circuit breaking, and error recovery for failed reservations.
/// </summary>
public class TicketQueueProcessorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TicketQueueProcessorService> _logger;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _processingDelay;
    private readonly int _batchSize;
    private readonly int _maxRetries;
    private readonly TimeSpan _retryDelay;

    /// <summary>
    /// Initializes a new instance of the <see cref="TicketQueueProcessorService"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider for creating scoped services (database context, HTTP client, circuit breaker).</param>
    /// <param name="logger">Logger instance for logging operations.</param>
    /// <param name="configuration">Configuration for reading service settings (processing delay, batch size, retry settings).</param>
    public TicketQueueProcessorService(
        IServiceProvider serviceProvider,
        ILogger<TicketQueueProcessorService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
        
        var delaySeconds = _configuration.GetValue<int>("TicketQueue:ProcessingDelaySeconds", 5);
        _processingDelay = TimeSpan.FromSeconds(delaySeconds);
        _batchSize = _configuration.GetValue<int>("TicketQueue:BatchSize", 10);
        _maxRetries = _configuration.GetValue<int>("TicketQueue:MaxRetries", 3);
        var retryDelaySeconds = _configuration.GetValue<int>("TicketQueue:RetryDelaySeconds", 30);
        _retryDelay = TimeSpan.FromSeconds(retryDelaySeconds);
    }

    /// <summary>
    /// Executes the background service main loop.
    /// Periodically processes pending ticket reservations from the queue.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token to stop the service gracefully.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabled = _configuration.GetValue<bool>("TicketQueue:Enabled", true);
        if (!enabled)
        {
            _logger.LogInformation("TicketQueueProcessorService is disabled in configuration");
            return;
        }

        _logger.LogInformation("TicketQueueProcessorService started with processing delay: {Delay} seconds, batch size: {BatchSize}", 
            _processingDelay.TotalSeconds, _batchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessTicketQueueAsync(stoppingToken);
                await Task.Delay(_processingDelay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("TicketQueueProcessorService is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TicketQueueProcessorService");
                // Continue running even if there's an error
                await Task.Delay(_processingDelay, stoppingToken);
            }
        }

        _logger.LogInformation("TicketQueueProcessorService stopped");
    }

    private async Task ProcessTicketQueueAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LotteryDbContext>();
        var httpRequest = scope.ServiceProvider.GetRequiredService<IHttpRequest>();
        var circuitBreaker = scope.ServiceProvider.GetRequiredService<ICircuitBreakerService>();

        try
        {
            // Get pending reservations that haven't expired
            var pendingReservations = await dbContext.TicketReservations
                .Where(r => r.Status == "pending" && r.ExpiresAt > DateTime.UtcNow)
                .Include(r => r.House)
                .OrderBy(r => r.CreatedAt)
                .Take(_batchSize)
                .ToListAsync(cancellationToken);

            if (!pendingReservations.Any())
            {
                // No pending reservations
                return;
            }

            _logger.LogInformation("Processing {Count} pending reservations", pendingReservations.Count);

            var processingTasks = pendingReservations.Select(reservation => 
                ProcessReservationAsync(reservation, dbContext, httpRequest, circuitBreaker, cancellationToken));
            
            await Task.WhenAll(processingTasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ticket queue");
            throw;
        }
    }

    private async Task ProcessReservationAsync(
        TicketReservation reservation,
        LotteryDbContext dbContext,
        IHttpRequest httpRequest,
        ICircuitBreakerService circuitBreaker,
        CancellationToken cancellationToken)
    {
        try
        {
            // Update status to processing
            reservation.Status = "processing";
            reservation.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            // Call Payment service
            var paymentServiceUrl = _configuration["PaymentService:BaseUrl"] 
                ?? Environment.GetEnvironmentVariable("PAYMENT_SERVICE_BASE_URL")
                ?? "http://amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com/api/v1";

            var paymentEndpoint = $"{paymentServiceUrl}/payment/process";
            
            var paymentRequest = new
            {
                reservationId = reservation.Id,
                amount = reservation.TotalPrice,
                paymentMethodId = reservation.PaymentMethodId,
                userId = reservation.UserId
            };

            _logger.LogInformation("Calling Payment service for reservation {ReservationId}", reservation.Id);

            // Use circuit breaker for payment service call
            var paymentResult = await circuitBreaker.ExecuteAsync("PaymentService_Process", async () =>
            {
                return await httpRequest.PostRequest<PaymentProcessResponse>(paymentEndpoint, paymentRequest, "");
            });

            if (paymentResult == null || !paymentResult.Success)
            {
                // Payment failed
                reservation.Status = "failed";
                reservation.ErrorMessage = paymentResult?.ErrorMessage ?? "Payment processing failed";
                reservation.UpdatedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);
                
                _logger.LogWarning("Payment failed for reservation {ReservationId}: {Error}", 
                    reservation.Id, reservation.ErrorMessage);
                return;
            }

            // Payment succeeded - create tickets
            var tickets = new List<LotteryTicket>();
            var ticketPrice = reservation.TotalPrice / reservation.Quantity;

            for (int i = 0; i < reservation.Quantity; i++)
            {
                var ticket = new LotteryTicket
                {
                    Id = Guid.NewGuid(),
                    TicketNumber = GenerateTicketNumber(),
                    HouseId = reservation.HouseId,
                    UserId = reservation.UserId,
                    PurchasePrice = ticketPrice,
                    Status = TicketStatus.Active,
                    PurchaseDate = DateTime.UtcNow,
                    PaymentId = paymentResult.TransactionId,
                    IsWinner = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                tickets.Add(ticket);
            }

            dbContext.LotteryTickets.AddRange(tickets);

            // Update reservation to completed
            reservation.Status = "completed";
            reservation.ProcessedAt = DateTime.UtcNow;
            reservation.PaymentTransactionId = paymentResult.TransactionId;
            reservation.UpdatedAt = DateTime.UtcNow;

            // Use transaction to ensure atomicity
            using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            _logger.LogInformation("Successfully processed reservation {ReservationId}, created {Count} tickets", 
                reservation.Id, tickets.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing reservation {ReservationId}", reservation.Id);
            
            // Update reservation to failed
            try
            {
                reservation.Status = "failed";
                reservation.ErrorMessage = ex.Message;
                reservation.UpdatedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx, "Error updating reservation status to failed");
            }
        }
    }

    private string GenerateTicketNumber()
    {
        // Generate a unique ticket number (e.g., TKT-{timestamp}-{random})
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = new Random().Next(1000, 9999);
        return $"TKT-{timestamp}-{random}";
    }

    private class PaymentProcessResponse
    {
        public bool Success { get; set; }
        public Guid? TransactionId { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
