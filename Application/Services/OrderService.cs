using System.Security.Cryptography;
using System.Text;
using FnBManager.Application.Ports;
using FnBManager.Models;

namespace FnBManager.Application.Services;

public class OrderService(IOrderRepository orderRepository, IOrderWriteStore orderWriteStore) : IOrderService
{
    public Task<List<Order>> GetAllAsync() => orderRepository.GetAllWithMenuAsync();

    public Task<List<Order>> GetRecentAsync(int take) => orderRepository.GetRecentWithMenuAsync(take);

    public Task<int> CountActiveAsync() => orderRepository.CountActiveAsync();

    public Task<(int Closed, int Total)> GetClosureCountsAsync() => orderRepository.GetClosureCountsAsync();

    public Task<BestSellingMenuItem?> GetBestSellingMenuItemAsync() => orderRepository.GetBestSellingMenuItemAsync();

    public Task<OrderCreationResult> CreateAsync(
        string tableNumber, int menuItemId, int quantity, string? requestKey = null)
    {
        var normalizedTableNumber = tableNumber.Trim();
        var order = new Order
        {
            TableNumber = normalizedTableNumber,
            MenuItemId = menuItemId,
            Quantity = quantity,
            Status = OrderStatus.New,
            RequestKey = requestKey,
            RequestHash = requestKey is null
                ? null
                : CreateRequestHash(normalizedTableNumber, menuItemId, quantity),
            CreatedAtUtc = DateTime.UtcNow
        };

        return orderWriteStore.CreateAsync(order);
    }

    private static string CreateRequestHash(string tableNumber, int menuItemId, int quantity)
    {
        var payload = $"{tableNumber}\n{menuItemId}\n{quantity}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }

    public async Task<OrderStatusUpdateResult> UpdateStatusAsync(
        int id, OrderStatus expectedStatus, OrderStatus status)
    {
        var order = await orderWriteStore.FindByIdAsync(id);
        if (order is null)
        {
            return OrderStatusUpdateResult.NotFound;
        }

        if (order.Status == status)
        {
            return OrderStatusUpdateResult.Replayed;
        }

        if (order.Status != expectedStatus)
        {
            return OrderStatusUpdateResult.Conflict;
        }

        order.Status = status;
        return await orderWriteStore.SaveStatusAsync(order);
    }
}
