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
        private readonly IAccountLockoutService _accountLockoutService;
        private readonly IPasswordValidatorService _passwordValidator;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            AuthDbContext context,
            IConfiguration configuration,
            IEventPublisher eventPublisher,
            IJwtTokenManager jwtTokenManager,
            IAccountLockoutService accountLockoutService,
            IPasswordValidatorService passwordValidator,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _eventPublisher = eventPublisher;
            _jwtTokenManager = jwtTokenManager;
            _accountLockoutService = accountLockoutService;
            _passwordValidator = passwordValidator;
            _httpContextAccessor = httpContextAccessor;
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

                // Validate password
                var passwordValidation = await _passwordValidator.ValidatePasswordAsync(request.Password);
                if (!passwordValidation.IsValid)
                {
                    throw new InvalidOperationException($"Password validation failed: {string.Join(", ", passwordValidation.Errors)}");
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

                // Use transaction to ensure atomicity
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    // Save password to history
                    var passwordHistory = new UserPasswordHistory
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        PasswordHash = user.PasswordHash,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Set<UserPasswordHistory>().Add(passwordHistory);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }

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
                    VerificationToken = user.EmailVerificationToken ?? string.Empty
                });

                // Only generate tokens if email is verified (OAuth users)
                // Regular email users must verify before getting tokens
                if (!user.EmailVerified)
                {
                    _logger.LogInformation("User registered successfully, email verification required: {Email}", user.Email);
                    return new AuthResponse
                    {
                        RequiresEmailVerification = true,
                        User = MapToUserDto(user)
                    };
                }

                // Generate tokens for verified users (OAuth)
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

                // Check lockout BEFORE password verification
                if (await _accountLockoutService.IsLockedAsync(request.Email))
                {
                    var lockedUntil = await _accountLockoutService.GetLockedUntilAsync(request.Email);
                    throw new UnauthorizedAccessException($"Account is locked until {lockedUntil:yyyy-MM-dd HH:mm:ss} UTC");
                }

                // Check email verification
                if (!user.EmailVerified)
                {
                    throw new UnauthorizedAccessException("Please verify your email before logging in. Check your inbox for the verification link.");
                }

                // Verify password
                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    await _accountLockoutService.RecordFailedAttemptAsync(request.Email);
                    throw new UnauthorizedAccessException("Invalid email or password");
                }

                // On success: clear failed attempts
                await _accountLockoutService.ClearFailedAttemptsAsync(request.Email);

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

                // Check if token was already rotated (potential theft)
                // If IsRotated is true, this token was already used to create a new token
                // Using it again indicates token theft or replay attack
                if (session.IsRotated)
                {
                    _logger.LogWarning("Potential token reuse detected for user {UserId} - token was already rotated", session.UserId);
                    // Invalidate all sessions for security
                    await InvalidateAllSessionsAsync(session.UserId!.Value);
                    throw new UnauthorizedAccessException("Security violation detected. Please log in again.");
                }

                // Invalidate old token (token rotation)
                session.IsActive = false;
                session.IsRotated = true;

                // Generate new tokens
                var tokens = await GenerateTokensAsync(session.User);

                // Create new session with rotated token - capture fresh IP/device from current request
                var httpContext = _httpContextAccessor.HttpContext;
                var ipAddress = httpContext?.Items["ClientIp"]?.ToString() 
                    ?? httpContext?.Connection.RemoteIpAddress?.ToString() 
                    ?? httpContext?.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
                    ?? session.IpAddress ?? "unknown";
                var userAgent = httpContext?.Request.Headers["User-Agent"].ToString() ?? session.UserAgent ?? "unknown";
                var deviceId = httpContext?.Items["DeviceId"]?.ToString() ?? GenerateDeviceId(userAgent, ipAddress);
                var deviceName = ExtractDeviceName(userAgent);

                var refreshExpiresAt = DateTime.UtcNow.AddDays(int.Parse(_configuration["JwtSettings:RefreshTokenExpiryInDays"] ?? "7"));
                var newSession = new UserSession
                {
                    Id = Guid.NewGuid(),
                    UserId = session.UserId,
                    SessionToken = tokens.RefreshToken,
                    PreviousSessionToken = session.SessionToken, // Track rotation chain
                    ExpiresAt = refreshExpiresAt,
                    IsActive = true,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    DeviceId = deviceId,
                    DeviceName = deviceName,
                    CreatedAt = DateTime.UtcNow,
                    LastActivity = DateTime.UtcNow
                };

                _context.UserSessions.Add(newSession);

                // Limit to 5 active sessions - remove oldest if exceeded
                await EnforceSessionLimitAsync(session.UserId!.Value);

                await _context.SaveChangesAsync();

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

                // Validate new password
                var passwordValidation = await _passwordValidator.ValidatePasswordAsync(request.NewPassword, user.Id);
                if (!passwordValidation.IsValid)
                {
                    throw new InvalidOperationException($"Password validation failed: {string.Join(", ", passwordValidation.Errors)}");
                }

                // Check password history
                if (await _passwordValidator.IsPasswordInHistoryAsync(request.NewPassword, user.Id))
                {
                    throw new InvalidOperationException("Password was recently used. Please choose a different password");
                }

                // Use transaction to ensure atomicity
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var oldPasswordHash = user.PasswordHash;
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                    user.PasswordResetToken = null;
                    user.PasswordResetExpiresAt = null;
                    await _context.SaveChangesAsync();

                    // Save to password history
                    var passwordHistory = new UserPasswordHistory
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        PasswordHash = user.PasswordHash,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Set<UserPasswordHistory>().Add(passwordHistory);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }

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

                // Invalidate email verification cache
                try
                {
                    var distributedCache = _httpContextAccessor.HttpContext?.RequestServices.GetService<Microsoft.Extensions.Caching.Distributed.IDistributedCache>();
                    if (distributedCache != null)
                    {
                        var cacheKey = $"email_verified:{user.Id}";
                        await distributedCache.RemoveAsync(cacheKey);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to invalidate email verification cache for user {UserId}", user.Id);
                    // Don't fail the request if cache invalidation fails
                }

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

        public async Task ResendVerificationEmailAsync(ResendVerificationRequest request)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (user == null)
                {
                    // Don't reveal if user exists - security best practice
                    _logger.LogWarning("Resend verification requested for non-existent email: {Email}", request.Email);
                    return;
                }

                if (user.EmailVerified)
                {
                    _logger.LogInformation("Resend verification requested for already verified email: {Email}", request.Email);
                    return;
                }

                // Regenerate token if expired (>24 hours) or missing
                var shouldRegenerate = string.IsNullOrEmpty(user.EmailVerificationToken) ||
                    (user.CreatedAt < DateTime.UtcNow.AddHours(-24));

                if (shouldRegenerate)
                {
                    user.EmailVerificationToken = GenerateSecureToken();
                    user.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                // Publish verification email event
                await _eventPublisher.PublishAsync(new EmailVerificationRequestedEvent
                {
                    UserId = user.Id,
                    Email = user.Email,
                    VerificationToken = user.EmailVerificationToken ?? string.Empty
                });

                _logger.LogInformation("Verification email resent for user: {Email}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending verification email");
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
                // TODO: Implement GetClaimsFromExpiredToken method in IJwtTokenManager
                // var claims = _jwtTokenManager.GetClaimsFromExpiredToken(token);
                // return claims != null && claims.Any();
                return false; // Temporary fix - always return false for expired tokens
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

        public async Task<(AuthResponse Response, bool IsNewUser)> CreateOrUpdateOAuthUserAsync(string email, string providerId, AuthProvider provider, string? firstName = null, string? lastName = null, DateTime? dateOfBirth = null, string? gender = null, string? profileImageUrl = null)
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
                    if (dateOfBirth.HasValue && !user.DateOfBirth.HasValue)
                    {
                        user.DateOfBirth = dateOfBirth;
                    }
                    if (!string.IsNullOrWhiteSpace(gender) && !user.Gender.HasValue)
                    {
                        if (Enum.TryParse<GenderType>(gender, out var genderEnum))
                        {
                            user.Gender = genderEnum;
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(profileImageUrl) && string.IsNullOrWhiteSpace(user.ProfileImageUrl))
                    {
                        user.ProfileImageUrl = profileImageUrl;
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
                        DateOfBirth = dateOfBirth,
                        Gender = !string.IsNullOrWhiteSpace(gender) && Enum.TryParse<GenderType>(gender, out var genderEnum) ? genderEnum : null,
                        ProfileImageUrl = profileImageUrl,
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

            // Extract IP and device info - use middleware values if available
            var httpContext = _httpContextAccessor.HttpContext;
            var ipAddress = httpContext?.Items["ClientIp"]?.ToString() 
                ?? httpContext?.Connection.RemoteIpAddress?.ToString() 
                ?? httpContext?.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
                ?? "unknown";
            var userAgent = httpContext?.Request.Headers["User-Agent"].ToString() ?? "unknown";
            var deviceId = httpContext?.Items["DeviceId"]?.ToString() ?? GenerateDeviceId(userAgent, ipAddress);
            var deviceName = ExtractDeviceName(userAgent);

            // Save session
            var session = new UserSession
            {
                UserId = user.Id,
                SessionToken = refreshToken,
                ExpiresAt = refreshExpiresAt,
                IsActive = true,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                DeviceId = deviceId,
                DeviceName = deviceName,
                CreatedAt = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow
            };

            _context.UserSessions.Add(session);

            // Limit to 5 active sessions
            await EnforceSessionLimitAsync(user.Id);

            await _context.SaveChangesAsync();

            return (accessToken, refreshToken, expiresAt);
        }

        private async Task EnforceSessionLimitAsync(Guid userId)
        {
            // Use transaction to prevent race conditions when multiple requests create sessions simultaneously
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var activeSessions = await _context.UserSessions
                    .Where(s => s.UserId == userId && s.IsActive && s.ExpiresAt > DateTime.UtcNow)
                    .OrderBy(s => s.CreatedAt)
                    .ToListAsync();

                if (activeSessions.Count >= 5)
                {
                    // Remove oldest sessions (keep 4 most recent)
                    var sessionsToRemove = activeSessions.Take(activeSessions.Count - 4).ToList();
                    foreach (var session in sessionsToRemove)
                    {
                        session.IsActive = false;
                    }
                    await _context.SaveChangesAsync();
                }
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task InvalidateAllSessionsAsync(Guid userId)
        {
            var sessions = await _context.UserSessions
                .Where(s => s.UserId == userId && s.IsActive)
                .ToListAsync();

            foreach (var session in sessions)
            {
                session.IsActive = false;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<UserSessionDto>> GetActiveSessionsAsync(Guid userId)
        {
            var sessions = await _context.UserSessions
                .Where(s => s.UserId == userId && s.IsActive && s.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(s => s.LastActivity)
                .ToListAsync();

            return sessions.Select(s => new UserSessionDto
            {
                Id = s.Id,
                DeviceName = s.DeviceName ?? "Unknown Device",
                IpAddress = s.IpAddress ?? "Unknown",
                LastActivity = s.LastActivity,
                CreatedAt = s.CreatedAt,
                ExpiresAt = s.ExpiresAt
            }).ToList();
        }

        public async Task LogoutFromDeviceAsync(Guid userId, string sessionToken)
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.SessionToken == sessionToken);

            if (session != null)
            {
                session.IsActive = false;
                await _context.SaveChangesAsync();
            }
        }

        public async Task LogoutAllDevicesAsync(Guid userId)
        {
            await InvalidateAllSessionsAsync(userId);
        }

        private string GenerateDeviceId(string userAgent, string ipAddress)
        {
            // Hash user agent + IP prefix for device fingerprinting
            var ipPrefix = ipAddress.Split('.').Take(3).Aggregate((a, b) => $"{a}.{b}");
            var input = $"{userAgent}|{ipPrefix}";
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hash).Substring(0, 16);
        }

        private string ExtractDeviceName(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent)) return "Unknown Device";

            // Simple device name extraction
            if (userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase))
                return "Windows Device";
            if (userAgent.Contains("Mac", StringComparison.OrdinalIgnoreCase))
                return "Mac Device";
            if (userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase))
                return "Linux Device";
            if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
                return "Android Device";
            if (userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) || userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase))
                return "iOS Device";

            return "Unknown Device";
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

