using FnBManager.Models;

namespace FnBManager.Application.Ports;

public interface IOrderService
{
    Task<List<Order>> GetAllAsync();
    Task<List<Order>> GetRecentAsync(int take);
    Task<int> CountActiveAsync();
    Task CreateAsync(string tableNumber, int menuItemId, int quantity);
    Task<bool> UpdateStatusAsync(int id, OrderStatus status);
}
