using Microsoft.EntityFrameworkCore;
using AmesaBackend.Auth.Data;
using AmesaBackend.Auth.DTOs;
using AmesaBackend.Auth.Models;
using AmesaBackend.Auth.Services.Interfaces;
using AmesaBackend.Shared.Authentication;
using AmesaBackend.Shared.Events;
using BCrypt.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using Npgsql;

namespace AmesaBackend.Auth.Services
{
    public class AuthService : IAuthService
    {
        private readonly AuthDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEventPublisher _eventPublisher;
        private readonly IAccountLockoutService _accountLockoutService;
        private readonly IPasswordValidatorService _passwordValidator;
        private readonly ITokenService _tokenService;
        private readonly ISessionService _sessionService;
        private readonly IEmailVerificationService _emailVerificationService;
        private readonly IPasswordResetService _passwordResetService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IJwtTokenManager _jwtTokenManager;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            AuthDbContext context,
            IConfiguration configuration,
            IEventPublisher eventPublisher,
            IAccountLockoutService accountLockoutService,
            IPasswordValidatorService passwordValidator,
            ITokenService tokenService,
            ISessionService sessionService,
            IEmailVerificationService emailVerificationService,
            IPasswordResetService passwordResetService,
            IHttpContextAccessor httpContextAccessor,
            IJwtTokenManager jwtTokenManager,
            ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _eventPublisher = eventPublisher;
            _accountLockoutService = accountLockoutService;
            _passwordValidator = passwordValidator;
            _tokenService = tokenService;
            _sessionService = sessionService;
            _emailVerificationService = emailVerificationService;
            _passwordResetService = passwordResetService;
            _httpContextAccessor = httpContextAccessor;
            _jwtTokenManager = jwtTokenManager;
            _logger = logger;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                // Validate DateOfBirth if provided
                if (request.DateOfBirth.HasValue)
                {
                    // Check if date is in the future
                    if (request.DateOfBirth.Value > DateTime.UtcNow)
                    {
                        throw new InvalidOperationException("Date of birth cannot be in the future");
                    }

                    // Check minimum age (default 13 years)
                    var minAge = _configuration.GetValue<int>("SecuritySettings:Registration:MinimumAge", 13);
                    var minDate = DateTime.UtcNow.AddYears(-minAge);
                    if (request.DateOfBirth.Value >= minDate)
                    {
                        throw new InvalidOperationException($"You must be at least {minAge} years old to register");
                    }
                }

                // Validate phone number format (E.164) if provided
                if (!string.IsNullOrWhiteSpace(request.Phone))
                {
                    // E.164 format: +[country code][number] (max 15 digits total)
                    var phoneRegex = new System.Text.RegularExpressions.Regex(@"^\+[1-9]\d{1,14}$");
                    if (!phoneRegex.IsMatch(request.Phone))
                    {
                        throw new InvalidOperationException("Phone number must be in E.164 format (e.g., +1234567890)");
                    }
                }

                // Validate email domain (optional - configurable)
                var enableDomainValidation = _configuration.GetValue<bool>("SecuritySettings:Registration:EnableDomainValidation", false);
                if (enableDomainValidation)
                {
                    // Validate email format first
                    if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
                    {
                        throw new InvalidOperationException("Registration failed. Please check your information.");
                    }

                    var emailParts = request.Email.Split('@');
                    if (emailParts.Length != 2 || string.IsNullOrWhiteSpace(emailParts[1]))
                    {
                        throw new InvalidOperationException("Registration failed. Please check your information.");
                    }

                    var emailDomain = emailParts[1];

                    // Check blacklist
                    var domainBlacklist = _configuration.GetSection("SecuritySettings:Registration:DomainBlacklist")
                        .Get<string[]>() ?? Array.Empty<string>();
                    if (domainBlacklist.Any(d => d.Equals(emailDomain, StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new InvalidOperationException("Registration failed. Please check your information.");
                    }

                    // Check whitelist (if configured, only allow whitelisted domains)
                    var domainWhitelist = _configuration.GetSection("SecuritySettings:Registration:DomainWhitelist")
                        .Get<string[]>();
                    if (domainWhitelist != null && domainWhitelist.Length > 0)
                    {
                        if (!domainWhitelist.Any(d => d.Equals(emailDomain, StringComparison.OrdinalIgnoreCase)))
                        {
                            throw new InvalidOperationException("Registration failed. Please check your information.");
                        }
                    }
                }

                // Validate password
                // Note: Password history validation is not performed for new registrations since there is no history.
                // Password history is only checked when existing users change their passwords.
                var passwordValidation = await _passwordValidator.ValidatePasswordAsync(request.Password);
                if (!passwordValidation.IsValid)
                {
                    // Log detailed validation errors server-side for debugging
                    _logger.LogWarning("Password validation failed for registration: {Errors}", string.Join(", ", passwordValidation.Errors));
                    // Use generic error message to prevent information leakage about validation rules
                    throw new InvalidOperationException("Password does not meet requirements. Please choose a stronger password.");
                }

                // Normalize username for case-insensitive check
                var normalizedUsername = request.Username.Trim().ToLowerInvariant();

                // Use execution strategy to support retry with transactions
                var strategy = _context.Database.CreateExecutionStrategy();
                User? user = null;
                bool isNewUser = true;

                await strategy.ExecuteAsync(async () =>
                {
                    // Use transaction to ensure atomicity
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        // Check for existing user (including soft-deleted) with row-level locking
                        // Use FOR UPDATE to prevent concurrent registrations
                        var existingUser = await _context.Users
                            .FromSqlRaw("SELECT * FROM amesa_auth.users WHERE LOWER(\"Email\") = LOWER({0}) OR LOWER(\"Username\") = LOWER({1}) FOR UPDATE", 
                                request.Email, normalizedUsername)
                            .IgnoreQueryFilters() // Check soft-deleted users too
                            .FirstOrDefaultAsync();

                        if (existingUser != null)
                        {
                            // Check if account is soft-deleted and within grace period
                            if (existingUser.DeletedAt.HasValue)
                            {
                                var gracePeriodDays = _configuration.GetValue<int>("SecuritySettings:AccountDeletion:GracePeriodDays", 30);
                                if (existingUser.DeletedAt.Value.AddDays(gracePeriodDays) >= DateTime.UtcNow)
                                {
                                    // Account is soft-deleted within grace period - restore it
                                    user = existingUser;
                                    isNewUser = false;
                                    
                                    // Restore account
                                    user.DeletedAt = null;
                                    user.Status = UserStatus.Pending;
                                    user.VerificationStatus = UserVerificationStatus.Unverified;
                                    user.EmailVerified = false;
                                    user.UpdatedAt = DateTime.UtcNow;
                                    
                                    // Update password if provided
                                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                                    
                                    // Update other fields if provided
                                    if (!string.IsNullOrWhiteSpace(request.FirstName))
                                        user.FirstName = request.FirstName;
                                    if (!string.IsNullOrWhiteSpace(request.LastName))
                                        user.LastName = request.LastName;
                                    if (request.DateOfBirth.HasValue)
                                        user.DateOfBirth = request.DateOfBirth;
                                    if (!string.IsNullOrWhiteSpace(request.Gender))
                                    {
                                        if (Enum.TryParse<GenderType>(request.Gender, out var gender))
                                            user.Gender = gender;
                                    }
                                    if (!string.IsNullOrWhiteSpace(request.Phone))
                                        user.Phone = request.Phone;
                                    
                                    // Regenerate email verification token if expired or missing
                                    var tokenExpiryHours = _configuration.GetValue<int>("SecuritySettings:EmailVerificationTokenExpiryHours", 24);
                                    if (string.IsNullOrEmpty(user.EmailVerificationToken) || 
                                        !user.EmailVerificationTokenExpiresAt.HasValue || 
                                        user.EmailVerificationTokenExpiresAt.Value < DateTime.UtcNow)
                                    {
                                        user.EmailVerificationToken = _tokenService.GenerateSecureToken();
                                        user.EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(tokenExpiryHours);
                                    }
                                }
                                else
                                {
                                    // Grace period expired - treat as new registration
                                    // But email/username is still taken, so throw error
                                    throw new InvalidOperationException("Registration failed. Please check your information.");
                                }
                            }
                            else
                            {
                                // Account exists and is not deleted
                                throw new InvalidOperationException("Registration failed. Please check your information.");
                            }
                        }

                        // Check for duplicate phone number if provided (with row-level locking to prevent race conditions)
                        if (!string.IsNullOrWhiteSpace(request.Phone) && user == null)
                        {
                            var phoneUser = await _context.Users
                                .FromSqlRaw("SELECT * FROM amesa_auth.users WHERE \"Phone\" = {0} AND \"DeletedAt\" IS NULL FOR UPDATE", 
                                    request.Phone)
                                .IgnoreQueryFilters()
                                .FirstOrDefaultAsync();
                            
                            if (phoneUser != null)
                            {
                                throw new InvalidOperationException("Registration failed. Please check your information.");
                            }
                        }

                        // Create new user if not restoring existing one
                        if (user == null)
                        {
                            user = new User
                            {
                                Username = request.Username, // Store original case
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
                                EmailVerificationToken = _tokenService.GenerateSecureToken(),
                                EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(
                                    _configuration.GetValue<int>("SecuritySettings:EmailVerificationTokenExpiryHours", 24)),
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                            _context.Users.Add(user);
                        }

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
                    catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
                    {
                        // Unique constraint violation - handle gracefully
                        await transaction.RollbackAsync();
                        _logger.LogWarning(ex, "Unique constraint violation during registration for email: {Email}", request.Email);
                        throw new InvalidOperationException("Registration failed. Please check your information.");
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                });

                // Ensure user is assigned (should always be true if we reach here)
                if (user == null)
                {
                    throw new InvalidOperationException("User creation failed: user object is null");
                }

                // Publish events (only for new users, not restored ones)
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
                var tokens = await _tokenService.GenerateTokensAsync(user);

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
            var startTime = DateTime.UtcNow;
            try
            {
                // Extract IP address for logging and tracking
                var httpContext = _httpContextAccessor.HttpContext;
                var ipAddress = httpContext?.Items["ClientIp"]?.ToString() 
                    ?? httpContext?.Connection.RemoteIpAddress?.ToString() 
                    ?? httpContext?.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
                    ?? "unknown";

                // Check for user (including soft-deleted) to allow login during grace period
                var user = await _context.Users
                    .IgnoreQueryFilters() // Allow soft-deleted users to be checked
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                // Check account status BEFORE password verification (fail fast for invalid states)
                if (user != null)
                {
                    // Check if account is soft-deleted
                    if (user.DeletedAt.HasValue)
                    {
                        var gracePeriodDays = _configuration.GetValue<int>("SecuritySettings:AccountDeletion:GracePeriodDays", 30);
                        if (user.DeletedAt.Value.AddDays(gracePeriodDays) < DateTime.UtcNow)
                        {
                            // Grace period expired - account is permanently deleted
                            _logger.LogWarning("Login attempt for permanently deleted account: {Email}", request.Email);
                            // Record failed attempt for tracking
                            await _accountLockoutService.RecordFailedAttemptAsync(request.Email);
                            
                            // Add artificial delay to prevent timing attacks
                            var elapsed = DateTime.UtcNow - startTime;
                            var minDelay = TimeSpan.FromMilliseconds(500);
                            if (elapsed < minDelay)
                            {
                                await Task.Delay(minDelay - elapsed);
                            }
                            
                            throw new UnauthorizedAccessException("Invalid email or password");
                        }
                        // Grace period not expired - allow login for account recovery
                    }

                    // Check account status (suspended/banned) before password verification
                    if (user.Status == UserStatus.Suspended || user.Status == UserStatus.Banned)
                    {
                        _logger.LogWarning("Login attempt for {Status} account: {Email}", user.Status, request.Email);
                        // Record failed attempt for tracking
                        await _accountLockoutService.RecordFailedAttemptAsync(request.Email);
                        
                        // Add artificial delay to prevent timing attacks
                        var elapsed = DateTime.UtcNow - startTime;
                        var minDelay = TimeSpan.FromMilliseconds(500);
                        if (elapsed < minDelay)
                        {
                            await Task.Delay(minDelay - elapsed);
                        }
                        
                        throw new UnauthorizedAccessException("Account is suspended or banned");
                    }

                    // Check lockout BEFORE password verification
                    if (await _accountLockoutService.IsLockedAsync(request.Email))
                    {
                        var lockedUntil = await _accountLockoutService.GetLockedUntilAsync(request.Email);
                        _logger.LogWarning("Login attempt for locked account: {Email}, locked until: {LockedUntil}", request.Email, lockedUntil);
                        
                        // Add artificial delay to prevent timing attacks
                        var elapsed = DateTime.UtcNow - startTime;
                        var minDelay = TimeSpan.FromMilliseconds(500);
                        if (elapsed < minDelay)
                        {
                            await Task.Delay(minDelay - elapsed);
                        }
                        
                        // Use generic error message to prevent information leakage
                        // Specific lockout time is logged server-side for debugging
                        throw new UnauthorizedAccessException("Account is temporarily locked. Please try again later.");
                    }

                    // Check email verification BEFORE password verification
                    if (!user.EmailVerified)
                    {
                        _logger.LogWarning("Login attempt for unverified email: {Email}", request.Email);
                        // Record failed attempt for tracking
                        await _accountLockoutService.RecordFailedAttemptAsync(request.Email);
                        
                        // Add artificial delay to prevent timing attacks
                        var elapsed = DateTime.UtcNow - startTime;
                        var minDelay = TimeSpan.FromMilliseconds(500);
                        if (elapsed < minDelay)
                        {
                            await Task.Delay(minDelay - elapsed);
                        }
                        
                        throw new UnauthorizedAccessException("Please verify your email before logging in. Check your inbox for the verification link.");
                    }
                }

                // Always perform password verification to prevent timing attacks
                // Use a dummy hash if user doesn't exist to maintain consistent timing
                var passwordHash = user?.PasswordHash ?? "$2a$11$dummyhashforsecuritytimingprotection123456789012345678901234567890";
                
                // Always verify password (even with dummy hash) to prevent timing attacks
                var passwordValid = user != null && BCrypt.Net.BCrypt.Verify(request.Password, passwordHash);

                if (user == null || !passwordValid)
                {
                    // Log enumeration attempt
                    _logger.LogWarning("Failed login attempt for email: {Email} from IP: {IpAddress} (user may not exist or password incorrect)", 
                        request.Email, ipAddress);
                    
                    // Record failed attempt for ALL login failures (including non-existent users)
                    await _accountLockoutService.RecordFailedAttemptAsync(request.Email);
                    
                    // Add artificial delay to prevent timing attacks (ensure consistent response time)
                    var elapsed = DateTime.UtcNow - startTime;
                    var minDelay = TimeSpan.FromMilliseconds(500); // Minimum 500ms delay
                    if (elapsed < minDelay)
                    {
                        await Task.Delay(minDelay - elapsed);
                    }
                    
                    // Generic error message - don't reveal if user exists
                    throw new UnauthorizedAccessException("Invalid email or password");
                }

                // On success: clear failed attempts
                await _accountLockoutService.ClearFailedAttemptsAsync(request.Email);

                // Update last login
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Track device changes during login
                var userAgent = httpContext?.Request.Headers["User-Agent"].ToString() ?? "unknown";
                var deviceId = httpContext?.Items["DeviceId"]?.ToString() ?? _sessionService.GenerateDeviceId(userAgent, ipAddress);
                
                // Get previous session to compare device
                var previousSession = await _context.UserSessions
                    .Where(s => s.UserId == user.Id && s.IsActive)
                    .OrderByDescending(s => s.LastActivity)
                    .FirstOrDefaultAsync();
                
                if (previousSession != null && !string.IsNullOrEmpty(previousSession.DeviceId) && previousSession.DeviceId != deviceId)
                {
                    _logger.LogWarning("Device change detected during login for user {UserId}. Old: {OldDeviceId}, New: {NewDeviceId}, IP: {IpAddress}", 
                        user.Id, previousSession.DeviceId, deviceId, ipAddress);
                }

                // Publish login event with IP address
                await _eventPublisher.PublishAsync(new UserLoginEvent
                {
                    UserId = user.Id,
                    Email = user.Email,
                    IpAddress = ipAddress
                });

                // Check if 2FA is enabled
                if (user.TwoFactorEnabled)
                {
                    _logger.LogInformation("User requires 2FA verification: {Email}", user.Email);
                    return new AuthResponse
                    {
                        RequiresTwoFactor = true,
                        RequiresEmailVerification = !user.EmailVerified,
                        User = MapToUserDto(user)
                    };
                }

                // Validate RememberMe flag (log for security monitoring)
                if (request.RememberMe)
                {
                    _logger.LogInformation("Remember Me enabled for login: {Email}, IP: {IpAddress}", user.Email, ipAddress);
                }

                // Generate tokens with RememberMe preference
                var tokens = await _tokenService.GenerateTokensAsync(user, request.RememberMe);

                _logger.LogInformation("User logged in successfully: {Email}, RememberMe: {RememberMe}, IP: {IpAddress}", 
                    user.Email, request.RememberMe, ipAddress);

                return new AuthResponse
                {
                    AccessToken = tokens.AccessToken,
                    RefreshToken = tokens.RefreshToken,
                    ExpiresAt = tokens.ExpiresAt,
                    RequiresEmailVerification = !user.EmailVerified,
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
                    .IgnoreQueryFilters() // CRITICAL: Allow soft-deleted users to be checked
                    .FirstOrDefaultAsync(s => s.SessionToken == request.RefreshToken && s.IsActive);

                if (session == null || session.ExpiresAt < DateTime.UtcNow)
                {
                    throw new UnauthorizedAccessException("Invalid or expired refresh token");
                }

                // Check if user exists (could be null if permanently deleted)
                if (session.User == null)
                {
                    throw new UnauthorizedAccessException("User not found");
                }

                // Check for soft-deleted accounts
                if (session.User.Status == UserStatus.Deleted || session.User.DeletedAt != null)
                {
                    throw new UnauthorizedAccessException("Account has been deleted");
                }

                // Check for suspended or banned accounts
                if (session.User.Status == UserStatus.Suspended || session.User.Status == UserStatus.Banned)
                {
                    throw new UnauthorizedAccessException("Account is suspended or banned");
                }

                // Check if token was already rotated (potential theft)
                // If IsRotated is true, this token was already used to create a new token
                // Using it again indicates token theft or replay attack
                var invalidateOnReuse = _configuration.GetValue<bool>("SecuritySettings:RefreshTokenRotation:InvalidateOnReuse", true);
                if (session.IsRotated)
                {
                    _logger.LogWarning("Potential token reuse detected for user {UserId} - token was already rotated", session.UserId);
                    // Invalidate all sessions for security (if configured)
                    if (invalidateOnReuse)
                    {
                        await _sessionService.InvalidateAllSessionsAsync(session.UserId!.Value);
                        throw new UnauthorizedAccessException("Security violation detected. Please log in again.");
                    }
                    else
                    {
                        throw new UnauthorizedAccessException("Invalid or expired refresh token");
                    }
                }

                // Token rotation: Invalidate old token and mark as rotated
                var rotationEnabled = _configuration.GetValue<bool>("SecuritySettings:RefreshTokenRotation:Enabled", true);
                
                // Use stored RememberMe field (more reliable than calculating from duration)
                // The RememberMe field is set during login/registration, so we can use it directly
                var isRememberMe = session.RememberMe;
                
                if (!rotationEnabled)
                {
                    // If rotation disabled, generate new access token but keep same refresh token
                    // This prevents multiple active refresh tokens while still refreshing access token
                    _logger.LogWarning("Token rotation disabled - keeping existing refresh token. This is not recommended for security.");
                    
                    // Generate new access token only
                    var newAccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("JwtSettings:ExpiryInMinutes", 60));
                    var claims = new List<System.Security.Claims.Claim>
                    {
                        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, session.User!.Id.ToString()),
                        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, session.User.Email),
                        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, session.User.Username),
                        new System.Security.Claims.Claim("firstName", session.User.FirstName),
                        new System.Security.Claims.Claim("lastName", session.User.LastName),
                        new System.Security.Claims.Claim("session_token", session.SessionToken)
                    };
                    var newAccessToken = _jwtTokenManager.GenerateAccessToken(claims, newAccessTokenExpiresAt);
                    
                    // Update session activity
                    session.LastActivity = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    
                    return new AuthResponse
                    {
                        AccessToken = newAccessToken,
                        RefreshToken = session.SessionToken,
                        ExpiresAt = newAccessTokenExpiresAt,
                        User = MapToUserDto(session.User)
                    };
                }

                // Rotation enabled: Invalidate old token and create new one
                session.IsActive = false;
                session.IsRotated = true;

                // Generate new tokens with rememberMe flag preserved
                // Note: GenerateTokensAsync creates a new session, which we'll update with rotation tracking
                var tokens = await _tokenService.GenerateTokensAsync(session.User, isRememberMe);

                // Find the session created by GenerateTokensAsync and update it with rotation tracking
                var newSession = await _context.UserSessions
                    .Where(s => s.SessionToken == tokens.RefreshToken && s.UserId == session.UserId)
                    .OrderByDescending(s => s.CreatedAt)
                    .FirstOrDefaultAsync();

                if (newSession == null)
                {
                    throw new InvalidOperationException("Failed to create new session during token refresh");
                }

                // Update the auto-created session with rotation tracking and fresh IP/device info
                var httpContext = _httpContextAccessor.HttpContext;
                var ipAddress = httpContext?.Items["ClientIp"]?.ToString() 
                    ?? httpContext?.Connection.RemoteIpAddress?.ToString() 
                    ?? httpContext?.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
                    ?? session.IpAddress ?? "unknown";
                var userAgent = httpContext?.Request.Headers["User-Agent"].ToString() ?? session.UserAgent ?? "unknown";
                var deviceId = httpContext?.Items["DeviceId"]?.ToString() ?? _sessionService.GenerateDeviceId(userAgent, ipAddress);
                var deviceName = _sessionService.ExtractDeviceName(userAgent);

                // Update session with rotation tracking and fresh metadata
                // ExpiresAt is already set correctly by GenerateTokensAsync based on rememberMe
                newSession.PreviousSessionToken = session.SessionToken; // Track rotation chain
                newSession.RememberMe = isRememberMe; // Preserve Remember Me status
                newSession.IpAddress = ipAddress;
                newSession.UserAgent = userAgent;
                newSession.DeviceId = deviceId;
                newSession.DeviceName = deviceName;
                newSession.LastActivity = DateTime.UtcNow;

                // Check if device changed (suspicious activity detection)
                if (session.DeviceId != null && session.DeviceId != deviceId)
                {
                    _logger.LogWarning("Device change detected during token refresh for user {UserId}. Old: {OldDeviceId}, New: {NewDeviceId}", 
                        session.UserId, session.DeviceId, deviceId);
                }

                // Limit to 5 active sessions - remove oldest if exceeded
                await _sessionService.EnforceSessionLimitAsync(session.UserId!.Value);

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
            await _passwordResetService.ForgotPasswordAsync(request);
        }

        public async Task ResetPasswordAsync(ResetPasswordRequest request)
        {
            await _passwordResetService.ResetPasswordAsync(request);
        }

        public async Task VerifyEmailAsync(VerifyEmailRequest request)
        {
            await _emailVerificationService.VerifyEmailAsync(request);
        }

        public async Task ResendVerificationEmailAsync(ResendVerificationRequest request)
        {
            await _emailVerificationService.ResendVerificationEmailAsync(request);
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
            return await _tokenService.ValidateTokenAsync(token);
        }

        public async Task<UserDto> GetCurrentUserAsync(Guid userId)
        {
            // Use AsNoTracking to avoid DbContext tracking issues and prevent concurrent operation errors
            // Also use FirstOrDefaultAsync instead of FindAsync to avoid potential query filter issues
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);
            
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
                    .IgnoreQueryFilters() // Check soft-deleted users too (consistent with email registration)
                    .FirstOrDefaultAsync(u => u.Email == email);

                User user = null!; // Will be assigned in either if or else block
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
                    // Generate unique username with retry logic to handle race conditions
                    const int maxRetries = 5;
                    const int baseDelayMs = 50;
                    var retryCount = 0;
                    var success = false;

                    while (!success && retryCount < maxRetries)
                    {
                        try
                        {
                            // Use database transaction for atomic username generation
                            using var transaction = await _context.Database.BeginTransactionAsync();
                            try
                            {
                                var baseUsername = email.Split('@')[0] + "_" + provider.ToString().ToLower();
                                var username = baseUsername;
                                var counter = 1;

                                // Check username availability within transaction
                                while (await _context.Users.AnyAsync(u => EF.Functions.ILike(u.Username, username)))
                                {
                                    username = $"{baseUsername}{counter}";
                                    counter++;
                                    
                                    // Safety check to prevent infinite loop
                                    if (counter > 1000)
                                    {
                                        throw new InvalidOperationException($"Unable to generate unique username after 1000 attempts for base: {baseUsername}");
                                    }
                                }

                                user = new User
                                {
                                    Username = username,
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

                                _context.Users.Add(user);
                                await _context.SaveChangesAsync();
                                
                                // Commit transaction - database constraint will catch any race condition
                                await transaction.CommitAsync();
                                
                                isNewUser = true;
                                success = true;
                                _logger.LogInformation("Created new user from OAuth {Provider}: {Email}, Username: {Username}", provider, email, username);
                            }
                            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
                            {
                                // Unique constraint violation (username collision) - rollback and retry
                                await transaction.RollbackAsync();
                                retryCount++;
                                
                                if (retryCount < maxRetries)
                                {
                                    // Exponential backoff: 50ms, 100ms, 200ms, 400ms, 800ms
                                    var delayMs = baseDelayMs * (int)Math.Pow(2, retryCount - 1);
                                    _logger.LogWarning("Username collision detected for OAuth user {Email}, retrying in {DelayMs}ms (attempt {RetryCount}/{MaxRetries})", 
                                        email, delayMs, retryCount, maxRetries);
                                    await Task.Delay(delayMs);
                                }
                                else
                                {
                                    _logger.LogError("Failed to generate unique username after {MaxRetries} attempts for OAuth user {Email}", maxRetries, email);
                                    throw new InvalidOperationException($"Unable to create OAuth user: failed to generate unique username after {maxRetries} retries", ex);
                                }
                            }
                        }
                        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
                        {
                            // Handle unique constraint violation outside transaction
                            retryCount++;
                            
                            if (retryCount < maxRetries)
                            {
                                var delayMs = baseDelayMs * (int)Math.Pow(2, retryCount - 1);
                                _logger.LogWarning("Username collision detected for OAuth user {Email}, retrying in {DelayMs}ms (attempt {RetryCount}/{MaxRetries})", 
                                    email, delayMs, retryCount, maxRetries);
                                await Task.Delay(delayMs);
                            }
                            else
                            {
                                _logger.LogError("Failed to generate unique username after {MaxRetries} attempts for OAuth user {Email}", maxRetries, email);
                                throw new InvalidOperationException($"Unable to create OAuth user: failed to generate unique username after {maxRetries} retries", ex);
                            }
                        }
                    }

                    if (!success)
                    {
                        throw new InvalidOperationException($"Failed to create OAuth user after {maxRetries} retries");
                    }
                    
                    // Ensure user is assigned (should always be true if we reach here)
                    if (user == null)
                    {
                        throw new InvalidOperationException("User creation failed: user object is null");
                    }
                }

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
                var tokens = await _tokenService.GenerateTokensAsync(user);

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


        public async Task<List<UserSessionDto>> GetActiveSessionsAsync(Guid userId)
        {
            return await _sessionService.GetActiveSessionsAsync(userId);
        }

        public async Task LogoutFromDeviceAsync(Guid userId, string sessionToken)
        {
            await _sessionService.LogoutFromDeviceAsync(userId, sessionToken);
        }

        public async Task LogoutAllDevicesAsync(Guid userId)
        {
            await _sessionService.LogoutAllDevicesAsync(userId);
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

