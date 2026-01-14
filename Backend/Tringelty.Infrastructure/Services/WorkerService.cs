using Tringelty.Core.DTOs;
using Tringelty.Core.Entities;
using Tringelty.Core.Interfaces;

namespace Tringelty.Infrastructure.Services;

public class WorkerService : IWorkerService
{
    private readonly IBusinessRepository _repository;
    private readonly IStripeService _stripeService;
    private readonly IQrCodeService _qrCodeService;
    private readonly IConfiguration _configuration;
    
    // 1. Объявляем просто как поле (не const)
    private readonly string _baseAppUrl;

    public WorkerService(
        IBusinessRepository repository, 
        IStripeService stripeService, 
        IQrCodeService qrCodeService,
        IConfiguration configuration)
    {
        _repository = repository;
        _stripeService = stripeService;
        _qrCodeService = qrCodeService;
        _configuration = configuration;
        
        _baseAppUrl = _configuration["AppSettings:FrontendUrl"] ?? "http://localhost:4200";
    }

    public async Task<WorkerDto> CreateWorkerAsync(CreateWorkerDto dto, Guid ownerId)
    {
        // 1. Получаем бизнес ВМЕСТЕ с сотрудниками
        var business = await _repository.GetBusinessByOwnerIdAsync(ownerId);
    
        if (business == null) 
            throw new KeyNotFoundException("Бизнес не найден.");

        // ---------------- ПРОВЕРКА 1: ДУБЛИКАТЫ ----------------
        // Проверяем, нет ли уже сотрудника с таким же именем (регистронезависимо)
        bool nameExists = business.Workers.Any(w => 
            w.Name.Equals(dto.Name.Trim(), StringComparison.OrdinalIgnoreCase));

        if (nameExists)
            throw new InvalidOperationException($"Сотрудник с именем '{dto.Name}' уже существует.");

        // ---------------- ПРОВЕРКА 2: ЛИМИТЫ (SaaS Логика) ----------------
        // Здесь должна быть ваша логика тарифов. Пока поставим хардкод, например 10.
        // В будущем: var limit = await _subscriptionService.GetWorkerLimit(business.Id);
        const int MaxWorkersLimit = 10; 

        if (business.Workers.Count >= MaxWorkersLimit)
        {
            throw new InvalidOperationException($"Вы достигли лимита сотрудников ({MaxWorkersLimit}). Обновите тариф.");
        }

        // ---------------- СОЗДАНИЕ ----------------
        var worker = new Worker
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(), // Убираем лишние пробелы
            Job = dto.Job.Trim(),
            BusinessId = business.Id,
            IsOnboarded = false
            // LinkedUserId оставляем пустым, он заполнится при приглашении
        };

        await _repository.AddWorkerAsync(worker);
        await _repository.SaveChangesAsync();

        return new WorkerDto 
        { 
            Id = worker.Id, 
            Name = worker.Name, 
            Job = worker.Job,
            IsOnboarded = false,
            IsLinked = false,
        };
    }

    public async Task ChangeJobAsync(Guid workerId, string newJob)
    {
        var worker = await _repository.GetWorkerByIdAsync(workerId);

        if (worker == null)
        {
            return;
        }
        
        worker.Job = newJob;
        
        _repository.UpdateWorker(worker);

        await _repository.SaveChangesAsync();
    }

    public async Task<string> CreateLoginLinkAsync(Guid workerId)
    {
        var worker = await _repository.GetWorkerByIdAsync(workerId);

        // 1. Проверяем, существует ли вообще такой пользователь в БД
        if (worker == null)
        {
            throw new KeyNotFoundException($"Worker with ID {workerId} not found");
        }

        // 2. Проверяем, подключил ли он Stripe (есть ли ID аккаунта)
        if (string.IsNullOrWhiteSpace(worker.StripeAccountId))
        {
            throw new InvalidOperationException("User has not connected a Stripe account yet.");
        }
    
        return await _stripeService.CreateLoginLinkAsync(worker.StripeAccountId);
    }

    public async Task<List<WorkerDto>> GetWorkersByOwnerAsync(Guid ownerId)
    {
        var business = await _repository.GetBusinessByOwnerIdAsync(ownerId);
        if (business == null) return new List<WorkerDto>();

        return business.Workers.Select(w => new WorkerDto
        {
            Id = w.Id,
            Name = w.Name,
            Job = w.Job,
            IsOnboarded = w.IsOnboarded,
            StripeAccountId = w.StripeAccountId,
            AvatarUrl = w.AvatarUrl,
            IsLinked = w.IsLinked,
        }).ToList();
    }

    // 1. Меняем название второго аргумента на currentUserId (так понятнее)
    public async Task<string> OnboardWorkerAsync(Guid workerId, Guid currentUserId)
    {
        // 2. Получаем воркера напрямую (вместе с Business, это важно для проверки)
        var worker = await _repository.GetWorkerByIdAsync(workerId);

        if (worker == null) 
            throw new KeyNotFoundException("Worker not found");

        // 3. ПРОВЕРКА ПРАВ: Владелец ИЛИ Сам работник
        bool isOwner = worker.Business != null && worker.Business.OwnerId == currentUserId;
        bool isSelf = worker.LinkedUserId == currentUserId.ToString();

        // Если ни то, ни другое — доступ запрещен
        if (!isOwner && !isSelf)
            throw new UnauthorizedAccessException("Access denied: You cannot onboard this worker.");

        // 4. Stripe Logic
        if (string.IsNullOrEmpty(worker.StripeAccountId))
        {
            // Создаем аккаунт в Stripe
            // Примечание: тут используется email поддержки, в будущем можно прокидывать реальный email юзера
            var accountId = await _stripeService.CreateConnectedAccountAsync("support@tringelty.com", worker.Name);
        
            worker.StripeAccountId = accountId;
        
            // ВАЖНО: Если аккаунт создан, можно сразу ставить флаг IsOnboarded = false (пока не заполнит)
            // Но обычно мы ставим true только после успешного возврата с Stripe (webhook или return_url)
            // В вашей текущей логике флаг ставится сидером или вручную.
        
            _repository.UpdateWorker(worker);
            await _repository.SaveChangesAsync();
        }

        // Генерируем ссылку
        return await _stripeService.CreateOnboardingLinkAsync(worker.StripeAccountId);
    }

    // 1. Изменяем ownerId -> currentUserId
    public async Task<byte[]> GenerateWorkerQrAsync(Guid workerId, Guid currentUserId)
    {
        // 2. Получаем воркера по ID (с подгрузкой Business)
        var worker = await _repository.GetWorkerByIdAsync(workerId);

        if (worker == null) 
            throw new KeyNotFoundException("Worker not found");

        // 3. ПРОВЕРКА ПРАВ: Владелец ИЛИ Сам работник
        bool isOwner = worker.Business != null && worker.Business.OwnerId == currentUserId;
        bool isSelf = worker.LinkedUserId == currentUserId.ToString();

        // Если ни то, ни другое — запрещаем
        if (!isOwner && !isSelf)
            throw new UnauthorizedAccessException("Access denied: You cannot view this QR code.");

        // 4. Генерируем URL для QR
        // (Убедитесь, что BaseAppUrl задан в вашем классе сервиса)
        var paymentUrl = $"{_baseAppUrl}/pay/{workerId}";

        // 5. Рисуем картинку
        return _qrCodeService.GenerateQrCode(paymentUrl);
    }
    
    public async Task<string> GeneratePayLinkAsync(Guid workerId, Guid currentUserId)
    {
        // 2. Получаем воркера по ID (с подгрузкой Business)
        var worker = await _repository.GetWorkerByIdAsync(workerId);

        if (worker == null) 
            throw new KeyNotFoundException("Worker not found");

        // 3. ПРОВЕРКА ПРАВ: Владелец ИЛИ Сам работник
        bool isOwner = worker.Business != null && worker.Business.OwnerId == currentUserId;
        bool isSelf = worker.LinkedUserId == currentUserId.ToString();

        // Если ни то, ни другое — запрещаем
        if (!isOwner && !isSelf)
            throw new UnauthorizedAccessException("Access denied: You cannot view this QR code.");

        // 4. Генерируем URL для QR
        // (Убедитесь, что BaseAppUrl задан в вашем классе сервиса)
        var paymentUrl = $"{_baseAppUrl}/pay/{workerId}";

        
        return paymentUrl;
    }
    
    public async Task<WorkerDto> GetWorkerByIdAsync(Guid workerId, Guid currentUserId)
    {
        // 1. Получаем воркера из базы (обязательно с подгрузкой Business)
        // Убедись, что метод репозитория делает .Include(w => w.Business)
        var worker = await _repository.GetWorkerByIdAsync(workerId);

        if (worker == null)
        {
            throw new KeyNotFoundException("Worker not found");
        }

        // 2. БЕЗОПАСНОСТЬ: Проверяем права доступа
        
        // А. Владелец бизнеса
        bool isOwner = worker.Business != null && worker.Business.OwnerId == currentUserId;
        
        // Б. Сам работник (если аккаунт привязан)
        bool isSelf = worker.LinkedUserId == currentUserId.ToString();

        // Если ни то, ни другое — запрещаем доступ
        if (!isOwner && !isSelf)
        {
            throw new UnauthorizedAccessException("Access Denied: You cannot view this worker.");
        }

        // 3. Маппинг в DTO
        return new WorkerDto
        {
            Id = worker.Id,
            Name = worker.Name,
            Job = worker.Job,
            AvatarUrl = worker.AvatarUrl,
            IsOnboarded = worker.IsOnboarded,
            StripeAccountId = worker.StripeAccountId,
            IsLinked = worker.IsLinked,
        };
    }
    
    public async Task<PublicWorkerDto> GetPublicWorkerInfoAsync(Guid workerId)
    {
        // Получаем воркера из репозитория
        var worker = await _repository.GetWorkerByIdAsync(workerId);

        if (worker == null)
            throw new KeyNotFoundException("Worker not found");

        // Маппим только безопасные данные
        return new PublicWorkerDto
        {
            Id = worker.Id,
            Name = worker.Name,
            Job = worker.Job,
            AvatarUrl = worker.AvatarUrl
        };
    }
}