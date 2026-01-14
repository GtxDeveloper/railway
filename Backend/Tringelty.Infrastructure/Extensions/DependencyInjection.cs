using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tringelty.Core.Interfaces;
using Tringelty.Infrastructure.Data;
using Tringelty.Infrastructure.Data.Repositories;
using Tringelty.Infrastructure.Services;

namespace Tringelty.Infrastructure;

public static class DependencyInjection
{
    // "this IServiceCollection services" делает этот метод расширением
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Репозитории (Работа с БД)
        services.AddScoped<IBusinessRepository, BusinessRepository>();
        services.AddScoped<IWorkerInvitationRepository, WorkerInvitationRepository>();
        
        // 2. Внешние сервисы (Stripe, Email, QR)
        services.AddScoped<IStripeService, StripeService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IQrCodeService, QrCodeService>();

        // 3. Бизнес-логика (Auth, Workers, Payments)
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();
        services.AddScoped<IWorkerService, WorkerService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IWebhookService, WebhookService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IImageService, CloudinaryService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IAdminService, AdminService>();

        
        return services;
    }
}