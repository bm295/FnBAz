using FnBManager.Application.Ports;
using FnBManager.Data;
using FnBManager.Models;

namespace FnBManager.Infrastructure.Persistence.Repositories;

public class DeadLetterRepository(AppDbContext db) : IDeadLetterRepository
{
    public async Task AddAsync(DeadLetterMessage message)
    {
        db.DeadLetterMessages.Add(message);
        await db.SaveChangesAsync();
    }
}
