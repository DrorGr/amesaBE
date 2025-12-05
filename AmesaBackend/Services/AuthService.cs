using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AmesaBackend.Data;
using AmesaBackend.DTOs;
using AmesaBackend.Models;
using BCrypt.Net;

namespace AmesaBackend.Services
{
    public class AuthService : IAuthService
    {
        private readonly AmesaDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            AmesaDbContext context,
            IConfiguration configuration,
            IEmailService emailService,
            ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
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

                // Send verification email
                await _emailService.SendEmailVerificationAsync(user.Email, user.EmailVerificationToken);

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

                // Check if user exists first - throw specific exception for user not found
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

                if (session.User?.Status == UserStatus.Suspended || session.User?.Status == UserStatus.Banned)
                {
                    throw new UnauthorizedAccessException("Account is suspended or banned");
                }

                if (session.User == null)
                {
                    throw new UnauthorizedAccessException("User not found for session");
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

                    await _emailService.SendPasswordResetAsync(user.Email, user.PasswordResetToken);
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
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_configuration["JwtSettings:SecretKey"]!);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["JwtSettings:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["JwtSettings:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5) // Allow 5 minute clock difference for reliability
                }, out SecurityToken validatedToken);

                return true;
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
            try
            {
                // Check if user exists by email
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);

                User user;
                bool isNewUser = false;

                if (existingUser != null)
                {
                    // User exists - update provider info if needed
                    user = existingUser;
                    isNewUser = false;
                    
                    // If user was created with email auth, link OAuth account
                    if (user.AuthProvider == AuthProvider.Email)
                    {
                        user.AuthProvider = provider;
                        user.ProviderId = providerId;
                        _logger.LogInformation("Linking OAuth provider {Provider} to existing email user {Email}", provider, email);
                    }
                    // If user already has this provider, just update last login
                    else if (user.AuthProvider == provider)
                    {
                        // Update ProviderId if it's missing or different
                        if (string.IsNullOrWhiteSpace(user.ProviderId) || user.ProviderId != providerId)
                        {
                            user.ProviderId = providerId;
                        }
                        _logger.LogInformation("OAuth login for existing {Provider} user {Email}", provider, email);
                    }
                    // If user has different provider, keep existing provider but log
                    else
                    {
                        _logger.LogWarning("User {Email} attempted login with {Provider} but account uses {ExistingProvider}", 
                            email, provider, user.AuthProvider);
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
                }
                else
                {
                    // Create new user from OAuth
                    // OAuth users don't have passwords, but database requires PasswordHash to be NOT NULL
                    // Set a placeholder that will never be used for authentication
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
                        PasswordHash = "OAUTH_USER_NO_PASSWORD", // Placeholder - OAuth users don't have passwords but DB requires NOT NULL
                        FirstName = firstName ?? string.Empty,
                        LastName = lastName ?? string.Empty,
                        DateOfBirth = dateOfBirth,
                        Gender = genderEnum,
                        ProfileImageUrl = profileImageUrl,
                        AuthProvider = provider,
                        ProviderId = providerId,
                        Status = UserStatus.Active,
                        VerificationStatus = UserVerificationStatus.EmailVerified, // OAuth emails are considered verified
                        EmailVerified = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        LastLoginAt = DateTime.UtcNow
                    };

                    // Ensure username is unique
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

                // Generate tokens
                var tokens = await GenerateTokensAsync(user);

                var response = new AuthResponse
                {
                    AccessToken = tokens.AccessToken,
                    RefreshToken = tokens.RefreshToken,
                    ExpiresAt = tokens.ExpiresAt,
                    User = MapToUserDto(user)
                };
                
                // Return response with IsNewUser flag
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
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var accessToken = tokenHandler.WriteToken(token);

            // Create refresh token
            var refreshToken = GenerateSecureToken();
            var refreshExpiresAt = DateTime.UtcNow.AddDays(int.Parse(_configuration["JwtSettings:RefreshTokenExpiryInDays"]!));

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
