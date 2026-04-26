using FnBManager.Application.Ports;
using FnBManager.Data;
using FnBManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FnBManager.Infrastructure.Persistence.Repositories;

public class InventoryRepository(AppDbContext db) : IInventoryRepository
{
    public Task<List<InventoryItem>> GetAllAsync() => db.InventoryItems.OrderByDescending(x => x.Id).ToListAsync();

    public Task<InventoryItem?> FindByIdAsync(int id) => db.InventoryItems.FindAsync(id).AsTask();

    public async Task AddAsync(InventoryItem item)
    {
        db.InventoryItems.Add(item);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(InventoryItem item)
    {
        db.InventoryItems.Remove(item);
        await db.SaveChangesAsync();
    }

    public Task<int> CountAsync() => db.InventoryItems.CountAsync();
}
