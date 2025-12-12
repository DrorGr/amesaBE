using Microsoft.EntityFrameworkCore;
using AmesaBackend.Auth.Data;
using AmesaBackend.Auth.Models;
using AmesaBackend.Admin.DTOs;

namespace AmesaBackend.Admin.Services
{
    public interface IUsersService
    {
        Task<PagedResult<UserDto>> GetUsersAsync(int page = 1, int pageSize = 20, string? search = null, UserStatus? status = null);
        Task<UserDto?> GetUserByIdAsync(Guid id);
        Task<UserDto> UpdateUserAsync(Guid id, UpdateUserRequest request);
        Task<bool> SuspendUserAsync(Guid id);
        Task<bool> ActivateUserAsync(Guid id);
    }

    public class UsersService : IUsersService
    {
        private readonly AuthDbContext _context;
        private readonly ILogger<UsersService> _logger;
        private readonly IRealTimeNotificationService? _notificationService;

        public UsersService(
            AuthDbContext context,
            ILogger<UsersService> logger,
            IRealTimeNotificationService? notificationService = null)
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<PagedResult<UserDto>> GetUsersAsync(int page = 1, int pageSize = 20, string? search = null, UserStatus? status = null)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u => 
                    u.Email.Contains(search) || 
                    u.Username.Contains(search) ||
                    u.FirstName.Contains(search) ||
                    u.LastName.Contains(search));
            }

            if (status.HasValue)
            {
                query = query.Where(u => u.Status == status.Value);
            }

            var totalCount = await query.CountAsync();
            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    Username = u.Username,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Phone = u.Phone,
                    EmailVerified = u.EmailVerified,
                    PhoneVerified = u.PhoneVerified,
                    Status = u.Status.ToString(),
                    VerificationStatus = u.VerificationStatus.ToString(),
                    DateOfBirth = u.DateOfBirth,
                    Gender = u.Gender != null ? u.Gender.ToString() : null,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt
                })
                .ToListAsync();

            return new PagedResult<UserDto>
            {
                Items = users,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        public async Task<UserDto?> GetUserByIdAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return null;

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.Phone,
                EmailVerified = user.EmailVerified,
                PhoneVerified = user.PhoneVerified,
                Status = user.Status.ToString(),
                VerificationStatus = user.VerificationStatus.ToString(),
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender?.ToString(),
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }

        public async Task<UserDto> UpdateUserAsync(Guid id, UpdateUserRequest request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                throw new KeyNotFoundException($"User with ID {id} not found");

            if (!string.IsNullOrWhiteSpace(request.FirstName))
                user.FirstName = request.FirstName;
            if (!string.IsNullOrWhiteSpace(request.LastName))
                user.LastName = request.LastName;
            if (!string.IsNullOrWhiteSpace(request.Phone))
                user.Phone = request.Phone;
            if (request.Status.HasValue)
                user.Status = request.Status.Value;

            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("User updated: {UserId} - {Email}", user.Id, user.Email);

            // Notify real-time clients
            if (_notificationService != null)
            {
                await _notificationService.NotifyUserUpdatedAsync(user.Id, user.Email);
            }

            return await GetUserByIdAsync(user.Id) ?? throw new InvalidOperationException("Failed to retrieve updated user");
        }

        public async Task<bool> SuspendUserAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            user.Status = UserStatus.Suspended;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("User suspended: {UserId} - {Email}", user.Id, user.Email);
            return true;
        }

        public async Task<bool> ActivateUserAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            user.Status = UserStatus.Active;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("User activated: {UserId} - {Email}", user.Id, user.Email);
            return true;
        }
    }
}

