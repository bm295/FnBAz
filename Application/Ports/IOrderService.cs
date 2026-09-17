using FnBManager.Models;

namespace FnBManager.Application.Ports;

public interface IOrderService
{
    Task<List<Order>> GetAllAsync();
    Task<List<Order>> GetRecentAsync(int take);
    Task<int> CountActiveAsync();
    Task<OrderCreationResult> CreateAsync(string tableNumber, int menuItemId, int quantity, string? requestKey = null);
    Task<OrderStatusUpdateResult> UpdateStatusAsync(int id, OrderStatus expectedStatus, OrderStatus status);
}

public sealed record OrderCreationResult(int OrderId, bool Replayed, bool RequestKeyConflict = false);

public enum OrderStatusUpdateResult
{
    Updated,
    Replayed,
    NotFound,
    Conflict
}
