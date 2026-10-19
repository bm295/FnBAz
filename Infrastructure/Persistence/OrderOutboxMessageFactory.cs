using System.Text.Json;
using FnBManager.Models;

namespace FnBManager.Infrastructure.Persistence;

internal static class OrderOutboxMessageFactory
{
    public static OutboxMessage Created(Order order) => new()
    {
        EventType = "order.created",
        Payload = JsonSerializer.Serialize(new
        {
            order.Id, order.TableNumber, order.MenuItemId,
            order.Quantity, order.Status, order.CreatedAtUtc
        }),
        OccurredAtUtc = DateTime.UtcNow
    };

    public static OutboxMessage StatusUpdated(Order order)
    {
        var now = DateTime.UtcNow;
        return new OutboxMessage
        {
            EventType = "order.status.updated",
            Payload = JsonSerializer.Serialize(new
            {
                order.Id, order.Status, UpdatedAtUtc = now
            }),
            OccurredAtUtc = now
        };
    }
}
