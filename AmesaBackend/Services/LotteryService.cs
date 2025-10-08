using Microsoft.EntityFrameworkCore;
using AmesaBackend.Data;
using AmesaBackend.DTOs;
using AmesaBackend.Models;

namespace AmesaBackend.Services
{
    public class LotteryService : ILotteryService
    {
        private readonly AmesaDbContext _context;
        private readonly ILogger<LotteryService> _logger;

        public LotteryService(AmesaDbContext context, ILogger<LotteryService> logger)
        {
            _context = context;
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
                .Include(d => d.WinnerUser)
                .OrderByDescending(d => d.DrawDate)
                .ToListAsync();

            return draws.Select(MapToDrawDto).ToList();
        }

        public async Task<LotteryDrawDto> GetDrawAsync(Guid drawId)
        {
            var draw = await _context.LotteryDraws
                .Include(d => d.House)
                .Include(d => d.WinnerUser)
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

            if (draw.DrawStatus != DrawStatus.Pending)
            {
                throw new InvalidOperationException("Draw has already been conducted");
            }

            // TODO: Implement actual draw logic
            draw.DrawStatus = DrawStatus.Completed;
            draw.ConductedAt = DateTime.UtcNow;
            draw.DrawMethod = request.DrawMethod;
            draw.DrawSeed = request.DrawSeed;

            await _context.SaveChangesAsync();
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
                Status = ticket.Status.ToString(),
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
                WinnerName = draw.WinnerUser != null ? $"{draw.WinnerUser.FirstName} {draw.WinnerUser.LastName}" : null,
                DrawStatus = draw.DrawStatus.ToString(),
                DrawMethod = draw.DrawMethod,
                ConductedAt = draw.ConductedAt,
                CreatedAt = draw.CreatedAt
            };
        }
    }
}
