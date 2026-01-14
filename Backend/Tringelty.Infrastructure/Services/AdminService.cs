using Microsoft.AspNetCore.Identity;
using Tringelty.Core.DTOs;
using Tringelty.Core.Entities;
using Tringelty.Core.Interfaces;

namespace Tringelty.Infrastructure.Services;

public class AdminService : IAdminService
{
    private readonly IBusinessRepository _repository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IQrCodeService _qrCodeService;
    private readonly IConfiguration _configuration;
    private string _baseAppUrl;
    
    public AdminService(IBusinessRepository repository, UserManager<ApplicationUser> userManager,IQrCodeService qrCodeService, IConfiguration configuration)
    {
        _repository = repository;
        _userManager = userManager;
        _qrCodeService = qrCodeService;
        _configuration = configuration;
        
        _baseAppUrl = _configuration["AppSettings:FrontendUrl"] ?? "http://localhost:4200";
    }

    public async Task<AdminStatsDto> GetStatsAsync()
    {
        return await _repository.GetGlobalStatsAsync();
    }

    public async Task<List<AdminTransactionDto>> GetRecentTransactionsAsync()
    {
        return await _repository.GetGlobalTransactionsAsync(50); // Берем последние 50
    }

    public async Task<List<AdminBusinessDto>> GetAllBusinessesWithWorkersAsync()
    {
        // 1. Загружаем бизнесы (без Owner, так как он грузится отдельно)
        var businesses = await _repository.GetAllWithDetailsAsync();

        // 2. Собираем ID владельцев
        // ВАЖНО: Identity User Id - это string, а Business.OwnerId - это Guid?
        // Нам нужно отфильтровать null и привести к string
        var ownerIds = businesses
            .Where(b => b.OwnerId.HasValue)
            .Select(b => b.OwnerId.Value.ToString())
            .Distinct()
            .ToList();

        // 3. Грузим пользователей через UserManager по списку ID
        var owners = _userManager.Users
            .Where(u => ownerIds.Contains(u.Id))
            .ToList();

        // Создаем словарь: Key=String(Id), Value=User
        var ownersDict = owners.ToDictionary(u => u.Id, u => u);

        // 4. Маппинг
        var result = businesses.Select(b =>
        {
            // Пытаемся найти владельца
            // Конвертируем Guid? в string для поиска в словаре
            ApplicationUser? owner = null;
            if (b.OwnerId.HasValue)
            {
                ownersDict.TryGetValue(b.OwnerId.Value.ToString(), out owner);
            }

            return new AdminBusinessDto
            {
                Id = b.Id,

                // Исправлено: берем Name из сущности Business
                BrandName = string.IsNullOrEmpty(b.Name) ? "No Brand" : b.Name,
                AvatarUrl = b.AvatarUrl,

                // Исправлено: City берем из Владельца (owner)
                City = owner?.City ?? "Unknown City",

                // OwnerId конвертируем обратно в Guid для DTO (если там Guid)
                OwnerId = b.OwnerId ?? Guid.Empty,

                OwnerEmail = owner?.Email ?? "No Email",
                OwnerName = owner != null
                    ? $"{owner.FirstName} {owner.LastName}".Trim()
                    : "Unknown Owner",

                // Работники
                Workers = b.Workers.Select(w => new AdminWorkerDetailDto
                {
                    Id = w.Id,
                    Name = w.Name,
                    Job = w.Job,
                    AvatarUrl = w.AvatarUrl,
                    StripeAccountId = w.StripeAccountId,
                    IsOnboarded = w.IsOnboarded,
                    IsLinked = w.IsLinked,
                    LinkedUserId = w.LinkedUserId
                }).ToList()
            };
        }).ToList();

        return result;
    }
    
    public async Task<byte[]> GenerateWorkerQrAnyAsync(Guid workerId)
    {
        // 1. Получаем воркера (нам не нужен OwnerId для проверки)
        // Если в BusinessRepository нет метода GetWorkerById, вам нужно его добавить
        // или внедрить IWorkerRepository в этот сервис.
        var worker = await _repository.GetWorkerByIdAsync(workerId);

        if (worker == null) 
            throw new KeyNotFoundException("Worker not found");

        // 2. Генерируем URL (Для админа проверки прав не нужны)
        var paymentUrl = $"{_baseAppUrl}/pay/{workerId}";

        // 3. Рисуем картинку
        return _qrCodeService.GenerateQrCode(paymentUrl);
    }

    public async Task<string> GeneratePayLinkAnyAsync(Guid workerId)
    {
        // 1. Получаем воркера
        var worker = await _repository.GetWorkerByIdAsync(workerId);

        if (worker == null) 
            throw new KeyNotFoundException("Worker not found");

        // 2. Возвращаем строку
        return $"{_baseAppUrl}/pay/{workerId}";
    }
}