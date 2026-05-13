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
        public DbSet<AdminRole> AdminRoles { get; set; }
        public DbSet<AdminPermission> AdminPermissions { get; set; }
        public DbSet<AdminUserRole> AdminUserRoles { get; set; }
        public DbSet<AdminRolePermission> AdminRolePermissions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure AuditLog entity
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.ToTable("audit_logs", "amesa_admin");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Action).IsRequired().HasMaxLength(100).HasColumnName("action");
                entity.Property(e => e.EntityType).IsRequired().HasMaxLength(100).HasColumnName("entity_type");
                entity.Property(e => e.EntityId).IsRequired().HasColumnName("entity_id");
                entity.Property(e => e.AdminUserId).IsRequired().HasColumnName("admin_user_id");
                entity.Property(e => e.ActionDetails).HasColumnType("jsonb").HasColumnName("action_details");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.HasIndex(e => new { e.EntityType, e.EntityId });
                entity.HasIndex(e => new { e.AdminUserId, e.CreatedAt });
                entity.HasIndex(e => e.CreatedAt);
            });

            // Configure AdminUser entity
            modelBuilder.Entity<AdminUser>(entity =>
            {
                entity.ToTable("admin_users", "amesa_admin");
                entity.HasKey(e => e.Id);
                
                // Explicit column mappings for PostgreSQL case-sensitivity
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255).HasColumnName("email");
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50).HasColumnName("username");
                entity.Property(e => e.PasswordHash).IsRequired().HasColumnName("password_hash");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                entity.Property(e => e.TwoFactorEnabled).HasColumnName("two_factor_enabled");
                entity.Property(e => e.TwoFactorSecret).HasMaxLength(255).HasColumnName("two_factor_secret");
                entity.Property(e => e.LastMfaAt).HasColumnName("last_mfa_at");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.LastLoginAt).HasColumnName("last_login_at");
                
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Username).IsUnique();
            });

            // Configure AdminSession entity
            modelBuilder.Entity<AdminSession>(entity =>
            {
                entity.ToTable("admin_sessions", "amesa_admin");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.AdminUserId).HasColumnName("admin_user_id");
                entity.Property(e => e.SessionToken).IsRequired().HasMaxLength(255).HasColumnName("session_token");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
                entity.Property(e => e.LastSeenAt).HasColumnName("last_seen_at");
                entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
                entity.Property(e => e.IpAddress).HasMaxLength(64).HasColumnName("ip_address");
                entity.Property(e => e.UserAgent).HasMaxLength(512).HasColumnName("user_agent");
                entity.HasIndex(e => e.SessionToken).IsUnique();
                entity.HasIndex(e => e.AdminUserId);
                entity.HasOne(e => e.AdminUser).WithMany().HasForeignKey(e => e.AdminUserId);
            });

            modelBuilder.Entity<AdminRole>(entity =>
            {
                entity.ToTable("admin_roles", "amesa_admin");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100).HasColumnName("name");
                entity.Property(e => e.Description).HasMaxLength(500).HasColumnName("description");
                entity.Property(e => e.IsSystemRole).HasColumnName("is_system_role");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            modelBuilder.Entity<AdminPermission>(entity =>
            {
                entity.ToTable("admin_permissions", "amesa_admin");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(150).HasColumnName("name");
                entity.Property(e => e.Description).HasMaxLength(500).HasColumnName("description");
                entity.Property(e => e.Category).IsRequired().HasMaxLength(100).HasColumnName("category");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.HasIndex(e => e.Name).IsUnique();
                entity.HasIndex(e => e.Category);
            });

            modelBuilder.Entity<AdminUserRole>(entity =>
            {
                entity.ToTable("admin_user_roles", "amesa_admin");
                entity.HasKey(e => new { e.AdminUserId, e.AdminRoleId });
                entity.Property(e => e.AdminUserId).HasColumnName("admin_user_id");
                entity.Property(e => e.AdminRoleId).HasColumnName("admin_role_id");
                entity.Property(e => e.AssignedAt).HasColumnName("assigned_at");
                entity.Property(e => e.AssignedByAdminUserId).HasColumnName("assigned_by_admin_user_id");
                entity.HasIndex(e => e.AdminRoleId);
                entity.HasOne(e => e.AdminUser)
                    .WithMany(e => e.UserRoles)
                    .HasForeignKey(e => e.AdminUserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.AdminRole)
                    .WithMany(e => e.UserRoles)
                    .HasForeignKey(e => e.AdminRoleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AdminRolePermission>(entity =>
            {
                entity.ToTable("admin_role_permissions", "amesa_admin");
                entity.HasKey(e => new { e.AdminRoleId, e.AdminPermissionId });
                entity.Property(e => e.AdminRoleId).HasColumnName("admin_role_id");
                entity.Property(e => e.AdminPermissionId).HasColumnName("admin_permission_id");
                entity.Property(e => e.GrantedAt).HasColumnName("granted_at");
                entity.HasIndex(e => e.AdminPermissionId);
                entity.HasOne(e => e.AdminRole)
                    .WithMany(e => e.RolePermissions)
                    .HasForeignKey(e => e.AdminRoleId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.AdminPermission)
                    .WithMany(e => e.RolePermissions)
                    .HasForeignKey(e => e.AdminPermissionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}

