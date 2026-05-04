using FnBManager.Application.Ports;
using FnBManager.Models;

namespace FnBManager.Application.Services;

public class MenuService(IMenuRepository menuRepository) : IMenuService
{
    public Task<List<MenuItem>> GetAllAsync() => menuRepository.GetAllAsync();

    public Task<List<MenuItem>> GetAvailableAsync() => menuRepository.GetAvailableAsync();

    public Task<int> CountAsync() => menuRepository.CountAsync();

    public Task CreateAsync(MenuItem item) => menuRepository.AddAsync(item);

    public async Task<bool> MarkUnavailableAsync(int id)
    {
        var item = await menuRepository.FindByIdAsync(id);
        if (item is null)
        {
            return false;
        }

        await menuRepository.MarkUnavailableAsync(item);
        return true;
    }
}
