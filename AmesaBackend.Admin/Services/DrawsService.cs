using Microsoft.EntityFrameworkCore;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Admin.DTOs;

namespace AmesaBackend.Admin.Services
{
    public interface IDrawsService
    {
        Task<PagedResult<DrawDto>> GetDrawsAsync(int page = 1, int pageSize = 20, Guid? houseId = null, string? status = null);
        Task<DrawDto?> GetDrawByIdAsync(Guid id);
        Task<DrawDto> ConductDrawAsync(Guid drawId, Guid conductedBy);
    }

    public class DrawsService : IDrawsService
    {
        private readonly LotteryDbContext _context;
        private readonly ILogger<DrawsService> _logger;

        public DrawsService(
            LotteryDbContext context,
            ILogger<DrawsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PagedResult<DrawDto>> GetDrawsAsync(int page = 1, int pageSize = 20, Guid? houseId = null, string? status = null)
        {
            var query = _context.LotteryDraws
                .Include(d => d.House)
                .AsQueryable();

            if (houseId.HasValue)
                query = query.Where(d => d.HouseId == houseId.Value);

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(d => d.DrawStatus == status);

            var totalCount = await query.CountAsync();
            var draws = await query
                .OrderByDescending(d => d.DrawDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new DrawDto
                {
                    Id = d.Id,
                    HouseId = d.HouseId,
                    HouseTitle = d.House.Title,
                    DrawDate = d.DrawDate,
                    TotalTicketsSold = d.TotalTicketsSold,
                    TotalParticipationPercentage = d.TotalParticipationPercentage,
                    WinningTicketNumber = d.WinningTicketNumber,
                    WinningTicketId = d.WinningTicketId,
                    WinnerUserId = d.WinnerUserId,
                    DrawStatus = d.DrawStatus,
                    DrawMethod = d.DrawMethod,
                    ConductedBy = d.ConductedBy,
                    ConductedAt = d.ConductedAt,
                    CreatedAt = d.CreatedAt
                })
                .ToListAsync();

            return new PagedResult<DrawDto>
            {
                Items = draws,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        public async Task<DrawDto?> GetDrawByIdAsync(Guid id)
        {
            var draw = await _context.LotteryDraws
                .Include(d => d.House)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (draw == null) return null;

            return new DrawDto
            {
                Id = draw.Id,
                HouseId = draw.HouseId,
                HouseTitle = draw.House.Title,
                DrawDate = draw.DrawDate,
                TotalTicketsSold = draw.TotalTicketsSold,
                TotalParticipationPercentage = draw.TotalParticipationPercentage,
                WinningTicketNumber = draw.WinningTicketNumber,
                WinningTicketId = draw.WinningTicketId,
                WinnerUserId = draw.WinnerUserId,
                DrawStatus = draw.DrawStatus,
                DrawMethod = draw.DrawMethod,
                ConductedBy = draw.ConductedBy,
                ConductedAt = draw.ConductedAt,
                CreatedAt = draw.CreatedAt
            };
        }

        public async Task<DrawDto> ConductDrawAsync(Guid drawId, Guid conductedBy)
        {
            var draw = await _context.LotteryDraws
                .Include(d => d.House)
                .FirstOrDefaultAsync(d => d.Id == drawId);

            if (draw == null)
                throw new KeyNotFoundException($"Draw with ID {drawId} not found");

            if (draw.DrawStatus != "Pending")
                throw new InvalidOperationException($"Draw is already {draw.DrawStatus}");

            // Get all tickets for this house
            var tickets = await _context.LotteryTickets
                .Where(t => t.HouseId == draw.HouseId && t.Status == "Active")
                .ToListAsync();

            if (!tickets.Any())
                throw new InvalidOperationException("No active tickets found for this draw");

            // Random selection
            var random = new Random();
            var winningTicket = tickets[random.Next(tickets.Count)];

            draw.WinningTicketId = winningTicket.Id;
            draw.WinningTicketNumber = winningTicket.TicketNumber;
            draw.WinnerUserId = winningTicket.UserId;
            draw.DrawStatus = "Completed";
            draw.DrawMethod = "random";
            draw.ConductedBy = conductedBy;
            draw.ConductedAt = DateTime.UtcNow;
            draw.UpdatedAt = DateTime.UtcNow;

            // Mark ticket as winner
            winningTicket.IsWinner = true;
            winningTicket.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Draw conducted: {DrawId} - Winner: {TicketNumber}", drawId, winningTicket.TicketNumber);

            return await GetDrawByIdAsync(drawId) ?? throw new InvalidOperationException("Failed to retrieve conducted draw");
        }
    }
}

