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
                entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.ProductType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.BasePrice).IsRequired().HasColumnType("decimal(15,2)");
                entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => new { e.ProductType, e.Status });
                entity.HasIndex(e => new { e.IsActive, e.Status });
            });

            modelBuilder.Entity<ProductLink>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.LinkedEntityType).IsRequired().HasMaxLength(50);
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
