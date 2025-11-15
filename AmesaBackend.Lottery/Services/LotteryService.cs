using Microsoft.EntityFrameworkCore;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.DTOs;
using AmesaBackend.Lottery.Models;
using AmesaBackend.Shared.Events;

namespace AmesaBackend.Lottery.Services
{
    public class LotteryService : ILotteryService
    {
        private readonly LotteryDbContext _context;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<LotteryService> _logger;

        public LotteryService(LotteryDbContext context, IEventPublisher eventPublisher, ILogger<LotteryService> logger)
        {
            _context = context;
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        public async Task<List<LotteryTicketDto>> GetUserTicketsAsync(Guid userId)
        {
            var tickets = await _context.LotteryTickets
                .Include(t => t.House)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.PurchaseDate)
                .ToListAsync();

            return tickets.Select(MapToTicketDto).ToList();
        }

        public async Task<LotteryTicketDto> GetTicketAsync(Guid ticketId)
        {
            var ticket = await _context.LotteryTickets
                .Include(t => t.House)
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket == null)
            {
                throw new KeyNotFoundException("Ticket not found");
            }

            return MapToTicketDto(ticket);
        }

        public async Task<List<LotteryDrawDto>> GetDrawsAsync()
        {
            var draws = await _context.LotteryDraws
                .Include(d => d.House)
                .OrderByDescending(d => d.DrawDate)
                .ToListAsync();

            return draws.Select(MapToDrawDto).ToList();
        }

        public async Task<LotteryDrawDto> GetDrawAsync(Guid drawId)
        {
            var draw = await _context.LotteryDraws
                .Include(d => d.House)
                .FirstOrDefaultAsync(d => d.Id == drawId);

            if (draw == null)
            {
                throw new KeyNotFoundException("Draw not found");
            }

            return MapToDrawDto(draw);
        }

        public async Task ConductDrawAsync(Guid drawId, ConductDrawRequest request)
        {
            var draw = await _context.LotteryDraws
                .Include(d => d.House)
                .FirstOrDefaultAsync(d => d.Id == drawId);

            if (draw == null)
            {
                throw new KeyNotFoundException("Draw not found");
            }

            if (draw.DrawStatus != "Pending")
            {
                throw new InvalidOperationException("Draw has already been conducted");
            }

            draw.DrawStatus = "Completed";
            draw.ConductedAt = DateTime.UtcNow;
            draw.DrawMethod = request.DrawMethod;
            draw.DrawSeed = request.DrawSeed;

            await _context.SaveChangesAsync();

            await _eventPublisher.PublishAsync(new LotteryDrawCompletedEvent
            {
                DrawId = draw.Id,
                HouseId = draw.HouseId,
                DrawDate = draw.DrawDate,
                TotalTickets = draw.TotalTicketsSold
            });

            if (draw.WinnerUserId.HasValue && draw.WinningTicketId.HasValue)
            {
                await _eventPublisher.PublishAsync(new LotteryDrawWinnerSelectedEvent
                {
                    DrawId = draw.Id,
                    HouseId = draw.HouseId,
                    WinnerTicketId = draw.WinningTicketId.Value,
                    WinnerUserId = draw.WinnerUserId.Value,
                    WinningTicketNumber = int.Parse(draw.WinningTicketNumber ?? "0")
                });
            }
        }

        private LotteryTicketDto MapToTicketDto(LotteryTicket ticket)
        {
            return new LotteryTicketDto
            {
                Id = ticket.Id,
                TicketNumber = ticket.TicketNumber,
                HouseId = ticket.HouseId,
                HouseTitle = ticket.House?.Title ?? "",
                PurchasePrice = ticket.PurchasePrice,
                Status = ticket.Status,
                PurchaseDate = ticket.PurchaseDate,
                IsWinner = ticket.IsWinner,
                CreatedAt = ticket.CreatedAt
            };
        }

        private LotteryDrawDto MapToDrawDto(LotteryDraw draw)
        {
            return new LotteryDrawDto
            {
                Id = draw.Id,
                HouseId = draw.HouseId,
                HouseTitle = draw.House?.Title ?? "",
                DrawDate = draw.DrawDate,
                TotalTicketsSold = draw.TotalTicketsSold,
                TotalParticipationPercentage = draw.TotalParticipationPercentage,
                WinningTicketNumber = draw.WinningTicketNumber,
                WinnerUserId = draw.WinnerUserId,
                DrawStatus = draw.DrawStatus,
                DrawMethod = draw.DrawMethod,
                ConductedAt = draw.ConductedAt,
                CreatedAt = draw.CreatedAt
            };
        }
    }
}
