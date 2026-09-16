using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using FnBManager.Application.Ports;
using FnBManager.Data;
using FnBManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FnBManager.Application.Services;

public class OrderService(IOrderRepository orderRepository, IOutboxRepository outboxRepository, AppDbContext db) : IOrderService
{
    public Task<List<Order>> GetAllAsync() => orderRepository.GetAllWithMenuAsync();

    public Task<List<Order>> GetRecentAsync(int take) => orderRepository.GetRecentWithMenuAsync(take);

    public Task<int> CountActiveAsync() => orderRepository.CountActiveAsync();

    public async Task<OrderCreationResult> CreateAsync(
        string tableNumber, int menuItemId, int quantity, string? requestKey = null)
    {
        var normalizedTableNumber = tableNumber.Trim();
        var requestHash = CreateRequestHash(normalizedTableNumber, menuItemId, quantity);
        var existing = requestKey is null
            ? null
            : await db.Orders.AsNoTracking().SingleOrDefaultAsync(x => x.RequestKey == requestKey);

        if (existing is not null)
        {
            return new OrderCreationResult(
                existing.Id,
                Replayed: true,
                RequestKeyConflict: !RequestMatches(existing, requestHash));
        }

        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var transaction = await db.Database.BeginTransactionAsync();
                var order = new Order
                {
                    TableNumber = normalizedTableNumber,
                    MenuItemId = menuItemId,
                    Quantity = quantity,
                    Status = OrderStatus.New,
                    RequestKey = requestKey,
                    RequestHash = requestKey is null ? null : requestHash,
                    CreatedAtUtc = DateTime.UtcNow
                };

                db.Orders.Add(order);
                await db.SaveChangesAsync();
                db.OutboxMessages.Add(new OutboxMessage
                {
                    EventType = "order.created",
                    Payload = JsonSerializer.Serialize(new { order.Id, order.TableNumber, order.MenuItemId, order.Quantity, order.Status, order.CreatedAtUtc }),
                    OccurredAtUtc = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
                await transaction.CommitAsync();
                return new OrderCreationResult(order.Id, Replayed: false);
            });
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            if (requestKey is null)
            {
                throw;
            }

            existing = await db.Orders.AsNoTracking()
                .SingleOrDefaultAsync(x => x.RequestKey == requestKey);
            if (existing is null)
            {
                throw;
            }

            return new OrderCreationResult(
                existing.Id,
                Replayed: true,
                RequestKeyConflict: !RequestMatches(existing, requestHash));
        }
    }

    private static string CreateRequestHash(string tableNumber, int menuItemId, int quantity)
    {
        var payload = $"{tableNumber}\n{menuItemId}\n{quantity}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }

    private static bool RequestMatches(Order order, string requestHash)
    {
        return order.RequestHash is not null && CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(order.RequestHash), Convert.FromHexString(requestHash));
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
