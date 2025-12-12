using Microsoft.EntityFrameworkCore;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Admin.DTOs;

namespace AmesaBackend.Admin.Services
{
    public interface ITicketsService
    {
        Task<PagedResult<TicketDto>> GetTicketsAsync(int page = 1, int pageSize = 20, Guid? houseId = null, Guid? userId = null, string? status = null);
        Task<TicketDto?> GetTicketByIdAsync(Guid id);
    }

    public class TicketsService : ITicketsService
    {
        private readonly LotteryDbContext _context;
        private readonly ILogger<TicketsService> _logger;

        public TicketsService(
            LotteryDbContext context,
            ILogger<TicketsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PagedResult<TicketDto>> GetTicketsAsync(int page = 1, int pageSize = 20, Guid? houseId = null, Guid? userId = null, string? status = null)
        {
            var query = _context.LotteryTickets
                .Include(t => t.House)
                .AsQueryable();

            if (houseId.HasValue)
                query = query.Where(t => t.HouseId == houseId.Value);

            if (userId.HasValue)
                query = query.Where(t => t.UserId == userId.Value);

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(t => t.Status == status);

            var totalCount = await query.CountAsync();
            var tickets = await query
                .OrderByDescending(t => t.PurchaseDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TicketDto
                {
                    Id = t.Id,
                    TicketNumber = t.TicketNumber,
                    HouseId = t.HouseId,
                    HouseTitle = t.House.Title,
                    UserId = t.UserId,
                    PurchasePrice = t.PurchasePrice,
                    Status = t.Status,
                    PurchaseDate = t.PurchaseDate,
                    IsWinner = t.IsWinner,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            return new PagedResult<TicketDto>
            {
                Items = tickets,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        public async Task<TicketDto?> GetTicketByIdAsync(Guid id)
        {
            var ticket = await _context.LotteryTickets
                .Include(t => t.House)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null) return null;

            return new TicketDto
            {
                Id = ticket.Id,
                TicketNumber = ticket.TicketNumber,
                HouseId = ticket.HouseId,
                HouseTitle = ticket.House.Title,
                UserId = ticket.UserId,
                PurchasePrice = ticket.PurchasePrice,
                Status = ticket.Status,
                PurchaseDate = ticket.PurchaseDate,
                IsWinner = ticket.IsWinner,
                CreatedAt = ticket.CreatedAt
            };
        }
    }
}

