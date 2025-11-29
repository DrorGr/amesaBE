using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AmesaBackend.Auth.Data;

namespace AmesaBackend.Auth.BackgroundServices
{
    public class SessionCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SessionCleanupService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);
        private readonly TimeSpan _archiveThreshold = TimeSpan.FromDays(30);

        public SessionCleanupService(
            IServiceProvider serviceProvider,
            ILogger<SessionCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupExpiredSessionsAsync();
                    await Task.Delay(_cleanupInterval, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during session cleanup");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Retry sooner on error
                }
            }
        }

        private async Task CleanupExpiredSessionsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

            try
            {
                var now = DateTime.UtcNow;
                
                // Use transaction for atomicity
                using var transaction = await dbContext.Database.BeginTransactionAsync();
                try
                {
                    // Mark expired sessions as inactive
                    var expiredSessions = await dbContext.UserSessions
                        .Where(s => s.ExpiresAt < now && s.IsActive)
                        .ToListAsync();

                    var expiredCount = expiredSessions.Count;
                    foreach (var session in expiredSessions)
                    {
                        session.IsActive = false;
                    }

                    // Archive old sessions (older than 30 days) - avoid double-processing
                    var oldSessions = await dbContext.UserSessions
                        .Where(s => s.CreatedAt < now - _archiveThreshold && s.IsActive)
                        .ToListAsync();

                    var archivedCount = oldSessions.Count;
                    // In a production system, you might move these to an archive table
                    // For now, we'll just mark them as inactive
                    foreach (var session in oldSessions)
                    {
                        session.IsActive = false;
                    }

                    await dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    if (expiredCount > 0 || archivedCount > 0)
                    {
                        _logger.LogInformation("Session cleanup completed: {ExpiredCount} expired sessions, {ArchivedCount} archived sessions",
                            expiredCount, archivedCount);
                    }
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up sessions");
                throw;
            }
        }
    }
}

