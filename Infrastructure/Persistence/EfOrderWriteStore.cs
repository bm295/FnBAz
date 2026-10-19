using System.Security.Cryptography;
using FnBManager.Application.Ports;
using FnBManager.Data;
using FnBManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FnBManager.Infrastructure.Persistence;

public class EfOrderWriteStore(AppDbContext db) : IOrderWriteStore
{
    public Task<Order?> FindByIdAsync(int id) => db.Orders.FindAsync(id).AsTask();

    public async Task<OrderCreationResult> CreateAsync(Order order)
    {
        var requestKey = order.RequestKey;
        var requestHash = order.RequestHash!;
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
                var newOrder = new Order
                {
                    TableNumber = order.TableNumber,
                    MenuItemId = order.MenuItemId,
                    Quantity = order.Quantity,
                    Status = order.Status,
                    RequestKey = order.RequestKey,
                    RequestHash = order.RequestHash,
                    CreatedAtUtc = order.CreatedAtUtc
                };
                db.Orders.Add(newOrder);
                await db.SaveChangesAsync();
                db.OutboxMessages.Add(OrderOutboxMessageFactory.Created(newOrder));
                await db.SaveChangesAsync();
                await transaction.CommitAsync();
                return new OrderCreationResult(newOrder.Id, Replayed: false);
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

    private static bool RequestMatches(Order order, string requestHash)
    {
        return order.RequestHash is not null && CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(order.RequestHash), Convert.FromHexString(requestHash));
    }

    public async Task<OrderStatusUpdateResult> SaveStatusAsync(Order order)
    {
        var id = order.Id;
        var status = order.Status;
        db.OutboxMessages.Add(OrderOutboxMessageFactory.StatusUpdated(order));
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            db.ChangeTracker.Clear();
            var currentStatus = await db.Orders.AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => (OrderStatus?)x.Status)
                .SingleOrDefaultAsync();

            return currentStatus switch
            {
                null => OrderStatusUpdateResult.NotFound,
                var current when current == status => OrderStatusUpdateResult.Replayed,
                _ => OrderStatusUpdateResult.Conflict
            };
        }

        return OrderStatusUpdateResult.Updated;
    }
}
