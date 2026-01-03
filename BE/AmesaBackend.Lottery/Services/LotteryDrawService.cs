using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AmesaBackend.Lottery.Hubs;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Models;
using AmesaBackend.Shared.Events;
using Microsoft.EntityFrameworkCore;

namespace AmesaBackend.Lottery.Services;

/// <summary>
/// Background service that periodically checks for scheduled lottery draws and executes them.
/// Monitors houses with scheduled draw dates and automatically conducts draws when due.
/// Broadcasts draw events via SignalR for real-time client updates.
/// </summary>
public class LotteryDrawService : BackgroundService
{
    private readonly IHubContext<LotteryHub> _hubContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LotteryDrawService> _logger;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _checkInterval;
    private readonly TimeSpan _drawExecutionTimeout;

    /// <summary>
    /// Initializes a new instance of the <see cref="LotteryDrawService"/> class.
    /// </summary>
    /// <param name="hubContext">SignalR hub context for broadcasting draw events to connected clients.</param>
    /// <param name="serviceProvider">Service provider for creating scoped services (database context, event publisher).</param>
    /// <param name="logger">Logger instance for logging operations.</param>
    /// <param name="configuration">Configuration for reading service settings (check interval, timeout).</param>
    public LotteryDrawService(
        IHubContext<LotteryHub> hubContext,
        IServiceProvider serviceProvider,
        ILogger<LotteryDrawService> logger,
        IConfiguration configuration)
    {
        _hubContext = hubContext;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
        
        var checkIntervalMinutes = _configuration.GetValue<int>("LotteryDraw:CheckIntervalMinutes", 1);
        _checkInterval = TimeSpan.FromMinutes(checkIntervalMinutes);
        var timeoutSeconds = _configuration.GetValue<int>("LotteryDraw:DrawExecutionTimeoutSeconds", 60);
        _drawExecutionTimeout = TimeSpan.FromSeconds(timeoutSeconds);
    }

    /// <summary>
    /// Executes the background service main loop.
    /// Periodically checks for houses with scheduled draws and executes them.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token to stop the service gracefully.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabled = _configuration.GetValue<bool>("LotteryDraw:Enabled", true);
        if (!enabled)
        {
            _logger.LogInformation("LotteryDrawService is disabled in configuration");
            return;
        }

        _logger.LogInformation("LotteryDrawService started with check interval: {Interval} minutes", _checkInterval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndExecuteDrawsAsync(stoppingToken);
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("LotteryDrawService is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in LotteryDrawService");
                // Continue running even if there's an error
                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        _logger.LogInformation("LotteryDrawService stopped");
    }

    private async Task CheckAndExecuteDrawsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LotteryDbContext>();
        var eventPublisher = scope.ServiceProvider.GetService<IEventPublisher>();

        try
        {
            // Find houses with scheduled draws that are due
            var housesDueForDraw = await dbContext.Houses
                .Where(h => h.DrawDate.HasValue && 
                           h.DrawDate.Value <= DateTime.UtcNow &&
                           h.Status == LotteryStatus.Active)
                .ToListAsync(cancellationToken);

            if (!housesDueForDraw.Any())
            {
                // No draws due
                return;
            }

            _logger.LogInformation("Found {Count} houses due for draw", housesDueForDraw.Count);

            foreach (var house in housesDueForDraw)
            {
                await ExecuteDrawForHouseAsync(house, dbContext, eventPublisher, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for scheduled draws");
            throw;
        }
    }

    private async Task ExecuteDrawForHouseAsync(
        House house,
        LotteryDbContext dbContext,
        IEventPublisher? eventPublisher,
        CancellationToken cancellationToken)
    {
        using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            _logger.LogInformation("Executing draw for house {HouseId}", house.Id);

            // Check if draw already exists
            var existingDraw = await dbContext.LotteryDraws
                .FirstOrDefaultAsync(d => d.HouseId == house.Id && d.DrawDate.Date == house.DrawDate!.Value.Date, cancellationToken);

            if (existingDraw != null && existingDraw.DrawStatus == DrawStatus.Completed)
            {
                _logger.LogInformation("Draw already completed for house {HouseId}", house.Id);
                return;
            }

            // Create or update draw record
            LotteryDraw draw;
            if (existingDraw == null)
            {
                draw = new LotteryDraw
                {
                    Id = Guid.NewGuid(),
                    HouseId = house.Id,
                    DrawDate = house.DrawDate!.Value,
                    DrawStatus = DrawStatus.InProgress,
                    DrawMethod = "random",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                dbContext.LotteryDraws.Add(draw);
            }
            else
            {
                draw = existingDraw;
                draw.DrawStatus = DrawStatus.InProgress;
                draw.UpdatedAt = DateTime.UtcNow;
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            // Broadcast draw started event
            await _hubContext.BroadcastDrawStarted(house.Id, draw);

            // Get active tickets for this house
            var activeTickets = await dbContext.LotteryTickets
                .Where(t => t.HouseId == house.Id && t.Status == TicketStatus.Active)
                .ToListAsync(cancellationToken);

            if (!activeTickets.Any())
            {
                _logger.LogWarning("No active tickets found for house {HouseId}, marking draw as failed", house.Id);
                draw.DrawStatus = DrawStatus.Failed;
                draw.UpdatedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return;
            }

            // Select random winner
            var random = new Random();
            var winningTicket = activeTickets[random.Next(activeTickets.Count)];

            // Update draw with winner information
            draw.WinningTicketId = winningTicket.Id;
            draw.WinningTicketNumber = winningTicket.TicketNumber;
            draw.WinnerUserId = winningTicket.UserId;
            draw.TotalTicketsSold = activeTickets.Count;
            draw.TotalParticipationPercentage = house.TotalTickets > 0 
                ? (decimal)activeTickets.Count / house.TotalTickets * 100 
                : 0;
            draw.DrawStatus = DrawStatus.Completed;
            draw.ConductedAt = DateTime.UtcNow;
            draw.UpdatedAt = DateTime.UtcNow;

            // Mark winning ticket
            winningTicket.IsWinner = true;
            winningTicket.Status = TicketStatus.Winner;
            winningTicket.UpdatedAt = DateTime.UtcNow;

            // Update house status
            house.Status = LotteryStatus.Completed;
            house.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Draw completed for house {HouseId}, winner: {TicketNumber} (User: {UserId})", 
                house.Id, winningTicket.TicketNumber, winningTicket.UserId);

            // Broadcast draw completed event
            await _hubContext.BroadcastDrawCompleted(house.Id, draw, winningTicket);

            // Publish EventBridge events if available
            if (eventPublisher != null)
            {
                try
                {
                    // Publish draw completed event
                    var drawCompletedEvent = new LotteryDrawCompletedEvent
                    {
                        DrawId = draw.Id,
                        HouseId = house.Id,
                        DrawDate = draw.DrawDate,
                        TotalTickets = activeTickets.Count
                    };
                    await eventPublisher.PublishAsync(drawCompletedEvent, cancellationToken);

                    // Publish winner selected event
                    var winnerEvent = new LotteryDrawWinnerSelectedEvent
                    {
                        DrawId = draw.Id,
                        HouseId = house.Id,
                        WinnerTicketId = winningTicket.Id,
                        WinnerUserId = winningTicket.UserId,
                        WinningTicketNumber = int.TryParse(winningTicket.TicketNumber.Replace("TKT-", "").Split('-')[0], out var ticketNum) ? ticketNum : 0,
                        HouseTitle = house.Title,
                        PrizeValue = house.Price
                    };
                    await eventPublisher.PublishAsync(winnerEvent, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error publishing EventBridge events for draw {DrawId}", draw.Id);
                    // Don't fail the draw if event publishing fails
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing draw for house {HouseId}", house.Id);
            await transaction.RollbackAsync(cancellationToken);
            
            // Try to update draw status to failed
            try
            {
                var draw = await dbContext.LotteryDraws
                    .FirstOrDefaultAsync(d => d.HouseId == house.Id && d.DrawDate.Date == house.DrawDate!.Value.Date, cancellationToken);
                if (draw != null)
                {
                    draw.DrawStatus = DrawStatus.Failed;
                    draw.UpdatedAt = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception updateEx)
            {
                _logger.LogError(updateEx, "Error updating draw status to failed");
            }
            
            throw;
        }
    }
}
