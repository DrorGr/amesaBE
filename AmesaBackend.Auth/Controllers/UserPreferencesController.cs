using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AmesaBackend.Auth.Data;
using AmesaBackend.Auth.DTOs;
using AmesaBackend.Auth.Models;
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

        public PreferencesController(
            AuthDbContext context,
            ILogger<PreferencesController> logger)
        {
            _context = context;
            _logger = logger;
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

                // #region agent log
                _logger.LogInformation("[DEBUG] GetUserPreferences:before-query userId={UserId}", userId);
                // #endregion
                
                var userPreferences = await _context.UserPreferences
                    .AsNoTracking()
                    .FirstOrDefaultAsync(up => up.UserId == userId);
                
                // #region agent log
                _logger.LogInformation("[DEBUG] GetUserPreferences:after-query found={Found}", userPreferences != null);
                // #endregion

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
                // #region agent log
                var innerEx = ex.InnerException;
                var postgresEx = innerEx as Npgsql.PostgresException;
                _logger.LogError(ex, "[DEBUG] GetPreferences:catch exType={ExType} exMessage={ExMessage} innerExType={InnerExType} innerExMessage={InnerExMessage} innerExFull={InnerExFull} stackTrace={StackTrace} postgresSqlState={PostgresSqlState} postgresMessageText={PostgresMessageText} postgresDetail={PostgresDetail}",
                    ex.GetType().Name,
                    ex.Message,
                    innerEx?.GetType().Name ?? "null",
                    innerEx?.Message ?? "null",
                    innerEx?.ToString() ?? "null",
                    ex.StackTrace,
                    postgresEx?.SqlState ?? "N/A",
                    postgresEx?.MessageText ?? "N/A",
                    postgresEx?.Detail ?? "N/A");
                // #endregion
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
                // #region agent log
                _logger.LogInformation("[DEBUG] UpdatePreferences:entry requestNull={RequestNull} requestVersion={RequestVersion}", 
                    request == null, request?.Version ?? "null");
                if (request != null)
                {
                    var preferencesValueKind = request.Preferences.ValueKind.ToString();
                    var preferencesHasValue = request.Preferences.ValueKind != System.Text.Json.JsonValueKind.Undefined;
                    string preferencesRawText = "N/A";
                    try
                    {
                        preferencesRawText = request.Preferences.GetRawText();
                    }
                    catch (Exception ex)
                    {
                        preferencesRawText = $"ERROR: {ex.Message}";
                    }
                    _logger.LogInformation("[DEBUG] UpdatePreferences:request-details preferencesValueKind={ValueKind} preferencesHasValue={HasValue} preferencesLength={Length}", 
                        preferencesValueKind, preferencesHasValue, preferencesRawText.Length);
                }
                // #endregion
                
                if (request == null)
                {
                    // #region agent log
                    _logger.LogWarning("[DEBUG] UpdatePreferences:bad-request request is null");
                    // #endregion
                    return BadRequest(new ApiResponse<UserPreferencesDto>
                    {
                        Success = false,
                        Message = "Request body is required"
                    });
                }
                
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    // #region agent log
                    _logger.LogWarning("[DEBUG] UpdatePreferences:unauthorized userId is null");
                    // #endregion
                    return Unauthorized(new ApiResponse<UserPreferencesDto>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                // #region agent log
                _logger.LogInformation("[DEBUG] UpdatePreferences:before-query userId={UserId}", userId);
                // #endregion
                
                var existingPreferences = await _context.UserPreferences
                    .FirstOrDefaultAsync(up => up.UserId == userId);

                // #region agent log
                _logger.LogInformation("[DEBUG] UpdatePreferences:after-query hasExistingPreferences={HasExistingPreferences}", existingPreferences != null);
                // #endregion

                if (existingPreferences == null)
                {
                    // Create new preferences
                    // #region agent log
                    string preferencesJson = "ERROR";
                    try
                    {
                        if (request.Preferences.ValueKind == JsonValueKind.Undefined || request.Preferences.ValueKind == JsonValueKind.Null)
                        {
                            _logger.LogWarning("[DEBUG] UpdatePreferences:getRawText-skipped ValueKind={ValueKind}", request.Preferences.ValueKind);
                            return BadRequest(new ApiResponse<UserPreferencesDto>
                            {
                                Success = false,
                                Message = "Preferences field is required and cannot be null or undefined"
                            });
                        }
                        preferencesJson = request.Preferences.GetRawText();
                        _logger.LogInformation("[DEBUG] UpdatePreferences:getRawText-success length={Length}", preferencesJson.Length);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[DEBUG] UpdatePreferences:getRawText-failed exType={ExType} exMessage={ExMessage}", 
                            ex.GetType().Name, ex.Message);
                        throw;
                    }
                    // #endregion
                    
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

                    // #region agent log
                    _logger.LogInformation("[DEBUG] UpdatePreferences:before-Add newPreferences.UserId={UserId}", newPreferences.UserId);
                    // #endregion
                    
                    _context.UserPreferences.Add(newPreferences);
                    
                    // #region agent log
                    _logger.LogInformation("[DEBUG] UpdatePreferences:before-SaveChangesAsync");
                    // #endregion
                    
                    await _context.SaveChangesAsync();
                    
                    // #region agent log
                    _logger.LogInformation("[DEBUG] UpdatePreferences:after-SaveChangesAsync success");
                    // #endregion

                    _logger.LogInformation("Created new user preferences for user {UserId}", userId);

                    return Ok(new ApiResponse<UserPreferencesDto>
                    {
                        Success = true,
                        Data = MapToDto(newPreferences),
                        Message = "Preferences created successfully"
                    });
                }
                else
                {
                    // Update existing preferences
                    // #region agent log
                    string preferencesJson = "ERROR";
                    try
                    {
                        if (request.Preferences.ValueKind == JsonValueKind.Undefined || request.Preferences.ValueKind == JsonValueKind.Null)
                        {
                            _logger.LogWarning("[DEBUG] UpdatePreferences:getRawText-update-skipped ValueKind={ValueKind}", request.Preferences.ValueKind);
                            return BadRequest(new ApiResponse<UserPreferencesDto>
                            {
                                Success = false,
                                Message = "Preferences field is required and cannot be null or undefined"
                            });
                        }
                        preferencesJson = request.Preferences.GetRawText();
                        _logger.LogInformation("[DEBUG] UpdatePreferences:getRawText-update-success length={Length}", preferencesJson.Length);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[DEBUG] UpdatePreferences:getRawText-update-failed exType={ExType} exMessage={ExMessage}", 
                            ex.GetType().Name, ex.Message);
                        throw;
                    }
                    // #endregion
                    
                    existingPreferences.PreferencesJson = preferencesJson;
                    existingPreferences.Version = request.Version ?? existingPreferences.Version;
                    existingPreferences.UpdatedAt = DateTime.UtcNow;
                    existingPreferences.UpdatedBy = userId.ToString()!;

                    // #region agent log
                    _logger.LogInformation("[DEBUG] UpdatePreferences:before-SaveChangesAsync-update existingPreferences.UserId={UserId}", existingPreferences.UserId);
                    // #endregion
                    
                    await _context.SaveChangesAsync();
                    
                    // #region agent log
                    _logger.LogInformation("[DEBUG] UpdatePreferences:after-SaveChangesAsync-update success");
                    // #endregion

                    _logger.LogInformation("Updated user preferences for user {UserId}", userId);

                    return Ok(new ApiResponse<UserPreferencesDto>
                    {
                        Success = true,
                        Data = MapToDto(existingPreferences),
                        Message = "Preferences updated successfully"
                    });
                }
            }
            catch (Exception ex)
            {
                // #region agent log
                var innerEx = ex.InnerException;
                var postgresEx = innerEx as Npgsql.PostgresException;
                _logger.LogError(ex, "[DEBUG] UpdatePreferences:catch exType={ExType} exMessage={ExMessage} innerExType={InnerExType} innerExMessage={InnerExMessage} innerExFull={InnerExFull} stackTrace={StackTrace} postgresSqlState={PostgresSqlState} postgresMessageText={PostgresMessageText} postgresDetail={PostgresDetail}",
                    ex.GetType().Name,
                    ex.Message,
                    innerEx?.GetType().Name ?? "null",
                    innerEx?.Message ?? "null",
                    innerEx?.ToString() ?? "null",
                    ex.StackTrace,
                    postgresEx?.SqlState ?? "N/A",
                    postgresEx?.MessageText ?? "N/A",
                    postgresEx?.Detail ?? "N/A");
                // #endregion
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