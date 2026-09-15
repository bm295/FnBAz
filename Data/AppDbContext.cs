using FnBManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FnBManager.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderIdempotencyRecord> OrderIdempotencyRecords => Set<OrderIdempotencyRecord>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<DeadLetterMessage> DeadLetterMessages => Set<DeadLetterMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MenuItem>()
            .Property(x => x.Price)
            .HasPrecision(18, 2);

        modelBuilder.Entity<InventoryItem>()
            .Property(x => x.Quantity)
            .HasPrecision(18, 2);

        modelBuilder.Entity<InventoryItem>()
            .Property(x => x.ReorderLevel)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.MenuItem)
            .WithMany()
            .HasForeignKey(o => o.MenuItemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InboxMessage>()
            .HasIndex(x => x.ExternalMessageId)
            .IsUnique();

        modelBuilder.Entity<OrderIdempotencyRecord>()
            .HasIndex(x => x.Key)
            .IsUnique();

        modelBuilder.Entity<DeadLetterMessage>()
            .HasIndex(x => x.OutboxMessageId);
    }
}
