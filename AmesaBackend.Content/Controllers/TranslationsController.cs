using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AmesaBackend.Content.Data;
using AmesaBackend.Content.DTOs;
using AmesaBackend.Content.Models;
using AmesaBackend.Shared.Events;
using AmesaBackend.Shared.Caching;

namespace AmesaBackend.Content.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class TranslationsController : ControllerBase
    {
        private readonly ContentDbContext _context;
        private readonly ILogger<TranslationsController> _logger;
        private readonly IEventPublisher _eventPublisher;
        private readonly ICache _cache;
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan LanguagesCacheExpiration = TimeSpan.FromHours(1);

        public TranslationsController(
            ContentDbContext context, 
            ILogger<TranslationsController> logger,
            IEventPublisher eventPublisher,
            ICache cache)
        {
            _context = context;
            _logger = logger;
            _eventPublisher = eventPublisher;
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        [HttpGet("{languageCode}")]
        [ResponseCache(Duration = 1800)] // 30 minutes
        public async Task<ActionResult<ApiResponse<TranslationsResponseDto>>> GetTranslations(string languageCode)
        {
            try
            {
                var cacheKey = $"translations_{languageCode}";
                
                // Try to get from Redis cache first
                try
                {
                    var cachedResponse = await _cache.GetRecordAsync<TranslationsResponseDto>(cacheKey);
                    if (cachedResponse != null)
                    {
                // #region agent log
                var cachedApiResponse = new ApiResponse<TranslationsResponseDto>
                {
                    Success = true,
                    Data = cachedResponse,
                    Message = "Translations retrieved successfully (cached)"
                };
                var cachedJson = System.Text.Json.JsonSerializer.Serialize(cachedApiResponse, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
                _logger.LogInformation("[DEBUG_TRANSLATIONS] Cached response - HasSuccess: {HasSuccess}, HasData: {HasData}, JsonPreview: {JsonPreview}", 
                    cachedApiResponse.Success, cachedApiResponse.Data != null, cachedJson.Substring(0, Math.Min(200, cachedJson.Length)));
                // #endregion
                return Ok(cachedApiResponse);
                    }
                }
                catch (Exception cacheEx)
                {
                    // Log cache error but continue with database query
                    // Fail-open design (matches Auth service pattern)
                    _logger.LogWarning(cacheEx, "Error retrieving from cache, falling back to database");
                }

                // Query database with AsNoTracking for better performance
                var translations = await _context.Translations
                    .AsNoTracking()
                    .Where(t => t.LanguageCode == languageCode && t.IsActive)
                    .OrderBy(t => t.Key)
                    .ToListAsync();

                var translationDict = translations.ToDictionary(t => t.Key, t => t.Value);
                
                var response = new TranslationsResponseDto
                {
                    LanguageCode = languageCode,
                    Translations = translationDict,
                    LastUpdated = translations.Any() ? translations.Max(t => t.UpdatedAt) : DateTime.UtcNow
                };

                // Cache the response in Redis
                try
                {
                    await _cache.SetRecordAsync(cacheKey, response, CacheExpiration);
                }
                catch (Exception cacheEx)
                {
                    // Log cache error but don't fail the request
                    // Fail-open design (matches Auth service pattern)
                    _logger.LogWarning(cacheEx, "Error caching translations, request still successful");
                }

                // #region agent log
                var apiResponse = new ApiResponse<TranslationsResponseDto>
                {
                    Success = true,
                    Data = response,
                    Message = "Translations retrieved successfully"
                };
                var jsonPreview = System.Text.Json.JsonSerializer.Serialize(apiResponse, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
                _logger.LogInformation("[DEBUG_TRANSLATIONS] Database response - HasSuccess: {HasSuccess}, HasData: {HasData}, JsonPreview: {JsonPreview}, PropertyNames: Success={Success}, Data={Data}", 
                    apiResponse.Success, apiResponse.Data != null, jsonPreview.Substring(0, Math.Min(200, jsonPreview.Length)), 
                    nameof(apiResponse.Success), nameof(apiResponse.Data));
                // #endregion
                return Ok(apiResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving translations for language {LanguageCode}", languageCode);
                return StatusCode(500, new ApiResponse<TranslationsResponseDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving translations",
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = ex.Message
                    }
                });
            }
        }

        [HttpGet("languages")]
        [ResponseCache(Duration = 3600)] // 1 hour
        public async Task<ActionResult<ApiResponse<List<LanguageDto>>>> GetLanguages()
        {
            try
            {
                const string cacheKey = "languages_list";
                
                // Try to get from cache first
                try
                {
                    var cachedResponse = await _cache.GetRecordAsync<List<LanguageDto>>(cacheKey);
                    if (cachedResponse != null)
                    {
                        _logger.LogDebug("Languages list retrieved from cache");
                        return Ok(new ApiResponse<List<LanguageDto>>
                        {
                            Success = true,
                            Data = cachedResponse,
                            Message = "Languages retrieved successfully (cached)"
                        });
                    }
                }
                catch (Exception cacheEx)
                {
                    // Log cache error but continue with database query
                    // Fail-open design (matches Auth service pattern)
                    _logger.LogWarning(cacheEx, "Error retrieving from cache, falling back to database");
                }
                
                var languages = await _context.Languages
                    .Where(l => l.IsActive)
                    .OrderBy(l => l.DisplayOrder)
                    .ThenBy(l => l.Name)
                    .ToListAsync();

                var languageDtos = languages.Select(l => new LanguageDto
                {
                    Code = l.Code,
                    Name = l.Name,
                    NativeName = l.NativeName,
                    FlagUrl = l.FlagUrl,
                    IsActive = l.IsActive,
                    IsDefault = l.IsDefault,
                    DisplayOrder = l.DisplayOrder
                }).ToList();

                // Cache the response
                try
                {
                    await _cache.SetRecordAsync(cacheKey, languageDtos, LanguagesCacheExpiration);
                    _logger.LogDebug("Languages list cached");
                }
                catch (Exception cacheEx)
                {
                    // Log cache error but don't fail the request
                    // Fail-open design (matches Auth service pattern)
                    _logger.LogWarning(cacheEx, "Error caching languages list, request still successful");
                }

                return Ok(new ApiResponse<List<LanguageDto>>
                {
                    Success = true,
                    Data = languageDtos,
                    Message = "Languages retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving languages");
                return StatusCode(500, new ApiResponse<List<LanguageDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving languages",
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = ex.Message
                    }
                });
            }
        }

        [HttpGet("{languageCode}/{key}")]
        public async Task<ActionResult<ApiResponse<TranslationDto>>> GetTranslation(string languageCode, string key)
        {
            try
            {
                var translation = await _context.Translations
                    .FirstOrDefaultAsync(t => t.LanguageCode == languageCode && t.Key == key && t.IsActive);

                if (translation == null)
                {
                    return NotFound(new ApiResponse<TranslationDto>
                    {
                        Success = false,
                        Message = $"Translation not found for key '{key}' in language '{languageCode}'"
                    });
                }

                var translationDto = new TranslationDto
                {
                    Key = translation.Key,
                    Value = translation.Value,
                    Description = translation.Description,
                    Category = translation.Category,
                    IsActive = translation.IsActive
                };

                return Ok(new ApiResponse<TranslationDto>
                {
                    Success = true,
                    Data = translationDto,
                    Message = "Translation retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving translation {Key} for language {LanguageCode}", key, languageCode);
                return StatusCode(500, new ApiResponse<TranslationDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving translation",
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = ex.Message
                    }
                });
            }
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<TranslationDto>>> CreateTranslation([FromBody] CreateTranslationRequest request)
        {
            try
            {
                var language = await _context.Languages
                    .FirstOrDefaultAsync(l => l.Code == request.LanguageCode && l.IsActive);

                if (language == null)
                {
                    return BadRequest(new ApiResponse<TranslationDto>
                    {
                        Success = false,
                        Message = $"Language '{request.LanguageCode}' not found or inactive"
                    });
                }

                var existingTranslation = await _context.Translations
                    .FirstOrDefaultAsync(t => t.LanguageCode == request.LanguageCode && t.Key == request.Key);

                if (existingTranslation != null)
                {
                    return Conflict(new ApiResponse<TranslationDto>
                    {
                        Success = false,
                        Message = $"Translation already exists for key '{request.Key}' in language '{request.LanguageCode}'"
                    });
                }

                var translation = new Translation
                {
                    Id = Guid.NewGuid(),
                    LanguageCode = request.LanguageCode,
                    Key = request.Key,
                    Value = request.Value,
                    Description = request.Description,
                    Category = request.Category,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                };

                _context.Translations.Add(translation);
                await _context.SaveChangesAsync();

                await _eventPublisher.PublishAsync(new TranslationUpdatedEvent
                {
                    Key = translation.Key,
                    Language = translation.LanguageCode,
                    Value = translation.Value
                });

                // Publish event
                await _eventPublisher.PublishAsync(new TranslationUpdatedEvent
                {
                    Key = translation.Key,
                    Language = translation.LanguageCode,
                    Value = translation.Value
                });

                var translationDto = new TranslationDto
                {
                    Key = translation.Key,
                    Value = translation.Value,
                    Description = translation.Description,
                    Category = translation.Category,
                    IsActive = translation.IsActive
                };

                return CreatedAtAction(nameof(GetTranslation), 
                    new { languageCode = request.LanguageCode, key = request.Key }, 
                    new ApiResponse<TranslationDto>
                    {
                        Success = true,
                        Data = translationDto,
                        Message = "Translation created successfully"
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating translation");
                return StatusCode(500, new ApiResponse<TranslationDto>
                {
                    Success = false,
                    Message = "An error occurred while creating translation",
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = ex.Message
                    }
                });
            }
        }

        [HttpPut("{languageCode}/{key}")]
        public async Task<ActionResult<ApiResponse<TranslationDto>>> UpdateTranslation(
            string languageCode, 
            string key, 
            [FromBody] UpdateTranslationRequest request)
        {
            try
            {
                var translation = await _context.Translations
                    .FirstOrDefaultAsync(t => t.LanguageCode == languageCode && t.Key == key);

                if (translation == null)
                {
                    return NotFound(new ApiResponse<TranslationDto>
                    {
                        Success = false,
                        Message = $"Translation not found for key '{key}' in language '{languageCode}'"
                    });
                }

                translation.Value = request.Value;
                translation.Description = request.Description;
                translation.Category = request.Category;
                translation.IsActive = request.IsActive;
                translation.UpdatedAt = DateTime.UtcNow;
                translation.UpdatedBy = "System";

                await _context.SaveChangesAsync();

                await _eventPublisher.PublishAsync(new TranslationUpdatedEvent
                {
                    Key = translation.Key,
                    Language = translation.LanguageCode,
                    Value = translation.Value
                });

                var translationDto = new TranslationDto
                {
                    Key = translation.Key,
                    Value = translation.Value,
                    Description = translation.Description,
                    Category = translation.Category,
                    IsActive = translation.IsActive
                };

                return Ok(new ApiResponse<TranslationDto>
                {
                    Success = true,
                    Data = translationDto,
                    Message = "Translation updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating translation {Key} for language {LanguageCode}", key, languageCode);
                return StatusCode(500, new ApiResponse<TranslationDto>
                {
                    Success = false,
                    Message = "An error occurred while updating translation",
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = ex.Message
                    }
                });
            }
        }

        [HttpDelete("{languageCode}/{key}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteTranslation(string languageCode, string key)
        {
            try
            {
                var translation = await _context.Translations
                    .FirstOrDefaultAsync(t => t.LanguageCode == languageCode && t.Key == key);

                if (translation == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Translation not found for key '{key}' in language '{languageCode}'"
                    });
                }

                _context.Translations.Remove(translation);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Translation deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting translation {Key} for language {LanguageCode}", key, languageCode);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while deleting translation",
                    Error = new ErrorResponse
                    {
                        Code = "INTERNAL_ERROR",
                        Message = ex.Message
                    }
                });
            }
        }
    }
}

