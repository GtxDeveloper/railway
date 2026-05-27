using Microsoft.AspNetCore.Mvc;
using Stripe;
using Tringelty.Core.Interfaces;

namespace Tringelty.Api.Controllers;

[Route("api/webhooks")]
[ApiController]
public class WebhooksController : ControllerBase
{
    private readonly string _whSecret; 
    private readonly string _connectWhSecret; // Добавляем второй ключ для Connect

    private readonly IWebhookService _webhookService;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(IWebhookService webhookService, ILogger<WebhooksController> logger, IConfiguration configuration)
    {
        _webhookService = webhookService;
        _logger = logger;
        _whSecret = configuration["StripeSettings:WhSecret"] 
                    ?? throw new InvalidOperationException("Stripe Webhook Secret is missing in configuration.");
                    
        _connectWhSecret = configuration["StripeSettings:ConnectWhSecret"] 
                    ?? throw new InvalidOperationException("Stripe Connect Webhook Secret is missing in configuration.");
    }

    [HttpPost]
    public async Task<IActionResult> Index()
    {
        var json = "";
        
        try 
        {
            // 1. Разрешаем буферизацию и перематываем поток в начало
            HttpContext.Request.EnableBuffering();
            HttpContext.Request.Body.Position = 0;

            // 2. Читаем тело
            using (var reader = new StreamReader(HttpContext.Request.Body))
            {
                json = await reader.ReadToEndAsync();
            }

            _logger.LogInformation($"Webhook received. Length: {json.Length}");

            if (string.IsNullOrEmpty(json))
            {
                _logger.LogError("Webhook body is empty!");
                return BadRequest("Empty body");
            }

            var signatureHeader = Request.Headers["Stripe-Signature"];
            Event stripeEvent;

            // 3. Проверяем подпись двумя ключами
            try
            {
                // Сначала пробуем ключ обычного вебхука (Платежи)
                stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, _whSecret);
            }
            catch (StripeException)
            {
                // Если не подошел, значит это запрос от воркера. Пробуем Connect-ключ
                stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, _connectWhSecret);
            }

            // 4. Делегируем логику
            await _webhookService.HandleEventAsync(stripeEvent);

            return Ok();
        }
        catch (StripeException e)
        {
            // Если мы попали сюда, значит оба ключа не подошли (ошибка подписи)
            _logger.LogError(e, "Stripe Webhook Error: Signature validation failed for both secrets.");
            return BadRequest();
        }
        catch (Exception e)
        {
            // Это любая другая ошибка (например, в сервисе)
            _logger.LogError(e, "General Webhook Error");
            return StatusCode(500);
        }
    }
}
