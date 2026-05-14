using Microsoft.EntityFrameworkCore;
using AmesaBackend.Auth.Data;
using AmesaBackend.Auth.Models;
using AmesaBackend.Admin.DTOs;
using AmesaBackend.Admin.Security;

namespace AmesaBackend.Admin.Services
{
    public interface IUsersService
    {
        Task<PagedResult<UserDto>> GetUsersAsync(int page = 1, int pageSize = 20, string? search = null, UserStatus? status = null);
        Task<UserDto?> GetUserByIdAsync(Guid id);
        Task<UserDto> CreateUserAsync(CreateUserRequest request);
        Task<UserDto> UpdateUserAsync(Guid id, UpdateUserRequest request);
        Task<bool> SuspendUserAsync(Guid id);
        Task<bool> ActivateUserAsync(Guid id);
        Task<bool> DeleteUserAsync(Guid id);
    }

    public class UsersService : IUsersService
    {
        private readonly AuthDbContext _context;
        private readonly ILogger<UsersService> _logger;
        private readonly IRealTimeNotificationService? _notificationService;
        private readonly IAdminPermissionService _permissions;
        private readonly IAdminAuditService _audit;

        public UsersService(
            AuthDbContext context,
            ILogger<UsersService> logger,
            IAdminPermissionService permissions,
            IAdminAuditService audit,
            IRealTimeNotificationService? notificationService = null)
        {
            _context = context;
            _logger = logger;
            _permissions = permissions;
            _audit = audit;
            _notificationService = notificationService;
        }

        public async Task<PagedResult<UserDto>> GetUsersAsync(int page = 1, int pageSize = 20, string? search = null, UserStatus? status = null)
        {
            await _permissions.RequirePermissionAsync(AdminPermissionNames.UsersRead);

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
            await _permissions.RequirePermissionAsync(AdminPermissionNames.UsersRead);

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

        public async Task<UserDto> CreateUserAsync(CreateUserRequest request)
        {
            await _permissions.RequirePermissionAsync(AdminPermissionNames.UsersWrite);

            var email = request.Email.Trim().ToLowerInvariant();
            var username = request.Username.Trim();

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(request.FirstName) ||
                string.IsNullOrWhiteSpace(request.LastName) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                throw new InvalidOperationException("Email, username, name, and password are required.");
            }

            if (await _context.Users.AnyAsync(u => u.Email == email || u.Username == username))
            {
                throw new InvalidOperationException("A user with this email or username already exists.");
            }

            var now = DateTime.UtcNow;
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                Username = username,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Status = request.Status,
                VerificationStatus = UserVerificationStatus.Unverified,
                AuthProvider = AuthProvider.Email,
                PreferredLanguage = string.IsNullOrWhiteSpace(request.PreferredLanguage) ? "en" : request.PreferredLanguage.Trim(),
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User created by admin: {UserId} - {Email}", user.Id, user.Email);
            await _audit.LogAsync("user.created", "user", user.Id, new { user.Email, user.Status });

            return await GetUserByIdAsync(user.Id) ?? throw new InvalidOperationException("Failed to retrieve created user");
        }

        public async Task<UserDto> UpdateUserAsync(Guid id, UpdateUserRequest request)
        {
            await _permissions.RequirePermissionAsync(AdminPermissionNames.UsersWrite);

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
            await _audit.LogAsync("user.updated", "user", user.Id, new { user.Email, user.Status });

            // Notify real-time clients
            if (_notificationService != null)
            {
                await _notificationService.NotifyUserUpdatedAsync(user.Id, user.Email);
            }

            return await GetUserByIdAsync(user.Id) ?? throw new InvalidOperationException("Failed to retrieve updated user");
        }

        public async Task<bool> SuspendUserAsync(Guid id)
        {
            await _permissions.RequirePermissionAsync(AdminPermissionNames.UsersSuspend);

            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            user.Status = UserStatus.Suspended;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("User suspended: {UserId} - {Email}", user.Id, user.Email);
            await _audit.LogAsync("user.suspended", "user", user.Id, new { user.Email, user.Status });
            return true;
        }

        public async Task<bool> ActivateUserAsync(Guid id)
        {
            await _permissions.RequirePermissionAsync(AdminPermissionNames.UsersSuspend);

            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            user.Status = UserStatus.Active;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("User activated: {UserId} - {Email}", user.Id, user.Email);
            await _audit.LogAsync("user.activated", "user", user.Id, new { user.Email, user.Status });
            return true;
        }

        public async Task<bool> DeleteUserAsync(Guid id)
        {
            await _permissions.RequirePermissionAsync(AdminPermissionNames.UsersWrite);

            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            user.Status = UserStatus.Deleted;
            user.DeletedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("User soft-deleted by admin: {UserId} - {Email}", user.Id, user.Email);
            await _audit.LogAsync("user.deleted", "user", user.Id, new { user.Email });
            return true;
        }
    }
}

