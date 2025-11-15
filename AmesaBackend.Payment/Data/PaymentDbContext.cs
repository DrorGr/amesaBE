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
                entity.HasOne(e => e.PaymentMethod).WithMany(pm => pm.Transactions).HasForeignKey(e => e.PaymentMethodId);
            });
        }
    }
}
