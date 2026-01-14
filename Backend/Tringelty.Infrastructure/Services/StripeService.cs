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
    
    public async Task<string> CreateCheckoutSessionAsync(string connectedAccountId, decimal amount, string currency, Worker worker)
    {
        // Stripe работает в "копейках" (центах). 5.00 EUR = 500 cents.
        var amountInCents = (long)(amount * 100);
    
        // Наша комиссия (например, 10%)
        var applicationFee = (long)(amountInCents * 0.1m); 
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
                        UnitAmount = amountInCents,
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
                ApplicationFeeAmount = applicationFee,
                TransferData = new SessionPaymentIntentDataTransferDataOptions
                {
                    Destination = worker.StripeAccountId,
                },
                // !!! ВОТ ЭТО НУЖНО ДОБАВИТЬ !!!
                Metadata = new Dictionary<string, string>
                {
                    { "WorkerId", worker.Id.ToString() }, // Наш GUID
                    { "PlatformFee", applicationFee.ToString() } // Наша комиссия в копейках
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