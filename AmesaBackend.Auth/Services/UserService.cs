using Microsoft.EntityFrameworkCore;
using AmesaBackend.Auth.Data;
using AmesaBackend.Auth.DTOs;
using AmesaBackend.Auth.Models;
using AmesaBackend.Auth.Services.Interfaces;
using AmesaBackend.Shared.Events;

namespace AmesaBackend.Auth.Services
{
    public class UserService : IUserService
    {
        private readonly AuthDbContext _context;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<UserService> _logger;

        public UserService(AuthDbContext context, IEventPublisher eventPublisher, ILogger<UserService> logger)
        {
            _context = context;
            _eventPublisher = eventPublisher;
            _logger = logger;
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

            // Check if user is verified - if so, lock certain fields
            bool isVerified = user.VerificationStatus == UserVerificationStatus.IdentityVerified || 
                             user.VerificationStatus == UserVerificationStatus.FullyVerified;

            if (isVerified)
            {
                // Locked fields after verification - can only be updated from ID document
                // Note: IdNumber is not locked as it can be updated from ID document OCR
                var lockedFields = new List<string>();
                
                if (!string.IsNullOrEmpty(request.FirstName))
                    lockedFields.Add("FirstName");
                if (!string.IsNullOrEmpty(request.LastName))
                    lockedFields.Add("LastName");
                if (request.DateOfBirth.HasValue)
                    lockedFields.Add("DateOfBirth");
                if (!string.IsNullOrEmpty(request.Gender))
                    lockedFields.Add("Gender");
                
                if (lockedFields.Any())
                {
                    throw new InvalidOperationException(
                        $"PROFILE_LOCKED_AFTER_VERIFICATION: The following fields are locked after ID verification and can only be updated from your ID document: {string.Join(", ", lockedFields)}. " +
                        "To update these fields, please contact support or re-verify your ID document.");
                }
            }

            // Update locked fields only if not verified
            if (!isVerified)
            {
                if (!string.IsNullOrEmpty(request.FirstName))
                    user.FirstName = request.FirstName;

                if (!string.IsNullOrEmpty(request.LastName))
                    user.LastName = request.LastName;

                if (request.DateOfBirth.HasValue)
                    user.DateOfBirth = request.DateOfBirth.Value;

                if (!string.IsNullOrEmpty(request.Gender) && Enum.TryParse<GenderType>(request.Gender, out var gender))
                    user.Gender = gender;

                if (!string.IsNullOrEmpty(request.IdNumber))
                    user.IdNumber = request.IdNumber;
            }
            else
            {
                // Even if verified, IdNumber can be updated (it's extracted from ID document)
                if (!string.IsNullOrEmpty(request.IdNumber))
                    user.IdNumber = request.IdNumber;
            }

            // Non-locked fields can always be updated
            if (!string.IsNullOrEmpty(request.PreferredLanguage))
                user.PreferredLanguage = request.PreferredLanguage;

            if (!string.IsNullOrEmpty(request.Timezone))
                user.Timezone = request.Timezone;

            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _eventPublisher.PublishAsync(new UserUpdatedEvent
            {
                UserId = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            });

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
                FrontImageUrl = request.FrontImage,
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
    }
}

