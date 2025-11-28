using Microsoft.EntityFrameworkCore;
using AmesaBackend.Auth.Data;
using AmesaBackend.Auth.DTOs;
using AmesaBackend.Auth.Models;
using AmesaBackend.Shared.Authentication;
using AmesaBackend.Shared.Events;
using BCrypt.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using Amazon.EventBridge;

namespace AmesaBackend.Auth.Services
{
    public class AuthService : IAuthService
    {
        private readonly AuthDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEventPublisher _eventPublisher;
        private readonly IJwtTokenManager _jwtTokenManager;
        private readonly ILogger<AuthService> _logger;
        private readonly IUserPreferencesService? _userPreferencesService;

        public AuthService(
            AuthDbContext context,
            IConfiguration configuration,
            IEventPublisher eventPublisher,
            IJwtTokenManager jwtTokenManager,
            ILogger<AuthService> logger,
            IUserPreferencesService? userPreferencesService = null)
        {
            _context = context;
            _configuration = configuration;
            _eventPublisher = eventPublisher;
            _jwtTokenManager = jwtTokenManager;
            _logger = logger;
            _userPreferencesService = userPreferencesService;
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

                var userDto = MapToUserDto(user);
                userDto.LotteryData = await GetUserLotteryDataAsync(user.Id);

                return new AuthResponse
                {
                    AccessToken = tokens.AccessToken,
                    RefreshToken = tokens.RefreshToken,
                    ExpiresAt = tokens.ExpiresAt,
                    User = userDto
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

        public Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                // TODO: Implement GetClaimsFromExpiredToken method in IJwtTokenManager
                // var claims = _jwtTokenManager.GetClaimsFromExpiredToken(token);
                // return claims != null && claims.Any();
                return Task.FromResult(false); // Temporary fix - always return false for expired tokens
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        public async Task<UserDto> GetCurrentUserAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException("User not found");
            }

            var userDto = MapToUserDto(user);
            userDto.LotteryData = await GetUserLotteryDataAsync(userId);
            return userDto;
        }

        private async Task<UserLotteryDataDto?> GetUserLotteryDataAsync(Guid userId)
        {
            try
            {
                // Query the user_lottery_dashboard view
                // Use column aliases to match PascalCase property names in UserLotteryDashboardResult
                var sql = @"
                    SELECT 
                        favorite_houses_count AS ""FavoriteHousesCount"",
                        active_entries_count AS ""ActiveEntriesCount"",
                        total_entries_count AS ""TotalEntriesCount"",
                        total_wins AS ""TotalWins"",
                        total_spending AS ""TotalSpending"",
                        total_winnings AS ""TotalWinnings"",
                        win_rate_percentage AS ""WinRatePercentage"",
                        average_spending_per_entry AS ""AverageSpendingPerEntry"",
                        favorite_house_id AS ""FavoriteHouseId"",
                        most_active_month AS ""MostActiveMonth"",
                        last_entry_date AS ""LastEntryDate""
                    FROM amesa_auth.user_lottery_dashboard
                    WHERE user_id = {0}";

                var result = await _context.Database
                    .SqlQueryRaw<UserLotteryDashboardResult>(sql, userId)
                    .FirstOrDefaultAsync();

                // Get favorite house IDs array
                var favoriteHouseIds = new List<Guid>();
                if (_userPreferencesService != null)
                {
                    try
                    {
                        favoriteHouseIds = await _userPreferencesService.GetFavoriteHouseIdsAsync(userId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error retrieving favorite house IDs for user {UserId}", userId);
                    }
                }

                if (result == null)
                {
                    // Return defaults with empty arrays
                    return new UserLotteryDataDto
                    {
                        FavoriteHouseIds = favoriteHouseIds,
                        ActiveEntries = new List<object>()
                    };
                }

                return new UserLotteryDataDto
                {
                    FavoriteHouseIds = favoriteHouseIds,
                    ActiveEntries = new List<object>(), // TODO: Populate with actual LotteryTicketDto objects from lottery service
                    TotalEntriesCount = result.TotalEntriesCount,
                    TotalWins = result.TotalWins,
                    TotalSpending = result.TotalSpending,
                    TotalWinnings = result.TotalWinnings,
                    WinRatePercentage = result.WinRatePercentage,
                    AverageSpendingPerEntry = result.AverageSpendingPerEntry,
                    FavoriteHouseId = result.FavoriteHouseId,
                    MostActiveMonth = result.MostActiveMonth,
                    LastEntryDate = result.LastEntryDate
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error retrieving lottery data for user {UserId}", userId);
                return null; // Return null if view doesn't exist or query fails
            }
        }

        private class UserLotteryDashboardResult
        {
            public int FavoriteHousesCount { get; set; }
            public int ActiveEntriesCount { get; set; }
            public int TotalEntriesCount { get; set; }
            public int TotalWins { get; set; }
            public decimal TotalSpending { get; set; }
            public decimal TotalWinnings { get; set; }
            public decimal WinRatePercentage { get; set; }
            public decimal AverageSpendingPerEntry { get; set; }
            public Guid? FavoriteHouseId { get; set; }
            public string? MostActiveMonth { get; set; }
            public DateTime? LastEntryDate { get; set; }
        }

        public async Task<(AuthResponse Response, bool IsNewUser)> CreateOrUpdateOAuthUserAsync(
            string email, 
            string providerId, 
            AuthProvider provider, 
            string? firstName = null, 
            string? lastName = null,
            DateTime? dateOfBirth = null,
            string? gender = null,
            string? profileImageUrl = null)
        {
            // #region agent log
            _logger.LogInformation("[DEBUG] CreateOrUpdateOAuthUserAsync:entry hypothesisId=E email={Email} provider={Provider}", email, provider);
            // #endregion
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

                    // Only update empty/null fields, don't overwrite existing data
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
                        user.DateOfBirth = dateOfBirth.Value;
                    }
                    if (!string.IsNullOrWhiteSpace(gender) && !user.Gender.HasValue)
                    {
                        if (Enum.TryParse<GenderType>(gender, true, out var genderEnum))
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

                    // Publish update event (non-fatal - don't fail OAuth if EventBridge fails)
                    try
                    {
                        await _eventPublisher.PublishAsync(new UserUpdatedEvent
                        {
                            UserId = user.Id,
                            Email = user.Email,
                            FirstName = user.FirstName,
                            LastName = user.LastName
                        });
                    }
                    catch (Exception ex)
                    {
                        // #region agent log
                        var exType = ex.GetType().FullName;
                        var isEventBridge = ex is Amazon.EventBridge.AmazonEventBridgeException;
                        var innerIsEventBridge = ex.InnerException is Amazon.EventBridge.AmazonEventBridgeException;
                        _logger.LogError(ex, "[DEBUG] EventBridge catch:entry exType={ExType} isEventBridge={IsEventBridge} innerIsEventBridge={InnerIsEventBridge} hypothesisId=D", exType, isEventBridge, innerIsEventBridge);
                        // #endregion
                        _logger.LogError(ex, "Failed to publish UserUpdatedEvent to EventBridge (non-fatal, continuing OAuth flow)");
                        // Don't re-throw - EventBridge errors should not break OAuth authentication
                    }
                }
                else
                {
                    // Parse gender if provided
                    GenderType? genderEnum = null;
                    if (!string.IsNullOrWhiteSpace(gender) && Enum.TryParse<GenderType>(gender, true, out var parsedGender))
                    {
                        genderEnum = parsedGender;
                    }

                    user = new User
                    {
                        Username = email.Split('@')[0] + "_" + provider.ToString().ToLower(),
                        Email = email,
                        PasswordHash = "OAUTH_USER_NO_PASSWORD",
                        FirstName = firstName ?? string.Empty,
                        LastName = lastName ?? string.Empty,
                        DateOfBirth = dateOfBirth,
                        Gender = genderEnum,
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

                // Publish user created event if new user (non-fatal - don't fail OAuth if EventBridge fails)
                if (isNewUser)
                {
                    try
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
                    catch (Exception ex)
                    {
                        // #region agent log
                        var exType = ex.GetType().FullName;
                        var isEventBridge = ex is Amazon.EventBridge.AmazonEventBridgeException;
                        var innerIsEventBridge = ex.InnerException is Amazon.EventBridge.AmazonEventBridgeException;
                        _logger.LogError(ex, "[DEBUG] EventBridge catch:entry exType={ExType} isEventBridge={IsEventBridge} innerIsEventBridge={InnerIsEventBridge} hypothesisId=D", exType, isEventBridge, innerIsEventBridge);
                        // #endregion
                        _logger.LogError(ex, "Failed to publish UserCreatedEvent to EventBridge (non-fatal, continuing OAuth flow)");
                        // Don't re-throw - EventBridge errors should not break OAuth authentication
                    }
                }

                // #region agent log
                _logger.LogInformation("[DEBUG] CreateOrUpdateOAuthUserAsync:before-GenerateTokensAsync hypothesisId=E");
                // #endregion

                // Generate tokens
                var tokens = await GenerateTokensAsync(user);

                // #region agent log
                _logger.LogInformation("[DEBUG] CreateOrUpdateOAuthUserAsync:after-GenerateTokensAsync hypothesisId=E hasTokens={HasTokens}", tokens.AccessToken != null);
                // #endregion

                var response = new AuthResponse
                {
                    AccessToken = tokens.AccessToken!,
                    RefreshToken = tokens.RefreshToken!,
                    ExpiresAt = tokens.ExpiresAt,
                    User = MapToUserDto(user)
                };
                
                // #region agent log
                _logger.LogInformation("[DEBUG] CreateOrUpdateOAuthUserAsync:success hypothesisId=E returning response");
                // #endregion
                
                return (response, isNewUser);
            }
            catch (Exception ex)
            {
                // #region agent log
                var exType = ex.GetType().FullName;
                var exMessage = ex.Message;
                var isEventBridge = ex is Amazon.EventBridge.AmazonEventBridgeException;
                var innerIsEventBridge = ex.InnerException is Amazon.EventBridge.AmazonEventBridgeException;
                _logger.LogError(ex, "[DEBUG] CreateOrUpdateOAuthUserAsync:catch exType={ExType} exMessage={ExMessage} isEventBridge={IsEventBridge} innerIsEventBridge={InnerIsEventBridge} hypothesisId=E", exType, exMessage, isEventBridge, innerIsEventBridge);
                // #endregion

                // Check if this is an EventBridge exception (or has EventBridge as inner exception)
                // EventBridge errors should not fail OAuth authentication
                var isEventBridgeException = ex is Amazon.EventBridge.AmazonEventBridgeException ||
                                           ex.InnerException is Amazon.EventBridge.AmazonEventBridgeException;
                
                if (isEventBridgeException)
                {
                    _logger.LogWarning(ex, "[DEBUG] CreateOrUpdateOAuthUserAsync:EventBridge-detected attempting recovery hypothesisId=E");
                    _logger.LogWarning(ex, "EventBridge error in CreateOrUpdateOAuthUserAsync for {Provider}: {Email} (non-fatal, attempting recovery)", provider, email);
                    // Don't rethrow EventBridge exceptions - they're non-fatal
                    // Try to recover by getting the user and generating tokens
                    // This should not happen if inner try-catch is working, but adding as safety net
                    try
                    {
                        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                        if (existingUser != null)
                        {
                            var tokens = await GenerateTokensAsync(existingUser);
                            var response = new AuthResponse
                            {
                                AccessToken = tokens.AccessToken,
                                RefreshToken = tokens.RefreshToken,
                                ExpiresAt = tokens.ExpiresAt,
                                User = MapToUserDto(existingUser)
                            };
                            _logger.LogInformation("Successfully recovered from EventBridge error for {Provider}: {Email}", provider, email);
                            return (response, false);
                        }
                    }
                    catch (Exception recoveryEx)
                    {
                        _logger.LogError(recoveryEx, "Failed to recover from EventBridge error for {Provider}: {Email}", provider, email);
                    }
                    // If recovery fails, rethrow the original exception
                    throw;
                }
                
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

