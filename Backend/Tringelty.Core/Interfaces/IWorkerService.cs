using Tringelty.Core.DTOs;

namespace Tringelty.Core.Interfaces;

public interface IWorkerService
{
    Task<WorkerDto> CreateWorkerAsync(CreateWorkerDto dto, Guid ownerId);
    Task<List<WorkerDto>> GetWorkersByOwnerAsync(Guid ownerId);
    Task<string> OnboardWorkerAsync(Guid workerId, Guid ownerId);
    Task<byte[]> GenerateWorkerQrAsync(Guid workerId, Guid ownerId);
    Task<string> GeneratePayLinkAsync(Guid workerId, Guid ownerId);
    Task ChangeJobAsync(Guid workerId, string newJob);
    
    Task<string> CreateLoginLinkAsync(Guid workerId);
    
    Task<WorkerDto> GetWorkerByIdAsync(Guid workerId, Guid currentUserId);
    
    Task<PublicWorkerDto> GetPublicWorkerInfoAsync(Guid workerId);
}