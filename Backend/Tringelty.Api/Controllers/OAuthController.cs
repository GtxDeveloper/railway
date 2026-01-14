using Microsoft.AspNetCore.Mvc;
using Tringelty.Core.DTOs;
using Tringelty.Core.Interfaces;

namespace Tringelty.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OAuthController : ControllerBase
{
    private readonly IGoogleAuthService _googleService;

    public OAuthController(IGoogleAuthService googleService)
    {
        _googleService = googleService;
    }

    [HttpPost("google/login")]
    public async Task<ActionResult<AuthResponseDto>> GoogleLogin([FromBody] GoogleLoginDto request)
    {
        try
        {
            var result = await _googleService.LoginAsync(request);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            // Специальный формат ответа для Фронтенда
            return NotFound(new 
            { 
                message = "User not registered", 
                needRegistration = true,
                email = "user_email_from_token_if_possible" // Опционально
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("google/register")]
    public async Task<ActionResult<AuthResponseDto>> GoogleRegister([FromBody] GoogleRegisterDto request)
    {
        try
        {
            var result = await _googleService.RegisterAsync(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}