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
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50).HasColumnName("type");
                entity.Property(e => e.Provider).HasMaxLength(50).HasColumnName("provider");
                entity.Property(e => e.ProviderPaymentMethodId).HasMaxLength(255).HasColumnName("provider_payment_method_id");
                entity.Property(e => e.CardLastFour).HasMaxLength(4).HasColumnName("card_last_four");
                entity.Property(e => e.CardBrand).HasMaxLength(50).HasColumnName("card_brand");
                entity.Property(e => e.CardExpMonth).HasColumnName("card_exp_month");
                entity.Property(e => e.CardExpYear).HasColumnName("card_exp_year");
                entity.Property(e => e.IsDefault).HasColumnName("is_default");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                entity.Property(e => e.WalletType).HasMaxLength(100).HasColumnName("wallet_type");
                entity.Property(e => e.WalletAccountId).HasMaxLength(255).HasColumnName("wallet_account_id");
                entity.Property(e => e.PaymentMethodMetadata).HasColumnName("payment_method_metadata");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
                entity.HasIndex(e => e.UserId);
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50).HasColumnName("type");
                entity.Property(e => e.Amount).IsRequired().HasColumnType("decimal(15,2)").HasColumnName("amount");
                entity.Property(e => e.Currency).HasMaxLength(3).HasColumnName("currency");
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50).HasColumnName("status");
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.ReferenceId).HasMaxLength(255).HasColumnName("reference_id");
                entity.Property(e => e.PaymentMethodId).HasColumnName("payment_method_id");
                entity.Property(e => e.ProviderTransactionId).HasMaxLength(255).HasColumnName("provider_transaction_id");
                entity.Property(e => e.ProductId).HasColumnName("product_id");
                entity.Property(e => e.IdempotencyKey).HasMaxLength(255).HasColumnName("idempotency_key");
                entity.Property(e => e.ClientSecret).HasMaxLength(255).HasColumnName("client_secret");
                entity.Property(e => e.IpAddress).HasMaxLength(45).HasColumnName("ip_address");
                entity.Property(e => e.UserAgent).HasColumnName("user_agent");
                entity.Property(e => e.ProviderResponse).HasColumnName("provider_response");
                entity.Property(e => e.Metadata).HasColumnName("metadata");
                entity.Property(e => e.ProcessedAt).HasColumnName("processed_at");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
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
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.TransactionId).IsRequired().HasColumnName("transaction_id");
                entity.Property(e => e.ProductId).HasColumnName("product_id");
                entity.Property(e => e.ItemType).IsRequired().HasMaxLength(50).HasColumnName("item_type");
                entity.Property(e => e.Description).IsRequired().HasMaxLength(255).HasColumnName("description");
                entity.Property(e => e.Quantity).IsRequired().HasColumnName("quantity");
                entity.Property(e => e.UnitPrice).IsRequired().HasColumnType("decimal(15,2)").HasColumnName("unit_price");
                entity.Property(e => e.TotalPrice).IsRequired().HasColumnType("decimal(15,2)").HasColumnName("total_price");
                entity.Property(e => e.LinkedEntityType).HasMaxLength(50).HasColumnName("linked_entity_type");
                entity.Property(e => e.LinkedEntityId).HasColumnName("linked_entity_id");
                entity.Property(e => e.ItemMetadata).HasColumnName("item_metadata");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.HasIndex(e => e.TransactionId);
                entity.HasIndex(e => e.ProductId);
                entity.HasIndex(e => new { e.LinkedEntityType, e.LinkedEntityId });
                entity.HasOne(e => e.Transaction).WithMany(t => t.TransactionItems).HasForeignKey(e => e.TransactionId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId);
            });

            modelBuilder.Entity<PaymentAuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId).IsRequired().HasColumnName("user_id");
                entity.Property(e => e.Action).IsRequired().HasMaxLength(50).HasColumnName("action");
                entity.Property(e => e.EntityType).IsRequired().HasMaxLength(50).HasColumnName("entity_type");
                entity.Property(e => e.EntityId).HasColumnName("entity_id");
                entity.Property(e => e.Amount).HasColumnType("decimal(15,2)").HasColumnName("amount");
                entity.Property(e => e.Currency).HasMaxLength(3).HasColumnName("currency");
                entity.Property(e => e.IpAddress).HasMaxLength(45).HasColumnName("ip_address");
                entity.Property(e => e.UserAgent).HasColumnName("user_agent");
                entity.Property(e => e.Metadata).HasColumnName("metadata");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.HasIndex(e => new { e.UserId, e.Action, e.CreatedAt });
                entity.HasIndex(e => new { e.EntityType, e.EntityId });
                entity.HasIndex(e => e.CreatedAt);
            });
        }
    }
}
