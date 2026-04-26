using FnBManager.Application.Ports;
using FnBManager.Data;
using FnBManager.Models;

namespace FnBManager.Infrastructure.Persistence.Repositories;

public class OutboxRepository(AppDbContext db) : IOutboxRepository
{
    public async Task AddAsync(OutboxMessage message)
    {
        db.OutboxMessages.Add(message);
        await db.SaveChangesAsync();
    }

    public async Task MoveToDeadLetterAsync(OutboxMessage message, string error)
    {
        message.Error = error;
        message.ProcessedAtUtc = DateTime.UtcNow;

        db.DeadLetterMessages.Add(new DeadLetterMessage
        {
            EventType = message.EventType,
            Payload = message.Payload,
            Error = error,
            FailedAtUtc = DateTime.UtcNow,
            OutboxMessageId = message.Id
        });

        await db.SaveChangesAsync();
    }
}
