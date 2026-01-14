using Microsoft.AspNetCore.Identity;
using Tringelty.Core.DTOs;
using Tringelty.Core.Entities;
using Tringelty.Core.Interfaces;

namespace Tringelty.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly IBusinessRepository _repository;
    private readonly IStripeService _stripeService;
    private readonly IImageService _imageService;
    private readonly IWorkerInvitationRepository _invitationRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    public DashboardService(
        IBusinessRepository repository, 
        IStripeService stripeService,
        IImageService imageService,
        IWorkerInvitationRepository invitationRepository,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        _repository = repository;
        _stripeService = stripeService;
        _imageService = imageService;
        _invitationRepository = invitationRepository;
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task<DashboardSummaryDto> GetOwnerSummaryAsync(Guid userId)
    {
        // 1. Получаем бизнес владельца
        var business = await _repository.GetBusinessByOwnerIdAsync(userId);
        if (business == null) return new DashboardSummaryDto();

        // 2. Получаем все транзакции этого бизнеса
        var transactions = await _repository.GetTransactionsByBusinessIdAsync(business.Id);

        // 3. Считаем статистику в памяти (для MVP это нормально)
        var now = DateTime.UtcNow;
        var today = now.Date;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);

        return new DashboardSummaryDto
        {
            // Считаем WorkerAmount (чистые деньги официантов)
            TotalEarnings = transactions.Sum(t => t.WorkerAmount),
            MonthEarnings = transactions.Where(t => t.CreatedAt >= startOfMonth).Sum(t => t.WorkerAmount),
            TodayEarnings = transactions.Where(t => t.CreatedAt >= today).Sum(t => t.WorkerAmount),
            TransactionsCount = transactions.Count
        };
    }
    
    public async Task<List<TransactionDto>> GetWorkerTransactionsAsync(Guid workerId)
    {
        // 1. Получаем данные из репозитория
        var transactions = await _repository.GetTransactionsByWorkerIdAsync(workerId);

        // 2. Превращаем (маппим) Transaction -> TransactionDto
        return transactions.Select(t => new TransactionDto
        {
            Id = t.Id,
            Amount = t.Amount,
            WorkerAmount = t.WorkerAmount,
            Currency = t.Currency,
            CreatedAt = t.CreatedAt
        }).ToList();
    }
    
    public async Task<BalanceDto> GetWorkerBalanceAsync(Guid workerId)
    {
        // 1. Находим работника в БД, чтобы получить его StripeAccountId
        var worker = await _repository.GetWorkerByIdAsync(workerId);
        
        if (worker == null) 
            throw new Exception("Worker not found");

        if (string.IsNullOrEmpty(worker.StripeAccountId))
            throw new Exception("Stripe wallet not connected");

        // 2. Обращаемся к StripeService (который мы писали в прошлых шагах)
        // Он сделает запрос к API Stripe
        return await _stripeService.GetWorkerBalanceAsync(worker.StripeAccountId);
    }
    
    // 1. Загрузка Логотипа Бизнеса
    public async Task<string> UploadBusinessAvatarAsync(Guid ownerId, Stream fileStream, string fileName)
    {
        // Ищем бизнес текущего владельца
        var business = await _repository.GetBusinessByOwnerIdAsync(ownerId);
        if (business == null) throw new Exception("Business not found");

        // Загружаем картинку
        var url = await _imageService.UploadImageAsync(fileStream, fileName);

        // Обновляем ссылку
        business.AvatarUrl = url;
        await _repository.SaveChangesAsync(); // Или UpdateAsync

        return url;
    }

    // 2. Загрузка Аватара Работника
    // 1. Переименовали ownerId -> currentUserId, так как загружать может и сам работник
    public async Task<string> UploadWorkerAvatarAsync(Guid currentUserId, Guid workerId, Stream fileStream, string fileName)
    {
        // 1. Получаем воркера
        var worker = await _repository.GetWorkerByIdAsync(workerId);
    
        if (worker == null) 
            throw new KeyNotFoundException("Worker not found");

        // 2. ПРОВЕРКА ПРАВ
        bool isOwner = worker.Business != null && worker.Business.OwnerId == currentUserId;
        bool isSelf = worker.LinkedUserId == currentUserId.ToString();

        if (!isOwner && !isSelf) 
            throw new UnauthorizedAccessException("You cannot upload avatar for this worker");

        // 3. Загружаем картинку в облако/хранилище
        var url = await _imageService.UploadImageAsync(fileStream, fileName);

        // 4. Обновляем URL у WORKER
        worker.AvatarUrl = url;

        // 5. СИНХРОНИЗАЦИЯ: Обновляем URL у USER (Global Account)
        if (!string.IsNullOrEmpty(worker.LinkedUserId))
        {
            var user = await _userManager.FindByIdAsync(worker.LinkedUserId);
        
            if (user != null)
            {
                // Предполагаем, что у вас в ApplicationUser есть поле AvatarUrl
                user.AvatarUrl = url; 
            
                var result = await _userManager.UpdateAsync(user);
            
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new Exception($"Failed to update linked user avatar: {errors}");
                }
            }
        }

        // 6. Сохраняем изменения WORKER
        _repository.UpdateWorker(worker); 
        await _repository.SaveChangesAsync();
    
        return url;
    }
    
    public async Task<BusinessDto?> GetBusinessProfileAsync(Guid ownerId)
    {
        // Предполагаем, что у вас в репозитории есть метод GetByOwnerIdAsync
        // Важно: убедитесь, что в репозитории есть .Include(b => b.Workers)
        var business = await _repository.GetBusinessByOwnerIdAsync(ownerId);

        if (business == null)
            return null;

        return new BusinessDto
        {
            Id = business.Id,
            Name = business.Name,       // BrandName
            LogoUrl = business.AvatarUrl, 
        };
    }
    
    public async Task UpdateWorkerAsync(Guid currentUserId, Guid workerId, UpdateWorkerDto dto)
    {
        // 1. Получаем воркера
        var worker = await _repository.GetWorkerByIdAsync(workerId);

        if (worker == null)
            throw new KeyNotFoundException("Worker not found");

        // 2. ПРОВЕРКА ПРАВ
        bool isOwner = worker.Business != null && worker.Business.OwnerId == currentUserId;
        bool isSelf = worker.LinkedUserId == currentUserId.ToString();

        if (!isOwner && !isSelf)
            throw new UnauthorizedAccessException("Access denied: You cannot edit this worker profile.");

        // 3. Обновляем сущность WORKER (локально в бизнесе)
        worker.Name = $"{dto.FirstName} {dto.LastName}".Trim();
        worker.Job = dto.Job; 

        // 4. СИНХРОНИЗАЦИЯ: Обновляем сущность USER (глобальный аккаунт)
        // Проверяем, привязан ли к этому воркеру реальный пользователь
        if (!string.IsNullOrEmpty(worker.LinkedUserId))
        {
            var user = await _userManager.FindByIdAsync(worker.LinkedUserId);
        
            // Если пользователь найден, обновляем его данные
            if (user != null)
            {
                user.FirstName = dto.FirstName;
                user.LastName = dto.LastName;
            
                // Сохраняем изменения пользователя через UserManager
                var result = await _userManager.UpdateAsync(user);
            
                if (!result.Succeeded)
                {
                    // Если не удалось обновить User, можно выбросить ошибку или залогировать это.
                    // Лучше выбросить ошибку, чтобы фронтенд знал, что что-то пошло не так.
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new Exception($"Failed to update linked user profile: {errors}");
                }
            }
        }

        // 5. Сохраняем изменения WORKER
        _repository.UpdateWorker(worker);
        await _repository.SaveChangesAsync();
    }
    
    public async Task<string> GenerateInviteLinkAsync(Guid ownerId, Guid workerId)
    {
        var worker = await _repository.GetWorkerByIdAsync(workerId);
        if (worker == null) throw new KeyNotFoundException("Worker not found");
    
        // Проверки...
        if (worker.Business.OwnerId != ownerId) 
            throw new UnauthorizedAccessException("Not your worker");
        if (!string.IsNullOrEmpty(worker.LinkedUserId))
            throw new InvalidOperationException("Worker is already linked");

        var token = Guid.NewGuid().ToString("N");
    
        var invite = new WorkerInvitation
        {
            WorkerId = workerId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        
        await _invitationRepository.AddAsync(invite);

        var frontendUrl = _configuration["AppSettings:FrontendUrl"];
        
        
        return $"{frontendUrl}/invite/{token}";
    }
    
    public async Task AcceptInviteAsync(string userId, string token)
    {
        // БЫЛО: _context.WorkerInvitations...
        // СТАЛО:
        var invite = await _invitationRepository.GetByTokenAsync(token);

        if (invite == null) throw new Exception("Invalid invite link");
        // ... проверки IsUsed, ExpiresAt ...

        var worker = invite.Worker; // Worker уже подгружен благодаря Include в репозитории
    
        // Линкуем
        worker.LinkedUserId = userId;
    
        // Помечаем инвайт использованным
        invite.IsUsed = true;

        // Сохраняем изменения (и в воркере, и в инвайте)
        await _invitationRepository.SaveChangesAsync();
    }
    
    public async Task<DashboardSummaryDto> GetWorkerSummaryAsync(Guid workerId, Guid currentUserId)
    {
        // 1. Получаем работника для проверки прав
        // (Убедитесь, что репозиторий делает .Include(w => w.Business))
        var worker = await _repository.GetWorkerByIdAsync(workerId);
    
        if (worker == null)
            throw new KeyNotFoundException("Worker not found");

        // 2. ПРОВЕРКА ПРАВ: Владелец ИЛИ Сам работник
        bool isOwner = worker.Business != null && worker.Business.OwnerId == currentUserId;
        bool isSelf = worker.LinkedUserId == currentUserId.ToString();

        if (!isOwner && !isSelf)
            throw new UnauthorizedAccessException("Access denied: You cannot view this summary.");

        // 3. Получаем транзакции работника
        var transactions = await _repository.GetTransactionsByWorkerIdAsync(workerId);

        // 4. Считаем статистику
        var now = DateTime.UtcNow;
        var today = now.Date;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);

        return new DashboardSummaryDto
        {
            // Считаем только WorkerAmount (чаевые работника)
            TotalEarnings = transactions.Sum(t => t.WorkerAmount),
        
            MonthEarnings = transactions
                .Where(t => t.CreatedAt >= startOfMonth)
                .Sum(t => t.WorkerAmount),
            
            TodayEarnings = transactions
                .Where(t => t.CreatedAt >= today)
                .Sum(t => t.WorkerAmount),
            
            TransactionsCount = transactions.Count
        };
    }
    
    public async Task DeleteWorkerAsync(Guid ownerId, Guid workerId)
    {
        // 1. Ищем работника
        var worker = await _repository.GetWorkerByIdAsync(workerId);
        if (worker == null)
        {
            throw new KeyNotFoundException("Worker not found");
        }

        // 2. Ищем бизнес этого работника, чтобы проверить владельца
        var business = await _repository.GetBusinessByIdAsync(worker.BusinessId);
    
        // 3. Проверяем права: действительно ли текущий юзер владеет этим бизнесом
        if (business == null || business.OwnerId != ownerId)
        {
            throw new UnauthorizedAccessException("You can only delete workers from your own business.");
        }

        // 4. Защита: Нельзя удалить самого себя (Владельца), если он записан как Worker
        // Или если worker.Job == "Owner"
        if (worker.Job == "Owner" || worker.LinkedUserId == ownerId.ToString())
        {
            throw new ArgumentException("Cannot delete the Business Owner from workers list.");
        }

        // 5. Удаляем
        await _repository.DeleteWorkerAsync(worker);
        await _repository.SaveChangesAsync();
    }
}