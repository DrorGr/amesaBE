using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AmesaBackend.Lottery.DTOs;
using AmesaBackend.Lottery.Services;
using AmesaBackend.Auth.Services;
using AmesaBackend.Shared.Caching;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Npgsql;

namespace AmesaBackend.Lottery.Controllers
{
    [ApiController]
    [Route("api/v1/houses")]
    public class HousesFavoritesController : ControllerBase
    {
        private readonly ILotteryService _lotteryService;
        private readonly ILogger<HousesFavoritesController> _logger;
        private readonly IRateLimitService? _rateLimitService;
        private readonly ICache? _cache;

        public HousesFavoritesController(
            ILotteryService lotteryService,
            ILogger<HousesFavoritesController> logger,
            IRateLimitService? rateLimitService = null,
            ICache? cache = null)
        {
            _lotteryService = lotteryService;
            _logger = logger;
            _rateLimitService = rateLimitService;
            _cache = cache;
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        /// <summary>
        /// Get user's favorite houses with pagination and sorting
        /// </summary>
        /// <param name="page">Page number (1-based, minimum 1)</param>
        /// <param name="limit">Items per page (1-100, default 20)</param>
        /// <param name="sortBy">Sort field: 'dateadded' (default), 'price', 'location', 'title'</param>
        /// <param name="sortOrder">Sort order: 'asc' (default) or 'desc'</param>
        /// <returns>Paginated list of favorite houses</returns>
        [HttpGet("favorites")]
        [Authorize]
        public async Task<ActionResult<AmesaBackend.Lottery.DTOs.ApiResponse<PagedResponse<HouseDto>>>> GetFavoriteHouses(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortOrder = null)
        {
            Guid? userId = null;
            try
            {
                userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new AmesaBackend.Lottery.DTOs.ApiResponse<PagedResponse<HouseDto>>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                // Validate pagination parameters
                if (page < 1)
                {
                    return BadRequest(new AmesaBackend.Lottery.DTOs.ApiResponse<PagedResponse<HouseDto>>
                    {
                        Success = false,
                        Message = "Page number must be greater than 0",
                        Error = new ErrorResponse { Code = "VALIDATION_ERROR", Message = "Page must be at least 1" }
                    });
                }
                if (limit < 1)
                {
                    return BadRequest(new AmesaBackend.Lottery.DTOs.ApiResponse<PagedResponse<HouseDto>>
                    {
                        Success = false,
                        Message = "Limit must be greater than 0",
                        Error = new ErrorResponse { Code = "VALIDATION_ERROR", Message = "Limit must be at least 1" }
                    });
                }
                if (limit > 100)
                {
                    return BadRequest(new AmesaBackend.Lottery.DTOs.ApiResponse<PagedResponse<HouseDto>>
                    {
                        Success = false,
                        Message = "Maximum limit is 100",
                        Error = new ErrorResponse { Code = "VALIDATION_ERROR", Message = "Limit cannot exceed 100" }
                    });
                }

                // Validate sorting parameters
                if (!string.IsNullOrWhiteSpace(sortBy))
                {
                    var validSortByValues = new[] { "dateadded", "price", "location", "title" };
                    var sortByLower = sortBy.ToLowerInvariant();
                    if (!validSortByValues.Contains(sortByLower))
                    {
                        return BadRequest(new AmesaBackend.Lottery.DTOs.ApiResponse<PagedResponse<HouseDto>>
                        {
                            Success = false,
                            Message = $"Invalid sortBy value. Allowed values: {string.Join(", ", validSortByValues)}",
                            Error = new ErrorResponse { Code = "VALIDATION_ERROR", Message = $"sortBy must be one of: {string.Join(", ", validSortByValues)}" }
                        });
                    }
                }

                if (!string.IsNullOrWhiteSpace(sortOrder))
                {
                    var sortOrderLower = sortOrder.ToLowerInvariant();
                    if (sortOrderLower != "asc" && sortOrderLower != "desc")
                    {
                        return BadRequest(new AmesaBackend.Lottery.DTOs.ApiResponse<PagedResponse<HouseDto>>
                        {
                            Success = false,
                            Message = "Invalid sortOrder value. Allowed values: asc, desc",
                            Error = new ErrorResponse { Code = "VALIDATION_ERROR", Message = "sortOrder must be 'asc' or 'desc'" }
                        });
                    }
                }

                // Rate limiting: 30 requests per 1 minute per user
                if (_rateLimitService != null)
                {
                    var rateLimitKey = $"favorites:{userId}:get";
                    var canRequest = await _rateLimitService.CheckRateLimitAsync(rateLimitKey, limit: 30, window: TimeSpan.FromMinutes(1));
                    
                    if (!canRequest)
                    {
                        _logger.LogWarning("Rate limit exceeded for user {UserId} on get favorites", userId);
                        Response.Headers["X-RateLimit-Limit"] = "30";
                        Response.Headers["X-RateLimit-Remaining"] = "0";
                        Response.Headers["X-RateLimit-Reset"] = DateTime.UtcNow.AddMinutes(1).ToString("R");
                        return StatusCode(429, new AmesaBackend.Lottery.DTOs.ApiResponse<PagedResponse<HouseDto>>
                        {
                            Success = false,
                            Message = "Rate limit exceeded. Please try again in a moment.",
                            Error = new ErrorResponse { Code = "RATE_LIMIT_EXCEEDED", Message = "Too many requests. Please wait before trying again." }
                        });
                    }

                    await _rateLimitService.IncrementRateLimitAsync(rateLimitKey, TimeSpan.FromMinutes(1));
                }
                else
                {
                    _logger.LogWarning("Rate limit service not available, skipping rate limiting for user {UserId}", userId);
                }

                // Get paginated and sorted results
                var favoriteHouses = await _lotteryService.GetUserFavoriteHousesAsync(userId.Value, page, limit, sortBy, sortOrder, HttpContext.RequestAborted);

                // Get total count using optimized count method (cached, fast path)
                var total = await _lotteryService.GetUserFavoriteHousesCountAsync(userId.Value, HttpContext.RequestAborted);

                var totalPages = (int)Math.Ceiling(total / (double)limit);
                
                // Validate page doesn't exceed total pages
                if (page > totalPages && totalPages > 0)
                {
                    return BadRequest(new AmesaBackend.Lottery.DTOs.ApiResponse<PagedResponse<HouseDto>>
                    {
                        Success = false,
                        Message = $"Page {page} exceeds total pages ({totalPages})",
                        Error = new ErrorResponse { Code = "VALIDATION_ERROR", Message = $"Page number must be between 1 and {totalPages}" }
                    });
                }

                var pagedResponse = new PagedResponse<HouseDto>
                {
                    Items = favoriteHouses,
                    Page = page,
                    Limit = limit,
                    Total = total,
                    TotalPages = totalPages,
                    HasNext = page < totalPages,
                    HasPrevious = page > 1
                };

                return Ok(new AmesaBackend.Lottery.DTOs.ApiResponse<PagedResponse<HouseDto>>
                {
                    Success = true,
                    Data = pagedResponse,
                    Message = "Favorite houses retrieved successfully"
                });
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error retrieving favorite houses for user {UserId}", userId);
                return StatusCode(503, new AmesaBackend.Lottery.DTOs.ApiResponse<PagedResponse<HouseDto>>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "SERVICE_UNAVAILABLE",
                        Message = "Service is temporarily unavailable. Please try again later."
                    }
                });
            }
            catch (PostgresException pgEx)
            {
                _logger.LogError(pgEx, "PostgreSQL error retrieving favorite houses for user {UserId}: {SqlState} - {Message}", userId, pgEx.SqlState, pgEx.MessageText);
                return StatusCode(503, new AmesaBackend.Lottery.DTOs.ApiResponse<PagedResponse<HouseDto>>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "SERVICE_UNAVAILABLE",
                        Message = "Service is temporarily unavailable. Please try again later."
                    }
                });
            }
            catch (System.Data.Common.DbException dbEx)
            {
                _logger.LogError(dbEx, "Database connectivity error retrieving favorite houses for user {UserId}", userId);
                return StatusCode(503, new AmesaBackend.Lottery.DTOs.ApiResponse<PagedResponse<HouseDto>>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "SERVICE_UNAVAILABLE",
                        Message = "Service is temporarily unavailable. Please try again later."
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving favorite houses for user {UserId}: {Message}", userId, ex.Message);
                return StatusCode(500, new AmesaBackend.Lottery.DTOs.ApiResponse<PagedResponse<HouseDto>>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = "An error occurred while processing your request. Please try again later."
                    }
                });
            }
        }

        /// <summary>
        /// Add a house to user's favorites
        /// </summary>
        /// <param name="id">House ID to add to favorites</param>
        /// <remarks>
        /// Supports idempotency via Idempotency-Key header (optional, max 128 chars, alphanumeric with dashes/underscores).
        /// Returns 409 Conflict if house is already in favorites.
        /// Returns 404 Not Found if house doesn't exist or is deleted.
        /// </remarks>
        /// <returns>Favorite house response</returns>
        [HttpPost("{id}/favorite")]
        [Authorize]
        public async Task<ActionResult<AmesaBackend.Lottery.DTOs.ApiResponse<FavoriteHouseResponse>>> AddToFavorites([FromRoute] Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new AmesaBackend.Lottery.DTOs.ApiResponse<FavoriteHouseResponse>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                // Idempotency check: If Idempotency-Key header is provided, check if operation was already processed
                var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
                if (!string.IsNullOrEmpty(idempotencyKey))
                {
                    // Validate idempotency key format (UUID or alphanumeric, max 128 chars)
                    if (idempotencyKey.Length > 128)
                    {
                        return BadRequest(new AmesaBackend.Lottery.DTOs.ApiResponse<FavoriteHouseResponse>
                        {
                            Success = false,
                            Message = "Idempotency-Key header is too long (maximum 128 characters)",
                            Error = new ErrorResponse { Code = "VALIDATION_ERROR", Message = "Idempotency-Key must be 128 characters or less" }
                        });
                    }

                    // Validate format (UUID or alphanumeric with dashes/underscores)
                    if (!Regex.IsMatch(idempotencyKey, @"^[a-zA-Z0-9\-_]+$"))
                    {
                        return BadRequest(new AmesaBackend.Lottery.DTOs.ApiResponse<FavoriteHouseResponse>
                        {
                            Success = false,
                            Message = "Invalid Idempotency-Key format. Must be alphanumeric with dashes or underscores",
                            Error = new ErrorResponse { Code = "VALIDATION_ERROR", Message = "Idempotency-Key contains invalid characters" }
                        });
                    }
                }

                if (!string.IsNullOrEmpty(idempotencyKey) && _cache != null)
                {
                    // Include operation type in cache key to prevent collision between add and remove operations
                    var idempotencyCacheKey = $"favorites:idempotency:{userId}:{id}:add:{idempotencyKey}";
                    try
                    {
                        var cachedResponse = await _cache.GetRecordAsync<FavoriteHouseResponse>(idempotencyCacheKey);
                        if (cachedResponse != null)
                        {
                            _logger.LogInformation("Idempotency key {IdempotencyKey} found for user {UserId}, returning cached response", idempotencyKey, userId);
                            return Ok(new AmesaBackend.Lottery.DTOs.ApiResponse<FavoriteHouseResponse>
                            {
                                Success = true,
                                Data = cachedResponse,
                                Message = "Operation already processed (idempotency)"
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to check idempotency key for user {UserId}", userId);
                        // Continue with operation if cache check fails
                    }
                }

                // Rate limiting: 10 requests per 1 minute per user
                if (_rateLimitService != null)
                {
                    var rateLimitKey = $"favorites:{userId}:add";
                    var canRequest = await _rateLimitService.CheckRateLimitAsync(rateLimitKey, limit: 10, window: TimeSpan.FromMinutes(1));
                    
                    if (!canRequest)
                    {
                        _logger.LogWarning("Rate limit exceeded for user {UserId} on add favorite", userId);
                        Response.Headers["X-RateLimit-Limit"] = "10";
                        Response.Headers["X-RateLimit-Remaining"] = "0";
                        Response.Headers["X-RateLimit-Reset"] = DateTime.UtcNow.AddMinutes(1).ToString("R");
                        return StatusCode(429, new AmesaBackend.Lottery.DTOs.ApiResponse<FavoriteHouseResponse>
                        {
                            Success = false,
                            Message = "Rate limit exceeded. Please try again in a moment.",
                            Error = new ErrorResponse { Code = "RATE_LIMIT_EXCEEDED", Message = "Too many requests. Please wait before trying again." }
                        });
                    }

                    await _rateLimitService.IncrementRateLimitAsync(rateLimitKey, TimeSpan.FromMinutes(1));
                }
                else
                {
                    _logger.LogWarning("Rate limit service not available, skipping rate limiting for user {UserId}", userId);
                }

                // Validate GUID format (ASP.NET Core model binding handles this, but add explicit check for safety)
                if (id == Guid.Empty)
                {
                    return BadRequest(new AmesaBackend.Lottery.DTOs.ApiResponse<FavoriteHouseResponse>
                    {
                        Success = false,
                        Message = "Invalid house ID format",
                        Error = new ErrorResponse { Code = "VALIDATION_ERROR", Message = "House ID cannot be empty" }
                    });
                }

                var result = await _lotteryService.AddHouseToFavoritesAsync(userId.Value, id, HttpContext.RequestAborted);

                if (!result.Success)
                {
                    // Use specific error from service result
                    var errorCode = result.Error?.Code ?? "UNKNOWN_ERROR";
                    var errorMessage = result.Error?.Message ?? "Failed to add house to favorites";
                    
                    if (errorCode == "ALREADY_FAVORITE")
                    {
                        return Conflict(new AmesaBackend.Lottery.DTOs.ApiResponse<FavoriteHouseResponse>
                        {
                            Success = false,
                            Message = errorMessage,
                            Error = new ErrorResponse { Code = errorCode, Message = errorMessage }
                        });
                    }
                    else if (errorCode == "HOUSE_NOT_FOUND")
                    {
                        return NotFound(new AmesaBackend.Lottery.DTOs.ApiResponse<FavoriteHouseResponse>
                        {
                            Success = false,
                            Message = errorMessage,
                            Error = new ErrorResponse { Code = errorCode, Message = errorMessage }
                        });
                    }
                    else
                    {
                        return StatusCode(503, new AmesaBackend.Lottery.DTOs.ApiResponse<FavoriteHouseResponse>
                        {
                            Success = false,
                            Message = errorMessage,
                            Error = new ErrorResponse { Code = errorCode, Message = errorMessage }
                        });
                    }
                }

                var response = new FavoriteHouseResponse
                {
                    HouseId = id,
                    Added = true,
                    Message = "House added to favorites successfully"
                };

                // Cache response for idempotency (24-hour TTL) - include operation type in key
                if (!string.IsNullOrEmpty(idempotencyKey) && _cache != null)
                {
                    try
                    {
                        var idempotencyCacheKey = $"favorites:idempotency:{userId}:{id}:add:{idempotencyKey}";
                        await _cache.SetRecordAsync(idempotencyCacheKey, response, TimeSpan.FromHours(24));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to cache idempotency response for user {UserId}", userId);
                        // Don't fail the operation if caching fails
                    }
                }

                return Ok(new AmesaBackend.Lottery.DTOs.ApiResponse<FavoriteHouseResponse>
                {
                    Success = true,
                    Data = response
                });
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error adding house {HouseId} to favorites", id);
                return StatusCode(503, new AmesaBackend.Lottery.DTOs.ApiResponse<FavoriteHouseResponse>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "SERVICE_UNAVAILABLE",
                        Message = "Service is temporarily unavailable. Please try again later."
                    }
                });
            }
            catch (System.Data.Common.DbException dbEx)
            {
                _logger.LogError(dbEx, "Database connectivity error adding house {HouseId} to favorites", id);
                return StatusCode(503, new AmesaBackend.Lottery.DTOs.ApiResponse<FavoriteHouseResponse>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "SERVICE_UNAVAILABLE",
                        Message = "Service is temporarily unavailable. Please try again later."
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding house {HouseId} to favorites", id);
                return StatusCode(500, new AmesaBackend.Lottery.DTOs.ApiResponse<FavoriteHouseResponse>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = "An error occurred while processing your request. Please try again later."
                    }
                });
            }
        }

        /// <summary>
        /// Remove a house from user's favorites
        /// </summary>
        /// <param name="id">House ID to remove from favorites</param>
        /// <remarks>
        /// Supports idempotency via Idempotency-Key header (optional, max 128 chars, alphanumeric with dashes/underscores).
        /// Returns 404 Not Found if house doesn't exist or is deleted.
        /// </remarks>
        /// <returns>Favorite house response</returns>
        [HttpDelete("{id}/favorite")]
        [Authorize]
        public async Task<ActionResult<AmesaBackend.Lottery.DTOs.ApiResponse<FavoriteHouseResponse>>> RemoveFromFavorites([FromRoute] Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new AmesaBackend.Lottery.DTOs.ApiResponse<FavoriteHouseResponse>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                // Idempotency check: If Idempotency-Key header is provided, check if operation was already processed
                var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
                if (!string.IsNullOrEmpty(idempotencyKey))
                {
                    // Validate idempotency key format (UUID or alphanumeric, max 128 chars)
                    if (idempotencyKey.Length > 128)
                    {
                        return BadRequest(new AmesaBackend.Lottery.DTOs.ApiResponse<FavoriteHouseResponse>
                        {
                            Success = false,
                            Message = "Idempotency-Key header is too long (maximum 128 characters)",
                            Error = new ErrorResponse { Code = "VALIDATION_ERROR", Message = "Idempotency-Key must be 128 characters or less" }
                        });
                    }

                    // Validate format (UUID or alphanumeric with dashes/underscores)
                    if (!Regex.IsMatch(idempotencyKey, @"^[a-zA-Z0-9\-_]+$"))
                    {
                        return BadRequest(new AmesaBackend.Lottery.DTOs.ApiResponse<FavoriteHouseResponse>
                        {
                            Success = false,
                            Message = "Invalid Idempotency-Key format. Must be alphanumeric with dashes or underscores",
                            Error = new ErrorResponse { Code = "VALIDATION_ERROR", Message = "Idempotency-Key contains invalid characters" }
                        });
                    }
                }

                if (!string.IsNullOrEmpty(idempotencyKey) && _cache != null)
                {
                    // Include operation type in cache key to prevent collision between add and remove operations
                    var idempotencyCacheKey = $"favorites:idempotency:{userId}:{id}:remove:{idempotencyKey}";
                    try
                    {
                        var cachedResponse = await _cache.GetRecordAsync<FavoriteHouseResponse>(idempotencyCacheKey);
                        if (cachedResponse != null)
                        {
                            _logger.LogInformation("Idempotency key {IdempotencyKey} found for user {UserId}, returning cached response", idempotencyKey, userId);
                            return Ok(new AmesaBackend.Lottery.DTOs.ApiResponse<FavoriteHouseResponse>
                            {
                                Success = true,
                                Data = cachedResponse,
                                Message = "Operation already processed (idempotency)"
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to check idempotency key for user {UserId}", userId);
                        // Continue with operation if cache check fails
                    }
                }

                // Rate limiting: 10 requests per 1 minute per user (fail-open if service unavailable)
                if (_rateLimitService != null)
                {
                    try
                    {
                        var rateLimitKey = $"favorites:{userId}:remove";
                        var canRequest = await _rateLimitService.CheckRateLimitAsync(rateLimitKey, limit: 10, window: TimeSpan.FromMinutes(1));
                        
                        if (!canRequest)
                        {
                            _logger.LogWarning("Rate limit exceeded for user {UserId} on remove favorite", userId);
                            Response.Headers["X-RateLimit-Limit"] = "10";
                            Response.Headers["X-RateLimit-Remaining"] = "0";
                            Response.Headers["X-RateLimit-Reset"] = DateTime.UtcNow.AddMinutes(1).ToString("R");
                            return StatusCode(429, new AmesaBackend.Lottery.DTOs.ApiResponse<FavoriteHouseResponse>
                            {
                                Success = false,
                                Message = "Rate limit exceeded. Please try again in a moment.",
                                Error = new ErrorResponse { Code = "RATE_LIMIT_EXCEEDED", Message = "Too many requests. Please wait before trying again." }
                            });
                        }

                        await _rateLimitService.IncrementRateLimitAsync(rateLimitKey, TimeSpan.FromMinutes(1));
                    }
                    catch (Exception ex)
                    {
                        // Fail-open: allow request if rate limiting service is unavailable
                        _logger.LogWarning(ex, "Rate limiting service unavailable for user {UserId}, allowing request (fail-open)", userId);
                    }
                }

                // Validate GUID format (ASP.NET Core model binding handles this, but add explicit check for safety)
                if (id == Guid.Empty)
                {
                    return BadRequest(new AmesaBackend.Lottery.DTOs.ApiResponse<FavoriteHouseResponse>
                    {
                        Success = false,
                        Message = "Invalid house ID format",
                        Error = new ErrorResponse { Code = "VALIDATION_ERROR", Message = "House ID cannot be empty" }
                    });
                }

                var result = await _lotteryService.RemoveHouseFromFavoritesAsync(userId.Value, id, HttpContext.RequestAborted);

                if (!result.Success)
                {
                    // Use specific error from service result
                    var errorCode = result.Error?.Code ?? "UNKNOWN_ERROR";
                    var errorMessage = result.Error?.Message ?? "Failed to remove house from favorites";
                    
                    if (errorCode == "NOT_IN_FAVORITES")
                    {
                        return NotFound(new AmesaBackend.Lottery.DTOs.ApiResponse<FavoriteHouseResponse>
                        {
                            Success = false,
                            Message = errorMessage,
                            Error = new ErrorResponse { Code = errorCode, Message = errorMessage }
                        });
                    }
                    else if (errorCode == "HOUSE_NOT_FOUND")
                    {
                        return NotFound(new AmesaBackend.Lottery.DTOs.ApiResponse<FavoriteHouseResponse>
                        {
                            Success = false,
                            Message = errorMessage,
                            Error = new ErrorResponse { Code = errorCode, Message = errorMessage }
                        });
                    }
                    else
                    {
                        return StatusCode(503, new AmesaBackend.Lottery.DTOs.ApiResponse<FavoriteHouseResponse>
                        {
                            Success = false,
                            Message = errorMessage,
                            Error = new ErrorResponse { Code = errorCode, Message = errorMessage }
                        });
                    }
                }

                var response = new FavoriteHouseResponse
                {
                    HouseId = id,
                    Added = false,
                    Message = "House removed from favorites successfully"
                };

                // Cache response for idempotency (24-hour TTL) - include operation type in key
                if (!string.IsNullOrEmpty(idempotencyKey) && _cache != null)
                {
                    try
                    {
                        var idempotencyCacheKey = $"favorites:idempotency:{userId}:{id}:remove:{idempotencyKey}";
                        await _cache.SetRecordAsync(idempotencyCacheKey, response, TimeSpan.FromHours(24));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to cache idempotency response for user {UserId}", userId);
                        // Don't fail the operation if caching fails
                    }
                }

                return Ok(new AmesaBackend.Lottery.DTOs.ApiResponse<FavoriteHouseResponse>
                {
                    Success = true,
                    Data = response
                });
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error removing house {HouseId} from favorites", id);
                return StatusCode(503, new AmesaBackend.Lottery.DTOs.ApiResponse<FavoriteHouseResponse>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "SERVICE_UNAVAILABLE",
                        Message = "Service is temporarily unavailable. Please try again later."
                    }
                });
            }
            catch (System.Data.Common.DbException dbEx)
            {
                _logger.LogError(dbEx, "Database connectivity error removing house {HouseId} from favorites", id);
                return StatusCode(503, new AmesaBackend.Lottery.DTOs.ApiResponse<FavoriteHouseResponse>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "SERVICE_UNAVAILABLE",
                        Message = "Service is temporarily unavailable. Please try again later."
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing house {HouseId} from favorites", id);
                return StatusCode(500, new AmesaBackend.Lottery.DTOs.ApiResponse<FavoriteHouseResponse>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = "An error occurred while processing your request. Please try again later."
                    }
                });
            }
        }

        /// <summary>
        /// Get count of user's favorite houses
        /// </summary>
        [HttpGet("favorites/count")]
        [Authorize]
        public async Task<ActionResult<AmesaBackend.Lottery.DTOs.ApiResponse<object>>> GetFavoriteHousesCount()
        {
            Guid? userId = null;
            try
            {
                userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new AmesaBackend.Lottery.DTOs.ApiResponse<object>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var count = await _lotteryService.GetUserFavoriteHousesCountAsync(userId.Value, HttpContext.RequestAborted);

                return Ok(new AmesaBackend.Lottery.DTOs.ApiResponse<object>
                {
                    Success = true,
                    Data = new { count },
                    Message = "Favorite houses count retrieved successfully"
                });
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error retrieving favorite houses count for user {UserId}", userId);
                return StatusCode(503, new AmesaBackend.Lottery.DTOs.ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "SERVICE_UNAVAILABLE",
                        Message = "Service is temporarily unavailable. Please try again later."
                    }
                });
            }
            catch (PostgresException pgEx)
            {
                _logger.LogError(pgEx, "PostgreSQL error retrieving favorite houses count for user {UserId}: {SqlState} - {Message}", userId, pgEx.SqlState, pgEx.MessageText);
                return StatusCode(503, new AmesaBackend.Lottery.DTOs.ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "SERVICE_UNAVAILABLE",
                        Message = "Service is temporarily unavailable. Please try again later."
                    }
                });
            }
            catch (System.Data.Common.DbException dbEx)
            {
                _logger.LogError(dbEx, "Database connectivity error retrieving favorite houses count for user {UserId}", userId);
                return StatusCode(503, new AmesaBackend.Lottery.DTOs.ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "SERVICE_UNAVAILABLE",
                        Message = "Service is temporarily unavailable. Please try again later."
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving favorite houses count for user {UserId}: {Message}", userId, ex.Message);
                return StatusCode(500, new AmesaBackend.Lottery.DTOs.ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = "An error occurred while processing your request. Please try again later."
                    }
                });
            }
        }

        /// <summary>
        /// Bulk add houses to user's favorites
        /// </summary>
        /// <param name="request">Request containing list of house IDs to add (max 50)</param>
        /// <returns>Bulk operation result with success/failure counts</returns>
        [HttpPost("favorites/bulk")]
        [Authorize]
        public async Task<ActionResult<AmesaBackend.Lottery.DTOs.ApiResponse<BulkFavoritesResponse>>> BulkAddFavorites([FromBody] BulkFavoritesRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new AmesaBackend.Lottery.DTOs.ApiResponse<BulkFavoritesResponse>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                // Validate request
                if (request == null || request.HouseIds == null || request.HouseIds.Count == 0)
                {
                    return BadRequest(new AmesaBackend.Lottery.DTOs.ApiResponse<BulkFavoritesResponse>
                    {
                        Success = false,
                        Message = "House IDs list is required",
                        Error = new ErrorResponse { Code = "VALIDATION_ERROR", Message = "House IDs list cannot be empty" }
                    });
                }

                if (request.HouseIds.Count > 50)
                {
                    return BadRequest(new AmesaBackend.Lottery.DTOs.ApiResponse<BulkFavoritesResponse>
                    {
                        Success = false,
                        Message = "Maximum 50 house IDs allowed per request",
                        Error = new ErrorResponse { Code = "VALIDATION_ERROR", Message = "Maximum 50 house IDs allowed" }
                    });
                }

                // Validate GUID format (no empty GUIDs)
                if (request.HouseIds.Any(id => id == Guid.Empty))
                {
                    return BadRequest(new AmesaBackend.Lottery.DTOs.ApiResponse<BulkFavoritesResponse>
                    {
                        Success = false,
                        Message = "Invalid house ID format",
                        Error = new ErrorResponse { Code = "VALIDATION_ERROR", Message = "House IDs cannot be empty" }
                    });
                }

                // Rate limiting: 10 requests per 1 minute per user (bulk operations are resource-intensive, fail-open if service unavailable)
                if (_rateLimitService != null)
                {
                    try
                    {
                        var rateLimitKey = $"favorites:{userId}:bulk-add";
                        var canRequest = await _rateLimitService.CheckRateLimitAsync(rateLimitKey, limit: 10, window: TimeSpan.FromMinutes(1));
                        
                        if (!canRequest)
                        {
                            _logger.LogWarning("Rate limit exceeded for user {UserId} on bulk add favorites", userId);
                            Response.Headers["X-RateLimit-Limit"] = "10";
                            Response.Headers["X-RateLimit-Remaining"] = "0";
                            Response.Headers["X-RateLimit-Reset"] = DateTime.UtcNow.AddMinutes(1).ToString("R");
                            return StatusCode(429, new AmesaBackend.Lottery.DTOs.ApiResponse<BulkFavoritesResponse>
                            {
                                Success = false,
                                Message = "Rate limit exceeded. Please try again in a moment.",
                                Error = new ErrorResponse { Code = "RATE_LIMIT_EXCEEDED", Message = "Too many bulk requests. Please wait before trying again." }
                            });
                        }

                        await _rateLimitService.IncrementRateLimitAsync(rateLimitKey, TimeSpan.FromMinutes(1));
                    }
                    catch (Exception ex)
                    {
                        // Fail-open: allow request if rate limiting service is unavailable
                        _logger.LogWarning(ex, "Rate limiting service unavailable for user {UserId}, allowing request (fail-open)", userId);
                    }
                }

                // Check for duplicates and warn user
                var duplicateCount = request.HouseIds.Count - request.HouseIds.Distinct().Count();
                if (duplicateCount > 0)
                {
                    _logger.LogInformation("Bulk add request for user {UserId} contains {DuplicateCount} duplicate house IDs, will be removed during processing", userId, duplicateCount);
                }

                var result = await _lotteryService.BulkAddFavoritesAsync(userId.Value, request.HouseIds, HttpContext.RequestAborted);
                
                // Note: TotalRequested is now set correctly in the service method after deduplication

                return Ok(new AmesaBackend.Lottery.DTOs.ApiResponse<BulkFavoritesResponse>
                {
                    Success = true,
                    Data = result,
                    Message = $"Bulk add completed: {result.Successful} successful, {result.Failed} failed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk add favorites");
                return StatusCode(500, new AmesaBackend.Lottery.DTOs.ApiResponse<BulkFavoritesResponse>
                {
                    Success = false,
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = ex.Message }
                });
            }
        }

        /// <summary>
        /// Bulk remove houses from user's favorites
        /// </summary>
        /// <param name="request">Request containing list of house IDs to remove (max 50)</param>
        /// <returns>Bulk operation result with success/failure counts</returns>
        [HttpDelete("favorites/bulk")]
        [Authorize]
        public async Task<ActionResult<AmesaBackend.Lottery.DTOs.ApiResponse<BulkFavoritesResponse>>> BulkRemoveFavorites([FromBody] BulkFavoritesRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new AmesaBackend.Lottery.DTOs.ApiResponse<BulkFavoritesResponse>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                // Validate request
                if (request == null || request.HouseIds == null || request.HouseIds.Count == 0)
                {
                    return BadRequest(new AmesaBackend.Lottery.DTOs.ApiResponse<BulkFavoritesResponse>
                    {
                        Success = false,
                        Message = "House IDs list is required",
                        Error = new ErrorResponse { Code = "VALIDATION_ERROR", Message = "House IDs list cannot be empty" }
                    });
                }

                if (request.HouseIds.Count > 50)
                {
                    return BadRequest(new AmesaBackend.Lottery.DTOs.ApiResponse<BulkFavoritesResponse>
                    {
                        Success = false,
                        Message = "Maximum 50 house IDs allowed per request",
                        Error = new ErrorResponse { Code = "VALIDATION_ERROR", Message = "Maximum 50 house IDs allowed" }
                    });
                }

                // Validate GUID format (no empty GUIDs)
                if (request.HouseIds.Any(id => id == Guid.Empty))
                {
                    return BadRequest(new AmesaBackend.Lottery.DTOs.ApiResponse<BulkFavoritesResponse>
                    {
                        Success = false,
                        Message = "Invalid house ID format",
                        Error = new ErrorResponse { Code = "VALIDATION_ERROR", Message = "House IDs cannot be empty" }
                    });
                }

                // Rate limiting: 10 requests per 1 minute per user (bulk operations are resource-intensive, fail-open if service unavailable)
                if (_rateLimitService != null)
                {
                    try
                    {
                        var rateLimitKey = $"favorites:{userId}:bulk-remove";
                        var canRequest = await _rateLimitService.CheckRateLimitAsync(rateLimitKey, limit: 10, window: TimeSpan.FromMinutes(1));
                        
                        if (!canRequest)
                        {
                            _logger.LogWarning("Rate limit exceeded for user {UserId} on bulk remove favorites", userId);
                            Response.Headers["X-RateLimit-Limit"] = "10";
                            Response.Headers["X-RateLimit-Remaining"] = "0";
                            Response.Headers["X-RateLimit-Reset"] = DateTime.UtcNow.AddMinutes(1).ToString("R");
                            return StatusCode(429, new AmesaBackend.Lottery.DTOs.ApiResponse<BulkFavoritesResponse>
                            {
                                Success = false,
                                Message = "Rate limit exceeded. Please try again in a moment.",
                                Error = new ErrorResponse { Code = "RATE_LIMIT_EXCEEDED", Message = "Too many bulk requests. Please wait before trying again." }
                            });
                        }

                        await _rateLimitService.IncrementRateLimitAsync(rateLimitKey, TimeSpan.FromMinutes(1));
                    }
                    catch (Exception ex)
                    {
                        // Fail-open: allow request if rate limiting service is unavailable
                        _logger.LogWarning(ex, "Rate limiting service unavailable for user {UserId}, allowing request (fail-open)", userId);
                    }
                }

                // Check for duplicates and warn user
                var duplicateCount = request.HouseIds.Count - request.HouseIds.Distinct().Count();
                if (duplicateCount > 0)
                {
                    _logger.LogInformation("Bulk remove request for user {UserId} contains {DuplicateCount} duplicate house IDs, will be removed during processing", userId, duplicateCount);
                }

                var result = await _lotteryService.BulkRemoveFavoritesAsync(userId.Value, request.HouseIds, HttpContext.RequestAborted);
                
                // Note: TotalRequested is now set correctly in the service method after deduplication

                return Ok(new AmesaBackend.Lottery.DTOs.ApiResponse<BulkFavoritesResponse>
                {
                    Success = true,
                    Data = result,
                    Message = $"Bulk remove completed: {result.Successful} successful, {result.Failed} failed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk remove favorites");
                return StatusCode(500, new AmesaBackend.Lottery.DTOs.ApiResponse<BulkFavoritesResponse>
                {
                    Success = false,
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred while processing your request. Please try again later." }
                });
            }
        }

        /// <summary>
        /// Get favorites analytics (aggregated statistics across all users)
        /// </summary>
        /// <remarks>
        /// Returns:
        /// - Total favorites count
        /// - Unique users with favorites
        /// - Most favorited houses (top 10)
        /// - Favorites by date (last 30 days)
        /// 
        /// Note: Currently available to all authenticated users. Consider adding admin-only policy in future.
        /// </remarks>
        /// <returns>Favorites analytics data</returns>
        [HttpGet("favorites/analytics")]
        [Authorize]
        public async Task<ActionResult<AmesaBackend.Lottery.DTOs.ApiResponse<FavoritesAnalyticsDto>>> GetFavoritesAnalytics()
        {
            try
            {
                // Rate limiting: 5 requests per 5 minutes per user (analytics is resource-intensive)
                var userId = GetCurrentUserId();
                if (userId != null && _rateLimitService != null)
                {
                    var rateLimitKey = $"favorites:{userId}:analytics";
                    var canRequest = await _rateLimitService.CheckRateLimitAsync(rateLimitKey, limit: 5, window: TimeSpan.FromMinutes(5));
                    
                    if (!canRequest)
                    {
                        _logger.LogWarning("Rate limit exceeded for user {UserId} on get favorites analytics", userId);
                        Response.Headers["X-RateLimit-Limit"] = "5";
                        Response.Headers["X-RateLimit-Remaining"] = "0";
                        Response.Headers["X-RateLimit-Reset"] = DateTime.UtcNow.AddMinutes(5).ToString("R");
                        return StatusCode(429, new AmesaBackend.Lottery.DTOs.ApiResponse<FavoritesAnalyticsDto>
                        {
                            Success = false,
                            Message = "Rate limit exceeded. Please try again later.",
                            Error = new ErrorResponse { Code = "RATE_LIMIT_EXCEEDED", Message = "Too many analytics requests. Please wait before trying again." }
                        });
                    }

                    await _rateLimitService.IncrementRateLimitAsync(rateLimitKey, TimeSpan.FromMinutes(5));
                }

                var analytics = await _lotteryService.GetFavoritesAnalyticsAsync(HttpContext.RequestAborted);

                return Ok(new AmesaBackend.Lottery.DTOs.ApiResponse<FavoritesAnalyticsDto>
                {
                    Success = true,
                    Data = analytics,
                    Message = "Favorites analytics retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving favorites analytics");
                return StatusCode(500, new AmesaBackend.Lottery.DTOs.ApiResponse<FavoritesAnalyticsDto>
                {
                    Success = false,
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred while processing your request. Please try again later." }
                });
            }
        }

        /// <summary>
        /// Export user's favorites as CSV or JSON
        /// </summary>
        /// <param name="format">Export format: 'csv' or 'json' (default: 'json')</param>
        /// <remarks>
        /// - Maximum 10,000 favorites can be exported at once
        /// - Rate limited to 5 requests per hour per user
        /// - CSV format includes: House ID, Title, Price, Location, Status, Created At
        /// - JSON format includes full house details
        /// </remarks>
        /// <returns>File download (CSV or JSON)</returns>
        [HttpGet("favorites/export")]
        [Authorize]
        public async Task<ActionResult> ExportFavorites([FromQuery] string format = "json")
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized();
                }

                // Rate limiting: 5 requests per 1 hour per user (export is resource-intensive)
                if (_rateLimitService != null)
                {
                    var rateLimitKey = $"favorites:{userId}:export";
                    var canRequest = await _rateLimitService.CheckRateLimitAsync(rateLimitKey, limit: 5, window: TimeSpan.FromHours(1));
                    
                    if (!canRequest)
                    {
                        _logger.LogWarning("Rate limit exceeded for user {UserId} on export favorites", userId);
                        Response.Headers["X-RateLimit-Limit"] = "5";
                        Response.Headers["X-RateLimit-Remaining"] = "0";
                        Response.Headers["X-RateLimit-Reset"] = DateTime.UtcNow.AddHours(1).ToString("R");
                        return StatusCode(429, new AmesaBackend.Lottery.DTOs.ApiResponse<object>
                        {
                            Success = false,
                            Message = "Rate limit exceeded. Please try again later.",
                            Error = new ErrorResponse { Code = "RATE_LIMIT_EXCEEDED", Message = "Too many export requests. Please wait before trying again." }
                        });
                    }

                    await _rateLimitService.IncrementRateLimitAsync(rateLimitKey, TimeSpan.FromHours(1));
                }

                // Get count first to check limits
                var totalCount = await _lotteryService.GetUserFavoriteHousesCountAsync(userId.Value, HttpContext.RequestAborted);
                
                // Check if user has no favorites
                if (totalCount == 0)
                {
                    return NotFound(new AmesaBackend.Lottery.DTOs.ApiResponse<object>
                    {
                        Success = false,
                        Message = "No favorites found to export",
                        Error = new ErrorResponse { Code = "NOT_FOUND", Message = "You don't have any favorite houses to export" }
                    });
                }
                
                // Limit export size to prevent memory issues (max 10,000 favorites)
                const int maxExportSize = 10000;
                if (totalCount > maxExportSize)
                {
                    return BadRequest(new AmesaBackend.Lottery.DTOs.ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Export limit exceeded. Maximum {maxExportSize} favorites can be exported at once.",
                        Error = new ErrorResponse { Code = "VALIDATION_ERROR", Message = $"You have {totalCount} favorites. Please use pagination or contact support for large exports." }
                    });
                }

                // Validate export format
                var formatLower = format.ToLowerInvariant();
                if (formatLower != "csv" && formatLower != "json")
                {
                    return BadRequest(new AmesaBackend.Lottery.DTOs.ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid export format. Allowed values: csv, json",
                        Error = new ErrorResponse { Code = "VALIDATION_ERROR", Message = "Format must be 'csv' or 'json'" }
                    });
                }

                // Sanitize filename to prevent HTTP header injection
                var sanitizedUserId = System.Text.RegularExpressions.Regex.Replace(userId.ToString(), @"[^a-zA-Z0-9\-_]", "_");
                var sanitizedFilename = formatLower == "csv" 
                    ? $"favorites_{sanitizedUserId}_{DateTime.UtcNow:yyyyMMdd}.csv"
                    : $"favorites_{sanitizedUserId}_{DateTime.UtcNow:yyyyMMdd}.json";
                // Limit filename length to prevent header issues (max 255 chars)
                if (sanitizedFilename.Length > 255)
                {
                    sanitizedFilename = sanitizedFilename.Substring(0, 255);
                }

                // Set response headers for file download
                Response.ContentType = formatLower == "csv" 
                    ? "text/csv; charset=utf-8" 
                    : "application/json; charset=utf-8";
                Response.Headers["Content-Disposition"] = $"attachment; filename=\"{sanitizedFilename}\"";

                // Stream export directly to response to handle large datasets efficiently
                // Use consistent default sorting (dateadded asc) to match user expectations
                const string defaultSortBy = "dateadded";
                const string defaultSortOrder = "asc";
                
                try
                {
                    if (formatLower == "csv")
                    {
                        // Write UTF-8 BOM for Excel compatibility
                        var bom = System.Text.Encoding.UTF8.GetPreamble();
                        await Response.Body.WriteAsync(bom, 0, bom.Length, HttpContext.RequestAborted);

                        // Use StreamWriter for efficient streaming
                        using (var writer = new System.IO.StreamWriter(Response.Body, System.Text.Encoding.UTF8, leaveOpen: true))
                        {
                            // Write CSV header
                            await writer.WriteLineAsync("House ID,Title,Price,Location,Status,Created At");

                            // Stream favorites in batches to avoid loading all into memory at once
                            const int batchSize = 100;
                            int page = 1;
                            bool hasMore = true;

                            while (hasMore)
                            {
                                var batch = await _lotteryService.GetUserFavoriteHousesAsync(
                                    userId.Value, 
                                    page, 
                                    batchSize, 
                                    defaultSortBy, 
                                    defaultSortOrder, 
                                    HttpContext.RequestAborted);

                                if (batch == null || batch.Count == 0)
                                {
                                    hasMore = false;
                                    break;
                                }

                                foreach (var house in batch)
                                {
                                    // Handle null values safely
                                    var houseId = house.Id.ToString();
                                    var title = EscapeCsvField(house.Title ?? string.Empty);
                                    // Use culture-invariant format for price (use . as decimal separator)
                                    var price = house.Price.ToString(System.Globalization.CultureInfo.InvariantCulture);
                                    var location = EscapeCsvField(house.Location ?? string.Empty);
                                    var status = house.Status ?? string.Empty;
                                    var createdAt = house.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                                    
                                    await writer.WriteLineAsync($"{houseId},{title},{price},{location},{status},{createdAt}");
                                }

                                // Check if we've reached the limit or there are no more items
                                if (batch.Count < batchSize || (page * batchSize) >= maxExportSize)
                                {
                                    hasMore = false;
                                }
                                else
                                {
                                    page++;
                                }

                                // Flush periodically to start sending data to client
                                await writer.FlushAsync();
                            }
                        }
                    }
                    else
                    {
                        // Stream JSON array directly to response
                        using (var writer = new System.IO.StreamWriter(Response.Body, System.Text.Encoding.UTF8, leaveOpen: true))
                        {
                            await writer.WriteAsync("[");

                            // Stream favorites in batches
                            const int batchSize = 100;
                            int page = 1;
                            bool hasMore = true;
                            bool isFirstItem = true;

                            while (hasMore)
                            {
                                var batch = await _lotteryService.GetUserFavoriteHousesAsync(
                                    userId.Value, 
                                    page, 
                                    batchSize, 
                                    defaultSortBy, 
                                    defaultSortOrder, 
                                    HttpContext.RequestAborted);

                                if (batch == null || batch.Count == 0)
                                {
                                    hasMore = false;
                                    break;
                                }

                                foreach (var house in batch)
                                {
                                    if (!isFirstItem)
                                    {
                                        await writer.WriteAsync(",");
                                    }
                                    isFirstItem = false;

                                    // Serialize individual house object
                                    var jsonOptions = new System.Text.Json.JsonSerializerOptions 
                                    { 
                                        WriteIndented = false, // Compact format for streaming
                                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                                    };
                                    var houseJson = System.Text.Json.JsonSerializer.Serialize(house, jsonOptions);
                                    await writer.WriteAsync(houseJson);
                                }

                                // Check if we've reached the limit or there are no more items
                                if (batch.Count < batchSize || (page * batchSize) >= maxExportSize)
                                {
                                    hasMore = false;
                                }
                                else
                                {
                                    page++;
                                }

                                // Flush periodically to start sending data to client
                                await writer.FlushAsync();
                            }

                            await writer.WriteAsync("]");
                        }
                    }

                    // Return empty result since we've written directly to Response.Body
                    return new EmptyResult();
                }
                catch (OperationCanceledException)
                {
                    // Client cancelled the request - response is already partially written, can't return error
                    _logger.LogInformation("Export cancelled by client for user {UserId}", userId);
                    throw; // Re-throw to let ASP.NET Core handle it
                }
                catch (Exception streamEx)
                {
                    // If we've already started writing, we can't return a proper error response
                    // Log the error and let the partial response be sent
                    _logger.LogError(streamEx, "Error during export streaming for user {UserId}. Response may be incomplete.", userId);
                    // Re-throw to let ASP.NET Core handle it (may result in 500, but response is already started)
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting favorites");
                return StatusCode(500, new AmesaBackend.Lottery.DTOs.ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An error occurred while processing your request. Please try again later." }
                });
            }
        }

        /// <summary>
        /// Escapes a CSV field according to RFC 4180 standard.
        /// Handles commas, quotes, and newlines by wrapping the field in quotes and escaping internal quotes.
        /// </summary>
        /// <param name="field">The field value to escape. Can be null or empty.</param>
        /// <returns>The escaped CSV field. Returns empty string if input is null or empty.</returns>
        /// <remarks>
        /// This method:
        /// 1. Normalizes line endings (converts \r\n and \r to \n)
        /// 2. Wraps fields containing special characters (comma, quote, newline) in double quotes
        /// 3. Escapes internal double quotes by doubling them (" becomes "")
        /// </remarks>
        private string EscapeCsvField(string? field)
        {
            if (string.IsNullOrEmpty(field))
                return string.Empty;
            
            // Normalize line endings (handle both \r\n and \n)
            var normalized = field.Replace("\r\n", "\n").Replace("\r", "\n");
            
            // Escape quotes and wrap in quotes if contains comma, newline, quote, or carriage return
            if (normalized.Contains(',') || normalized.Contains('"') || normalized.Contains('\n') || normalized.Contains('\r'))
            {
                // Double-quote escape for CSV: replace " with ""
                return $"\"{normalized.Replace("\"", "\"\"")}\"";
            }
            return normalized;
        }
    }
}






