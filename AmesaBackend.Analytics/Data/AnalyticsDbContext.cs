using Microsoft.EntityFrameworkCore;
using AmesaBackend.Analytics.Models;

namespace AmesaBackend.Analytics.Data
{
    public class AnalyticsDbContext : DbContext
    {
        public AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) : base(options)
        {
        }

        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<UserActivityLog> UserActivityLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserSession>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SessionToken).IsRequired().HasMaxLength(255);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.SessionToken);
            });

            modelBuilder.Entity<UserActivityLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.SessionId);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasOne(e => e.Session).WithMany(s => s.ActivityLogs).HasForeignKey(e => e.SessionId);
            });
        }
    }
}

