using System.Text.Json;
using FnBManager.Application.Ports;
using FnBManager.Application.Services;
using FnBManager.Data;
using FnBManager.Infrastructure.Persistence.Repositories;
using FnBManager.Infrastructure.Persistence.Queries;
using FnBManager.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace FnBManager.Tests;

public sealed class OrderServiceTests
{
    [Fact]
    public async Task Create_replays_normalized_request_without_another_order_or_event()
    {
        using var connection = OpenDatabase();
        await using var db = await CreateContextAsync(connection);
        var service = CreateService(db);

        var created = await service.CreateAsync(" 12 ", 1, 2, "request-1");
        var replay = await service.CreateAsync("12", 1, 2, "request-1");
        var conflict = await service.CreateAsync("12", 1, 3, "request-1");

        Assert.False(created.Replayed);
        Assert.Equal(created.OrderId, replay.OrderId);
        Assert.True(replay.Replayed);
        Assert.False(replay.RequestKeyConflict);
        Assert.True(conflict.RequestKeyConflict);
        Assert.Equal(1, await db.Orders.CountAsync());
        var message = await db.OutboxMessages.SingleAsync();
        Assert.Equal("order.created", message.EventType);
        using var payload = JsonDocument.Parse(message.Payload);
        Assert.Equal(created.OrderId, payload.RootElement.GetProperty("Id").GetInt32());
        Assert.Equal("12", payload.RootElement.GetProperty("TableNumber").GetString());
        Assert.Equal(2, payload.RootElement.GetProperty("Quantity").GetInt32());
    }

    [Fact]
    public async Task Create_without_request_key_does_not_deduplicate()
    {
        using var connection = OpenDatabase();
        await using var db = await CreateContextAsync(connection);
        var service = CreateService(db);
        await service.CreateAsync("12", 1, 2);
        await service.CreateAsync("12", 1, 2);
        Assert.Equal(2, await db.Orders.CountAsync());
        Assert.Equal(2, await db.OutboxMessages.CountAsync());
    }

    [Fact]
    public async Task Outbox_failure_rolls_back_created_order()
    {
        using var connection = OpenDatabase();
        await using (var setup = await CreateContextAsync(connection)) { }
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection).AddInterceptors(new RejectOutboxSave()).Options;
        await using (var failing = new AppDbContext(options))
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => CreateService(failing).CreateAsync("12", 1, 2, "rollback"));
        }
        await using var verify = new AppDbContext(Options(connection));
        Assert.Equal(0, await verify.Orders.CountAsync());
        Assert.Equal(0, await verify.OutboxMessages.CountAsync());
    }

    [Fact]
    public async Task Status_replay_stale_request_and_missing_order_do_not_emit_events()
    {
        using var connection = OpenDatabase();
        await using var db = await CreateContextAsync(connection);
        var service = CreateService(db);
        var created = await service.CreateAsync("12", 1, 2, "status");
        Assert.Equal(OrderStatusUpdateResult.Updated,
            await service.UpdateStatusAsync(created.OrderId, OrderStatus.New, OrderStatus.Preparing));
        Assert.Equal(OrderStatusUpdateResult.Replayed,
            await service.UpdateStatusAsync(created.OrderId, OrderStatus.New, OrderStatus.Preparing));
        Assert.Equal(OrderStatusUpdateResult.Conflict,
            await service.UpdateStatusAsync(created.OrderId, OrderStatus.New, OrderStatus.Served));
        Assert.Equal(OrderStatusUpdateResult.NotFound,
            await service.UpdateStatusAsync(999, OrderStatus.New, OrderStatus.Preparing));
        Assert.Equal(2, await db.OutboxMessages.CountAsync());
        var message = await db.OutboxMessages.SingleAsync(x => x.EventType == "order.status.updated");
        using var payload = JsonDocument.Parse(message.Payload);
        Assert.Equal((int)OrderStatus.Preparing, payload.RootElement.GetProperty("Status").GetInt32());
    }

    [Theory]
    [InlineData(OrderStatus.Served, OrderStatusUpdateResult.Conflict)]
    [InlineData(OrderStatus.Preparing, OrderStatusUpdateResult.Replayed)]
    public async Task Concurrent_status_write_maps_outcome_and_rolls_back_losing_event(
        OrderStatus winningStatus, OrderStatusUpdateResult expected)
    {
        using var connection = OpenDatabase();
        await using var staleDb = await CreateContextAsync(connection);
        var created = await CreateService(staleDb).CreateAsync("12", 1, 2, "concurrent");
        var repository = new OrderRepository(staleDb);
        var stale = (await repository.FindByIdAsync(created.OrderId))!;
        await using (var winnerDb = new AppDbContext(Options(connection)))
        {
            await CreateService(winnerDb).UpdateStatusAsync(created.OrderId, OrderStatus.New, winningStatus);
        }
        stale.Status = OrderStatus.Preparing;
        Assert.Equal(expected, await repository.SaveStatusAsync(stale));
        Assert.Equal(2, await staleDb.OutboxMessages.CountAsync());
        Assert.Equal(winningStatus, (await staleDb.Orders.AsNoTracking().SingleAsync()).Status);
    }

    private static OrderService CreateService(AppDbContext db) =>
        new(new OrderQueries(db), new OrderRepository(db));

    private static SqliteConnection OpenDatabase()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    private static DbContextOptions<AppDbContext> Options(SqliteConnection connection) =>
        new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

    private static async Task<AppDbContext> CreateContextAsync(SqliteConnection connection)
    {
        var db = new AppDbContext(Options(connection));
        await db.Database.EnsureCreatedAsync();
        db.MenuItems.Add(new MenuItem { Id = 1, Name = "Coffee", Category = "Drink", Price = 10 });
        await db.SaveChangesAsync();
        return db;
    }

    private sealed class RejectOutboxSave : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<OutboxMessage>()
                .Any(x => x.State == EntityState.Added))
            {
                throw new InvalidOperationException("Simulated outbox failure.");
            }
            return ValueTask.FromResult(result);
        }
    }
}
