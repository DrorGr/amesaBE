using Microsoft.EntityFrameworkCore;
using AmesaBackend.Admin.Models;

namespace AmesaBackend.Admin.Data
{
    public class AdminDbContext : DbContext
    {
        public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options)
        {
        }

        // Admin-specific tables
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<AdminUser> AdminUsers { get; set; }
        public DbSet<AdminSession> AdminSessions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure AuditLog entity
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.ToTable("audit_logs", "amesa_admin");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
                entity.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.EntityId).IsRequired();
                entity.Property(e => e.AdminUserId).IsRequired();
                entity.Property(e => e.ActionDetails).HasColumnType("jsonb");
                entity.HasIndex(e => new { e.EntityType, e.EntityId });
                entity.HasIndex(e => new { e.AdminUserId, e.CreatedAt });
                entity.HasIndex(e => e.CreatedAt);
            });

            // Configure AdminUser entity
            modelBuilder.Entity<AdminUser>(entity =>
            {
                entity.ToTable("admin_users", "amesa_admin");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Username).IsUnique();
            });

            // Configure AdminSession entity
            modelBuilder.Entity<AdminSession>(entity =>
            {
                entity.ToTable("admin_sessions", "amesa_admin");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SessionToken).IsRequired().HasMaxLength(255);
                entity.HasIndex(e => e.SessionToken).IsUnique();
                entity.HasIndex(e => e.AdminUserId);
                entity.HasOne(e => e.AdminUser).WithMany().HasForeignKey(e => e.AdminUserId);
            });
        }
    }
}

