using FnBManager.Application.Ports;
using FnBManager.Data;
using FnBManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FnBManager.Infrastructure.Persistence.Repositories;

public class MenuRepository(AppDbContext db) : IMenuRepository
{
    public Task<List<MenuItem>> GetAllAsync() => db.MenuItems.OrderByDescending(x => x.Id).ToListAsync();

    public Task<MenuItem?> FindByIdAsync(int id) => db.MenuItems.FindAsync(id).AsTask();

    public async Task AddAsync(MenuItem item)
    {
        db.MenuItems.Add(item);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(MenuItem item)
    {
        db.MenuItems.Remove(item);
        await db.SaveChangesAsync();
    }

    public Task<int> CountAsync() => db.MenuItems.CountAsync();

    public Task<List<MenuItem>> GetAvailableAsync() => db.MenuItems.Where(m => m.IsAvailable).OrderBy(m => m.Name).ToListAsync();
}
