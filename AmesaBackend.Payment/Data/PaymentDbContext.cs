using Microsoft.EntityFrameworkCore;
using AmesaBackend.Payment.Models;

namespace AmesaBackend.Payment.Data
{
    public class PaymentDbContext : DbContext
    {
        public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
        {
        }

        public DbSet<UserPaymentMethod> UserPaymentMethods { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductLink> ProductLinks { get; set; }
        public DbSet<TransactionItem> TransactionItems { get; set; }
        public DbSet<PaymentAuditLog> PaymentAuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserPaymentMethod>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.UserId);
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Amount).HasColumnType("decimal(15,2)");
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ReferenceId);
                entity.HasIndex(e => e.ProductId);
                entity.HasIndex(e => e.IdempotencyKey).IsUnique();
                entity.HasIndex(e => new { e.UserId, e.Status });
                entity.HasOne(e => e.PaymentMethod).WithMany(pm => pm.Transactions).HasForeignKey(e => e.PaymentMethodId);
                entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId);
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Code).IsRequired().HasMaxLength(50).HasColumnName("code");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255).HasColumnName("name");
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.ProductType).IsRequired().HasMaxLength(50).HasColumnName("product_type");
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasColumnName("status");
                entity.Property(e => e.BasePrice).IsRequired().HasColumnType("decimal(15,2)").HasColumnName("base_price");
                entity.Property(e => e.Currency).IsRequired().HasMaxLength(3).HasColumnName("currency");
                entity.Property(e => e.PricingModel).HasMaxLength(50).HasColumnName("pricing_model");
                entity.Property(e => e.PricingMetadata).HasColumnName("pricing_metadata");
                entity.Property(e => e.AvailableFrom).HasColumnName("available_from");
                entity.Property(e => e.AvailableUntil).HasColumnName("available_until");
                entity.Property(e => e.MaxQuantityPerUser).HasColumnName("max_quantity_per_user");
                entity.Property(e => e.TotalQuantityAvailable).HasColumnName("total_quantity_available");
                entity.Property(e => e.QuantitySold).HasColumnName("quantity_sold");
                entity.Property(e => e.ProductMetadata).HasColumnName("product_metadata");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                entity.Property(e => e.CreatedBy).HasColumnName("created_by");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
                entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => new { e.ProductType, e.Status });
                entity.HasIndex(e => new { e.IsActive, e.Status });
            });

            modelBuilder.Entity<ProductLink>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ProductId).HasColumnName("product_id");
                entity.Property(e => e.LinkedEntityType).IsRequired().HasMaxLength(50).HasColumnName("linked_entity_type");
                entity.Property(e => e.LinkedEntityId).HasColumnName("linked_entity_id");
                entity.Property(e => e.LinkMetadata).HasColumnName("link_metadata");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.HasIndex(e => new { e.ProductId, e.LinkedEntityType, e.LinkedEntityId }).IsUnique();
                entity.HasIndex(e => new { e.LinkedEntityType, e.LinkedEntityId });
                entity.HasOne(e => e.Product).WithMany(p => p.Links).HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<TransactionItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ItemType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(255);
                entity.Property(e => e.UnitPrice).IsRequired().HasColumnType("decimal(15,2)");
                entity.Property(e => e.TotalPrice).IsRequired().HasColumnType("decimal(15,2)");
                entity.HasIndex(e => e.TransactionId);
                entity.HasIndex(e => e.ProductId);
                entity.HasIndex(e => new { e.LinkedEntityType, e.LinkedEntityId });
                entity.HasOne(e => e.Transaction).WithMany(t => t.TransactionItems).HasForeignKey(e => e.TransactionId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId);
            });

            modelBuilder.Entity<PaymentAuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
                entity.Property(e => e.EntityType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Currency).HasMaxLength(3);
                entity.Property(e => e.IpAddress).HasMaxLength(45);
                entity.HasIndex(e => new { e.UserId, e.Action, e.CreatedAt });
                entity.HasIndex(e => new { e.EntityType, e.EntityId });
                entity.HasIndex(e => e.CreatedAt);
            });
        }
    }
}
