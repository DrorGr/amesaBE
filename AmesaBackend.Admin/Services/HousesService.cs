using Microsoft.EntityFrameworkCore;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Lottery.Models;
using AmesaBackend.Admin.Models;
using AmesaBackend.Admin.DTOs;

namespace AmesaBackend.Admin.Services
{
    public interface IHousesService
    {
        Task<PagedResult<HouseDto>> GetHousesAsync(int page = 1, int pageSize = 20, string? search = null, string? status = null);
        Task<HouseDto?> GetHouseByIdAsync(Guid id);
        Task<HouseDto> CreateHouseAsync(CreateHouseRequest request);
        Task<HouseDto> UpdateHouseAsync(Guid id, UpdateHouseRequest request);
        Task<bool> DeleteHouseAsync(Guid id);
        Task<bool> ActivateHouseAsync(Guid id);
        Task<bool> DeactivateHouseAsync(Guid id);
    }

    public class HousesService : IHousesService
    {
        private readonly LotteryDbContext _context;
        private readonly ILogger<HousesService> _logger;
        private readonly IRealTimeNotificationService? _notificationService;

        public HousesService(
            LotteryDbContext context,
            ILogger<HousesService> logger,
            IRealTimeNotificationService? notificationService = null)
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<PagedResult<HouseDto>> GetHousesAsync(int page = 1, int pageSize = 20, string? search = null, string? status = null)
        {
            var query = _context.Houses.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(h => 
                    h.Title.Contains(search) || 
                    h.Location.Contains(search) ||
                    (h.Description != null && h.Description.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(h => h.Status == status);
            }

            var totalCount = await query.CountAsync();
            var houses = await query
                .OrderByDescending(h => h.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(h => new HouseDto
                {
                    Id = h.Id,
                    Title = h.Title,
                    Description = h.Description,
                    Price = h.Price,
                    Location = h.Location,
                    Address = h.Address,
                    Bedrooms = h.Bedrooms,
                    Bathrooms = h.Bathrooms,
                    SquareFeet = h.SquareFeet,
                    PropertyType = h.PropertyType,
                    YearBuilt = h.YearBuilt,
                    LotSize = h.LotSize,
                    Features = h.Features,
                    Status = h.Status,
                    TotalTickets = h.TotalTickets,
                    TicketPrice = h.TicketPrice,
                    LotteryStartDate = h.LotteryStartDate,
                    LotteryEndDate = h.LotteryEndDate,
                    DrawDate = h.DrawDate,
                    MinimumParticipationPercentage = h.MinimumParticipationPercentage,
                    MaxParticipants = h.MaxParticipants,
                    CreatedAt = h.CreatedAt,
                    UpdatedAt = h.UpdatedAt
                })
                .ToListAsync();

            return new PagedResult<HouseDto>
            {
                Items = houses,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        public async Task<HouseDto?> GetHouseByIdAsync(Guid id)
        {
            var house = await _context.Houses
                .Include(h => h.Images)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (house == null) return null;

            return new HouseDto
            {
                Id = house.Id,
                Title = house.Title,
                Description = house.Description,
                Price = house.Price,
                Location = house.Location,
                Address = house.Address,
                Bedrooms = house.Bedrooms,
                Bathrooms = house.Bathrooms,
                SquareFeet = house.SquareFeet,
                PropertyType = house.PropertyType,
                YearBuilt = house.YearBuilt,
                LotSize = house.LotSize,
                Features = house.Features,
                Status = house.Status,
                TotalTickets = house.TotalTickets,
                TicketPrice = house.TicketPrice,
                LotteryStartDate = house.LotteryStartDate,
                LotteryEndDate = house.LotteryEndDate,
                DrawDate = house.DrawDate,
                MinimumParticipationPercentage = house.MinimumParticipationPercentage,
                MaxParticipants = house.MaxParticipants,
                Images = house.Images.Select(i => new HouseImageDto
                {
                    Id = i.Id,
                    ImageUrl = i.ImageUrl,
                    AltText = i.AltText,
                    DisplayOrder = i.DisplayOrder,
                    IsPrimary = i.IsPrimary,
                    MediaType = i.MediaType
                }).ToList(),
                CreatedAt = house.CreatedAt,
                UpdatedAt = house.UpdatedAt
            };
        }

        public async Task<HouseDto> CreateHouseAsync(CreateHouseRequest request)
        {
            var house = new House
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Description = request.Description,
                Price = request.Price,
                Location = request.Location,
                Address = request.Address,
                Bedrooms = request.Bedrooms,
                Bathrooms = request.Bathrooms,
                SquareFeet = request.SquareFeet,
                PropertyType = request.PropertyType,
                YearBuilt = request.YearBuilt,
                LotSize = request.LotSize,
                Features = request.Features,
                Status = request.Status ?? "Pending",
                TotalTickets = request.TotalTickets,
                TicketPrice = request.TicketPrice,
                LotteryStartDate = request.LotteryStartDate,
                LotteryEndDate = request.LotteryEndDate,
                DrawDate = request.DrawDate,
                MinimumParticipationPercentage = request.MinimumParticipationPercentage ?? 75.00m,
                MaxParticipants = request.MaxParticipants,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Houses.Add(house);
            await _context.SaveChangesAsync();

            _logger.LogInformation("House created: {HouseId} - {Title}", house.Id, house.Title);

            // Notify real-time clients
            if (_notificationService != null)
            {
                await _notificationService.NotifyHouseCreatedAsync(house.Id, house.Title);
            }

            return await GetHouseByIdAsync(house.Id) ?? throw new InvalidOperationException("Failed to retrieve created house");
        }

        public async Task<HouseDto> UpdateHouseAsync(Guid id, UpdateHouseRequest request)
        {
            var house = await _context.Houses.FindAsync(id);
            if (house == null)
                throw new KeyNotFoundException($"House with ID {id} not found");

            house.Title = request.Title ?? house.Title;
            house.Description = request.Description ?? house.Description;
            house.Price = request.Price ?? house.Price;
            house.Location = request.Location ?? house.Location;
            house.Address = request.Address ?? house.Address;
            house.Bedrooms = request.Bedrooms ?? house.Bedrooms;
            house.Bathrooms = request.Bathrooms ?? house.Bathrooms;
            house.SquareFeet = request.SquareFeet ?? house.SquareFeet;
            house.PropertyType = request.PropertyType ?? house.PropertyType;
            house.YearBuilt = request.YearBuilt ?? house.YearBuilt;
            house.LotSize = request.LotSize ?? house.LotSize;
            house.Features = request.Features ?? house.Features;
            house.Status = request.Status ?? house.Status;
            house.TotalTickets = request.TotalTickets ?? house.TotalTickets;
            house.TicketPrice = request.TicketPrice ?? house.TicketPrice;
            house.LotteryStartDate = request.LotteryStartDate ?? house.LotteryStartDate;
            house.LotteryEndDate = request.LotteryEndDate ?? house.LotteryEndDate;
            house.DrawDate = request.DrawDate ?? house.DrawDate;
            house.MinimumParticipationPercentage = request.MinimumParticipationPercentage ?? house.MinimumParticipationPercentage;
            house.MaxParticipants = request.MaxParticipants ?? house.MaxParticipants;
            house.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("House updated: {HouseId} - {Title}", house.Id, house.Title);

            // Notify real-time clients
            if (_notificationService != null)
            {
                await _notificationService.NotifyHouseUpdatedAsync(house.Id, house.Title);
            }

            return await GetHouseByIdAsync(house.Id) ?? throw new InvalidOperationException("Failed to retrieve updated house");
        }

        public async Task<bool> DeleteHouseAsync(Guid id)
        {
            var house = await _context.Houses.FindAsync(id);
            if (house == null) return false;

            house.DeletedAt = DateTime.UtcNow;
            house.Status = "Deleted";
            await _context.SaveChangesAsync();

            _logger.LogInformation("House deleted: {HouseId} - {Title}", house.Id, house.Title);
            
            // Notify real-time clients
            if (_notificationService != null)
            {
                await _notificationService.NotifyHouseDeletedAsync(id);
            }
            
            return true;
        }

        public async Task<bool> ActivateHouseAsync(Guid id)
        {
            var house = await _context.Houses.FindAsync(id);
            if (house == null) return false;

            house.Status = "Active";
            house.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeactivateHouseAsync(Guid id)
        {
            var house = await _context.Houses.FindAsync(id);
            if (house == null) return false;

            house.Status = "Inactive";
            house.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }
    }
}

