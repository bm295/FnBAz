using FnBManager.Models;

namespace FnBManager.Application.Ports;

public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage message);
    Task MoveToDeadLetterAsync(OutboxMessage message, string error);
}
