using AdPlay.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AdPlay.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<WebhookEvent> WebhookEvents => Set<WebhookEvent>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.Property(x => x.Mobile).HasMaxLength(20).IsRequired();
            // one user per mobile, enforced by the database
            e.HasIndex(x => x.Mobile).IsUnique();
        });

        b.Entity<Payment>(e =>
        {
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.IdempotencyKey).HasMaxLength(100).IsRequired();
            e.Property(x => x.RequestHash).HasMaxLength(64).IsRequired();
            e.Property(x => x.OrderRef).HasMaxLength(100).IsRequired();
            e.Property(x => x.ActiveOrderRef).HasMaxLength(100);
            e.Property(x => x.Amount).HasPrecision(12, 2);
            e.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.ProviderRef).HasMaxLength(100);
            e.Property(x => x.FailureReason).HasMaxLength(200);

            e.HasIndex(x => x.IdempotencyKey).IsUnique();
            e.HasIndex(x => x.ActiveOrderRef).IsUnique(); // MySQL allows many NULLs here
        });

        b.Entity<WebhookEvent>(e =>
        {
            e.Property(x => x.EventId).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.EventId).IsUnique();
        });

        b.Entity<Category>(e => e.Property(x => x.Name).HasMaxLength(100).IsRequired());
        b.Entity<Brand>(e => e.Property(x => x.Name).HasMaxLength(100).IsRequired());

        b.Entity<Product>(e =>
        {
            e.ToTable("CatalogProducts");
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Price).HasPrecision(12, 2);

            e.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId);
            e.HasOne(x => x.Brand).WithMany().HasForeignKey(x => x.BrandId);

            e.HasIndex(x => x.Name);
            e.HasIndex(x => x.Price);
            e.HasIndex(x => x.CreatedAt);
            e.HasIndex(x => new { x.CategoryId, x.Price });
            e.HasIndex(x => new { x.BrandId, x.Price });
        });
    }
}