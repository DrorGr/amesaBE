using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AmesaBackend.Auth.Data;
using AmesaBackend.Auth.DTOs;
using AmesaBackend.Auth.Models;
using AmesaBackend.Auth.Services;
using AmesaBackend.Auth.Services.Interfaces;
using System.Security.Claims;
using System.Text.Json;
using Npgsql;

namespace AmesaBackend.Auth.Controllers
{
    [ApiController]
    [Route("api/v1/user/[controller]")]
    [Authorize]
    public class PreferencesController : ControllerBase
    {
        private readonly AuthDbContext _context;
        private readonly ILogger<PreferencesController> _logger;
        private readonly INotificationPreferencesSyncService? _notificationSyncService;

        public PreferencesController(
            AuthDbContext context,
            ILogger<PreferencesController> logger,
            INotificationPreferencesSyncService? notificationSyncService = null)
        {
            _context = context;
            _logger = logger;
            _notificationSyncService = notificationSyncService;
        }

        /// <summary>
        /// Get user preferences
        /// </summary>
        /// <returns>User preferences</returns>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<UserPreferencesDto>>> GetPreferences()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new ApiResponse<UserPreferencesDto>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var userPreferences = await _context.UserPreferences
                    .AsNoTracking()
                    .FirstOrDefaultAsync(up => up.UserId == userId);

                if (userPreferences == null)
                {
                    // Return default preferences if none exist
                    var defaultPreferences = CreateDefaultPreferences(userId.Value);
                    return Ok(new ApiResponse<UserPreferencesDto>
                    {
                        Success = true,
                        Data = MapToDto(defaultPreferences),
                        Message = "Default preferences returned"
                    });
                }

                return Ok(new ApiResponse<UserPreferencesDto>
                {
                    Success = true,
                    Data = MapToDto(userPreferences),
                    Message = "Preferences retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user preferences");
                return StatusCode(500, new ApiResponse<UserPreferencesDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving preferences",
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = ex.Message
                    }
                });
            }
        }

        /// <summary>
        /// Update user preferences
        /// </summary>
        /// <param name="request">User preferences update request</param>
        /// <returns>Updated preferences</returns>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<UserPreferencesDto>>> UpdatePreferences([FromBody] UpdateUserPreferencesRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new ApiResponse<UserPreferencesDto>
                    {
                        Success = false,
                        Message = "Request body is required"
                    });
                }
                
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new ApiResponse<UserPreferencesDto>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }
                
                // Check if preferences exist (use AsNoTracking for the check only)
                var existingPreferences = await _context.UserPreferences
                    .AsNoTracking()
                    .FirstOrDefaultAsync(up => up.UserId == userId);

                if (existingPreferences == null)
                {
                    // Create new preferences
                    try
                    {
                        if (request.Preferences.ValueKind == JsonValueKind.Undefined || request.Preferences.ValueKind == JsonValueKind.Null)
                        {
                            return BadRequest(new ApiResponse<UserPreferencesDto>
                            {
                                Success = false,
                                Message = "Preferences field is required and cannot be null or undefined"
                            });
                        }
                        var preferencesJson = request.Preferences.GetRawText();
                        
                        // Double-check that preferences weren't created between the check and now (race condition)
                        var doubleCheckPreferences = await _context.UserPreferences
                            .FirstOrDefaultAsync(up => up.UserId == userId);
                        
                        if (doubleCheckPreferences != null)
                        {
                            // Another request created preferences, update instead
                            _logger.LogInformation("Preferences were created concurrently; updating existing preferences for user {UserId}", userId);
                            doubleCheckPreferences.PreferencesJson = preferencesJson;
                            doubleCheckPreferences.Version = request.Version ?? doubleCheckPreferences.Version;
                            doubleCheckPreferences.UpdatedAt = DateTime.UtcNow;
                            doubleCheckPreferences.UpdatedBy = userId.ToString()!;
                            
                            await _context.SaveChangesAsync();
                            
                            // Sync notification preferences
                            if (_notificationSyncService != null)
                            {
                                try
                                {
                                    await _notificationSyncService.SyncNotificationPreferencesAsync(
                                        userId.Value, preferencesJson);
                                }
                                catch (Exception syncEx)
                                {
                                    _logger.LogWarning(syncEx,
                                        "Failed to sync notification preferences for user {UserId}. Preferences saved in Auth service.",
                                        userId);
                                }
                            }
                            
                            return Ok(new ApiResponse<UserPreferencesDto>
                            {
                                Success = true,
                                Data = MapToDto(doubleCheckPreferences),
                                Message = "Preferences updated successfully"
                            });
                        }
                        
                        var newPreferences = new UserPreferences
                        {
                            Id = Guid.NewGuid(),
                            UserId = userId.Value,
                            PreferencesJson = preferencesJson,
                            Version = request.Version ?? "1.0.0",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            CreatedBy = userId.ToString()!,
                            UpdatedBy = userId.ToString()!
                        };
                        
                        _context.UserPreferences.Add(newPreferences);
                        await _context.SaveChangesAsync();
                        
                        // Sync notification preferences to Notification service (if service is available)
                        if (_notificationSyncService != null)
                        {
                            try
                            {
                                await _notificationSyncService.SyncNotificationPreferencesAsync(
                                    userId.Value, preferencesJson);
                            }
                            catch (Exception syncEx)
                            {
                                // Log but don't fail the request - preferences are saved in Auth service
                                _logger.LogWarning(syncEx,
                                    "Failed to sync notification preferences for user {UserId}. Preferences saved in Auth service.",
                                    userId);
                            }
                        }
                        
                        _logger.LogInformation("Created new user preferences for user {UserId}", userId);
                        
                        return Ok(new ApiResponse<UserPreferencesDto>
                        {
                            Success = true,
                            Data = MapToDto(newPreferences),
                            Message = "Preferences created successfully"
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating new user preferences for user {UserId}", userId);
                        throw;
                    }
                }
                else
                {
                    // Update existing preferences
                    string preferencesJson;
                    try
                    {
                        if (request.Preferences.ValueKind == JsonValueKind.Undefined || request.Preferences.ValueKind == JsonValueKind.Null)
                        {
                            return BadRequest(new ApiResponse<UserPreferencesDto>
                            {
                                Success = false,
                                Message = "Preferences field is required and cannot be null or undefined"
                            });
                        }
                        preferencesJson = request.Preferences.GetRawText();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to parse preferences JSON for update");
                        throw;
                    }
                    
                    // Since UpdatedAt is a concurrency token, we need to read the entity with tracking
                    // to get the original UpdatedAt value, then update it
                    var trackedPreferences = await _context.UserPreferences
                        .FirstOrDefaultAsync(up => up.UserId == userId);
                    
                    if (trackedPreferences == null)
                    {
                        // This shouldn't happen, but handle it gracefully
                        _logger.LogWarning("Tracked preferences entity not found for user {UserId}", userId);
                        return NotFound(new ApiResponse<UserPreferencesDto>
                        {
                            Success = false,
                            Message = "Preferences not found"
                        });
                    }
                    
                    // Update the entity properties
                    trackedPreferences.PreferencesJson = preferencesJson;
                    trackedPreferences.Version = request.Version ?? trackedPreferences.Version;
                    trackedPreferences.UpdatedAt = DateTime.UtcNow;
                    trackedPreferences.UpdatedBy = userId.ToString()!;
                    
                    // Save changes - EF Core will handle the concurrency token check
                    await _context.SaveChangesAsync();

                    // Sync notification preferences to Notification service (if service is available)
                    if (_notificationSyncService != null)
                    {
                        try
                        {
                            await _notificationSyncService.SyncNotificationPreferencesAsync(
                                userId.Value, preferencesJson);
                        }
                        catch (Exception syncEx)
                        {
                            // Log but don't fail the request - preferences are saved in Auth service
                            _logger.LogWarning(syncEx,
                                "Failed to sync notification preferences for user {UserId}. Preferences saved in Auth service.",
                                userId);
                        }
                    }

                    _logger.LogInformation("Updated user preferences for user {UserId}", userId);

                    return Ok(new ApiResponse<UserPreferencesDto>
                    {
                        Success = true,
                        Data = MapToDto(trackedPreferences),
                        Message = "Preferences updated successfully"
                    });
                }
            }
            catch (DbUpdateConcurrencyException concurrencyEx)
            {
                // Handle concurrency conflicts (UpdatedAt concurrency token)
                var userIdForRetry = GetCurrentUserId();
                _logger.LogWarning(concurrencyEx, 
                    "Preferences concurrency conflict for user {UserId}. Retrying with fresh data.",
                    userIdForRetry);
                
                // Retry once with fresh data
                try
                {
                    if (userIdForRetry == null)
                    {
                        return Unauthorized(new ApiResponse<UserPreferencesDto>
                        {
                            Success = false,
                            Message = "User not authenticated"
                        });
                    }
                    
                    // Validate request is still available
                    if (request == null)
                    {
                        return BadRequest(new ApiResponse<UserPreferencesDto>
                        {
                            Success = false,
                            Message = "Request body is required"
                        });
                    }
                    
                    var freshPreferences = await _context.UserPreferences
                        .FirstOrDefaultAsync(up => up.UserId == userIdForRetry);
                    
                    if (freshPreferences == null)
                    {
                        return NotFound(new ApiResponse<UserPreferencesDto>
                        {
                            Success = false,
                            Message = "Preferences not found after concurrency conflict"
                        });
                    }
                    
                    string preferencesJson;
                    try
                    {
                        if (request.Preferences.ValueKind == JsonValueKind.Undefined || request.Preferences.ValueKind == JsonValueKind.Null)
                        {
                            return BadRequest(new ApiResponse<UserPreferencesDto>
                            {
                                Success = false,
                                Message = "Preferences field is required and cannot be null or undefined"
                            });
                        }
                        preferencesJson = request.Preferences.GetRawText();
                    }
                    catch (Exception jsonEx)
                    {
                        _logger.LogError(jsonEx, "Error parsing preferences JSON in retry");
                        throw;
                    }
                    freshPreferences.PreferencesJson = preferencesJson;
                    freshPreferences.Version = request.Version ?? freshPreferences.Version;
                    freshPreferences.UpdatedAt = DateTime.UtcNow;
                    freshPreferences.UpdatedBy = userIdForRetry.ToString()!;
                    
                    await _context.SaveChangesAsync();
                    
                    // Sync notification preferences
                    if (_notificationSyncService != null)
                    {
                        try
                        {
                            await _notificationSyncService.SyncNotificationPreferencesAsync(
                                userIdForRetry.Value, preferencesJson);
                        }
                        catch (Exception syncEx)
                        {
                            _logger.LogWarning(syncEx,
                                "Failed to sync notification preferences for user {UserId} after retry.",
                                userIdForRetry);
                        }
                    }
                    
                    return Ok(new ApiResponse<UserPreferencesDto>
                    {
                        Success = true,
                        Data = MapToDto(freshPreferences),
                        Message = "Preferences updated successfully (after retry)"
                    });
                }
                catch (Exception retryEx)
                {
                    _logger.LogError(retryEx, "Error retrying preferences update after concurrency conflict");
                    return StatusCode(500, new ApiResponse<UserPreferencesDto>
                    {
                        Success = false,
                        Message = "An error occurred while updating preferences (concurrency conflict)",
                        Error = new ErrorResponse
                        {
                            Code = "CONCURRENCY_ERROR",
                            Message = "Preferences were modified by another request. Please try again."
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user preferences");
                return StatusCode(500, new ApiResponse<UserPreferencesDto>
                {
                    Success = false,
                    Message = "An error occurred while updating preferences",
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = ex.Message
                    }
                });
            }
        }

        /// <summary>
        /// Reset user preferences to defaults
        /// </summary>
        /// <returns>Default preferences</returns>
        [HttpPost("reset")]
        public async Task<ActionResult<ApiResponse<UserPreferencesDto>>> ResetPreferences()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new ApiResponse<UserPreferencesDto>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                // Read with tracking to handle concurrency token properly
                var existingPreferences = await _context.UserPreferences
                    .FirstOrDefaultAsync(up => up.UserId == userId);

                var defaultPreferences = CreateDefaultPreferences(userId.Value);

                if (existingPreferences == null)
                {
                    _context.UserPreferences.Add(defaultPreferences);
                }
                else
                {
                    existingPreferences.PreferencesJson = defaultPreferences.PreferencesJson;
                    existingPreferences.Version = defaultPreferences.Version;
                    existingPreferences.UpdatedAt = DateTime.UtcNow;
                    existingPreferences.UpdatedBy = userId.ToString()!;
                    // Entity is already tracked, no need to call Update()
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Reset user preferences to defaults for user {UserId}", userId);

                return Ok(new ApiResponse<UserPreferencesDto>
                {
                    Success = true,
                    Data = MapToDto(existingPreferences ?? defaultPreferences),
                    Message = "Preferences reset to defaults successfully"
                });
            }
            catch (DbUpdateConcurrencyException concurrencyEx)
            {
                var userId = GetCurrentUserId();
                _logger.LogWarning(concurrencyEx, 
                    "Concurrency conflict while resetting preferences for user {UserId}. Retrying.",
                    userId);
                
                // Retry once with fresh data
                try
                {
                    if (userId == null)
                    {
                        return Unauthorized(new ApiResponse<UserPreferencesDto>
                        {
                            Success = false,
                            Message = "User not authenticated"
                        });
                    }
                    
                    var freshPreferences = await _context.UserPreferences
                        .FirstOrDefaultAsync(up => up.UserId == userId);
                    
                    var defaultPreferences = CreateDefaultPreferences(userId.Value);
                    
                    if (freshPreferences == null)
                    {
                        _context.UserPreferences.Add(defaultPreferences);
                    }
                    else
                    {
                        freshPreferences.PreferencesJson = defaultPreferences.PreferencesJson;
                        freshPreferences.Version = defaultPreferences.Version;
                        freshPreferences.UpdatedAt = DateTime.UtcNow;
                        freshPreferences.UpdatedBy = userId.ToString()!;
                    }
                    
                    await _context.SaveChangesAsync();
                    
                    return Ok(new ApiResponse<UserPreferencesDto>
                    {
                        Success = true,
                        Data = MapToDto(freshPreferences ?? defaultPreferences),
                        Message = "Preferences reset to defaults successfully (after retry)"
                    });
                }
                catch (Exception retryEx)
                {
                    _logger.LogError(retryEx, "Error retrying preferences reset after concurrency conflict");
                    return StatusCode(500, new ApiResponse<UserPreferencesDto>
                    {
                        Success = false,
                        Message = "An error occurred while resetting preferences (concurrency conflict)",
                        Error = new ErrorResponse
                        {
                            Code = "CONCURRENCY_ERROR",
                            Message = "Preferences were modified by another request. Please try again."
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting user preferences");
                return StatusCode(500, new ApiResponse<UserPreferencesDto>
                {
                    Success = false,
                    Message = "An error occurred while resetting preferences",
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = ex.Message
                    }
                });
            }
        }

        /// <summary>
        /// Delete user preferences
        /// </summary>
        /// <returns>Success response</returns>
        [HttpDelete]
        public async Task<ActionResult<ApiResponse<object>>> DeletePreferences()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var existingPreferences = await _context.UserPreferences
                    .FirstOrDefaultAsync(up => up.UserId == userId);

                if (existingPreferences != null)
                {
                    _context.UserPreferences.Remove(existingPreferences);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Deleted user preferences for user {UserId}", userId);
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Preferences deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user preferences");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while deleting preferences",
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = ex.Message
                    }
                });
            }
        }

        /// <summary>
        /// Get preferences sync status
        /// </summary>
        /// <returns>Sync status information</returns>
        [HttpGet("sync-status")]
        public async Task<ActionResult<ApiResponse<PreferencesSyncStatusDto>>> GetSyncStatus()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new ApiResponse<PreferencesSyncStatusDto>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var preferences = await _context.UserPreferences
                    .AsNoTracking()
                    .FirstOrDefaultAsync(up => up.UserId == userId);

                var syncStatus = new PreferencesSyncStatusDto
                {
                    LastSync = preferences?.UpdatedAt ?? DateTime.MinValue,
                    SyncInProgress = false,
                    ConflictResolution = "local",
                    HasLocalChanges = false,
                    HasRemoteChanges = false
                };

                return Ok(new ApiResponse<PreferencesSyncStatusDto>
                {
                    Success = true,
                    Data = syncStatus,
                    Message = "Sync status retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sync status");
                return StatusCode(500, new ApiResponse<PreferencesSyncStatusDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving sync status",
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = ex.Message
                    }
                });
            }
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        private UserPreferences CreateDefaultPreferences(Guid userId)
        {
            var defaultPreferencesJson = JsonSerializer.Serialize(new
            {
                version = "1.0.0",
                lastUpdated = DateTime.UtcNow,
                syncEnabled = true,
                appearance = new
                {
                    theme = "auto",
                    primaryColor = "#3B82F6",
                    accentColor = "#10B981",
                    fontSize = "medium",
                    fontFamily = "Inter, system-ui, sans-serif",
                    uiDensity = "comfortable",
                    borderRadius = 8,
                    showAnimations = true,
                    animationLevel = "normal",
                    reducedMotion = false
                },
                localization = new
                {
                    language = "en",
                    dateFormat = "MM/DD/YYYY",
                    timeFormat = "12h",
                    numberFormat = "US",
                    currency = "USD",
                    timezone = "UTC",
                    rtlSupport = false
                },
                accessibility = new
                {
                    highContrast = false,
                    colorBlindAssist = false,
                    colorBlindType = "none",
                    screenReaderOptimized = false,
                    keyboardNavigation = true,
                    focusIndicators = true,
                    skipLinks = true,
                    altTextVerbosity = "standard",
                    captionsEnabled = false,
                    audioDescriptions = false,
                    largeClickTargets = false,
                    reducedFlashing = false
                },
                notifications = new
                {
                    emailNotifications = true,
                    pushNotifications = false,
                    browserNotifications = false,
                    smsNotifications = false,
                    lotteryResults = true,
                    newLotteries = true,
                    promotions = false,
                    accountUpdates = true,
                    securityAlerts = true,
                    quietHours = new
                    {
                        enabled = false,
                        startTime = "22:00",
                        endTime = "08:00"
                    },
                    soundEnabled = true,
                    soundVolume = 50,
                    customSounds = false
                },
                lotteryPreferences = new
                {
                    favoriteHouseIds = new List<string>()
                }
            });

            return new UserPreferences
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PreferencesJson = defaultPreferencesJson,
                Version = "1.0.0",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = userId.ToString(),
                UpdatedBy = userId.ToString()
            };
        }

        private UserPreferencesDto MapToDto(UserPreferences preferences)
        {
            return new UserPreferencesDto
            {
                Id = preferences.Id,
                UserId = preferences.UserId,
                PreferencesJson = preferences.PreferencesJson,
                Version = preferences.Version,
                LastUpdated = preferences.UpdatedAt
            };
        }
    }
}