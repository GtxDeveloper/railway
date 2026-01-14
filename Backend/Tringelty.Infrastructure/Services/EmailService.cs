using MailKit.Net.Smtp; // <-- Библиотека MailKit
using MailKit.Security;
using Microsoft.Extensions.Configuration; // Или IOptions
using Microsoft.Extensions.Logging;
using MimeKit; // <-- Создание письма
using MimeKit.Text;
using Tringelty.Core.Interfaces;
using Tringelty.Infrastructure.Options;

namespace Tringelty.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly EmailOptions _emailOptions;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _logger = logger;
        
        // Маппим настройки из JSON в объект
        _emailOptions = configuration.GetSection("EmailSettings").Get<EmailOptions>() 
                        ?? throw new Exception("EmailSettings not found in config");
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            // 1. Создаем письмо
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_emailOptions.SenderName, _emailOptions.SenderEmail));
            email.To.Add(new MailboxAddress("", to));
            email.Subject = subject;
            
            // Тело письма (HTML)
            email.Body = new TextPart(TextFormat.Html) 
            { 
                Text = body 
            };

            // 2. Отправляем через SMTP
            using var smtp = new SmtpClient();
            
            // Подключаемся (587 - стандартный порт для TLS)
            await smtp.ConnectAsync(_emailOptions.SmtpServer, _emailOptions.Port, SecureSocketOptions.StartTls);
            
            // Авторизуемся
            await smtp.AuthenticateAsync(_emailOptions.Username, _emailOptions.Password);
            
            // Шлем
            await smtp.SendAsync(email);
            
            // Отключаемся
            await smtp.DisconnectAsync(true);

            _logger.LogInformation($"Письмо отправлено на {to}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка отправки письма на {to}");
            // Можно пробросить ошибку дальше, если хотим, чтобы юзер видел ошибку
            // throw; 
        }
    }
}