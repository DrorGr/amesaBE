using Microsoft.EntityFrameworkCore;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.Models;
using AmesaBackend.Admin.DTOs;
using AmesaBackend.Admin.Security;
using System.Security.Cryptography;

namespace AmesaBackend.Admin.Services
{
    public interface IDrawsService
    {
        Task<PagedResult<DrawDto>> GetDrawsAsync(int page = 1, int pageSize = 20, Guid? houseId = null, string? status = null);
        Task<DrawDto?> GetDrawByIdAsync(Guid id);
        Task<DrawDto> CreateDrawAsync(CreateDrawRequest request);
        Task<DrawDto> ConductDrawAsync(Guid drawId, Guid conductedBy);
    }

    public class DrawsService : IDrawsService
    {
        private readonly LotteryDbContext _context;
        private readonly ILogger<DrawsService> _logger;
        private readonly IAdminPermissionService _permissions;
        private readonly IAdminAuditService _audit;

        public DrawsService(
            LotteryDbContext context,
            ILogger<DrawsService> logger,
            IAdminPermissionService permissions,
            IAdminAuditService audit)
        {
            _context = context;
            _logger = logger;
            _permissions = permissions;
            _audit = audit;
        }

        public async Task<PagedResult<DrawDto>> GetDrawsAsync(int page = 1, int pageSize = 20, Guid? houseId = null, string? status = null)
        {
            await _permissions.RequirePermissionAsync(AdminPermissionNames.DrawsRead);
            await EnsureScheduledDrawsAsync();

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
            await _permissions.RequirePermissionAsync(AdminPermissionNames.DrawsRead);
            await EnsureScheduledDrawsAsync();

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

        public async Task<DrawDto> CreateDrawAsync(CreateDrawRequest request)
        {
            await _permissions.RequirePermissionAsync(AdminPermissionNames.DrawsConduct);

            var house = await _context.Houses.FirstOrDefaultAsync(h => h.Id == request.HouseId && h.DeletedAt == null);
            if (house == null)
            {
                throw new KeyNotFoundException("House not found.");
            }

            var existingPendingDraw = await _context.LotteryDraws
                .FirstOrDefaultAsync(d => d.HouseId == request.HouseId && d.DrawStatus == "Pending");
            if (existingPendingDraw != null)
            {
                throw new InvalidOperationException("This house already has a pending draw.");
            }

            var ticketsSold = await _context.LotteryTickets
                .CountAsync(t => t.HouseId == request.HouseId && t.Status == "Active");
            var participation = house.TotalTickets > 0
                ? Math.Round((decimal)ticketsSold / house.TotalTickets * 100, 2)
                : 0;

            var now = DateTime.UtcNow;
            var draw = new LotteryDraw
            {
                Id = Guid.NewGuid(),
                HouseId = request.HouseId,
                DrawDate = request.DrawDate,
                TotalTicketsSold = ticketsSold,
                TotalParticipationPercentage = participation,
                DrawStatus = "Pending",
                DrawMethod = "random",
                CreatedAt = now,
                UpdatedAt = now
            };

            house.DrawDate = request.DrawDate;
            house.UpdatedAt = now;
            _context.LotteryDraws.Add(draw);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Draw created by admin: {DrawId} for house {HouseId}", draw.Id, request.HouseId);
            await _audit.LogAsync("draw.created", "draw", draw.Id, new
            {
                draw.HouseId,
                draw.DrawDate,
                draw.TotalTicketsSold,
                draw.TotalParticipationPercentage
            });

            return await GetDrawByIdAsync(draw.Id) ?? throw new InvalidOperationException("Failed to retrieve created draw");
        }

        public async Task<DrawDto> ConductDrawAsync(Guid drawId, Guid conductedBy)
        {
            await _permissions.RequirePermissionAsync(AdminPermissionNames.DrawsConduct);

            var draw = await _context.LotteryDraws
                .Include(d => d.House)
                .FirstOrDefaultAsync(d => d.Id == drawId);

            if (draw == null)
                throw new KeyNotFoundException($"Draw with ID {drawId} not found");

            if (draw.DrawStatus != "Pending")
                throw new InvalidOperationException($"Draw is already {draw.DrawStatus}");

            if (draw.DrawDate > DateTime.UtcNow)
                throw new InvalidOperationException("Draw cannot be conducted before its scheduled draw date");

            if (draw.TotalParticipationPercentage < draw.House.MinimumParticipationPercentage)
                throw new InvalidOperationException("Draw cannot be conducted before the minimum participation threshold is met");

            // Get all tickets for this house
            var tickets = await _context.LotteryTickets
                .Where(t => t.HouseId == draw.HouseId && t.Status == "Active")
                .ToListAsync();

            if (!tickets.Any())
                throw new InvalidOperationException("No active tickets found for this draw");

            var winningTicket = tickets[RandomNumberGenerator.GetInt32(tickets.Count)];

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
            await _audit.LogAsync("draw.conducted", "draw", drawId, new
            {
                draw.HouseId,
                winningTicket.Id,
                winningTicket.TicketNumber,
                winningTicket.UserId,
                conductedBy,
                draw.TotalTicketsSold,
                draw.TotalParticipationPercentage
            });

            return await GetDrawByIdAsync(drawId) ?? throw new InvalidOperationException("Failed to retrieve conducted draw");
        }

        private async Task EnsureScheduledDrawsAsync()
        {
            var houses = await _context.Houses
                .Where(h => h.DrawDate.HasValue && h.DeletedAt == null)
                .ToListAsync();

            if (!houses.Any())
            {
                return;
            }

            var houseIds = houses.Select(h => h.Id).ToList();
            var existingDraws = await _context.LotteryDraws
                .Where(d => houseIds.Contains(d.HouseId))
                .ToListAsync();

            var ticketCounts = await _context.LotteryTickets
                .Where(t => houseIds.Contains(t.HouseId) && t.Status == "Active")
                .GroupBy(t => t.HouseId)
                .Select(g => new { HouseId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.HouseId, x => x.Count);

            var hasChanges = false;
            foreach (var house in houses)
            {
                var ticketsSold = ticketCounts.GetValueOrDefault(house.Id);
                var participation = house.TotalTickets > 0
                    ? Math.Round((decimal)ticketsSold / house.TotalTickets * 100, 2)
                    : 0;

                var draw = existingDraws.FirstOrDefault(d => d.HouseId == house.Id);
                if (draw == null)
                {
                    _context.LotteryDraws.Add(new LotteryDraw
                    {
                        Id = Guid.NewGuid(),
                        HouseId = house.Id,
                        DrawDate = house.DrawDate!.Value,
                        TotalTicketsSold = ticketsSold,
                        TotalParticipationPercentage = participation,
                        DrawStatus = "Pending",
                        DrawMethod = "random",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                    hasChanges = true;
                    continue;
                }

                if (draw.DrawStatus == "Pending" &&
                    (draw.DrawDate != house.DrawDate.Value ||
                     draw.TotalTicketsSold != ticketsSold ||
                     draw.TotalParticipationPercentage != participation))
                {
                    draw.DrawDate = house.DrawDate.Value;
                    draw.TotalTicketsSold = ticketsSold;
                    draw.TotalParticipationPercentage = participation;
                    draw.UpdatedAt = DateTime.UtcNow;
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await _context.SaveChangesAsync();
            }
        }
    }
}

