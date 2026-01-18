using Microsoft.AspNetCore.Mvc;
using Stripe;
using Tringelty.Core.Interfaces;

namespace Tringelty.Api.Controllers;

[Route("api/webhooks")]
[ApiController]
public class WebhooksController : ControllerBase
{
    private readonly string _whSecret; 

    private readonly IWebhookService _webhookService;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(IWebhookService webhookService, ILogger<WebhooksController> logger, IConfiguration configuration)
    {
        _webhookService = webhookService;
        _logger = logger;
        _whSecret = configuration["StripeSettings:WhSecret"] 
                    ?? throw new InvalidOperationException("Stripe Webhook Secret is missing in configuration.");
    }

    [HttpPost]
    public async Task<IActionResult> Index()
    {
        var json = "";
        
        try 
        {
            // 1. ВАЖНО: Разрешаем буферизацию и перематываем поток в начало
            HttpContext.Request.EnableBuffering();
            HttpContext.Request.Body.Position = 0;

            // 2. Читаем тело
            using (var reader = new StreamReader(HttpContext.Request.Body))
            {
                json = await reader.ReadToEndAsync();
            }

            // Лог для отладки (увидишь в Railway logs)
            _logger.LogInformation($"Webhook received. Length: {json.Length}");

            if (string.IsNullOrEmpty(json))
            {
                _logger.LogError("Webhook body is empty!");
                return BadRequest("Empty body");
            }

            // 3. Проверяем подпись
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                _whSecret
            );

            // 4. Делегируем логику
            await _webhookService.HandleEventAsync(stripeEvent);

            return Ok();
        }
        catch (StripeException e)
        {
            // Это ошибка подписи или формата Stripe
            _logger.LogError(e, "Stripe Webhook Error");
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