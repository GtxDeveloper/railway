using Microsoft.Extensions.Logging;
using Stripe;
using Tringelty.Core.Entities;
using Tringelty.Core.Interfaces;

namespace Tringelty.Infrastructure.Services;

public class WebhookService : IWebhookService
{
    private readonly IBusinessRepository _repository;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(IBusinessRepository repository, ILogger<WebhookService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task HandleEventAsync(Event stripeEvent)
    {
        // 1. Активация аккаунта (старая логика)
        if (stripeEvent.Type == EventTypes.AccountUpdated)
        {
            var account = stripeEvent.Data.Object as Account;
            if (account != null) await HandleAccountUpdateAsync(account);
        }
        // 2. Успешный платеж (НОВАЯ ЛОГИКА)
        else if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded)
        {
            var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
            if (paymentIntent != null) await HandlePaymentSuccessAsync(paymentIntent);
        }
    }

    private async Task HandleAccountUpdateAsync(Account account)
    {
        // Проверяем, разрешил ли Stripe работу (Payouts + Charges)
        bool isFullyOnboarded = account.PayoutsEnabled && account.ChargesEnabled;

        if (isFullyOnboarded)
        {
            // Ищем работника по Stripe ID (метод в репозитории мы уже добавили ранее)
            var worker = await _repository.GetWorkerByStripeIdAsync(account.Id);

            if (worker != null && !worker.IsOnboarded)
            {
                worker.IsOnboarded = true;
                await _repository.SaveChangesAsync();
                
                _logger.LogInformation($"Webhooks: Worker {worker.Name} ({worker.Id}) успешно активирован.");
            }
        }
    }
    
    private async Task HandlePaymentSuccessAsync(PaymentIntent intent)
    {
        // Пытаемся достать WorkerId из метаданных, которые мы положили в Шаге 4
        if (!intent.Metadata.TryGetValue("WorkerId", out var workerIdStr))
        {
            _logger.LogWarning($"Payment {intent.Id} skipped: No WorkerId metadata.");
            return;
        }

        if (!Guid.TryParse(workerIdStr, out var workerId))
        {
            _logger.LogError($"Payment {intent.Id} skipped: Invalid WorkerId format.");
            return;
        }

        // Считаем деньги (Stripe шлет копейки, переводим в валюту)
        decimal amountTotal = intent.Amount / 100m;
        decimal platformFee = 0;

        if (intent.Metadata.TryGetValue("PlatformFee", out var feeStr))
        {
            platformFee = long.Parse(feeStr) / 100m;
        }

        // Создаем запись
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            StripePaymentIntentId = intent.Id,
            WorkerId = workerId,
            Amount = amountTotal,
            PlatformFee = platformFee,
            WorkerAmount = amountTotal - platformFee, 
            Currency = intent.Currency,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddTransactionAsync(transaction);
        await _repository.SaveChangesAsync();

        _logger.LogInformation($"💰 Payment Saved: {amountTotal} {intent.Currency} for Worker {workerId}");
    }
}