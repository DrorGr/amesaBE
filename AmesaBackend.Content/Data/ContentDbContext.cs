using Microsoft.EntityFrameworkCore;
using AmesaBackend.Content.Models;

namespace AmesaBackend.Content.Data
{
    public class ContentDbContext : DbContext
    {
        public ContentDbContext(DbContextOptions<ContentDbContext> options) : base(options)
        {
        }

        public DbSet<Translation> Translations { get; set; }
        public DbSet<Language> Languages { get; set; }
        public DbSet<Models.Content> Contents { get; set; }
        public DbSet<ContentCategory> ContentCategories { get; set; }
        public DbSet<ContentMedia> ContentMedia { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Translation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.LanguageCode, e.Key }).IsUnique();
                entity.Property(e => e.LanguageCode).IsRequired().HasMaxLength(10);
                entity.Property(e => e.Key).IsRequired().HasMaxLength(255);
                entity.HasOne(e => e.Language)
                      .WithMany(l => l.Translations)
                      .HasForeignKey(e => e.LanguageCode)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Language>(entity =>
            {
                entity.HasKey(e => e.Code);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(10);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Code).IsUnique();
            });

            modelBuilder.Entity<Models.Content>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Slug).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Status).HasConversion<string>();
                entity.HasOne(e => e.Category).WithMany().HasForeignKey(e => e.CategoryId);
            });

            modelBuilder.Entity<ContentCategory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Slug).IsRequired().HasMaxLength(100);
                entity.HasOne(e => e.Parent).WithMany(e => e.Children).HasForeignKey(e => e.ParentId);
            });

            modelBuilder.Entity<ContentMedia>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MediaUrl).IsRequired();
                entity.Property(e => e.MediaType).HasConversion<string>();
                entity.HasOne(e => e.Content).WithMany(c => c.Media).HasForeignKey(e => e.ContentId);
            });
        }
    }
}

