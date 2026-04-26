using FnBManager.Application.Ports;
using FnBManager.Models;

namespace FnBManager.Application.Services;

public class InventoryService(IInventoryRepository inventoryRepository) : IInventoryService
{
    public Task<List<InventoryItem>> GetAllAsync() => inventoryRepository.GetAllAsync();

    public Task<int> CountAsync() => inventoryRepository.CountAsync();

    public Task CreateAsync(InventoryItem item) => inventoryRepository.AddAsync(item);

    public async Task<bool> DeleteAsync(int id)
    {
        var item = await inventoryRepository.FindByIdAsync(id);
        if (item is null)
        {
            return false;
        }

        await inventoryRepository.DeleteAsync(item);
        return true;
    }
}
