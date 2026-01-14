using Tringelty.Core.Entities;

namespace Tringelty.Core.Interfaces;

public interface IWorkerInvitationRepository
{
    // Добавить новый
    Task AddAsync(WorkerInvitation invitation);
    
    // Найти по токену (вместе с Worker)
    Task<WorkerInvitation?> GetByTokenAsync(string token);
    
    // Сохранить изменения (нужно для метода AcceptInvite, когда мы меняем флаг IsUsed)
    Task SaveChangesAsync();
}