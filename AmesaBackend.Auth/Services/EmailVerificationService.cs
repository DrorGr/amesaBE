using AmesaBackend.Auth.Data;
using AmesaBackend.Auth.DTOs;
using AmesaBackend.Auth.Models;
using AmesaBackend.Shared.Events;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;

namespace AmesaBackend.Auth.Services;

public class EmailVerificationService : IEmailVerificationService
{
    private readonly AuthDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailVerificationService> _logger;

    public EmailVerificationService(
        AuthDbContext context,
        IEventPublisher eventPublisher,
        IHttpContextAccessor httpContextAccessor,
        ITokenService tokenService,
        IConfiguration configuration,
        ILogger<EmailVerificationService> logger)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _httpContextAccessor = httpContextAccessor;
        _tokenService = tokenService;
        _configuration = configuration;
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

            // Check if token has expired
            if (user.EmailVerificationTokenExpiresAt.HasValue && 
                user.EmailVerificationTokenExpiresAt.Value < DateTime.UtcNow)
            {
                // Clear expired token and expiration
                user.EmailVerificationToken = null;
                user.EmailVerificationTokenExpiresAt = null;
                await _context.SaveChangesAsync();
                
                throw new InvalidOperationException("TOKEN_EXPIRED:Verification token has expired. Please request a new verification email.");
            }

            user.EmailVerified = true;
            user.EmailVerificationToken = null;
            user.EmailVerificationTokenExpiresAt = null; // Clear expiration when token is cleared
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

            // Regenerate token ONLY if missing or expired
            // Don't regenerate if token exists and is not expired
            var shouldRegenerate = false;
            
            if (string.IsNullOrEmpty(user.EmailVerificationToken))
            {
                // Token is missing - regenerate
                shouldRegenerate = true;
            }
            else if (user.EmailVerificationTokenExpiresAt.HasValue)
            {
                // Token has expiration - check if expired
                if (user.EmailVerificationTokenExpiresAt.Value < DateTime.UtcNow)
                {
                    shouldRegenerate = true;
                }
                // If token exists and not expired, don't regenerate
            }
            else
            {
                // Token exists but no expiration set (legacy data) - treat as expired if >24 hours old
                if (user.CreatedAt < DateTime.UtcNow.AddHours(-24))
                {
                    shouldRegenerate = true;
                }
            }

            if (shouldRegenerate)
            {
                var expiryHours = _configuration.GetValue<int>("SecuritySettings:EmailVerificationTokenExpiryHours", 24);

                user.EmailVerificationToken = _tokenService.GenerateSecureToken();
                user.EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(expiryHours);
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Email verification token regenerated for user: {Email} (token was missing or expired)", user.Email);
            }
            else
            {
                _logger.LogInformation("Email verification token not regenerated for user: {Email} (valid token exists)", user.Email);
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











