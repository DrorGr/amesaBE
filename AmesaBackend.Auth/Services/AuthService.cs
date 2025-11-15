using Microsoft.EntityFrameworkCore;
using AmesaBackend.Auth.Data;
using AmesaBackend.Auth.DTOs;
using AmesaBackend.Auth.Models;
using AmesaBackend.Shared.Authentication;
using AmesaBackend.Shared.Events;
using BCrypt.Net;
using System.Security.Claims;
using System.Security.Cryptography;

namespace AmesaBackend.Auth.Services
{
    public class AuthService : IAuthService
    {
        private readonly AuthDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEventPublisher _eventPublisher;
        private readonly IJwtTokenManager _jwtTokenManager;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            AuthDbContext context,
            IConfiguration configuration,
            IEventPublisher eventPublisher,
            IJwtTokenManager jwtTokenManager,
            ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _eventPublisher = eventPublisher;
            _jwtTokenManager = jwtTokenManager;
            _logger = logger;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                // Check if user already exists
                if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                {
                    throw new InvalidOperationException("User with this email already exists");
                }

                if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                {
                    throw new InvalidOperationException("Username is already taken");
                }

                // Create new user
                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    DateOfBirth = request.DateOfBirth,
                    Gender = Enum.TryParse<GenderType>(request.Gender, out var gender) ? gender : null,
                    Phone = request.Phone,
                    AuthProvider = Enum.TryParse<AuthProvider>(request.AuthProvider, out var provider) ? provider : AuthProvider.Email,
                    Status = UserStatus.Pending,
                    VerificationStatus = UserVerificationStatus.Unverified,
                    EmailVerificationToken = GenerateSecureToken(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Publish events
                await _eventPublisher.PublishAsync(new UserCreatedEvent
                {
                    UserId = user.Id,
                    Email = user.Email,
                    Username = user.Username,
                    FirstName = user.FirstName,
                    LastName = user.LastName
                });

                await _eventPublisher.PublishAsync(new EmailVerificationRequestedEvent
                {
                    UserId = user.Id,
                    Email = user.Email,
                    VerificationToken = user.EmailVerificationToken
                });

                // Generate tokens
                var tokens = await GenerateTokensAsync(user);

                _logger.LogInformation("User registered successfully: {Email}", user.Email);

                return new AuthResponse
                {
                    AccessToken = tokens.AccessToken,
                    RefreshToken = tokens.RefreshToken,
                    ExpiresAt = tokens.ExpiresAt,
                    User = MapToUserDto(user)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration");
                throw;
            }
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (user == null)
                {
                    throw new UnauthorizedAccessException("USER_NOT_FOUND: User does not exist. Please sign up first.");
                }

                // Verify password
                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    throw new UnauthorizedAccessException("Invalid email or password");
                }

                if (user.Status == UserStatus.Suspended || user.Status == UserStatus.Banned)
                {
                    throw new UnauthorizedAccessException("Account is suspended or banned");
                }

                // Update last login
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Publish login event
                await _eventPublisher.PublishAsync(new UserLoginEvent
                {
                    UserId = user.Id,
                    Email = user.Email,
                    IpAddress = string.Empty // Will be set by middleware
                });

                // Generate tokens
                var tokens = await GenerateTokensAsync(user);

                _logger.LogInformation("User logged in successfully: {Email}", user.Email);

                return new AuthResponse
                {
                    AccessToken = tokens.AccessToken,
                    RefreshToken = tokens.RefreshToken,
                    ExpiresAt = tokens.ExpiresAt,
                    User = MapToUserDto(user)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user login");
                throw;
            }
        }

        public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            try
            {
                var session = await _context.UserSessions
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.SessionToken == request.RefreshToken && s.IsActive);

                if (session == null || session.ExpiresAt < DateTime.UtcNow)
                {
                    throw new UnauthorizedAccessException("Invalid or expired refresh token");
                }

                if (session.User!.Status == UserStatus.Suspended || session.User.Status == UserStatus.Banned)
                {
                    throw new UnauthorizedAccessException("Account is suspended or banned");
                }

                // Generate new tokens
                var tokens = await GenerateTokensAsync(session.User);

                _logger.LogInformation("Token refreshed successfully for user: {Email}", session.User.Email);

                return new AuthResponse
                {
                    AccessToken = tokens.AccessToken,
                    RefreshToken = tokens.RefreshToken,
                    ExpiresAt = tokens.ExpiresAt,
                    User = MapToUserDto(session.User)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                throw;
            }
        }

        public async Task LogoutAsync(string refreshToken)
        {
            try
            {
                var session = await _context.UserSessions
                    .FirstOrDefaultAsync(s => s.SessionToken == refreshToken);

                if (session != null)
                {
                    session.IsActive = false;
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("User logged out successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                throw;
            }
        }

        public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (user != null)
                {
                    user.PasswordResetToken = GenerateSecureToken();
                    user.PasswordResetExpiresAt = DateTime.UtcNow.AddHours(1);
                    await _context.SaveChangesAsync();

                    await _eventPublisher.PublishAsync(new PasswordResetRequestedEvent
                    {
                        UserId = user.Id,
                        Email = user.Email,
                        ResetToken = user.PasswordResetToken
                    });
                }

                _logger.LogInformation("Password reset requested for email: {Email}", request.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forgot password");
                throw;
            }
        }

        public async Task ResetPasswordAsync(ResetPasswordRequest request)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.PasswordResetToken == request.Token && 
                                            u.PasswordResetExpiresAt > DateTime.UtcNow);

                if (user == null)
                {
                    throw new InvalidOperationException("Invalid or expired reset token");
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                user.PasswordResetToken = null;
                user.PasswordResetExpiresAt = null;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Password reset successfully for user: {Email}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset");
                throw;
            }
        }

        public async Task VerifyEmailAsync(VerifyEmailRequest request)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.EmailVerificationToken == request.Token);

                if (user == null)
                {
                    throw new InvalidOperationException("Invalid verification token");
                }

                user.EmailVerified = true;
                user.EmailVerificationToken = null;
                user.VerificationStatus = UserVerificationStatus.EmailVerified;
                user.Status = UserStatus.Active;
                await _context.SaveChangesAsync();

                await _eventPublisher.PublishAsync(new UserEmailVerifiedEvent
                {
                    UserId = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName
                });

                await _eventPublisher.PublishAsync(new UserVerifiedEvent
                {
                    UserId = user.Id,
                    Email = user.Email,
                    VerificationType = "Email"
                });

                _logger.LogInformation("Email verified successfully for user: {Email}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during email verification");
                throw;
            }
        }

        public async Task VerifyPhoneAsync(VerifyPhoneRequest request)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Phones)
                    .FirstOrDefaultAsync(u => u.Phone == request.Phone);

                if (user == null)
                {
                    throw new InvalidOperationException("User not found");
                }

                var phone = user.Phones.FirstOrDefault(p => p.PhoneNumber == request.Phone);
                if (phone == null || phone.VerificationCode != request.Code || 
                    phone.VerificationExpiresAt < DateTime.UtcNow)
                {
                    throw new InvalidOperationException("Invalid verification code");
                }

                phone.IsVerified = true;
                phone.VerificationCode = null;
                phone.VerificationExpiresAt = null;
                user.PhoneVerified = true;

                if (user.VerificationStatus == UserVerificationStatus.EmailVerified)
                {
                    user.VerificationStatus = UserVerificationStatus.FullyVerified;
                }

                await _context.SaveChangesAsync();

                // Publish verification event
                await _eventPublisher.PublishAsync(new UserVerifiedEvent
                {
                    UserId = user.Id,
                    Email = user.Email,
                    VerificationType = "Phone"
                });

                _logger.LogInformation("Phone verified successfully for user: {Email}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during phone verification");
                throw;
            }
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                // Use shared library's JWT token manager for validation
                var claims = _jwtTokenManager.GetClaimsFromExpiredToken(token);
                return claims != null && claims.Any();
            }
            catch
            {
                return false;
            }
        }

        public async Task<UserDto> GetCurrentUserAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException("User not found");
            }

            return MapToUserDto(user);
        }

        public async Task<(AuthResponse Response, bool IsNewUser)> CreateOrUpdateOAuthUserAsync(string email, string providerId, AuthProvider provider, string? firstName = null, string? lastName = null)
        {
            try
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);

                User user;
                bool isNewUser = false;

                if (existingUser != null)
                {
                    user = existingUser;
                    isNewUser = false;
                    
                    if (user.AuthProvider == AuthProvider.Email)
                    {
                        user.AuthProvider = provider;
                        user.ProviderId = providerId;
                        _logger.LogInformation("Linking OAuth provider {Provider} to existing email user {Email}", provider, email);
                    }
                    else if (user.AuthProvider == provider)
                    {
                        if (string.IsNullOrWhiteSpace(user.ProviderId) || user.ProviderId != providerId)
                        {
                            user.ProviderId = providerId;
                        }
                        _logger.LogInformation("OAuth login for existing {Provider} user {Email}", provider, email);
                    }

                    if (!string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(user.FirstName))
                    {
                        user.FirstName = firstName;
                    }
                    if (!string.IsNullOrWhiteSpace(lastName) && string.IsNullOrWhiteSpace(user.LastName))
                    {
                        user.LastName = lastName;
                    }

                    user.LastLoginAt = DateTime.UtcNow;
                    user.UpdatedAt = DateTime.UtcNow;

                    // Publish update event
                    await _eventPublisher.PublishAsync(new UserUpdatedEvent
                    {
                        UserId = user.Id,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName
                    });
                }
                else
                {
                    user = new User
                    {
                        Username = email.Split('@')[0] + "_" + provider.ToString().ToLower(),
                        Email = email,
                        PasswordHash = "OAUTH_USER_NO_PASSWORD",
                        FirstName = firstName ?? string.Empty,
                        LastName = lastName ?? string.Empty,
                        AuthProvider = provider,
                        ProviderId = providerId,
                        Status = UserStatus.Active,
                        VerificationStatus = UserVerificationStatus.EmailVerified,
                        EmailVerified = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        LastLoginAt = DateTime.UtcNow
                    };

                    var baseUsername = user.Username;
                    var counter = 1;
                    while (await _context.Users.AnyAsync(u => u.Username == user.Username))
                    {
                        user.Username = $"{baseUsername}{counter}";
                        counter++;
                    }

                    _context.Users.Add(user);
                    isNewUser = true;
                    _logger.LogInformation("Created new user from OAuth {Provider}: {Email}", provider, email);
                }

                await _context.SaveChangesAsync();

                // Publish user created event if new user
                if (isNewUser)
                {
                    await _eventPublisher.PublishAsync(new UserCreatedEvent
                    {
                        UserId = user.Id,
                        Email = user.Email,
                        Username = user.Username,
                        FirstName = user.FirstName,
                        LastName = user.LastName
                    });
                }

                // Generate tokens
                var tokens = await GenerateTokensAsync(user);

                var response = new AuthResponse
                {
                    AccessToken = tokens.AccessToken,
                    RefreshToken = tokens.RefreshToken,
                    ExpiresAt = tokens.ExpiresAt,
                    User = MapToUserDto(user)
                };
                
                return (response, isNewUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating/updating OAuth user for {Provider}: {Email}", provider, email);
                throw;
            }
        }

        private async Task<(string AccessToken, string RefreshToken, DateTime ExpiresAt)> GenerateTokensAsync(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("firstName", user.FirstName),
                new Claim("lastName", user.LastName)
            };

            var expiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["JwtSettings:ExpiryInMinutes"] ?? "60"));
            var accessToken = _jwtTokenManager.GenerateAccessToken(claims, expiresAt);

            // Create refresh token
            var refreshToken = GenerateSecureToken();
            var refreshExpiresAt = DateTime.UtcNow.AddDays(int.Parse(_configuration["JwtSettings:RefreshTokenExpiryInDays"] ?? "7"));

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

            return (accessToken, refreshToken, expiresAt);
        }

        private string GenerateSecureToken()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[32];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
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
    }
}

