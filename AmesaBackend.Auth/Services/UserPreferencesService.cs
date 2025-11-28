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
            // #region agent log
            _logger.LogInformation("[DEBUG] UserPreferencesService.GetUserPreferencesAsync:entry userId={UserId} contextNull={Null}", userId, _context == null);
            // #endregion
            try
            {
                // #region agent log
                _logger.LogInformation("[DEBUG] UserPreferencesService.GetUserPreferencesAsync:before-db-query userId={UserId}", userId);
                // #endregion
                var preferences = await _context.UserPreferences
                    .FirstOrDefaultAsync(up => up.UserId == userId);
                // #region agent log
                _logger.LogInformation("[DEBUG] UserPreferencesService.GetUserPreferencesAsync:after-db-query userId={UserId} preferencesIsNull={IsNull} preferencesId={Id} preferencesJsonLength={Length}", userId, preferences == null, preferences?.Id, preferences?.PreferencesJson?.Length ?? 0);
                // #endregion

                if (preferences == null)
                {
                    // #region agent log
                    _logger.LogInformation("[DEBUG] UserPreferencesService.GetUserPreferencesAsync:preferences-null userId={UserId} - returning null", userId);
                    // #endregion
                    return null;
                }

                // #region agent log
                _logger.LogInformation("[DEBUG] UserPreferencesService.GetUserPreferencesAsync:returning-dto userId={UserId} preferencesId={Id} version={Version}", userId, preferences.Id, preferences.Version);
                // #endregion
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
                // #region agent log
                _logger.LogError(ex, "[DEBUG] UserPreferencesService.GetUserPreferencesAsync:exception userId={UserId} exceptionType={Type} message={Message}", userId, ex.GetType().Name, ex.Message);
                // #endregion
                throw;
            }
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
                // #region agent log
                _logger.LogInformation("[DEBUG] UserPreferencesService.UpdateUserPreferencesAsync:updating-existing userId={UserId} existingJsonLength={Length} incomingJsonLength={IncomingLength}", userId, existingPreferences.PreferencesJson?.Length ?? 0, preferences.GetRawText().Length);
                // #endregion
                
                // CRITICAL FIX: Merge incoming preferences with existing preferences to preserve lotteryPreferences
                // Deserialize existing preferences
                Dictionary<string, object> existingPrefsDict;
                try
                {
                    existingPrefsDict = JsonSerializer.Deserialize<Dictionary<string, object>>(existingPreferences.PreferencesJson) 
                        ?? new Dictionary<string, object>();
                    // #region agent log
                    _logger.LogInformation("[DEBUG] UserPreferencesService.UpdateUserPreferencesAsync:deserialized-existing userId={UserId} existingKeys={Keys} hasLotteryPrefs={HasKey}", userId, string.Join(",", existingPrefsDict.Keys), existingPrefsDict.ContainsKey("lotteryPreferences"));
                    // #endregion
                }
                catch (Exception ex)
                {
                    // #region agent log
                    _logger.LogWarning(ex, "[DEBUG] UserPreferencesService.UpdateUserPreferencesAsync:failed-deserialize-existing userId={UserId} - creating empty dict", userId);
                    // #endregion
                    existingPrefsDict = new Dictionary<string, object>();
                }

                // Preserve existing lotteryPreferences before merging
                object? existingLotteryPrefs = null;
                if (existingPrefsDict.TryGetValue("lotteryPreferences", out var existingLotteryPrefsObj))
                {
                    existingLotteryPrefs = existingLotteryPrefsObj;
                    // #region agent log
                    _logger.LogInformation("[DEBUG] UserPreferencesService.UpdateUserPreferencesAsync:preserved-lottery-prefs userId={UserId} lotteryPrefsType={Type}", userId, existingLotteryPrefs?.GetType().Name ?? "null");
                    // #endregion
                }

                // Deserialize incoming preferences
                Dictionary<string, object> incomingPrefsDict;
                if (preferences.ValueKind == JsonValueKind.Object)
                {
                    try
                    {
                        incomingPrefsDict = JsonSerializer.Deserialize<Dictionary<string, object>>(preferences.GetRawText()) 
                            ?? new Dictionary<string, object>();
                        // #region agent log
                        _logger.LogInformation("[DEBUG] UserPreferencesService.UpdateUserPreferencesAsync:deserialized-incoming userId={UserId} incomingKeys={Keys} hasLotteryPrefs={HasKey}", userId, string.Join(",", incomingPrefsDict.Keys), incomingPrefsDict.ContainsKey("lotteryPreferences"));
                        // #endregion
                    }
                    catch (Exception ex)
                    {
                        // #region agent log
                        _logger.LogWarning(ex, "[DEBUG] UserPreferencesService.UpdateUserPreferencesAsync:failed-deserialize-incoming userId={UserId} - using existing", userId);
                        // #endregion
                        incomingPrefsDict = existingPrefsDict;
                    }
                }
                else
                {
                    // #region agent log
                    _logger.LogWarning("[DEBUG] UserPreferencesService.UpdateUserPreferencesAsync:incoming-not-object userId={UserId} valueKind={ValueKind} - using existing", userId, preferences.ValueKind);
                    // #endregion
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
                        // #region agent log
                        _logger.LogInformation("[DEBUG] UserPreferencesService.UpdateUserPreferencesAsync:restored-lottery-prefs userId={UserId}", userId);
                        // #endregion
                    }
                    else
                    {
                        // Create new lotteryPreferences structure if it doesn't exist
                        existingPrefsDict["lotteryPreferences"] = new Dictionary<string, object>
                        {
                            ["favoriteHouseIds"] = new List<string>()
                        };
                        // #region agent log
                        _logger.LogInformation("[DEBUG] UserPreferencesService.UpdateUserPreferencesAsync:created-lottery-prefs userId={UserId}", userId);
                        // #endregion
                    }
                }
                else
                {
                    // #region agent log
                    _logger.LogInformation("[DEBUG] UserPreferencesService.UpdateUserPreferencesAsync:incoming-has-lottery-prefs userId={UserId} - using incoming", userId);
                    // #endregion
                }

                // Serialize merged preferences
                var mergedJson = JsonSerializer.Serialize(existingPrefsDict);
                // #region agent log
                _logger.LogInformation("[DEBUG] UserPreferencesService.UpdateUserPreferencesAsync:merged-preferences userId={UserId} mergedJsonLength={Length} finalKeys={Keys}", userId, mergedJson.Length, string.Join(",", existingPrefsDict.Keys));
                // #endregion

                existingPreferences.PreferencesJson = mergedJson;
                if (!string.IsNullOrEmpty(version))
                {
                    existingPreferences.Version = version;
                }
                existingPreferences.UpdatedAt = DateTime.UtcNow;
                existingPreferences.UpdatedBy = userId.ToString();

                await _context.SaveChangesAsync();

                // #region agent log
                _logger.LogInformation("[DEBUG] UserPreferencesService.UpdateUserPreferencesAsync:update-success userId={UserId}", userId);
                // #endregion
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
            // #region agent log
            _logger.LogInformation("[DEBUG] UserPreferencesService.GetFavoriteHouseIdsAsync:entry userId={UserId}", userId);
            // #endregion
            var preferences = await GetUserPreferencesAsync(userId);
            // #region agent log
            _logger.LogInformation("[DEBUG] UserPreferencesService.GetFavoriteHouseIdsAsync:after-get-preferences userId={UserId} preferencesIsNull={IsNull} preferencesJsonIsNull={JsonNull} preferencesJsonLength={Length}", userId, preferences == null, preferences?.PreferencesJson == null, preferences?.PreferencesJson?.Length ?? 0);
            // #endregion
            if (preferences == null)
            {
                // #region agent log
                _logger.LogInformation("[DEBUG] UserPreferencesService.GetFavoriteHouseIdsAsync:no-preferences userId={UserId} - returning empty list", userId);
                // #endregion
                return new List<Guid>();
            }

            try
            {
                // #region agent log
                _logger.LogInformation("[DEBUG] UserPreferencesService.GetFavoriteHouseIdsAsync:before-parse userId={UserId} jsonLength={Length}", userId, preferences.PreferencesJson?.Length ?? 0);
                // #endregion
                var jsonDoc = JsonDocument.Parse(preferences.PreferencesJson);
                // #region agent log
                _logger.LogInformation("[DEBUG] UserPreferencesService.GetFavoriteHouseIdsAsync:after-parse userId={UserId} rootElementValueKind={ValueKind}", userId, jsonDoc.RootElement.ValueKind);
                // #endregion
                if (jsonDoc.RootElement.TryGetProperty("lotteryPreferences", out var lotteryPrefs))
                {
                    // #region agent log
                    _logger.LogInformation("[DEBUG] UserPreferencesService.GetFavoriteHouseIdsAsync:found-lottery-prefs userId={UserId} lotteryPrefsValueKind={ValueKind}", userId, lotteryPrefs.ValueKind);
                    // #endregion
                    if (lotteryPrefs.TryGetProperty("favoriteHouseIds", out var favoriteIds))
                    {
                        // #region agent log
                        _logger.LogInformation("[DEBUG] UserPreferencesService.GetFavoriteHouseIdsAsync:found-favorite-ids userId={UserId} favoriteIdsValueKind={ValueKind}", userId, favoriteIds.ValueKind);
                        // #endregion
                        if (favoriteIds.ValueKind == JsonValueKind.Array)
                        {
                            var ids = new List<Guid>();
                            var arrayLength = favoriteIds.GetArrayLength();
                            // #region agent log
                            _logger.LogInformation("[DEBUG] UserPreferencesService.GetFavoriteHouseIdsAsync:parsing-array userId={UserId} arrayLength={Length}", userId, arrayLength);
                            // #endregion
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
                                        // #region agent log
                                        _logger.LogWarning("[DEBUG] UserPreferencesService.GetFavoriteHouseIdsAsync:invalid-guid userId={UserId} idString={IdString}", userId, idString);
                                        // #endregion
                                    }
                                }
                            }
                            // #region agent log
                            _logger.LogInformation("[DEBUG] UserPreferencesService.GetFavoriteHouseIdsAsync:parsed-ids userId={UserId} idsCount={Count} ids={Ids}", userId, ids.Count, string.Join(",", ids));
                            // #endregion
                            return ids;
                        }
                        else
                        {
                            // #region agent log
                            _logger.LogWarning("[DEBUG] UserPreferencesService.GetFavoriteHouseIdsAsync:favorite-ids-not-array userId={UserId} valueKind={ValueKind}", userId, favoriteIds.ValueKind);
                            // #endregion
                        }
                    }
                    else
                    {
                        // #region agent log
                        _logger.LogInformation("[DEBUG] UserPreferencesService.GetFavoriteHouseIdsAsync:no-favorite-ids-property userId={UserId}", userId);
                        // #endregion
                    }
                }
                else
                {
                    // #region agent log
                    _logger.LogInformation("[DEBUG] UserPreferencesService.GetFavoriteHouseIdsAsync:no-lottery-prefs-property userId={UserId}", userId);
                    // #endregion
                }
            }
            catch (Exception ex)
            {
                // #region agent log
                _logger.LogWarning(ex, "[DEBUG] UserPreferencesService.GetFavoriteHouseIdsAsync:exception userId={UserId} exceptionType={Type} message={Message}", userId, ex.GetType().Name, ex.Message);
                // #endregion
                _logger.LogWarning(ex, "Error parsing favorite house IDs for user {UserId}", userId);
            }

            // #region agent log
            _logger.LogInformation("[DEBUG] UserPreferencesService.GetFavoriteHouseIdsAsync:returning-empty userId={UserId}", userId);
            // #endregion
            return new List<Guid>();
        }

        public async Task<bool> AddHouseToFavoritesAsync(Guid userId, Guid houseId)
        {
            // #region agent log
            _logger.LogInformation("[DEBUG] UserPreferencesService.AddHouseToFavoritesAsync:entry houseId={HouseId} userId={UserId}", houseId, userId);
            // #endregion
            try
            {
                // #region agent log
                _logger.LogInformation("[DEBUG] UserPreferencesService.AddHouseToFavoritesAsync:before-get-favorites houseId={HouseId} userId={UserId}", houseId, userId);
                // #endregion
                var favoriteIds = await GetFavoriteHouseIdsAsync(userId);
                // #region agent log
                _logger.LogInformation("[DEBUG] UserPreferencesService.AddHouseToFavoritesAsync:after-get-favorites houseId={HouseId} userId={UserId} favoriteIdsCount={Count} isAlreadyFavorite={AlreadyFavorite}", houseId, userId, favoriteIds.Count, favoriteIds.Contains(houseId));
                // #endregion
                
                if (favoriteIds.Contains(houseId))
                {
                    // #region agent log
                    _logger.LogInformation("[DEBUG] UserPreferencesService.AddHouseToFavoritesAsync:already-favorite houseId={HouseId} userId={UserId} - returning true (idempotent)", houseId, userId);
                    // #endregion
                    // Idempotent operation: adding an already-favorited house should succeed
                    return true;
                }

                favoriteIds.Add(houseId);

                // Update favorites in preferences
                // #region agent log
                _logger.LogInformation("[DEBUG] UserPreferencesService.AddHouseToFavoritesAsync:before-get-preferences houseId={HouseId} userId={UserId}", houseId, userId);
                // #endregion
                var preferences = await GetUserPreferencesAsync(userId);
                // #region agent log
                _logger.LogInformation("[DEBUG] UserPreferencesService.AddHouseToFavoritesAsync:after-get-preferences houseId={HouseId} userId={UserId} preferencesIsNull={IsNull} preferencesJsonLength={Length}", houseId, userId, preferences == null, preferences?.PreferencesJson?.Length ?? 0);
                // #endregion
                JsonDocument? jsonDoc = null;
                JsonElement rootElement;

                // #region agent log
                _logger.LogInformation("[DEBUG] UserPreferencesService.AddHouseToFavoritesAsync:before-json-parse houseId={HouseId} userId={UserId} preferencesIsNull={IsNull} preferencesJsonIsNull={JsonNull} preferencesJsonLength={Length}", houseId, userId, preferences == null, preferences?.PreferencesJson == null, preferences?.PreferencesJson?.Length ?? 0);
                // #endregion
                if (preferences != null)
                {
                    try
                    {
                        // #region agent log
                        _logger.LogInformation("[DEBUG] UserPreferencesService.AddHouseToFavoritesAsync:parsing-json houseId={HouseId} userId={UserId} jsonLength={Length}", houseId, userId, preferences.PreferencesJson?.Length ?? 0);
                        // #endregion
                        jsonDoc = JsonDocument.Parse(preferences.PreferencesJson);
                        rootElement = jsonDoc.RootElement.Clone();
                        // #region agent log
                        _logger.LogInformation("[DEBUG] UserPreferencesService.AddHouseToFavoritesAsync:json-parsed houseId={HouseId} userId={UserId} rootElementValueKind={ValueKind}", houseId, userId, rootElement.ValueKind);
                        // #endregion
                    }
                    catch (Exception parseEx)
                    {
                        // #region agent log
                        _logger.LogWarning(parseEx, "[DEBUG] UserPreferencesService.AddHouseToFavoritesAsync:json-parse-failed houseId={HouseId} userId={UserId} exceptionType={Type} message={Message}", houseId, userId, parseEx.GetType().Name, parseEx.Message);
                        // #endregion
                        rootElement = new JsonElement();
                    }
                }
                else
                {
                    // #region agent log
                    _logger.LogInformation("[DEBUG] UserPreferencesService.AddHouseToFavoritesAsync:preferences-null houseId={HouseId} userId={UserId} - creating empty rootElement", houseId, userId);
                    // #endregion
                    rootElement = new JsonElement();
                }

                // Get or create lottery preferences
                // Parse the entire preferences JSON into a dictionary we can modify
                Dictionary<string, object> existingPrefs;
                // #region agent log
                _logger.LogInformation("[DEBUG] UserPreferencesService.AddHouseToFavoritesAsync:before-deserialize houseId={HouseId} userId={UserId} preferencesIsNull={IsNull} rootElementValueKind={ValueKind}", houseId, userId, preferences == null, rootElement.ValueKind);
                // #endregion
                if (preferences != null && rootElement.ValueKind == JsonValueKind.Object)
                {
                    try
                    {
                        // Deserialize the entire preferences JSON to a dictionary
                        existingPrefs = JsonSerializer.Deserialize<Dictionary<string, object>>(preferences.PreferencesJson) 
                            ?? new Dictionary<string, object>();
                        // #region agent log
                        _logger.LogInformation("[DEBUG] UserPreferencesService.AddHouseToFavoritesAsync:deserialized houseId={HouseId} userId={UserId} existingPrefsCount={Count} keys={Keys}", houseId, userId, existingPrefs.Count, string.Join(",", existingPrefs.Keys));
                        // #endregion
                    }
                    catch (Exception deserEx)
                    {
                        // #region agent log
                        _logger.LogWarning(deserEx, "[DEBUG] UserPreferencesService.AddHouseToFavoritesAsync:deserialize-failed houseId={HouseId} userId={UserId} exceptionType={Type} message={Message} - creating empty dict", houseId, userId, deserEx.GetType().Name, deserEx.Message);
                        // #endregion
                        existingPrefs = new Dictionary<string, object>();
                    }
                }
                else
                {
                    // #region agent log
                    _logger.LogInformation("[DEBUG] UserPreferencesService.AddHouseToFavoritesAsync:creating-empty-prefs houseId={HouseId} userId={UserId} reason={Reason}", houseId, userId, preferences == null ? "preferencesIsNull" : "rootElementNotObject");
                    // #endregion
                    existingPrefs = new Dictionary<string, object>();
                }

                // Update or create lotteryPreferences
                Dictionary<string, object> lotteryPrefsDict;
                // #region agent log
                _logger.LogInformation("[DEBUG] UserPreferencesService.AddHouseToFavoritesAsync:before-lottery-prefs houseId={HouseId} userId={UserId} existingPrefsHasLotteryPrefs={HasKey}", houseId, userId, existingPrefs.ContainsKey("lotteryPreferences"));
                // #endregion
                if (existingPrefs.TryGetValue("lotteryPreferences", out var existingLotteryPrefsObj))
                {
                    // #region agent log
                    _logger.LogInformation("[DEBUG] UserPreferencesService.AddHouseToFavoritesAsync:found-lottery-prefs houseId={HouseId} userId={UserId} existingLotteryPrefsObjType={Type}", houseId, userId, existingLotteryPrefsObj?.GetType().Name ?? "null");
                    // #endregion
                    // Parse existing lotteryPreferences
                    if (existingLotteryPrefsObj is JsonElement existingLotteryPrefs)
                    {
                        // #region agent log
                        _logger.LogInformation("[DEBUG] UserPreferencesService.AddHouseToFavoritesAsync:parsing-json-element houseId={HouseId} userId={UserId}", houseId, userId);
                        // #endregion
                        lotteryPrefsDict = JsonSerializer.Deserialize<Dictionary<string, object>>(existingLotteryPrefs.GetRawText()) 
                            ?? new Dictionary<string, object>();
                    }
                    else if (existingLotteryPrefsObj is Dictionary<string, object> existingDict)
                    {
                        // #region agent log
                        _logger.LogInformation("[DEBUG] UserPreferencesService.AddHouseToFavoritesAsync:using-existing-dict houseId={HouseId} userId={UserId} dictCount={Count}", houseId, userId, existingDict.Count);
                        // #endregion
                        lotteryPrefsDict = existingDict;
                    }
                    else
                    {
                        // #region agent log
                        _logger.LogInformation("[DEBUG] UserPreferencesService.AddHouseToFavoritesAsync:serializing-unknown-type houseId={HouseId} userId={UserId} type={Type}", houseId, userId, existingLotteryPrefsObj?.GetType().Name ?? "null");
                        // #endregion
                        // Try to deserialize from string representation
                        lotteryPrefsDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                            JsonSerializer.Serialize(existingLotteryPrefsObj)) 
                            ?? new Dictionary<string, object>();
                    }
                }
                else
                {
                    // #region agent log
                    _logger.LogInformation("[DEBUG] UserPreferencesService.AddHouseToFavoritesAsync:creating-new-lottery-prefs houseId={HouseId} userId={UserId}", houseId, userId);
                    // #endregion
                    lotteryPrefsDict = new Dictionary<string, object>();
                }

                // Update favoriteHouseIds
                // #region agent log
                var favoriteIdsStringList = favoriteIds.Select(id => id.ToString()).ToList();
                _logger.LogInformation("[DEBUG] UserPreferencesService.AddHouseToFavoritesAsync:before-update-favorite-ids houseId={HouseId} userId={UserId} favoriteIdsCount={Count} favoriteIds={Ids} lotteryPrefsDictHasFavoriteIds={HasKey}", houseId, userId, favoriteIds.Count, string.Join(",", favoriteIdsStringList), lotteryPrefsDict.ContainsKey("favoriteHouseIds"));
                // #endregion
                lotteryPrefsDict["favoriteHouseIds"] = favoriteIdsStringList;
                // #region agent log
                _logger.LogInformation("[DEBUG] UserPreferencesService.AddHouseToFavoritesAsync:after-update-favorite-ids houseId={HouseId} userId={UserId} lotteryPrefsDictKeys={Keys}", houseId, userId, string.Join(",", lotteryPrefsDict.Keys));
                // #endregion
                
                // Update the main preferences dictionary
                existingPrefs["lotteryPreferences"] = lotteryPrefsDict;
                // #region agent log
                _logger.LogInformation("[DEBUG] UserPreferencesService.AddHouseToFavoritesAsync:after-update-lottery-prefs houseId={HouseId} userId={UserId} existingPrefsKeys={Keys}", houseId, userId, string.Join(",", existingPrefs.Keys));
                // #endregion

                // Serialize the entire preferences dictionary back to JSON
                // #region agent log
                _logger.LogInformation("[DEBUG] UserPreferencesService.AddHouseToFavoritesAsync:before-serialize houseId={HouseId} userId={UserId} existingPrefsCount={Count}", houseId, userId, existingPrefs.Count);
                // #endregion
                var serializedJson = JsonSerializer.SerializeToElement(existingPrefs);
                // #region agent log
                _logger.LogInformation("[DEBUG] UserPreferencesService.AddHouseToFavoritesAsync:after-serialize houseId={HouseId} userId={UserId} serializedJsonValueKind={ValueKind}", houseId, userId, serializedJson.ValueKind);
                // #endregion
                // #region agent log
                _logger.LogInformation("[DEBUG] UserPreferencesService.AddHouseToFavoritesAsync:before-update-db houseId={HouseId} userId={UserId}", houseId, userId);
                // #endregion
                await UpdateUserPreferencesAsync(userId, serializedJson);
                // #region agent log
                _logger.LogInformation("[DEBUG] UserPreferencesService.AddHouseToFavoritesAsync:after-update-db houseId={HouseId} userId={UserId}", houseId, userId);
                // #endregion

                _logger.LogInformation("Added house {HouseId} to favorites for user {UserId}", houseId, userId);
                return true;
            }
            catch (JsonException ex)
            {
                // #region agent log
                _logger.LogError(ex, "[DEBUG] UserPreferencesService.AddHouseToFavoritesAsync:json-exception houseId={HouseId} userId={UserId} message={Message}", houseId, userId, ex.Message);
                // #endregion
                _logger.LogError(ex, "JSON serialization error adding house {HouseId} to favorites for user {UserId}", houseId, userId);
                return false;
            }
            catch (DbUpdateException ex)
            {
                // #region agent log
                _logger.LogError(ex, "[DEBUG] UserPreferencesService.AddHouseToFavoritesAsync:db-exception houseId={HouseId} userId={UserId} message={Message}", houseId, userId, ex.Message);
                // #endregion
                _logger.LogError(ex, "Database error adding house {HouseId} to favorites for user {UserId}", houseId, userId);
                return false;
            }
            catch (Exception ex)
            {
                // #region agent log
                _logger.LogError(ex, "[DEBUG] UserPreferencesService.AddHouseToFavoritesAsync:exception houseId={HouseId} userId={UserId} exceptionType={Type} message={Message}", houseId, userId, ex.GetType().Name, ex.Message);
                // #endregion
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
                    // Idempotent operation: removing a non-favorited house should succeed
                    _logger.LogInformation("House {HouseId} not in favorites for user {UserId} - returning true (idempotent)", houseId, userId);
                    return true;
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
