using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using AmesaBackend.Auth.Data;
using AmesaBackend.Auth.Models;

namespace AmesaBackend.Auth.BackgroundServices
{
    /// <summary>
    /// Background service to clean up old password history records.
    /// Keeps only the last N passwords per user (configurable).
    /// </summary>
    public class PasswordHistoryCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PasswordHistoryCleanupService> _logger;
        private readonly IConfiguration _configuration;
        private readonly TimeSpan _interval;
        private readonly int _keepLastPasswords;

        public PasswordHistoryCleanupService(
            IServiceProvider serviceProvider,
            ILogger<PasswordHistoryCleanupService> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;

            // Default: Run daily at 2 AM
            var intervalHours = _configuration.GetValue<int>("SecuritySettings:PasswordHistory:CleanupIntervalHours", 24);
            _interval = TimeSpan.FromHours(intervalHours);

            // Keep last N passwords (default: 5, same as CheckLastPasswords)
            _keepLastPasswords = _configuration.GetValue<int>("SecuritySettings:PasswordHistory:KeepLastPasswords", 5);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Password History Cleanup Service started. Interval: {Interval} hours, Keep last: {KeepLast} passwords", 
                _interval.TotalHours, _keepLastPasswords);

            // Wait for the first interval before running cleanup (don't run immediately on startup)
            await Task.Delay(_interval, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupPasswordHistoryAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing Password History Cleanup Service.");
                }

                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("Password History Cleanup Service stopped.");
        }

        private async Task CleanupPasswordHistoryAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

            try
            {
                // Get all users with password history
                var usersWithHistory = await context.Set<UserPasswordHistory>()
                    .GroupBy(h => h.UserId)
                    .Select(g => new { UserId = g.Key, Count = g.Count() })
                    .Where(u => u.Count > _keepLastPasswords)
                    .ToListAsync();

                if (!usersWithHistory.Any())
                {
                    _logger.LogInformation("No password history cleanup needed. All users have {KeepLast} or fewer password history records.", _keepLastPasswords);
                    return;
                }

                // Use transaction to ensure atomic cleanup
                using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    var totalDeleted = 0;

                    foreach (var userHistory in usersWithHistory)
                    {
                        // Get IDs of passwords to keep (most recent N)
                        var passwordsToKeep = await context.Set<UserPasswordHistory>()
                            .Where(h => h.UserId == userHistory.UserId)
                            .OrderByDescending(h => h.CreatedAt)
                            .Take(_keepLastPasswords)
                            .Select(h => h.Id)
                            .ToListAsync();

                        // Delete all passwords except the ones to keep
                        var passwordsToDelete = await context.Set<UserPasswordHistory>()
                            .Where(h => h.UserId == userHistory.UserId && !passwordsToKeep.Contains(h.Id))
                            .ToListAsync();

                        if (passwordsToDelete.Any())
                        {
                            context.Set<UserPasswordHistory>().RemoveRange(passwordsToDelete);
                            totalDeleted += passwordsToDelete.Count;
                            _logger.LogDebug("Cleaned up {Count} old password history records for user {UserId}", 
                                passwordsToDelete.Count, userHistory.UserId);
                        }
                    }

                    if (totalDeleted > 0)
                    {
                        await context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        _logger.LogInformation("Password history cleanup completed. Deleted {TotalDeleted} old password history records for {UserCount} users.", 
                            totalDeleted, usersWithHistory.Count);
                    }
                    else
                    {
                        await transaction.RollbackAsync();
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error during password history cleanup transaction - rolled back");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password history cleanup");
                throw;
            }
        }
    }
}

