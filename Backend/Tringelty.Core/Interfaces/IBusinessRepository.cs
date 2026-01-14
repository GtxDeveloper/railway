using Tringelty.Core.DTOs;
using Tringelty.Core.Entities;

namespace Tringelty.Core.Interfaces;

public interface IBusinessRepository
{
    Task AddAsync(Business business);
    Task AddWorkerAsync(Worker worker); // Добавим сразу и для воркера
    Task SaveChangesAsync();
    
    Task<Worker?> GetWorkerByStripeIdAsync(string stripeAccountId);
    
    Task<Worker?> GetWorkerByIdAsync(Guid id);
    
    Task<Business?> GetBusinessByOwnerIdAsync(Guid ownerId);
    
    Task<Business?> GetBusinessByIdAsync(Guid businessId);
    
    Task AddTransactionAsync(Transaction transaction);
    
    Task<List<Transaction>> GetTransactionsByBusinessIdAsync(Guid businessId);
    
    Task<AdminStatsDto> GetGlobalStatsAsync();
    Task<List<AdminTransactionDto>> GetGlobalTransactionsAsync(int take = 20);
    
    void UpdateWorker(Worker worker);
    
    Task<List<Transaction>> GetTransactionsByWorkerIdAsync(Guid workerId);
    
    Task<Worker?> GetWorkerByLinkedUserIdAsync(string linkedUserId);
    
    Task<List<Business>> GetAllWithDetailsAsync();
    
    Task DeleteWorkerAsync(Worker worker);
}