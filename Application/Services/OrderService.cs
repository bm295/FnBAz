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

    public async Task<OrderCreationResult> CreateIdempotentAsync(
        string tableNumber, int menuItemId, int quantity, string idempotencyKey)
    {
        var normalizedTableNumber = tableNumber.Trim();
        var requestHash = CreateRequestHash(normalizedTableNumber, menuItemId, quantity);
        var existing = await db.OrderIdempotencyRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Key == idempotencyKey);

        if (existing is not null)
        {
            EnsureMatchingRequest(existing, requestHash);
            return new OrderCreationResult(existing.OrderId, Replayed: true);
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
                    CreatedAtUtc = DateTime.UtcNow
                };

                db.Orders.Add(order);
                db.OrderIdempotencyRecords.Add(new OrderIdempotencyRecord
                {
                    Key = idempotencyKey,
                    RequestHash = requestHash,
                    OrderId = order.Id
                });
                await db.SaveChangesAsync();

                // The order ID is generated above; keep the record and outbox event in this transaction.
                var record = db.OrderIdempotencyRecords.Local.Single(x => x.Key == idempotencyKey);
                record.OrderId = order.Id;
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
            existing = await db.OrderIdempotencyRecords.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Key == idempotencyKey);
            if (existing is null)
            {
                throw;
            }

            EnsureMatchingRequest(existing, requestHash);
            return new OrderCreationResult(existing.OrderId, Replayed: true);
        }
    }

    private static string CreateRequestHash(string tableNumber, int menuItemId, int quantity)
    {
        var payload = $"{tableNumber}\n{menuItemId}\n{quantity}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }

    private static void EnsureMatchingRequest(OrderIdempotencyRecord record, string requestHash)
    {
        if (!CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(record.RequestHash), Convert.FromHexString(requestHash)))
        {
            throw new IdempotencyKeyReuseException();
        }
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

public sealed class IdempotencyKeyReuseException : Exception
{
    public IdempotencyKeyReuseException() : base("Idempotency-Key was already used with a different request payload.") { }
}
