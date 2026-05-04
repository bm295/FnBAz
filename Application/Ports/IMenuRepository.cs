using FnBManager.Models;

namespace FnBManager.Application.Ports;

public interface IMenuRepository
{
    Task<List<MenuItem>> GetAllAsync();
    Task<MenuItem?> FindByIdAsync(int id);
    Task AddAsync(MenuItem item);
    Task MarkUnavailableAsync(MenuItem item);
    Task<int> CountAsync();
    Task<List<MenuItem>> GetAvailableAsync();
}
