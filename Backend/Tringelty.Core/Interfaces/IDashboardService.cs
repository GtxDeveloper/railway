using Tringelty.Core.DTOs;

namespace Tringelty.Core.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetOwnerSummaryAsync(Guid userId);
    
    Task<BalanceDto> GetWorkerBalanceAsync(Guid workerId);
    
    Task<List<TransactionDto>> GetWorkerTransactionsAsync(Guid workerId);
    
    Task<string> UploadBusinessAvatarAsync(Guid ownerId, Stream fileStream, string fileName);
    Task<string> UploadWorkerAvatarAsync(Guid ownerId, Guid workerId, Stream fileStream, string fileName);
    Task<BusinessDto?> GetBusinessProfileAsync(Guid ownerId);
    
    Task UpdateWorkerAsync(Guid ownerId, Guid workerId, UpdateWorkerDto dto);

    Task<string> GenerateInviteLinkAsync(Guid ownerId, Guid workerId);

    Task AcceptInviteAsync(string userId, string token);
    
    Task<DashboardSummaryDto> GetWorkerSummaryAsync(Guid workerId, Guid currentUserId);
    
    Task DeleteWorkerAsync(Guid ownerId, Guid workerId);
}