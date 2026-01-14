using Tringelty.Core.Entities;

namespace Tringelty.Core.Interfaces;

public interface IStripeService
{
    // Создает "пустой" аккаунт в Stripe и возвращает его ID (acct_...)
    Task<string> CreateConnectedAccountAsync(string email, string name);

    // Генерирует временную ссылку для ввода банковских данных
    Task<string> CreateOnboardingLinkAsync(string accountId);
    
    Task<string> CreateCheckoutSessionAsync(string connectedAccountId, decimal amount, string currency, Worker worker);
    Task<string> CreateLoginLinkAsync(string workerStripeAccountId);

    Task<BalanceDto> GetWorkerBalanceAsync(string workerStripeAccountId);
}