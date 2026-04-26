using FnBManager.Models;

namespace FnBManager.Application.Ports;

public interface IInventoryRepository
{
    Task<List<InventoryItem>> GetAllAsync();
    Task<InventoryItem?> FindByIdAsync(int id);
    Task AddAsync(InventoryItem item);
    Task DeleteAsync(InventoryItem item);
    Task<int> CountAsync();
}
