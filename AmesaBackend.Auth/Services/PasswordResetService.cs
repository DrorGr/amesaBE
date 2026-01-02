using AmesaBackend.Auth.Data;
using AmesaBackend.Auth.DTOs;
using AmesaBackend.Auth.Models;
using AmesaBackend.Auth.Services.Interfaces;
using AmesaBackend.Shared.Events;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;

namespace AmesaBackend.Auth.Services;

public class PasswordResetService : IPasswordResetService
{
    private const int DefaultTokenExpiryHours = 1;

    private readonly AuthDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly IPasswordValidatorService _passwordValidator;
    private readonly ITokenService _tokenService;
    private readonly ISessionService _sessionService;
    private readonly Lazy<IAccountRecoveryService> _accountRecoveryService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PasswordResetService> _logger;
    private readonly int _tokenExpiryHours;
    private readonly int _gracePeriodDays;

    public PasswordResetService(
        AuthDbContext context,
        IEventPublisher eventPublisher,
        IPasswordValidatorService passwordValidator,
        ITokenService tokenService,
        ISessionService sessionService,
        Lazy<IAccountRecoveryService> accountRecoveryService,
        IConfiguration configuration,
        ILogger<PasswordResetService> logger)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _passwordValidator = passwordValidator;
        _tokenService = tokenService;
        _sessionService = sessionService;
        _accountRecoveryService = accountRecoveryService;
        _configuration = configuration;
        _logger = logger;
        _tokenExpiryHours = _configuration.GetValue<int>("SecuritySettings:PasswordReset:TokenExpiryHours", DefaultTokenExpiryHours);
        _gracePeriodDays = _configuration.GetValue<int>("SecuritySettings:AccountDeletion:GracePeriodDays", 30);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            // Always send password reset email (even if user doesn't exist) - security best practice
            // This prevents email enumeration attacks
            if (user != null)
            {
                user.PasswordResetToken = _tokenService.GenerateSecureToken();
                user.PasswordResetExpiresAt = DateTime.UtcNow.AddHours(_tokenExpiryHours);
                await _context.SaveChangesAsync();

                await _eventPublisher.PublishAsync(new PasswordResetRequestedEvent
                {
                    UserId = user.Id,
                    Email = user.Email,
                    ResetToken = user.PasswordResetToken
                });
                
                _logger.LogInformation("Password reset token generated for user: {UserId}", user.Id);
                // Note: Email and token values are NOT logged for security
            }
            else
            {
                // Log enumeration attempt (without email to prevent enumeration)
                _logger.LogWarning("Password reset requested for non-existent email (enumeration attempt)");
            }

            // Add artificial delay to prevent timing attacks (ensure consistent response time)
            var elapsed = DateTime.UtcNow - startTime;
            var minDelay = TimeSpan.FromMilliseconds(300); // Minimum 300ms delay
            if (elapsed < minDelay)
            {
                await Task.Delay(minDelay - elapsed);
            }

            // Always return success message (don't reveal if email exists)
            _logger.LogInformation("Password reset email sent (or would be sent)");
            // Note: Email is NOT logged to prevent enumeration
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
            // Validate request: Either Token OR (RecoveryCode + Identifier + RecoveryMethod) must be provided
            if (string.IsNullOrEmpty(request.Token) && 
                (string.IsNullOrEmpty(request.RecoveryCode) || 
                 string.IsNullOrEmpty(request.Identifier) || 
                 string.IsNullOrEmpty(request.RecoveryMethod)))
            {
                throw new InvalidOperationException("Either Token or RecoveryCode with Identifier and RecoveryMethod must be provided");
            }

            if (!string.IsNullOrEmpty(request.Token) && !string.IsNullOrEmpty(request.RecoveryCode))
            {
                throw new InvalidOperationException("Cannot provide both Token and RecoveryCode");
            }

            User? user = null;

            // Handle recovery code flow
            if (!string.IsNullOrEmpty(request.RecoveryCode) && !string.IsNullOrEmpty(request.Identifier))
            {
                var method = request.RecoveryMethod?.ToLower() switch
                {
                    "email" => RecoveryMethod.Email,
                    "phone" => RecoveryMethod.Phone,
                    _ => throw new ArgumentException("Invalid recovery method. Must be 'email' or 'phone'")
                };

                // Verify recovery code first
                var isValid = await _accountRecoveryService.Value.VerifyRecoveryCodeAsync(
                    request.Identifier, request.RecoveryCode, method);

                if (!isValid)
                {
                    throw new InvalidOperationException("Invalid or expired recovery code");
                }

                // Get user by identifier (with IgnoreQueryFilters to allow soft-deleted users during grace period)
                if (method == RecoveryMethod.Email)
                {
                    user = await _context.Users
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(u => u.Email == request.Identifier);
                }
                else
                {
                    user = await _context.Users
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(u => u.Phone == request.Identifier);
                }

                if (user == null)
                {
                    throw new InvalidOperationException("User not found");
                }

                // Check if account is soft-deleted and grace period expired
                if (user.DeletedAt != null && user.DeletedAt.Value.AddDays(_gracePeriodDays) < DateTime.UtcNow)
                {
                    throw new InvalidOperationException("Account recovery period has expired");
                }
            }
            else
            {
                // Existing token-based flow
                user = await _context.Users
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.PasswordResetToken == request.Token && 
                                            u.PasswordResetExpiresAt > DateTime.UtcNow);
            }

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

            // Use execution strategy to support retry with transactions
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                // Use transaction with row-level locking to prevent concurrent recovery conflicts
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Reload user with row-level lock (FOR UPDATE) to prevent concurrent modifications
                    // EF Core parameterizes the query automatically, preventing SQL injection
                    var lockedUser = await _context.Users
                        .FromSqlRaw("SELECT * FROM amesa_auth.users WHERE \"Id\" = {0} FOR UPDATE", user.Id)
                        .IgnoreQueryFilters() // Allow soft-deleted users during grace period
                        .FirstOrDefaultAsync();

                    if (lockedUser == null)
                    {
                        throw new InvalidOperationException("User not found");
                    }

                    var oldPasswordHash = lockedUser.PasswordHash;
                    lockedUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                    // Clear ALL recovery tokens (both email and phone) after successful password reset
                    lockedUser.PasswordResetToken = null;
                    lockedUser.PasswordResetExpiresAt = null;
                    lockedUser.PhoneVerificationToken = null;
                    await _context.SaveChangesAsync();

                    // Save to password history
                    var passwordHistory = new UserPasswordHistory
                    {
                        Id = Guid.NewGuid(),
                        UserId = lockedUser.Id,
                        PasswordHash = lockedUser.PasswordHash,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Set<UserPasswordHistory>().Add(passwordHistory);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    // Update user reference for session invalidation
                    user = lockedUser;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });

            // Invalidate all sessions for security (force re-login after password reset)
            await _sessionService.InvalidateAllSessionsAsync(user.Id);

            _logger.LogInformation("Password reset successfully for user: {Email}. All sessions invalidated.", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset");
            throw;
        }
    }
}




