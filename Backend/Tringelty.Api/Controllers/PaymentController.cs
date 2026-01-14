using Microsoft.AspNetCore.Mvc;
using Tringelty.Core.DTOs;
using Tringelty.Core.Interfaces;

namespace Tringelty.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    /// <summary>
    /// Generates a Stripe Checkout session link for a guest (tipper).
    /// Public endpoint, no auth required.
    /// </summary>
    [HttpPost("checkout")]
    public async Task<IActionResult> CreateCheckout(CreatePaymentDto request)
    {
        try
        {
            var url = await _paymentService.GeneratePaymentLinkAsync(request);
            return Ok(new { Url = url });
        }
        catch (KeyNotFoundException ex)
        {
            // Worker doesn't exist -> 404
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            // Worker exists but not onboarded -> 400
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            // Stripe error or something else -> 500
            // TODO: Log this error properly
            return StatusCode(500, "An error occurred while processing payment: " + ex.Message);
        }
    }
}