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

    public Task<Order?> FindByIdAsync(int id) => db.Orders.FindAsync(id).AsTask();

    public async Task AddAsync(Order order)
    {
        db.Orders.Add(order);
        await db.SaveChangesAsync();
    }

    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
