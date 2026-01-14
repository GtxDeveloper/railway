using Microsoft.EntityFrameworkCore;
using Tringelty.Core.Entities;
using Tringelty.Core.Interfaces;

namespace Tringelty.Infrastructure.Data.Repositories;

public class WorkerInvitationRepository : IWorkerInvitationRepository
{
    private readonly AppDbContext _context;

    public WorkerInvitationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(WorkerInvitation invitation)
    {
        await _context.WorkerInvitations.AddAsync(invitation);
        await _context.SaveChangesAsync();
    }

    public async Task<WorkerInvitation?> GetByTokenAsync(string token)
    {
        return await _context.WorkerInvitations
            .Include(i => i.Worker) // <--- ВАЖНО: Грузим связанного воркера
            .ThenInclude(w => w.Business) // Опционально, если нужно проверять владельца при активации
            .FirstOrDefaultAsync(i => i.Token == token);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}