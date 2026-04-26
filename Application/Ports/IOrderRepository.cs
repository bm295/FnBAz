using FnBManager.Models;

namespace FnBManager.Application.Ports;

public interface IOrderRepository
{
    Task<List<Order>> GetAllWithMenuAsync();
    Task<List<Order>> GetRecentWithMenuAsync(int take);
    Task<int> CountActiveAsync();
    Task<Order?> FindByIdAsync(int id);
    Task AddAsync(Order order);
    Task SaveChangesAsync();
}
