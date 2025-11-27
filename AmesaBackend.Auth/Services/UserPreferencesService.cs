using Microsoft.EntityFrameworkCore;
using AmesaBackend.Auth.Data;
using AmesaBackend.Auth.DTOs;
using AmesaBackend.Auth.Models;
using System.Text.Json;

namespace AmesaBackend.Auth.Services
{
    /// <summary>
    /// Service for managing user preferences including lottery-specific preferences
    /// </summary>
    public class UserPreferencesService : IUserPreferencesService
    {
        private readonly AuthDbContext _context;
        private readonly ILogger<UserPreferencesService> _logger;

        public UserPreferencesService(
            AuthDbContext context,
            ILogger<UserPreferencesService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<UserPreferencesDto?> GetUserPreferencesAsync(Guid userId)
        {
            var preferences = await _context.UserPreferences
                .FirstOrDefaultAsync(up => up.UserId == userId);

            if (preferences == null)
            {
                return null;
            }

            return new UserPreferencesDto
            {
                Id = preferences.Id,
                UserId = preferences.UserId,
                PreferencesJson = preferences.PreferencesJson,
                Version = preferences.Version,
                LastUpdated = preferences.UpdatedAt
            };
        }

        public async Task<UserPreferencesDto> UpdateUserPreferencesAsync(Guid userId, JsonElement preferences, string? version = null)
        {
            var existingPreferences = await _context.UserPreferences
                .FirstOrDefaultAsync(up => up.UserId == userId);

            if (existingPreferences == null)
            {
                // Ensure lotteryPreferences structure exists in new preferences
                Dictionary<string, object> prefsDict;
                if (preferences.ValueKind == JsonValueKind.Object)
                {
                    prefsDict = JsonSerializer.Deserialize<Dictionary<string, object>>(preferences.GetRawText()) 
                        ?? new Dictionary<string, object>();
                }
                else
                {
                    prefsDict = new Dictionary<string, object>();
                }

                // Add lotteryPreferences if missing (systemic fix for all code paths)
                if (!prefsDict.ContainsKey("lotteryPreferences"))
                {
                    prefsDict["lotteryPreferences"] = new Dictionary<string, object>
                    {
                        ["favoriteHouseIds"] = new List<string>()
                    };
                }

                var newPreferences = new UserPreferences
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    PreferencesJson = JsonSerializer.Serialize(prefsDict),
                    Version = version ?? "1.0.0",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = userId.ToString(),
                    UpdatedBy = userId.ToString()
                };

                _context.UserPreferences.Add(newPreferences);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created new user preferences for user {UserId}", userId);

                return new UserPreferencesDto
                {
                    Id = newPreferences.Id,
                    UserId = newPreferences.UserId,
                    PreferencesJson = newPreferences.PreferencesJson,
                    Version = newPreferences.Version,
                    LastUpdated = newPreferences.UpdatedAt
                };
            }
            else
            {
                existingPreferences.PreferencesJson = JsonSerializer.Serialize(preferences);
                if (!string.IsNullOrEmpty(version))
                {
                    existingPreferences.Version = version;
                }
                existingPreferences.UpdatedAt = DateTime.UtcNow;
                existingPreferences.UpdatedBy = userId.ToString();

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated user preferences for user {UserId}", userId);

                return new UserPreferencesDto
                {
                    Id = existingPreferences.Id,
                    UserId = existingPreferences.UserId,
                    PreferencesJson = existingPreferences.PreferencesJson,
                    Version = existingPreferences.Version,
                    LastUpdated = existingPreferences.UpdatedAt
                };
            }
        }

        public async Task<LotteryPreferencesDto?> GetLotteryPreferencesAsync(Guid userId)
        {
            var preferences = await GetUserPreferencesAsync(userId);
            if (preferences == null)
            {
                return null;
            }

            try
            {
                var jsonDoc = JsonDocument.Parse(preferences.PreferencesJson);
                if (jsonDoc.RootElement.TryGetProperty("lotteryPreferences", out var lotteryPrefs))
                {
                    var dto = JsonSerializer.Deserialize<LotteryPreferencesDto>(lotteryPrefs.GetRawText()) 
                        ?? new LotteryPreferencesDto();
                    
                    // Parse favoriteHouseIds manually since they might be stored as strings
                    if (lotteryPrefs.TryGetProperty("favoriteHouseIds", out var favoriteIds))
                    {
                        if (favoriteIds.ValueKind == JsonValueKind.Array)
                        {
                            dto.FavoriteHouseIds = new List<Guid>();
                            foreach (var idElement in favoriteIds.EnumerateArray())
                            {
                                if (idElement.ValueKind == JsonValueKind.String)
                                {
                                    if (Guid.TryParse(idElement.GetString(), out var guid))
                                    {
                                        dto.FavoriteHouseIds.Add(guid);
                                    }
                                }
                            }
                        }
                    }
                    
                    return dto;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing lottery preferences for user {UserId}", userId);
            }

            return new LotteryPreferencesDto(); // Return defaults if not found
        }

        public async Task<LotteryPreferencesDto> UpdateLotteryPreferencesAsync(Guid userId, UpdateLotteryPreferencesRequest request)
        {
            var preferences = await GetUserPreferencesAsync(userId);
            JsonDocument? jsonDoc = null;
            JsonElement rootElement;

            if (preferences != null)
            {
                try
                {
                    jsonDoc = JsonDocument.Parse(preferences.PreferencesJson);
                    rootElement = jsonDoc.RootElement.Clone();
                }
                catch
                {
                    rootElement = new JsonElement();
                }
            }
            else
            {
                rootElement = new JsonElement();
            }

            // Create or update lottery preferences
            var lotteryPrefs = new Dictionary<string, object?>
            {
                ["favoriteCategories"] = request.FavoriteCategories ?? new List<string>(),
                ["priceRangeMin"] = request.PriceRangeMin,
                ["priceRangeMax"] = request.PriceRangeMax,
                ["preferredLocations"] = request.PreferredLocations ?? new List<string>(),
                ["houseTypes"] = request.HouseTypes ?? new List<string>(),
                ["defaultView"] = request.DefaultView ?? "grid",
                ["itemsPerPage"] = request.ItemsPerPage ?? 25,
                ["sortBy"] = request.SortBy ?? "price",
                ["sortOrder"] = request.SortOrder ?? "asc",
                ["priceDropAlerts"] = request.PriceDropAlerts ?? false,
                ["newMatchingLotteries"] = request.NewMatchingLotteries ?? true,
                ["endingSoonAlerts"] = request.EndingSoonAlerts ?? false,
                ["winnerAnnouncements"] = request.WinnerAnnouncements ?? true
            };

            // Merge with existing preferences
            var existingPrefs = new Dictionary<string, JsonElement>();
            if (rootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in rootElement.EnumerateObject())
                {
                    existingPrefs[prop.Name] = prop.Value;
                }
            }

            // Update lottery preferences
            existingPrefs["lotteryPreferences"] = JsonSerializer.SerializeToElement(lotteryPrefs);

            // Update the full preferences JSON
            await UpdateUserPreferencesAsync(userId, JsonSerializer.SerializeToElement(existingPrefs));

            return await GetLotteryPreferencesAsync(userId) ?? new LotteryPreferencesDto();
        }

        public async Task<List<Guid>> GetFavoriteHouseIdsAsync(Guid userId)
        {
            var preferences = await GetUserPreferencesAsync(userId);
            if (preferences == null)
            {
                return new List<Guid>();
            }

            try
            {
                var jsonDoc = JsonDocument.Parse(preferences.PreferencesJson);
                if (jsonDoc.RootElement.TryGetProperty("lotteryPreferences", out var lotteryPrefs))
                {
                    if (lotteryPrefs.TryGetProperty("favoriteHouseIds", out var favoriteIds))
                    {
                        if (favoriteIds.ValueKind == JsonValueKind.Array)
                        {
                            var ids = new List<Guid>();
                            foreach (var idElement in favoriteIds.EnumerateArray())
                            {
                                if (idElement.ValueKind == JsonValueKind.String)
                                {
                                    if (Guid.TryParse(idElement.GetString(), out var guid))
                                    {
                                        ids.Add(guid);
                                    }
                                }
                            }
                            return ids;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing favorite house IDs for user {UserId}", userId);
            }

            return new List<Guid>();
        }

        public async Task<bool> AddHouseToFavoritesAsync(Guid userId, Guid houseId)
        {
            try
            {
                var favoriteIds = await GetFavoriteHouseIdsAsync(userId);
                
                if (favoriteIds.Contains(houseId))
                {
                    return false; // Already in favorites
                }

                favoriteIds.Add(houseId);

                // Update favorites in preferences
                var preferences = await GetUserPreferencesAsync(userId);
                JsonDocument? jsonDoc = null;
                JsonElement rootElement;

                if (preferences != null)
                {
                    try
                    {
                        jsonDoc = JsonDocument.Parse(preferences.PreferencesJson);
                        rootElement = jsonDoc.RootElement.Clone();
                    }
                    catch
                    {
                        rootElement = new JsonElement();
                    }
                }
                else
                {
                    rootElement = new JsonElement();
                }

                // Get or create lottery preferences
                // Parse the entire preferences JSON into a dictionary we can modify
                Dictionary<string, object> existingPrefs;
                if (preferences != null && rootElement.ValueKind == JsonValueKind.Object)
                {
                    // Deserialize the entire preferences JSON to a dictionary
                    existingPrefs = JsonSerializer.Deserialize<Dictionary<string, object>>(preferences.PreferencesJson) 
                        ?? new Dictionary<string, object>();
                }
                else
                {
                    existingPrefs = new Dictionary<string, object>();
                }

                // Update or create lotteryPreferences
                Dictionary<string, object> lotteryPrefsDict;
                if (existingPrefs.TryGetValue("lotteryPreferences", out var existingLotteryPrefsObj))
                {
                    // Parse existing lotteryPreferences
                    if (existingLotteryPrefsObj is JsonElement existingLotteryPrefs)
                    {
                        lotteryPrefsDict = JsonSerializer.Deserialize<Dictionary<string, object>>(existingLotteryPrefs.GetRawText()) 
                            ?? new Dictionary<string, object>();
                    }
                    else if (existingLotteryPrefsObj is Dictionary<string, object> existingDict)
                    {
                        lotteryPrefsDict = existingDict;
                    }
                    else
                    {
                        // Try to deserialize from string representation
                        lotteryPrefsDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                            JsonSerializer.Serialize(existingLotteryPrefsObj)) 
                            ?? new Dictionary<string, object>();
                    }
                }
                else
                {
                    lotteryPrefsDict = new Dictionary<string, object>();
                }

                // Update favoriteHouseIds
                lotteryPrefsDict["favoriteHouseIds"] = favoriteIds.Select(id => id.ToString()).ToList();
                
                // Update the main preferences dictionary
                existingPrefs["lotteryPreferences"] = lotteryPrefsDict;

                // Serialize the entire preferences dictionary back to JSON
                await UpdateUserPreferencesAsync(userId, JsonSerializer.SerializeToElement(existingPrefs));

                _logger.LogInformation("Added house {HouseId} to favorites for user {UserId}", houseId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding house {HouseId} to favorites for user {UserId}", houseId, userId);
                return false;
            }
        }

        public async Task<bool> RemoveHouseFromFavoritesAsync(Guid userId, Guid houseId)
        {
            try
            {
                var favoriteIds = await GetFavoriteHouseIdsAsync(userId);
                
                if (!favoriteIds.Contains(houseId))
                {
                    return false; // Not in favorites
                }

                favoriteIds.Remove(houseId);

                // Update favorites in preferences
                var preferences = await GetUserPreferencesAsync(userId);
                if (preferences == null)
                {
                    return false;
                }

                JsonDocument? jsonDoc = null;
                JsonElement rootElement;

                try
                {
                    jsonDoc = JsonDocument.Parse(preferences.PreferencesJson);
                    rootElement = jsonDoc.RootElement.Clone();
                }
                catch
                {
                    return false;
                }

                // Get existing preferences - parse entire JSON to dictionary
                Dictionary<string, object> existingPrefs;
                if (rootElement.ValueKind == JsonValueKind.Object)
                {
                    // Deserialize the entire preferences JSON to a dictionary
                    existingPrefs = JsonSerializer.Deserialize<Dictionary<string, object>>(preferences.PreferencesJson) 
                        ?? new Dictionary<string, object>();
                }
                else
                {
                    existingPrefs = new Dictionary<string, object>();
                }

                // Update or create lotteryPreferences
                Dictionary<string, object> lotteryPrefsDict;
                if (existingPrefs.TryGetValue("lotteryPreferences", out var existingLotteryPrefsObj))
                {
                    // Parse existing lotteryPreferences
                    if (existingLotteryPrefsObj is JsonElement existingLotteryPrefs)
                    {
                        lotteryPrefsDict = JsonSerializer.Deserialize<Dictionary<string, object>>(existingLotteryPrefs.GetRawText()) 
                            ?? new Dictionary<string, object>();
                    }
                    else if (existingLotteryPrefsObj is Dictionary<string, object> existingDict)
                    {
                        lotteryPrefsDict = existingDict;
                    }
                    else
                    {
                        // Try to deserialize from string representation
                        lotteryPrefsDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                            JsonSerializer.Serialize(existingLotteryPrefsObj)) 
                            ?? new Dictionary<string, object>();
                    }
                }
                else
                {
                    lotteryPrefsDict = new Dictionary<string, object>();
                }

                // Update favoriteHouseIds
                lotteryPrefsDict["favoriteHouseIds"] = favoriteIds.Select(id => id.ToString()).ToList();
                
                // Update the main preferences dictionary
                existingPrefs["lotteryPreferences"] = lotteryPrefsDict;

                // Serialize the entire preferences dictionary back to JSON
                await UpdateUserPreferencesAsync(userId, JsonSerializer.SerializeToElement(existingPrefs));

                _logger.LogInformation("Removed house {HouseId} from favorites for user {UserId}", houseId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing house {HouseId} from favorites for user {UserId}", houseId, userId);
                return false;
            }
        }

        public async Task<bool> IsHouseFavoriteAsync(Guid userId, Guid houseId)
        {
            var favoriteIds = await GetFavoriteHouseIdsAsync(userId);
            return favoriteIds.Contains(houseId);
        }

        public PreferenceValidationDto ValidateLotteryPreferences(JsonElement preferences)
        {
            var validation = new PreferenceValidationDto { IsValid = true };

            try
            {
                if (preferences.ValueKind != JsonValueKind.Object)
                {
                    validation.IsValid = false;
                    validation.Errors.Add("Preferences must be a JSON object");
                    return validation;
                }

                if (preferences.TryGetProperty("lotteryPreferences", out var lotteryPrefs))
                {
                    // Validate favoriteHouseIds is an array
                    if (lotteryPrefs.TryGetProperty("favoriteHouseIds", out var favoriteIds))
                    {
                        if (favoriteIds.ValueKind != JsonValueKind.Array)
                        {
                            validation.IsValid = false;
                            validation.Errors.Add("favoriteHouseIds must be an array");
                        }
                    }

                    // Validate price range
                    if (lotteryPrefs.TryGetProperty("priceRangeMin", out var minPrice) && 
                        lotteryPrefs.TryGetProperty("priceRangeMax", out var maxPrice))
                    {
                        if (minPrice.ValueKind == JsonValueKind.Number && 
                            maxPrice.ValueKind == JsonValueKind.Number)
                        {
                            var min = minPrice.GetDecimal();
                            var max = maxPrice.GetDecimal();
                            if (min > max)
                            {
                                validation.IsValid = false;
                                validation.Errors.Add("priceRangeMin cannot be greater than priceRangeMax");
                            }
                        }
                    }

                    // Validate itemsPerPage
                    if (lotteryPrefs.TryGetProperty("itemsPerPage", out var itemsPerPage))
                    {
                        if (itemsPerPage.ValueKind == JsonValueKind.Number)
                        {
                            var count = itemsPerPage.GetInt32();
                            if (count < 1 || count > 100)
                            {
                                validation.Warnings.Add("itemsPerPage should be between 1 and 100");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                validation.IsValid = false;
                validation.Errors.Add($"Validation error: {ex.Message}");
            }

            return validation;
        }
    }
}
