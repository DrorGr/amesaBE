using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AmesaBackend.Data;
using AmesaBackend.DTOs;
using AmesaBackend.Models;

namespace AmesaBackend.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class TranslationsController : ControllerBase
    {
        private readonly AmesaDbContext _context;
        private readonly ILogger<TranslationsController> _logger;

        public TranslationsController(
            AmesaDbContext context, 
            ILogger<TranslationsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all translations for a specific language
        /// </summary>
        /// <param name="languageCode">Language code (e.g., 'en', 'pl')</param>
        /// <returns>Dictionary of translation keys and values</returns>
        [HttpGet("{languageCode}")]
        public async Task<ActionResult<ApiResponse<TranslationsResponseDto>>> GetTranslations(string languageCode)
        {
            try
            {
                var translations = await _context.Translations
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

                return Ok(new ApiResponse<TranslationsResponseDto>
                {
                    Success = true,
                    Data = response,
                    Message = "Translations retrieved successfully",
                    Timestamp = DateTime.UtcNow
                });
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
                    },
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Get all available languages
        /// </summary>
        /// <returns>List of available languages</returns>
        [HttpGet("languages")]
        public async Task<ActionResult<ApiResponse<List<LanguageDto>>>> GetLanguages()
        {
            try
            {
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

                return Ok(new ApiResponse<List<LanguageDto>>
                {
                    Success = true,
                    Data = languageDtos,
                    Message = "Languages retrieved successfully",
                    Timestamp = DateTime.UtcNow
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
                    },
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Get a specific translation by key and language
        /// </summary>
        /// <param name="languageCode">Language code</param>
        /// <param name="key">Translation key</param>
        /// <returns>Translation value</returns>
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
                        Message = $"Translation not found for key '{key}' in language '{languageCode}'",
                        Timestamp = DateTime.UtcNow
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
                    Message = "Translation retrieved successfully",
                    Timestamp = DateTime.UtcNow
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
                    },
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Create a new translation (Admin only)
        /// </summary>
        /// <param name="request">Translation creation request</param>
        /// <returns>Created translation</returns>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<TranslationDto>>> CreateTranslation([FromBody] CreateTranslationRequest request)
        {
            try
            {
                // Check if language exists
                var language = await _context.Languages
                    .FirstOrDefaultAsync(l => l.Code == request.LanguageCode && l.IsActive);

                if (language == null)
                {
                    return BadRequest(new ApiResponse<TranslationDto>
                    {
                        Success = false,
                        Message = $"Language '{request.LanguageCode}' not found or inactive",
                        Timestamp = DateTime.UtcNow
                    });
                }

                // Check if translation already exists
                var existingTranslation = await _context.Translations
                    .FirstOrDefaultAsync(t => t.LanguageCode == request.LanguageCode && t.Key == request.Key);

                if (existingTranslation != null)
                {
                    return Conflict(new ApiResponse<TranslationDto>
                    {
                        Success = false,
                        Message = $"Translation already exists for key '{request.Key}' in language '{request.LanguageCode}'",
                        Timestamp = DateTime.UtcNow
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
                    CreatedBy = "System" // TODO: Get from authenticated user
                };

                _context.Translations.Add(translation);
                await _context.SaveChangesAsync();

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
                        Message = "Translation created successfully",
                        Timestamp = DateTime.UtcNow
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
                    },
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Update an existing translation (Admin only)
        /// </summary>
        /// <param name="languageCode">Language code</param>
        /// <param name="key">Translation key</param>
        /// <param name="request">Translation update request</param>
        /// <returns>Updated translation</returns>
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
                        Message = $"Translation not found for key '{key}' in language '{languageCode}'",
                        Timestamp = DateTime.UtcNow
                    });
                }

                translation.Value = request.Value;
                translation.Description = request.Description;
                translation.Category = request.Category;
                translation.IsActive = request.IsActive;
                translation.UpdatedAt = DateTime.UtcNow;
                translation.UpdatedBy = "System"; // TODO: Get from authenticated user

                await _context.SaveChangesAsync();

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
                    Message = "Translation updated successfully",
                    Timestamp = DateTime.UtcNow
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
                    },
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Delete a translation (Admin only)
        /// </summary>
        /// <param name="languageCode">Language code</param>
        /// <param name="key">Translation key</param>
        /// <returns>Success response</returns>
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
                        Message = $"Translation not found for key '{key}' in language '{languageCode}'",
                        Timestamp = DateTime.UtcNow
                    });
                }

                _context.Translations.Remove(translation);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Translation deleted successfully",
                    Timestamp = DateTime.UtcNow
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
                    },
                    Timestamp = DateTime.UtcNow
                });
            }
        }
    }
}
