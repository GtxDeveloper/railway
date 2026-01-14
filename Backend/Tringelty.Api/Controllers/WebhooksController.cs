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
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        try
        {
            // 1. Проверяем подпись (Security Layer)
            // Это должно остаться в контроллере, так как зависит от HTTP заголовков
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                _whSecret
            );

            // 2. Делегируем логику Сервису
            await _webhookService.HandleEventAsync(stripeEvent);

            return Ok();
        }
        catch (StripeException e)
        {
            _logger.LogError(e, "Stripe Webhook Error");
            return BadRequest();
        }
    }
}