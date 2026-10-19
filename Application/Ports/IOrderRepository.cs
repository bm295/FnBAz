using FnBManager.Models;

namespace FnBManager.Application.Ports;

/// <summary>Persists order changes and their outbox messages atomically.</summary>
public interface IOrderRepository
{
    Task<OrderCreationResult> CreateAsync(Order order);

    /// <summary>Loads an order for modification within this scoped repository.</summary>
    Task<Order?> FindByIdAsync(int id);

    /// <summary>
    /// Saves an order loaded by this repository, including its status event.
    /// Maps optimistic concurrency failures to the current write outcome.
    /// </summary>
    Task<OrderStatusUpdateResult> SaveStatusAsync(Order order);
}
