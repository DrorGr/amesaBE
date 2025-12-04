using AmesaBackend.Auth.Data;
using AmesaBackend.Auth.DTOs;
using AmesaBackend.Auth.Models;
using AmesaBackend.Shared.Events;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;

namespace AmesaBackend.Auth.Services;

public class PasswordResetService : IPasswordResetService
{
    private readonly AuthDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly IPasswordValidatorService _passwordValidator;
    private readonly ITokenService _tokenService;
    private readonly ISessionService _sessionService;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(
        AuthDbContext context,
        IEventPublisher eventPublisher,
        IPasswordValidatorService passwordValidator,
        ITokenService tokenService,
        ISessionService sessionService,
        ILogger<PasswordResetService> logger)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _passwordValidator = passwordValidator;
        _tokenService = tokenService;
        _sessionService = sessionService;
        _logger = logger;
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user != null)
            {
                user.PasswordResetToken = _tokenService.GenerateSecureToken();
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

            // Use execution strategy to support retry with transactions
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
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




