using AdPlay.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AdPlay.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<WebhookEvent> WebhookEvents => Set<WebhookEvent>();

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
    }
}