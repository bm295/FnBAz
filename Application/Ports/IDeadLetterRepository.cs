using FnBManager.Models;

namespace FnBManager.Application.Ports;

public interface IDeadLetterRepository
{
    Task AddAsync(DeadLetterMessage message);
}
