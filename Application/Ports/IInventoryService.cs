using FnBManager.Models;

namespace FnBManager.Application.Ports;

public interface IInventoryService
{
    Task<List<InventoryItem>> GetAllAsync();
    Task CreateAsync(InventoryItem item);
    Task<bool> DeleteAsync(int id);
    Task<int> CountAsync();
}
