using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AmesaBackend.Content.Data;
using AmesaBackend.Content.DTOs;
using AmesaBackend.Content.Models;
using AmesaBackend.Shared.Events;

namespace AmesaBackend.Content.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class TranslationsController : ControllerBase
    {
        private readonly ContentDbContext _context;
        private readonly ILogger<TranslationsController> _logger;
        private readonly IEventPublisher _eventPublisher;

        public TranslationsController(
            ContentDbContext context, 
            ILogger<TranslationsController> logger,
            IEventPublisher eventPublisher)
        {
            _context = context;
            _logger = logger;
            _eventPublisher = eventPublisher;
        }

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
                    Message = "Translations retrieved successfully"
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
                    }
                });
            }
        }

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

