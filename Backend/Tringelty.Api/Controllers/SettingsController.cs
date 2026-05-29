using Microsoft.AspNetCore.Mvc;

namespace Tringelty.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public SettingsController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Возвращает публичные настройки платформы (комиссию)
    /// </summary>
    [HttpGet("platform-fee")]
    public IActionResult GetPlatformFee()
    {
        // Читаем ту же самую переменную, что и при создании платежа
        // Если ее нет, возвращаем дефолтные 10%
        var fee = _configuration.GetValue<decimal>("StripeSettings:PlatformFeePercent", 10m);
        
        return Ok(new { feePercent = fee });
    }
}