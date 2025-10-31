using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using AmesaBackend.Data;
using AmesaBackend.DTOs;
using AmesaBackend.Models;

namespace AmesaBackend.Services
{
    public class UserService : IUserService
    {
        private readonly AmesaDbContext _context;
        private readonly ILogger<UserService> _logger;
        private readonly IConfiguration _configuration;

        public UserService(AmesaDbContext context, ILogger<UserService> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<UserDto> GetUserProfileAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            return MapToUserDto(user);
        }

        public async Task<UserDto> UpdateUserProfileAsync(Guid userId, UpdateUserProfileRequest request)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            if (!string.IsNullOrEmpty(request.FirstName))
                user.FirstName = request.FirstName;

            if (!string.IsNullOrEmpty(request.LastName))
                user.LastName = request.LastName;

            if (request.DateOfBirth.HasValue)
                user.DateOfBirth = request.DateOfBirth.Value;

            if (!string.IsNullOrEmpty(request.Gender) && Enum.TryParse<GenderType>(request.Gender, out var gender))
                user.Gender = gender;

            if (!string.IsNullOrEmpty(request.PreferredLanguage))
                user.PreferredLanguage = request.PreferredLanguage;

            if (!string.IsNullOrEmpty(request.Timezone))
                user.Timezone = request.Timezone;

            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return MapToUserDto(user);
        }

        public async Task<List<UserAddressDto>> GetUserAddressesAsync(Guid userId)
        {
            var addresses = await _context.UserAddresses
                .Where(a => a.UserId == userId)
                .OrderBy(a => a.IsPrimary)
                .ThenBy(a => a.CreatedAt)
                .ToListAsync();

            return addresses.Select(MapToAddressDto).ToList();
        }

        public async Task<UserAddressDto> AddUserAddressAsync(Guid userId, CreateAddressRequest request)
        {
            var address = new UserAddress
            {
                UserId = userId,
                Type = request.Type,
                Country = request.Country,
                City = request.City,
                Street = request.Street,
                HouseNumber = request.HouseNumber,
                ZipCode = request.ZipCode,
                IsPrimary = request.IsPrimary,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.UserAddresses.Add(address);
            await _context.SaveChangesAsync();

            return MapToAddressDto(address);
        }

        public async Task<UserAddressDto> UpdateUserAddressAsync(Guid userId, Guid addressId, CreateAddressRequest request)
        {
            var address = await _context.UserAddresses
                .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

            if (address == null)
            {
                throw new KeyNotFoundException("Address not found");
            }

            address.Type = request.Type;
            address.Country = request.Country;
            address.City = request.City;
            address.Street = request.Street;
            address.HouseNumber = request.HouseNumber;
            address.ZipCode = request.ZipCode;
            address.IsPrimary = request.IsPrimary;
            address.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return MapToAddressDto(address);
        }

        public async Task DeleteUserAddressAsync(Guid userId, Guid addressId)
        {
            var address = await _context.UserAddresses
                .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

            if (address == null)
            {
                throw new KeyNotFoundException("Address not found");
            }

            _context.UserAddresses.Remove(address);
            await _context.SaveChangesAsync();
        }

        public async Task<List<UserPhoneDto>> GetUserPhonesAsync(Guid userId)
        {
            var phones = await _context.UserPhones
                .Where(p => p.UserId == userId)
                .OrderBy(p => p.IsPrimary)
                .ThenBy(p => p.CreatedAt)
                .ToListAsync();

            return phones.Select(MapToPhoneDto).ToList();
        }

        public async Task<UserPhoneDto> AddUserPhoneAsync(Guid userId, AddPhoneRequest request)
        {
            var phone = new UserPhone
            {
                UserId = userId,
                PhoneNumber = request.PhoneNumber,
                IsPrimary = request.IsPrimary,
                IsVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.UserPhones.Add(phone);
            await _context.SaveChangesAsync();

            return MapToPhoneDto(phone);
        }

        public async Task DeleteUserPhoneAsync(Guid userId, Guid phoneId)
        {
            var phone = await _context.UserPhones
                .FirstOrDefaultAsync(p => p.Id == phoneId && p.UserId == userId);

            if (phone == null)
            {
                throw new KeyNotFoundException("Phone not found");
            }

            _context.UserPhones.Remove(phone);
            await _context.SaveChangesAsync();
        }

        public async Task<IdentityDocumentDto> UploadIdentityDocumentAsync(Guid userId, UploadIdentityDocumentRequest request)
        {
            var document = new UserIdentityDocument
            {
                UserId = userId,
                DocumentType = request.DocumentType,
                DocumentNumber = request.DocumentNumber,
                FrontImageUrl = request.FrontImage, // In real implementation, save to file storage
                BackImageUrl = request.BackImage,
                SelfieImageUrl = request.SelfieImage,
                VerificationStatus = "pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.UserIdentityDocuments.Add(document);
            await _context.SaveChangesAsync();

            return MapToIdentityDocumentDto(document);
        }

        public async Task<IdentityDocumentDto> GetIdentityDocumentAsync(Guid userId)
        {
            var document = await _context.UserIdentityDocuments
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (document == null)
            {
                throw new KeyNotFoundException("Identity document not found");
            }

            return MapToIdentityDocumentDto(document);
        }

        private UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                EmailVerified = user.EmailVerified,
                Phone = user.Phone,
                PhoneVerified = user.PhoneVerified,
                FirstName = user.FirstName,
                LastName = user.LastName,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender?.ToString(),
                IdNumber = user.IdNumber,
                Status = user.Status.ToString(),
                VerificationStatus = user.VerificationStatus.ToString(),
                AuthProvider = user.AuthProvider.ToString(),
                ProfileImageUrl = user.ProfileImageUrl,
                PreferredLanguage = user.PreferredLanguage,
                Timezone = user.Timezone,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt
            };
        }

        private UserAddressDto MapToAddressDto(UserAddress address)
        {
            return new UserAddressDto
            {
                Id = address.Id,
                Type = address.Type,
                Country = address.Country,
                City = address.City,
                Street = address.Street,
                HouseNumber = address.HouseNumber,
                ZipCode = address.ZipCode,
                IsPrimary = address.IsPrimary,
                CreatedAt = address.CreatedAt
            };
        }

        private UserPhoneDto MapToPhoneDto(UserPhone phone)
        {
            return new UserPhoneDto
            {
                Id = phone.Id,
                PhoneNumber = phone.PhoneNumber,
                IsPrimary = phone.IsPrimary,
                IsVerified = phone.IsVerified,
                CreatedAt = phone.CreatedAt
            };
        }

        private IdentityDocumentDto MapToIdentityDocumentDto(UserIdentityDocument document)
        {
            return new IdentityDocumentDto
            {
                Id = document.Id,
                DocumentType = document.DocumentType,
                DocumentNumber = document.DocumentNumber,
                FrontImageUrl = document.FrontImageUrl,
                BackImageUrl = document.BackImageUrl,
                SelfieImageUrl = document.SelfieImageUrl,
                VerificationStatus = document.VerificationStatus,
                VerifiedAt = document.VerifiedAt,
                RejectionReason = document.RejectionReason,
                CreatedAt = document.CreatedAt
            };
        }

        // OAuth methods
        public async Task<User> FindOrCreateOAuthUserAsync(string email, string name, AuthProvider provider, string providerId)
        {
            try
            {
                // Check if user exists with this email
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (existingUser != null)
                {
                    // Update OAuth info if not set
                    if (string.IsNullOrEmpty(existingUser.ProviderId))
                    {
                        existingUser.AuthProvider = provider;
                        existingUser.ProviderId = providerId;
                        existingUser.EmailVerified = true; // OAuth emails are pre-verified
                        existingUser.UpdatedAt = DateTime.UtcNow;
                        existingUser.LastLoginAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Updated existing user {Email} with OAuth provider {Provider}", email, provider);
                    }
                    else
                    {
                        // Just update last login
                        existingUser.LastLoginAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                    return existingUser;
                }

                // Create new user
                var nameParts = name?.Split(' ', 2) ?? new[] { email, "" };
                var newUser = new User
                {
                    Id = Guid.NewGuid(),
                    Email = email,
                    EmailVerified = true, // OAuth emails are pre-verified
                    Username = email.Split('@')[0] + "_" + Guid.NewGuid().ToString().Substring(0, 6),
                    FirstName = nameParts[0],
                    LastName = nameParts.Length > 1 ? nameParts[1] : "",
                    AuthProvider = provider,
                    ProviderId = providerId,
                    PasswordHash = null, // OAuth users don't have passwords
                    Status = UserStatus.Active,
                    VerificationStatus = UserVerificationStatus.EmailVerified,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created new OAuth user {Email} with provider {Provider}", email, provider);
                return newUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding or creating OAuth user for email {Email}", email);
                throw;
            }
        }

        public async Task<string> GenerateJwtTokenAsync(User user)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_configuration["JwtSettings:SecretKey"]!);
                var expiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["JwtSettings:ExpiryInMinutes"]!));

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim("firstName", user.FirstName),
                        new Claim("lastName", user.LastName)
                    }),
                    Expires = expiresAt,
                    Issuer = _configuration["JwtSettings:Issuer"],
                    Audience = _configuration["JwtSettings:Audience"],
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var accessToken = tokenHandler.WriteToken(token);

                _logger.LogInformation("Generated JWT token for user {UserId}", user.Id);
                return accessToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating JWT token for user {UserId}", user.Id);
                throw;
            }
        }

        public async Task<string> GenerateRefreshTokenAsync(User user)
        {
            try
            {
                // Generate secure random token
                var refreshToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + 
                                   Convert.ToBase64String(Guid.NewGuid().ToByteArray());
                var refreshExpiresAt = DateTime.UtcNow.AddDays(
                    int.Parse(_configuration["JwtSettings:RefreshTokenExpiryInDays"]!));

                // Save session
                var session = new UserSession
                {
                    UserId = user.Id,
                    SessionToken = refreshToken,
                    ExpiresAt = refreshExpiresAt,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.UserSessions.Add(session);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Generated refresh token for user {UserId}", user.Id);
                return refreshToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating refresh token for user {UserId}", user.Id);
                throw;
            }
        }
    }
}
