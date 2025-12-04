using AmesaBackend.Auth.Data;
using AmesaBackend.Auth.DTOs;
using AmesaBackend.Auth.Models;
using AmesaBackend.Shared.Events;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace AmesaBackend.Auth.Services;

public class EmailVerificationService : IEmailVerificationService
{
    private readonly AuthDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITokenService _tokenService;
    private readonly ILogger<EmailVerificationService> _logger;

    public EmailVerificationService(
        AuthDbContext context,
        IEventPublisher eventPublisher,
        IHttpContextAccessor httpContextAccessor,
        ITokenService tokenService,
        ILogger<EmailVerificationService> logger)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _httpContextAccessor = httpContextAccessor;
        _tokenService = tokenService;
        _logger = logger;
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
                var distributedCache = _httpContextAccessor.HttpContext?.RequestServices.GetService<IDistributedCache>();
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
                user.EmailVerificationToken = _tokenService.GenerateSecureToken();
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
}






