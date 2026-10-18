using FnBManager.Application.Ports;
using FnBManager.Data;
using FnBManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FnBManager.Infrastructure.Persistence.Repositories;

public class OrderRepository(AppDbContext db) : IOrderRepository
{
    public Task<List<Order>> GetAllWithMenuAsync() => db.Orders
        .Include(o => o.MenuItem)
        .OrderByDescending(o => o.Id)
        .ToListAsync();

    public Task<List<Order>> GetRecentWithMenuAsync(int take) => db.Orders
        .Include(o => o.MenuItem)
        .OrderByDescending(o => o.Id)
        .Take(take)
        .ToListAsync();

    public Task<int> CountActiveAsync() => db.Orders.CountAsync(o => o.Status == OrderStatus.New || o.Status == OrderStatus.Preparing);

    public async Task<BestSellingMenuItem?> GetBestSellingMenuItemAsync()
    {
        // Count quantities across all orders; the earliest inserted order wins ties.
        var winner = await db.Orders
            .GroupBy(o => o.MenuItemId)
            .Select(group => new
            {
                MenuItemId = group.Key,
                TotalQuantity = group.Sum(o => (long)o.Quantity),
                FirstOrderId = group.Min(o => o.Id)
            })
            .Join(db.MenuItems, item => item.MenuItemId, menu => menu.Id,
                (item, menu) => new { item.MenuItemId, menu.Name, item.TotalQuantity, item.FirstOrderId })
            .OrderByDescending(item => item.TotalQuantity)
            .ThenBy(item => item.FirstOrderId)
            .FirstOrDefaultAsync();

        return winner is null ? null : new BestSellingMenuItem(winner.MenuItemId, winner.Name, winner.TotalQuantity);
    }

    public async Task<(int Closed, int Total)> GetClosureCountsAsync()
    {
        var counts = await db.Orders
            .GroupBy(o => 1)
            .Select(group => new
            {
                Closed = group.Count(o => o.Status == OrderStatus.Closed),
                Total = group.Count()
            })
            .SingleOrDefaultAsync();

        return counts is null ? (0, 0) : (counts.Closed, counts.Total);
    }

    public Task<Order?> FindByIdAsync(int id) => db.Orders.FindAsync(id).AsTask();

    public async Task AddAsync(Order order)
    {
        db.Orders.Add(order);
        await db.SaveChangesAsync();
    }

    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
