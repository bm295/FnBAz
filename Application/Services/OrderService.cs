using System.Text.Json;
using FnBManager.Application.Ports;
using FnBManager.Models;

namespace FnBManager.Application.Services;

public class OrderService(IOrderRepository orderRepository, IOutboxRepository outboxRepository) : IOrderService
{
    public Task<List<Order>> GetAllAsync() => orderRepository.GetAllWithMenuAsync();

    public Task<List<Order>> GetRecentAsync(int take) => orderRepository.GetRecentWithMenuAsync(take);

    public Task<int> CountActiveAsync() => orderRepository.CountActiveAsync();

    public async Task CreateAsync(string tableNumber, int menuItemId, int quantity)
    {
        var order = new Order
        {
            TableNumber = tableNumber.Trim(),
            MenuItemId = menuItemId,
            Quantity = quantity,
            Status = OrderStatus.New,
            CreatedAtUtc = DateTime.UtcNow
        };

        await orderRepository.AddAsync(order);

        await outboxRepository.AddAsync(new OutboxMessage
        {
            EventType = "order.created",
            Payload = JsonSerializer.Serialize(new
            {
                order.Id,
                order.TableNumber,
                order.MenuItemId,
                order.Quantity,
                order.Status,
                order.CreatedAtUtc
            }),
            OccurredAtUtc = DateTime.UtcNow
        });
    }

    public async Task<bool> UpdateStatusAsync(int id, OrderStatus status)
    {
        var order = await orderRepository.FindByIdAsync(id);
        if (order is null)
        {
            return false;
        }

        order.Status = status;
        await orderRepository.SaveChangesAsync();

        await outboxRepository.AddAsync(new OutboxMessage
        {
            EventType = "order.status.updated",
            Payload = JsonSerializer.Serialize(new
            {
                order.Id,
                order.Status,
                UpdatedAtUtc = DateTime.UtcNow
            }),
            OccurredAtUtc = DateTime.UtcNow
        });

        return true;
    }
}
