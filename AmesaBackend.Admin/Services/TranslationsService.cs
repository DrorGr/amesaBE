using Microsoft.EntityFrameworkCore;
using AmesaBackend.Content.Data;
using AmesaBackend.Admin.DTOs;

namespace AmesaBackend.Admin.Services
{
    public interface ITranslationsService
    {
        Task<PagedResult<TranslationDto>> GetTranslationsAsync(int page = 1, int pageSize = 50, string? languageCode = null, string? category = null, string? search = null);
        Task<TranslationDto?> GetTranslationAsync(string key, string languageCode);
        Task<TranslationDto> CreateTranslationAsync(CreateTranslationRequest request);
        Task<TranslationDto> UpdateTranslationAsync(string key, string languageCode, UpdateTranslationRequest request);
        Task<bool> DeleteTranslationAsync(string key, string languageCode);
        Task<List<string>> GetLanguagesAsync();
        Task<List<string>> GetCategoriesAsync();
    }

    public class TranslationsService : ITranslationsService
    {
        private readonly ContentDbContext _context;
        private readonly ILogger<TranslationsService> _logger;

        public TranslationsService(
            ContentDbContext context,
            ILogger<TranslationsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PagedResult<TranslationDto>> GetTranslationsAsync(int page = 1, int pageSize = 50, string? languageCode = null, string? category = null, string? search = null)
        {
            var query = _context.Translations.AsQueryable();

            if (!string.IsNullOrWhiteSpace(languageCode))
            {
                query = query.Where(t => t.LanguageCode == languageCode);
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(t => t.Category == category);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(t => 
                    t.Key.Contains(search) || 
                    t.Value.Contains(search));
            }

            var totalCount = await query.CountAsync();
            var translations = await query
                .OrderBy(t => t.Category)
                .ThenBy(t => t.Key)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TranslationDto
                {
                    Key = t.Key,
                    LanguageCode = t.LanguageCode,
                    Category = t.Category,
                    Value = t.Value,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt
                })
                .ToListAsync();

            return new PagedResult<TranslationDto>
            {
                Items = translations,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        public async Task<TranslationDto?> GetTranslationAsync(string key, string languageCode)
        {
            var translation = await _context.Translations
                .FirstOrDefaultAsync(t => t.Key == key && t.LanguageCode == languageCode);

            if (translation == null) return null;

            return new TranslationDto
            {
                Key = translation.Key,
                LanguageCode = translation.LanguageCode,
                Category = translation.Category,
                Value = translation.Value,
                CreatedAt = translation.CreatedAt,
                UpdatedAt = translation.UpdatedAt
            };
        }

        public async Task<TranslationDto> CreateTranslationAsync(CreateTranslationRequest request)
        {
            var translation = new AmesaBackend.Content.Models.Translation
            {
                Id = Guid.NewGuid(),
                Key = request.Key,
                LanguageCode = request.LanguageCode,
                Category = request.Category ?? "common",
                Value = request.Value,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Translations.Add(translation);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Translation created: {Key} - {LanguageCode}", translation.Key, translation.LanguageCode);

            return await GetTranslationAsync(translation.Key, translation.LanguageCode) 
                ?? throw new InvalidOperationException("Failed to retrieve created translation");
        }

        public async Task<TranslationDto> UpdateTranslationAsync(string key, string languageCode, UpdateTranslationRequest request)
        {
            var translation = await _context.Translations
                .FirstOrDefaultAsync(t => t.Key == key && t.LanguageCode == languageCode);

            if (translation == null)
                throw new KeyNotFoundException($"Translation with key {key} and language {languageCode} not found");

            if (!string.IsNullOrWhiteSpace(request.Value))
                translation.Value = request.Value;
            if (!string.IsNullOrWhiteSpace(request.Category))
                translation.Category = request.Category;

            translation.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Translation updated: {Key} - {LanguageCode}", translation.Key, translation.LanguageCode);

            return await GetTranslationAsync(translation.Key, translation.LanguageCode) 
                ?? throw new InvalidOperationException("Failed to retrieve updated translation");
        }

        public async Task<bool> DeleteTranslationAsync(string key, string languageCode)
        {
            var translation = await _context.Translations
                .FirstOrDefaultAsync(t => t.Key == key && t.LanguageCode == languageCode);

            if (translation == null) return false;

            _context.Translations.Remove(translation);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Translation deleted: {Key} - {LanguageCode}", key, languageCode);
            return true;
        }

        public async Task<List<string>> GetLanguagesAsync()
        {
            return await _context.Translations
                .Select(t => t.LanguageCode)
                .Distinct()
                .OrderBy(l => l)
                .ToListAsync();
        }

        public async Task<List<string>> GetCategoriesAsync()
        {
            return await _context.Translations
                .Select(t => t.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }
    }
}

