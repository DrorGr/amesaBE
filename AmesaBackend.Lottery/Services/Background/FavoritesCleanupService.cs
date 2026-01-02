using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AmesaBackend.Lottery.Data;
using AmesaBackend.Auth.Data;
using AmesaBackend.Auth.Services;
using AmesaBackend.Auth.Services.Interfaces;
using System.Text.Json;

namespace AmesaBackend.Lottery.Services.Background
{
    /// <summary>
    /// Background service to clean up favorites for soft-deleted houses
    /// Runs daily to remove invalid favorites from user preferences
    /// </summary>
    public class FavoritesCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<FavoritesCleanupService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromDays(1); // Run daily

        public FavoritesCleanupService(
            IServiceProvider serviceProvider,
            ILogger<FavoritesCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("FavoritesCleanupService started. Will run daily cleanup of favorites for soft-deleted houses.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PerformCleanupAsync(stoppingToken);
                    
                    // Wait for the next cleanup cycle (24 hours)
                    await Task.Delay(_cleanupInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("FavoritesCleanupService is stopping");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in FavoritesCleanupService, will retry in next cycle");
                    // Wait 1 hour before retrying on error
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }
        }

        private async Task PerformCleanupAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting favorites cleanup for soft-deleted houses");

            using var scope = _serviceProvider.CreateScope();
            var lotteryContext = scope.ServiceProvider.GetRequiredService<LotteryDbContext>();
            var authContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var userPreferencesService = scope.ServiceProvider.GetRequiredService<IUserPreferencesService>();

            // Get all soft-deleted house IDs
            var deletedHouseIds = await lotteryContext.Houses
                .Where(h => h.DeletedAt != null)
                .Select(h => h.Id)
                .ToListAsync(cancellationToken);

            if (deletedHouseIds.Count == 0)
            {
                _logger.LogInformation("No soft-deleted houses found, cleanup complete");
                return;
            }

            _logger.LogInformation("Found {Count} soft-deleted houses to clean up favorites for", deletedHouseIds.Count);

            // Get all users with preferences
            var usersWithPreferences = await authContext.UserPreferences
                .Where(up => up.PreferencesJson != null && up.PreferencesJson.Length > 0)
                .Select(up => up.UserId)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {Count} users with preferences to check", usersWithPreferences.Count);

            int totalCleaned = 0;
            int usersProcessed = 0;
            const int batchSize = 100; // Process in batches to avoid memory issues

            // Process users in batches
            for (int i = 0; i < usersWithPreferences.Count; i += batchSize)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var batch = usersWithPreferences.Skip(i).Take(batchSize).ToList();
                
                foreach (var userId in batch)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        var favoriteIds = await userPreferencesService.GetFavoriteHouseIdsAsync(userId);
                        var invalidFavorites = favoriteIds.Where(id => deletedHouseIds.Contains(id)).ToList();

                        if (invalidFavorites.Count > 0)
                        {
                            _logger.LogDebug("User {UserId} has {Count} invalid favorites to remove", userId, invalidFavorites.Count);
                            
                            // Remove each invalid favorite
                            foreach (var houseId in invalidFavorites)
                            {
                                try
                                {
                                    var removed = await userPreferencesService.RemoveHouseFromFavoritesAsync(userId, houseId);
                                    if (removed)
                                    {
                                        totalCleaned++;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Failed to remove invalid favorite {HouseId} for user {UserId}, will retry next cycle", houseId, userId);
                                    // Continue with other favorites
                                }
                            }
                        }

                        usersProcessed++;
                        
                        // Log progress every 100 users
                        if (usersProcessed % 100 == 0)
                        {
                            _logger.LogInformation("Processed {Processed}/{Total} users, cleaned {Cleaned} invalid favorites so far", 
                                usersProcessed, usersWithPreferences.Count, totalCleaned);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error processing user {UserId} for favorites cleanup, skipping", userId);
                        // Continue with next user
                    }
                }
            }

            _logger.LogInformation("Favorites cleanup completed. Processed {UsersProcessed} users, removed {TotalCleaned} invalid favorites", 
                usersProcessed, totalCleaned);
        }
    }
}


