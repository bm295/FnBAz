using FnBManager.Models;

namespace FnBManager.Application.Ports;

public interface IMenuService
{
    Task<List<MenuItem>> GetAllAsync();
    Task CreateAsync(MenuItem item);
    Task<bool> MarkUnavailableAsync(int id);
    Task<List<MenuItem>> GetAvailableAsync();
    Task<int> CountAsync();
}
