using Microsoft.Extensions.Configuration;
using Stripe;
using Tringelty.Core.Interfaces;
using Stripe.Checkout;
using Tringelty.Core.Entities;

namespace Tringelty.Infrastructure.Services;

public class StripeService : IStripeService
{
    
    private readonly IConfiguration _configuration;
    
    public StripeService(IConfiguration config)
    {
        // Инициализируем Stripe глобально при создании сервиса
        StripeConfiguration.ApiKey = config["StripeSettings:SecretKey"];
        _configuration = config;
    }

    public async Task<string> CreateConnectedAccountAsync(string email, string name)
    {
        var options = new AccountCreateOptions
        {
            Type = "express", // Express - самый простой вариант, Stripe сам рисует формы
            Country = "SK",   // TODO: Make dynamic based on user location
            Email = email,
            Capabilities = new AccountCapabilitiesOptions
            {
                CardPayments = new AccountCapabilitiesCardPaymentsOptions { Requested = true },
                Transfers = new AccountCapabilitiesTransfersOptions { Requested = true },
            },
            Metadata = new Dictionary<string, string>
            {
                { "WorkerName", name }
            }
        };

        var service = new Stripe.AccountService();
        var account = await service.CreateAsync(options);

        return account.Id;
    }

    public async Task<string> CreateOnboardingLinkAsync(string accountId)
    {
        
        var frontendUrl = _configuration["AppSettings:FrontendUrl"];
        
        var options = new AccountLinkCreateOptions
        {
            Account = accountId,
            // Сюда Stripe вернет юзера, если тот нажмет "Обновить страницу" или ссылка протухнет
            // В реальном Angular приложении это будет роут типа /onboarding/refresh
            RefreshUrl = $"{frontendUrl}/dashboard", 
            
            // Сюда Stripe вернет юзера после успеха
            ReturnUrl = $"{frontendUrl}/onboarding/success",
            
            Type = "account_onboarding",
        };

        var service = new AccountLinkService();
        var link = await service.CreateAsync(options);

        return link.Url;
    }
    
    public async Task<string> CreateCheckoutSessionAsync(string connectedAccountId, decimal amount, string currency, Worker worker, bool coverFee)
{
    var feePercent = _configuration.GetValue<decimal>("StripeSettings:PlatformFeePercent", 10m);
    var feeMultiplier = feePercent / 100m;
    
    // 1. Высчитываем саму комиссию в валюте (например, от 10 EUR комиссия составит 1 EUR)
    decimal feeAmount = amount * feeMultiplier;
    
    // 2. Определяем итоговую сумму списания с карты гостя
    decimal totalChargeAmount = amount;
    if (coverFee)
    {
        totalChargeAmount = amount + feeAmount; // Спишем 11 EUR вместо 10 EUR
    }

    // 3. Переводим всё в центы для Stripe
    var amountInCents = (long)(totalChargeAmount * 100);
    var applicationFeeInCents = (long)(feeAmount * 100);
    
    var frontendUrl = _configuration["AppSettings:FrontendUrl"];
    
    var options = new SessionCreateOptions
    {
        Mode = "payment",
        PaymentMethodTypes = new List<string> { "card" },
        LineItems = new List<SessionLineItemOptions>
        {
            new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    UnitAmount = amountInCents, // Передаем ИТОГОВУЮ сумму к списанию
                    Currency = currency,
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = $"Tips for {worker.Name}",
                    },
                },
                Quantity = 1,
            },
        },
        PaymentIntentData = new SessionPaymentIntentDataOptions
        {
            ApplicationFeeAmount = applicationFeeInCents, // Платформа забирает свою комсу
            TransferData = new SessionPaymentIntentDataTransferDataOptions
            {
                Destination = worker.StripeAccountId, // Воркер получает остаток
            },
            Metadata = new Dictionary<string, string>
            {
                { "WorkerId", worker.Id.ToString() }, 
                { "OriginalTipAmount", amount.ToString("0.00") }, // Исходная сумма чаевых
                { "PlatformFee", applicationFeeInCents.ToString() },
                { "FeePercent", feePercent.ToString() },
                { "FeeCoveredByGuest", coverFee.ToString() } // Записываем, кто оплатил банкет
            }
        },
        SuccessUrl = $"{frontendUrl}/payment/success",
        CancelUrl = $"{frontendUrl}/payment/cancel",
    };

    var service = new SessionService();
    var session = await service.CreateAsync(options);

    return session.Url;
}

    
    public async Task<string> CreateLoginLinkAsync(string workerStripeAccountId)
    {
        var service = new AccountLoginLinkService();
        // Просто возвращаем URL. Ошибки поймает контроллер.
        var loginLink = await service.CreateAsync(workerStripeAccountId);
        return loginLink.Url;
    }

    public async Task<BalanceDto> GetWorkerBalanceAsync(string workerStripeAccountId)
    {
        var service = new BalanceService();

        var requestOptions = new RequestOptions
        {
            StripeAccount = workerStripeAccountId 
        };

        var balance = await service.GetAsync(requestOptions);
        
        var available = balance.Available.FirstOrDefault(b => b.Currency == "eur");
        var pending = balance.Pending.FirstOrDefault(b => b.Currency == "eur");

        return new BalanceDto
        {
            Available = (available?.Amount ?? 0) / 100.0m,
            Pending = (pending?.Amount ?? 0) / 100.0m,
            Currency = available?.Currency ?? "eur"
        };
    }
}