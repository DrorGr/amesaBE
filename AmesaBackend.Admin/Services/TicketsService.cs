using Microsoft.EntityFrameworkCore;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Admin.DTOs;
using AmesaBackend.Admin.Security;

namespace AmesaBackend.Admin.Services
{
    public interface ITicketsService
    {
        Task<PagedResult<TicketDto>> GetTicketsAsync(int page = 1, int pageSize = 20, Guid? houseId = null, Guid? userId = null, string? status = null);
        Task<TicketDto?> GetTicketByIdAsync(Guid id);
        Task<List<HouseTicketStatsDto>> GetHouseTicketStatsAsync(Guid? houseId = null);
    }

    public class TicketsService : ITicketsService
    {
        private readonly LotteryDbContext _context;
        private readonly ILogger<TicketsService> _logger;
        private readonly IAdminPermissionService _permissions;

        public TicketsService(
            LotteryDbContext context,
            ILogger<TicketsService> logger,
            IAdminPermissionService permissions)
        {
            _context = context;
            _logger = logger;
            _permissions = permissions;
        }

        public async Task<PagedResult<TicketDto>> GetTicketsAsync(int page = 1, int pageSize = 20, Guid? houseId = null, Guid? userId = null, string? status = null)
        {
            await _permissions.RequirePermissionAsync(AdminPermissionNames.TicketsRead);

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
            await _permissions.RequirePermissionAsync(AdminPermissionNames.TicketsRead);

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

        public async Task<List<HouseTicketStatsDto>> GetHouseTicketStatsAsync(Guid? houseId = null)
        {
            await _permissions.RequirePermissionAsync(AdminPermissionNames.TicketsRead);

            var housesQuery = _context.Houses.AsNoTracking().AsQueryable();
            if (houseId.HasValue)
            {
                housesQuery = housesQuery.Where(h => h.Id == houseId.Value);
            }

            var houses = await housesQuery
                .OrderBy(h => h.LotteryEndDate)
                .ThenBy(h => h.Title)
                .ToListAsync();

            if (!houses.Any())
            {
                return new List<HouseTicketStatsDto>();
            }

            var houseIds = houses.Select(h => h.Id).ToList();
            var now = DateTime.UtcNow;

            var ticketStats = await _context.LotteryTickets
                .AsNoTracking()
                .Where(t => houseIds.Contains(t.HouseId) && t.Status == "Active")
                .GroupBy(t => t.HouseId)
                .Select(g => new
                {
                    HouseId = g.Key,
                    BoughtTickets = g.Count(),
                    UniqueBuyers = g.Select(t => t.UserId).Distinct().Count(),
                    FirstPurchase = g.Min(t => t.PurchaseDate),
                    LastPurchase = g.Max(t => t.PurchaseDate)
                })
                .ToDictionaryAsync(x => x.HouseId);

            var pendingReservations = await _context.TicketReservations
                .AsNoTracking()
                .Where(r => houseIds.Contains(r.HouseId) && r.Status == "pending" && r.ExpiresAt > now)
                .GroupBy(r => r.HouseId)
                .Select(g => new
                {
                    HouseId = g.Key,
                    ReservedTickets = g.Sum(r => r.Quantity)
                })
                .ToDictionaryAsync(x => x.HouseId, x => x.ReservedTickets);

            return houses.Select(house =>
            {
                ticketStats.TryGetValue(house.Id, out var stats);

                var boughtTickets = stats?.BoughtTickets ?? 0;
                var uniqueBuyers = stats?.UniqueBuyers ?? 0;
                var reservedTickets = pendingReservations.GetValueOrDefault(house.Id);
                var availableTickets = Math.Max(0, house.TotalTickets - boughtTickets - reservedTickets);
                var soldPercentage = house.TotalTickets > 0
                    ? Math.Round((decimal)boughtTickets / house.TotalTickets * 100, 2)
                    : 0;
                var averageTicketsPerBuyer = uniqueBuyers > 0
                    ? Math.Round((decimal)boughtTickets / uniqueBuyers, 2)
                    : 0;

                var firstPurchase = stats?.FirstPurchase;
                var lastPurchase = stats?.LastPurchase;
                var hoursSelling = firstPurchase.HasValue
                    ? Math.Max((now - firstPurchase.Value).TotalHours, 1)
                    : 0;
                var ticketsPerHour = boughtTickets > 0
                    ? Math.Round((decimal)boughtTickets / (decimal)hoursSelling, 2)
                    : 0;

                double? hoursToSellOut = null;
                DateTime? estimatedSoldOutAt = null;
                if (availableTickets == 0)
                {
                    hoursToSellOut = 0;
                    estimatedSoldOutAt = lastPurchase ?? now;
                }
                else if (ticketsPerHour > 0)
                {
                    hoursToSellOut = (double)(availableTickets / ticketsPerHour);
                    estimatedSoldOutAt = now.AddHours(hoursToSellOut.Value);
                }

                return new HouseTicketStatsDto
                {
                    HouseId = house.Id,
                    HouseTitle = house.Title,
                    HouseStatus = house.Status,
                    TotalTickets = house.TotalTickets,
                    BoughtTickets = boughtTickets,
                    ReservedTickets = reservedTickets,
                    AvailableTickets = availableTickets,
                    SoldPercentage = soldPercentage,
                    UniqueBuyers = uniqueBuyers,
                    AverageTicketsPerBuyer = averageTicketsPerBuyer,
                    LotteryStartDate = house.LotteryStartDate,
                    LotteryEndDate = house.LotteryEndDate,
                    DrawDate = house.DrawDate,
                    FirstTicketPurchasedAt = firstPurchase,
                    LastTicketPurchasedAt = lastPurchase,
                    TicketsPerHour = ticketsPerHour,
                    HoursUntilLotteryEnd = (house.LotteryEndDate - now).TotalHours,
                    HoursToSellOut = hoursToSellOut,
                    EstimatedSoldOutAt = estimatedSoldOutAt,
                    IsSoldOut = availableTickets == 0
                };
            }).ToList();
        }
    }
}

