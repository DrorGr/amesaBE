using Microsoft.EntityFrameworkCore;
using AmesaBackend.Auth.Data;
using AmesaBackend.Auth.DTOs;
using AmesaBackend.Auth.Models;
using AmesaBackend.Shared.Rest;
using AmesaBackend.Shared.Contracts;
using System.Text.Json;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace AmesaBackend.Auth.Services
{
    /// <summary>
    /// Service for managing user preferences including lottery-specific preferences
    /// </summary>
    public class UserPreferencesService : IUserPreferencesService
    {
        private readonly AuthDbContext _context;
        private readonly ILogger<UserPreferencesService> _logger;
        private readonly IHttpRequest? _httpRequest;
        private readonly IConfiguration? _configuration;

        public UserPreferencesService(
            AuthDbContext context,
            ILogger<UserPreferencesService> logger,
            IHttpRequest? httpRequest = null,
            IConfiguration? configuration = null)
        {
            _context = context;
            _logger = logger;
            _httpRequest = httpRequest;
            _configuration = configuration;
        }

        public async Task<UserPreferencesDto?> GetUserPreferencesAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_context == null)
                {
                    throw new InvalidOperationException("Database context is not initialized");
                }
                var preferences = await _context.UserPreferences
                    .FirstOrDefaultAsync(up => up.UserId == userId, cancellationToken);

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user preferences for user {UserId}", userId);
                throw;
            }
        }

        public async Task<UserPreferencesDto> UpdateUserPreferencesAsync(Guid userId, JsonElement preferences, string? version = null, CancellationToken cancellationToken = default)
        {
            var existingPreferences = await _context.UserPreferences
                .FirstOrDefaultAsync(up => up.UserId == userId, cancellationToken);

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
                await _context.SaveChangesAsync(cancellationToken);

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
                // CRITICAL FIX: Merge incoming preferences with existing preferences to preserve lotteryPreferences
                // Deserialize existing preferences
                Dictionary<string, object> existingPrefsDict;
                try
                {
                    existingPrefsDict = JsonSerializer.Deserialize<Dictionary<string, object>>(existingPreferences.PreferencesJson ?? "{}") 
                        ?? new Dictionary<string, object>();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize existing preferences for user {UserId}, creating empty dictionary", userId);
                    existingPrefsDict = new Dictionary<string, object>();
                }

                // Preserve existing lotteryPreferences before merging
                object? existingLotteryPrefs = null;
                if (existingPrefsDict.TryGetValue("lotteryPreferences", out var existingLotteryPrefsObj))
                {
                    existingLotteryPrefs = existingLotteryPrefsObj;
                }

                // Deserialize incoming preferences
                Dictionary<string, object> incomingPrefsDict;
                if (preferences.ValueKind == JsonValueKind.Object)
                {
                    try
                    {
                        incomingPrefsDict = JsonSerializer.Deserialize<Dictionary<string, object>>(preferences.GetRawText()) 
                            ?? new Dictionary<string, object>();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize incoming preferences for user {UserId}, using existing", userId);
                        incomingPrefsDict = existingPrefsDict;
                    }
                }
                else
                {
                    _logger.LogWarning("Incoming preferences for user {UserId} is not a JSON object (ValueKind: {ValueKind}), using existing", userId, preferences.ValueKind);
                    incomingPrefsDict = existingPrefsDict;
                }

                // Merge: incoming preferences override existing, but preserve lotteryPreferences if not in incoming
                foreach (var kvp in incomingPrefsDict)
                {
                    existingPrefsDict[kvp.Key] = kvp.Value;
                }

                // CRITICAL: Preserve or restore lotteryPreferences
                if (!incomingPrefsDict.ContainsKey("lotteryPreferences"))
                {
                    if (existingLotteryPrefs != null)
                    {
                        // Restore preserved lotteryPreferences
                        existingPrefsDict["lotteryPreferences"] = existingLotteryPrefs;
                    }
                    else
                    {
                        // Create new lotteryPreferences structure if it doesn't exist
                        existingPrefsDict["lotteryPreferences"] = new Dictionary<string, object>
                        {
                            ["favoriteHouseIds"] = new List<string>()
                        };
                    }
                }

                // Serialize merged preferences
                // CRITICAL FIX: Convert all JsonElement values to proper objects before serialization
                var cleanedPrefs = ConvertJsonElementsToObjects(existingPrefsDict);
                var mergedJson = JsonSerializer.Serialize(cleanedPrefs);

                existingPreferences.PreferencesJson = mergedJson;
                if (!string.IsNullOrEmpty(version))
                {
                    existingPreferences.Version = version;
                }
                existingPreferences.UpdatedAt = DateTime.UtcNow;
                existingPreferences.UpdatedBy = userId.ToString();

                await _context.SaveChangesAsync(cancellationToken);

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

        public async Task<LotteryPreferencesDto?> GetLotteryPreferencesAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var preferences = await GetUserPreferencesAsync(userId, cancellationToken);
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

        public async Task<LotteryPreferencesDto> UpdateLotteryPreferencesAsync(Guid userId, UpdateLotteryPreferencesRequest request, CancellationToken cancellationToken = default)
        {
            var preferences = await GetUserPreferencesAsync(userId, cancellationToken);
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
            // CRITICAL FIX: Use Dictionary<string, object> instead of Dictionary<string, JsonElement>
            // and convert JsonElement values to proper objects before serialization
            Dictionary<string, object> existingPrefs;
            if (preferences != null && rootElement.ValueKind == JsonValueKind.Object)
            {
                try
                {
                    // Deserialize the entire preferences JSON to a dictionary
                    existingPrefs = JsonSerializer.Deserialize<Dictionary<string, object>>(preferences.PreferencesJson) 
                        ?? new Dictionary<string, object>();
                }
                catch
                {
                    existingPrefs = new Dictionary<string, object>();
                }
            }
            else
            {
                existingPrefs = new Dictionary<string, object>();
            }

            // Update lottery preferences
            existingPrefs["lotteryPreferences"] = lotteryPrefs;

            // CRITICAL FIX: Convert all JsonElement values to proper objects before serialization
            var cleanedPrefs = ConvertJsonElementsToObjects(existingPrefs);
            
            // Update the full preferences JSON
            await UpdateUserPreferencesAsync(userId, JsonSerializer.SerializeToElement(cleanedPrefs), cancellationToken: cancellationToken);

            return await GetLotteryPreferencesAsync(userId) ?? new LotteryPreferencesDto();
        }

        public async Task<List<Guid>> GetFavoriteHouseIdsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var preferences = await GetUserPreferencesAsync(userId, cancellationToken);
                if (preferences == null)
                {
                    return new List<Guid>();
                }

                if (string.IsNullOrEmpty(preferences.PreferencesJson))
                {
                    return new List<Guid>();
                }
                using var jsonDoc = JsonDocument.Parse(preferences.PreferencesJson);
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
                                    var idString = idElement.GetString();
                                    if (Guid.TryParse(idString, out var guid))
                                    {
                                        ids.Add(guid);
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Invalid GUID format in favorite house IDs for user {UserId}: {IdString}", userId, idString);
                                    }
                                }
                            }
                            // Remove duplicates (defensive - in case any exist from before race condition fix)
                            var uniqueIds = ids.Distinct().ToList();
                            if (uniqueIds.Count != ids.Count)
                            {
                                _logger.LogWarning("Removed {DuplicateCount} duplicate favorite house IDs for user {UserId}", ids.Count - uniqueIds.Count, userId);
                            }
                            return uniqueIds;
                        }
                        else
                        {
                            _logger.LogWarning("favoriteHouseIds is not an array for user {UserId} (ValueKind: {ValueKind})", userId, favoriteIds.ValueKind);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving favorite house IDs for user {UserId}", userId);
                return new List<Guid>();
            }

            return new List<Guid>();
        }

        public async Task<bool> AddHouseToFavoritesAsync(Guid userId, Guid houseId, CancellationToken cancellationToken = default)
        {
            // Optimistic concurrency retry logic to prevent race conditions
            const int maxRetries = 3;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var favoriteIds = await GetFavoriteHouseIdsAsync(userId, cancellationToken);
                    
                    // Check if already favorite (idempotent operation)
                    if (favoriteIds.Contains(houseId))
                    {
                        return true;
                    }

                    // Add to list
                    favoriteIds.Add(houseId);

                    // Update favorites in preferences
                    var preferences = await GetUserPreferencesAsync(userId);
                    JsonDocument? jsonDoc = null;
                    JsonElement rootElement;

                    if (preferences != null)
                    {
                        try
                        {
                            if (string.IsNullOrEmpty(preferences.PreferencesJson))
                            {
                                throw new InvalidOperationException("PreferencesJson is null or empty");
                            }
                            jsonDoc = JsonDocument.Parse(preferences.PreferencesJson);
                            rootElement = jsonDoc.RootElement.Clone();
                        }
                        catch (Exception parseEx)
                        {
                            _logger.LogWarning(parseEx, "Failed to parse preferences JSON for user {UserId}", userId);
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
                        try
                        {
                            // Deserialize the entire preferences JSON to a dictionary
                            existingPrefs = JsonSerializer.Deserialize<Dictionary<string, object>>(preferences.PreferencesJson) 
                                ?? new Dictionary<string, object>();
                        }
                        catch (Exception deserEx)
                        {
                            _logger.LogWarning(deserEx, "Failed to deserialize preferences for user {UserId}, creating empty dictionary", userId);
                            existingPrefs = new Dictionary<string, object>();
                        }
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

                    // Update favoriteHouseIds - ensure no duplicates
                    var favoriteIdsStringList = favoriteIds.Distinct().Select(id => id.ToString()).ToList();
                    lotteryPrefsDict["favoriteHouseIds"] = favoriteIdsStringList;
                    
                    // Update the main preferences dictionary
                    existingPrefs["lotteryPreferences"] = lotteryPrefsDict;

                    // Serialize the entire preferences dictionary back to JSON
                    // CRITICAL FIX: Convert all JsonElement values to proper objects before serialization
                    // When deserializing JSONB to Dictionary<string, object>, nested objects become JsonElement
                    // JsonElement values can't be serialized properly, so we need to convert them first
                    var cleanedPrefs = ConvertJsonElementsToObjects(existingPrefs);
                    var serializedJson = JsonSerializer.SerializeToElement(cleanedPrefs);
                    await UpdateUserPreferencesAsync(userId, serializedJson, cancellationToken: cancellationToken);

                    // Gamification integration (award +5 points on favorite add)
                    if (_httpRequest != null && _configuration != null)
                    {
                        try
                        {
                            var lotteryServiceUrl = _configuration["LotteryService:BaseUrl"]
                                ?? Environment.GetEnvironmentVariable("LOTTERY_SERVICE_URL")
                                ?? "http://amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com";

                            var awardPointsRequest = new
                            {
                                UserId = userId,
                                Points = 5, // +5 points for favorite
                                Reason = "Favorite Added",
                                ReferenceId = houseId
                            };

                            var token = string.Empty; // Service-to-service auth will be handled by middleware
                            await _httpRequest.PostRequest<object>(
                                $"{lotteryServiceUrl}/api/v1/gamification/award-points",
                                awardPointsRequest,
                                token);

                            _logger.LogInformation("Awarded 5 points to user {UserId} for adding favorite {HouseId}", userId, houseId);
                        }
                        catch (Exception ex)
                        {
                            // Log but don't fail favorite operation if gamification fails
                            _logger.LogWarning(ex, "Gamification integration failed for user {UserId} after adding favorite {HouseId}", userId, houseId);
                        }
                    }

                    _logger.LogInformation("Added house {HouseId} to favorites for user {UserId} (attempt {Attempt})", houseId, userId, attempt);
                    return true;
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    // Concurrency conflict - another request modified preferences
                    // Retry by re-reading and checking again
                    if (attempt < maxRetries)
                    {
                        _logger.LogInformation("Concurrency conflict adding house {HouseId} to favorites for user {UserId}, retrying (attempt {Attempt}/{MaxRetries})", houseId, userId, attempt + 1, maxRetries);
                        // Small delay before retry to allow other request to complete
                        await Task.Delay(50 * attempt, cancellationToken); // Exponential backoff: 50ms, 100ms, 150ms
                        continue; // Retry
                    }
                    else
                    {
                        _logger.LogError(ex, "Failed to add house {HouseId} to favorites for user {UserId} after {Attempts} attempts due to concurrency conflicts", houseId, userId, maxRetries);
                        return false;
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "JSON serialization error adding house {HouseId} to favorites for user {UserId}", houseId, userId);
                    return false; // Don't retry on JSON errors
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Database error adding house {HouseId} to favorites for user {UserId}", houseId, userId);
                    return false; // Don't retry on general DB errors (only concurrency)
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adding house {HouseId} to favorites for user {UserId}", houseId, userId);
                    return false; // Don't retry on unexpected errors
                }
            }
            
            // Should never reach here, but just in case
            _logger.LogError("Unexpected exit from AddHouseToFavoritesAsync for house {HouseId} and user {UserId}", houseId, userId);
            return false;
        }

        /// <summary>
        /// Converts JsonElement values in a dictionary to proper objects for serialization
        /// When deserializing JSONB to Dictionary&lt;string, object&gt;, nested objects become JsonElement
        /// JsonElement values can't be serialized properly, so we need to convert them first
        /// </summary>
        private Dictionary<string, object> ConvertJsonElementsToObjects(Dictionary<string, object> dict)
        {
            var result = new Dictionary<string, object>();
            foreach (var kvp in dict)
            {
                if (kvp.Value is JsonElement jsonElement)
                {
                    // Convert JsonElement to proper object based on its type
                    result[kvp.Key] = ConvertJsonElementToObject(jsonElement);
                }
                else if (kvp.Value is Dictionary<string, object> nestedDict)
                {
                    // Recursively convert nested dictionaries
                    result[kvp.Key] = ConvertJsonElementsToObjects(nestedDict);
                }
                else
                {
                    // Already a proper object, keep as is
                    result[kvp.Key] = kvp.Value;
                }
            }
            return result;
        }

        /// <summary>
        /// Converts a JsonElement to a proper object (Dictionary, List, or primitive)
        /// </summary>
        private object ConvertJsonElementToObject(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Object => element.EnumerateObject()
                    .ToDictionary(prop => prop.Name, prop => ConvertJsonElementToObject(prop.Value)),
                JsonValueKind.Array => element.EnumerateArray()
                    .Select(ConvertJsonElementToObject)
                    .ToList(),
                JsonValueKind.String => element.GetString()!,
                JsonValueKind.Number => element.TryGetInt64(out var intVal) ? intVal : element.GetDecimal(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => (object?)null,
                _ => element.GetRawText()
            };
        }

        public async Task<bool> RemoveHouseFromFavoritesAsync(Guid userId, Guid houseId, CancellationToken cancellationToken = default)
        {
            // Optimistic concurrency retry logic to prevent race conditions
            const int maxRetries = 3;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var favoriteIds = await GetFavoriteHouseIdsAsync(userId, cancellationToken);
                    
                    if (!favoriteIds.Contains(houseId))
                    {
                        // Idempotent operation: removing a non-favorited house should succeed
                        _logger.LogInformation("House {HouseId} not in favorites for user {UserId} - returning true (idempotent)", houseId, userId);
                        return true;
                    }

                    favoriteIds.Remove(houseId);

                    // Update favorites in preferences
                    var preferences = await GetUserPreferencesAsync(userId, cancellationToken);
                    
                    if (preferences == null)
                    {
                        _logger.LogWarning("Preferences not found for user {UserId} when removing favorite {HouseId}", userId, houseId);
                        return false;
                    }

                    JsonDocument? jsonDoc = null;
                    JsonElement rootElement;

                    try
                    {
                        if (string.IsNullOrEmpty(preferences.PreferencesJson))
                        {
                            throw new InvalidOperationException("PreferencesJson is null or empty");
                        }
                        jsonDoc = JsonDocument.Parse(preferences.PreferencesJson);
                        rootElement = jsonDoc.RootElement.Clone();
                    }
                    catch (Exception parseEx)
                    {
                        _logger.LogWarning(parseEx, "Failed to parse preferences JSON for user {UserId}", userId);
                        return false; // Don't retry on JSON parsing errors
                    }

                    // Get existing preferences - parse entire JSON to dictionary
                    Dictionary<string, object> existingPrefs;
                    if (rootElement.ValueKind == JsonValueKind.Object)
                    {
                        try
                        {
                            // Deserialize the entire preferences JSON to a dictionary
                            existingPrefs = JsonSerializer.Deserialize<Dictionary<string, object>>(preferences.PreferencesJson) 
                                ?? new Dictionary<string, object>();
                        }
                        catch (Exception deserEx)
                        {
                            _logger.LogWarning(deserEx, "Failed to deserialize preferences for user {UserId}, creating empty dictionary", userId);
                            existingPrefs = new Dictionary<string, object>();
                        }
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
                    var favoriteIdsStringList = favoriteIds.Distinct().Select(id => id.ToString()).ToList();
                    lotteryPrefsDict["favoriteHouseIds"] = favoriteIdsStringList;
                    
                    // Update the main preferences dictionary
                    existingPrefs["lotteryPreferences"] = lotteryPrefsDict;

                    // Serialize the entire preferences dictionary back to JSON
                    // CRITICAL FIX: Convert all JsonElement values to proper objects before serialization
                    var cleanedPrefs = ConvertJsonElementsToObjects(existingPrefs);
                    var serializedJson = JsonSerializer.SerializeToElement(cleanedPrefs);
                    
                    // Wrap in transaction for atomicity
                    await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cancellationToken);
                    try
                    {
                        await UpdateUserPreferencesAsync(userId, serializedJson, cancellationToken: cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                    }
                    catch
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        throw; // Re-throw to be caught by outer catch blocks
                    }

                    _logger.LogInformation("Removed house {HouseId} from favorites for user {UserId} (attempt {Attempt})", houseId, userId, attempt);
                    return true;
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    // Concurrency conflict - another request modified preferences
                    // Retry by re-reading and checking again
                    if (attempt < maxRetries)
                    {
                        _logger.LogInformation("Concurrency conflict removing house {HouseId} from favorites for user {UserId}, retrying (attempt {Attempt}/{MaxRetries})", houseId, userId, attempt + 1, maxRetries);
                        // Small delay before retry to allow other request to complete
                        await Task.Delay(50 * attempt, cancellationToken); // Exponential backoff: 50ms, 100ms, 150ms
                        continue; // Retry
                    }
                    else
                    {
                        _logger.LogError(ex, "Failed to remove house {HouseId} from favorites for user {UserId} after {Attempts} attempts due to concurrency conflicts", houseId, userId, maxRetries);
                        return false;
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "JSON serialization error removing house {HouseId} from favorites for user {UserId}", houseId, userId);
                    return false; // Don't retry on JSON errors
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Database error removing house {HouseId} from favorites for user {UserId}", houseId, userId);
                    return false; // Don't retry on general DB errors (only concurrency)
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error removing house {HouseId} from favorites for user {UserId}", houseId, userId);
                    return false; // Don't retry on unexpected errors
                }
            }
            
            // Should never reach here, but just in case
            _logger.LogError("Unexpected exit from RemoveHouseFromFavoritesAsync for house {HouseId} and user {UserId}", houseId, userId);
            return false;
        }

        public async Task<bool> IsHouseFavoriteAsync(Guid userId, Guid houseId, CancellationToken cancellationToken = default)
        {
            var favoriteIds = await GetFavoriteHouseIdsAsync(userId, cancellationToken);
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
