using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AmesaBackend.Notification.Data;
using AmesaBackend.Notification.DTOs;
using AmesaBackend.Notification.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Security.Claims;
using System.Text.Json;
using DTOs = AmesaBackend.Notification.DTOs;

namespace AmesaBackend.Notification.Controllers
{
    [ApiController]
    [Route("api/v1/notifications/telegram")]
    public class TelegramController : ControllerBase
    {
        private readonly NotificationDbContext _context;
        private readonly ILogger<TelegramController> _logger;
        private readonly ITelegramBotClient? _botClient;

        public TelegramController(
            NotificationDbContext context,
            ILogger<TelegramController> logger,
            ITelegramBotClient? botClient = null)
        {
            _context = context;
            _logger = logger;
            _botClient = botClient;
        }

        [HttpPost("link")]
        [Authorize]
        public async Task<ActionResult<DTOs.ApiResponse<TelegramLinkDto>>> RequestLink()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new DTOs.ApiResponse<TelegramLinkDto>
                    {
                        Success = false,
                        Message = "Invalid user authentication"
                    });
                }

                // Check if already linked
                var existing = await _context.TelegramUserLinks
                    .FirstOrDefaultAsync(l => l.UserId == userId);

                if (existing != null && existing.Verified)
                {
                    return BadRequest(new DTOs.ApiResponse<TelegramLinkDto>
                    {
                        Success = false,
                        Message = "Telegram account already linked"
                    });
                }

                // Generate verification token
                var verificationToken = Guid.NewGuid().ToString("N")[..8].ToUpper();

                if (existing != null)
                {
                    existing.VerificationToken = verificationToken;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    var link = new TelegramUserLink
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        VerificationToken = verificationToken,
                        Verified = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.TelegramUserLinks.Add(link);
                }

                await _context.SaveChangesAsync();

                // Send verification code via Telegram Bot API if bot is configured
                if (_botClient != null && existing != null && !string.IsNullOrEmpty(existing.VerificationToken))
                {
                    try
                    {
                        // If user already has a TelegramUserId, send directly
                        if (existing.TelegramUserId > 0)
                        {
                            await _botClient.SendTextMessageAsync(
                                chatId: existing.ChatId,
                                text: $"Your Amesa verification code is: *{verificationToken}*\n\nSend this code to the bot to complete linking.",
                                parseMode: ParseMode.Markdown);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send verification code via Telegram bot. User will see code in API response.");
                    }
                }

                var linkDto = new TelegramLinkDto
                {
                    Id = existing?.Id ?? Guid.NewGuid(),
                    UserId = userId,
                    TelegramUserId = existing?.TelegramUserId ?? 0,
                    TelegramUsername = existing?.TelegramUsername,
                    Verified = false
                };

                return Ok(new DTOs.ApiResponse<TelegramLinkDto>
                {
                    Success = true,
                    Data = linkDto,
                    Message = $"Verification code: {verificationToken}. Send this code to the Telegram bot to complete linking."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting Telegram link");
                return StatusCode(500, new DTOs.ApiResponse<TelegramLinkDto>
                {
                    Success = false,
                    Message = "Failed to request Telegram link"
                });
            }
        }

        [HttpPost("verify")]
        [Authorize]
        public async Task<ActionResult<DTOs.ApiResponse<TelegramLinkDto>>> VerifyLink([FromBody] TelegramLinkRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new DTOs.ApiResponse<TelegramLinkDto>
                    {
                        Success = false,
                        Message = "Invalid user authentication"
                    });
                }

                var link = await _context.TelegramUserLinks
                    .FirstOrDefaultAsync(l => l.UserId == userId && l.VerificationToken == request.VerificationCode);

                if (link == null)
                {
                    return BadRequest(new DTOs.ApiResponse<TelegramLinkDto>
                    {
                        Success = false,
                        Message = "Invalid verification code"
                    });
                }

                link.Verified = true;
                link.VerificationToken = null;
                link.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var linkDto = new TelegramLinkDto
                {
                    Id = link.Id,
                    UserId = link.UserId,
                    TelegramUserId = link.TelegramUserId,
                    TelegramUsername = link.TelegramUsername,
                    Verified = true
                };

                return Ok(new DTOs.ApiResponse<TelegramLinkDto>
                {
                    Success = true,
                    Data = linkDto,
                    Message = "Telegram account linked successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying Telegram link");
                return StatusCode(500, new DTOs.ApiResponse<TelegramLinkDto>
                {
                    Success = false,
                    Message = "Failed to verify Telegram link"
                });
            }
        }

        [HttpDelete("unlink")]
        [Authorize]
        public async Task<ActionResult<DTOs.ApiResponse<bool>>> Unlink()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new DTOs.ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid user authentication"
                    });
                }

                var link = await _context.TelegramUserLinks
                    .FirstOrDefaultAsync(l => l.UserId == userId);

                if (link == null)
                {
                    return NotFound(new DTOs.ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Telegram account not linked"
                    });
                }

                _context.TelegramUserLinks.Remove(link);
                await _context.SaveChangesAsync();

                return Ok(new DTOs.ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Telegram account unlinked successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlinking Telegram account");
                return StatusCode(500, new DTOs.ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to unlink Telegram account"
                });
            }
        }

        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> Webhook([FromBody] Update update)
        {
            try
            {
                if (update == null)
                {
                    _logger.LogWarning("Telegram webhook received null update");
                    return Ok();
                }

                // Handle message updates
                if (update.Message != null)
                {
                    var message = update.Message;
                    var chatId = message.Chat.Id;
                    var userId = message.From?.Id ?? 0;
                    var username = message.From?.Username;
                    var text = message.Text?.Trim();

                    _logger.LogInformation("Telegram webhook: Received message from user {UserId} (@{Username}) in chat {ChatId}: {Text}", 
                        userId, username, chatId, text);

                    // Handle /start command
                    if (text == "/start")
                    {
                        if (_botClient != null)
                        {
                            await _botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "Welcome to Amesa Lottery Bot! 👋\n\n" +
                                      "To link your Telegram account:\n" +
                                      "1. Go to your Amesa account settings\n" +
                                      "2. Request a verification code\n" +
                                      "3. Send the code to this bot using: /link <code>\n\n" +
                                      "Use /help for more information.");
                        }
                        return Ok();
                    }

                    // Handle /link command with verification code
                    if (text != null && text.StartsWith("/link", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length < 2)
                        {
                            if (_botClient != null)
                            {
                                await _botClient.SendTextMessageAsync(
                                    chatId: chatId,
                                    text: "❌ Invalid format. Please use: /link <verification_code>\n\n" +
                                          "Get your verification code from your Amesa account settings.");
                            }
                            return Ok();
                        }

                        var verificationCode = parts[1].ToUpper();

                        // Find link by verification code
                        var link = await _context.TelegramUserLinks
                            .FirstOrDefaultAsync(l => l.VerificationToken == verificationCode);

                        if (link == null)
                        {
                            if (_botClient != null)
                            {
                                await _botClient.SendTextMessageAsync(
                                    chatId: chatId,
                                    text: "❌ Invalid verification code. Please check your code and try again.\n\n" +
                                          "Get a new verification code from your Amesa account settings.");
                            }
                            return Ok();
                        }

                        // Update link with Telegram user information
                        link.TelegramUserId = userId;
                        link.TelegramUsername = username;
                        link.ChatId = chatId;
                        link.Verified = true;
                        link.VerificationToken = null;
                        link.UpdatedAt = DateTime.UtcNow;

                        await _context.SaveChangesAsync();

                        if (_botClient != null)
                        {
                            await _botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "✅ Your Telegram account has been successfully linked to your Amesa account!\n\n" +
                                      "You will now receive lottery notifications via Telegram.");
                        }

                        _logger.LogInformation("Telegram account linked successfully for user {UserId} (Telegram: {TelegramUserId})", 
                            link.UserId, userId);

                        return Ok();
                    }

                    // Handle /help command
                    if (text == "/help")
                    {
                        if (_botClient != null)
                        {
                            await _botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "📖 Amesa Lottery Bot Commands:\n\n" +
                                      "/start - Start the bot\n" +
                                      "/link <code> - Link your Telegram account\n" +
                                      "/help - Show this help message\n\n" +
                                      "To get a verification code, go to your Amesa account settings and request a Telegram link.");
                        }
                        return Ok();
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Telegram webhook");
                return StatusCode(500);
            }
        }
    }
}

