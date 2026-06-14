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
        // Читаем те же самые переменные, что и при создании платежа.
        // Тариф двухуровневый: до порога (вкл.) и выше порога.
        var threshold = _configuration.GetValue<decimal>("StripeSettings:FeeThresholdAmount", 5m);

        return Ok(new
        {
            thresholdAmount = threshold,
            lowTier = new
            {
                percent = _configuration.GetValue<decimal>("StripeSettings:LowTierFeePercent", 5m),
                fixedCents = _configuration.GetValue<decimal>("StripeSettings:LowTierFeeFixedCents", 5m)
            },
            highTier = new
            {
                percent = _configuration.GetValue<decimal>("StripeSettings:HighTierFeePercent", 1.5m),
                fixedCents = _configuration.GetValue<decimal>("StripeSettings:HighTierFeeFixedCents", 25m)
            }
        });
    }
}