using Tringelty.Core.DTOs;

namespace Tringelty.Core.Interfaces;

public interface IAdminService
{
    Task<AdminStatsDto> GetStatsAsync();
    Task<List<AdminTransactionDto>> GetRecentTransactionsAsync();
    
    Task<List<AdminBusinessDto>> GetAllBusinessesWithWorkersAsync();
    
    Task<byte[]> GenerateWorkerQrAnyAsync(Guid workerId);
    Task<string> GeneratePayLinkAnyAsync(Guid workerId);
}