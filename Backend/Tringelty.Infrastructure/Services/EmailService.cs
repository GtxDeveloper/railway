using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Resend;
using Tringelty.Core.Interfaces;

namespace Tringelty.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IResend _resend;
    
    private readonly string _senderName;
    private readonly string _senderEmail;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger, IResend resend)
    {
        _logger = logger;
        _resend = resend;

        
        _senderName = configuration["EmailSettings:SenderName"] ?? "Tringelty App";
        _senderEmail = configuration["EmailSettings:SenderEmail"] ?? "noreply@tringelty.com";
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            var message = new EmailMessage();

           
            message.From = $"{_senderName} <{_senderEmail}>";
            
            message.To.Add(to);
            message.Subject = subject;
            message.HtmlBody = body;

            // Отправляем через API (это обычный HTTP запрос, Railway его пропустит)
            var response = await _resend.EmailSendAsync(message);

            if (response.Success)
            {
                _logger.LogInformation($" Письмо успешно отправлено на {to}. ID: {response.Content}");
            }
            else
            {
                
                _logger.LogError($" Ошибка Resend при отправке на {to}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $" Критическая ошибка отправки письма на {to}");
        }
    }
}