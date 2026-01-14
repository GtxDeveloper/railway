using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tringelty.Core.DTOs;
using Tringelty.Core.Interfaces;

namespace Tringelty.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto request)
    {
        try
        {
            await _authService.RegisterAsync(request);
            // Возвращаем 200, но без токена. Фронт должен переключиться на экран "Введите код".
            return Ok(new { message = "Код отправлен на почту. Пожалуйста, подтвердите аккаунт." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("verify-email")]
    public async Task<ActionResult<AuthResponseDto>> VerifyEmail(VerifyEmailDto request)
    {
        try
        {
            var result = await _authService.VerifyEmailAsync(request);
            return Ok(result); // Вот теперь отдаем токен
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }
    
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDto>> Refresh([FromBody] RefreshTokenDto request)
    {
        try
        {
            var result = await _authService.RefreshTokenAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            // Любая ошибка при рефреше = юзер должен залогиниться заново
            return Unauthorized("Invalid attempt: " + ex.Message);
        }
    }
    
    [HttpPost("logout")]
    [Authorize] // Только авторизованный может выйти
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _authService.LogoutAsync(userId);
        return Ok(new { message = "Logged out successfully" });
    }
    
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto request)
    {
        await _authService.ForgotPasswordAsync(request.Email);
        // Всегда возвращаем ОК, чтобы хакеры не могли перебирать базу email-ов
        return Ok(new { message = "Если такой email существует, мы отправили инструкцию." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto request)
    {
        try
        {
            await _authService.ResetPasswordAsync(request);
            return Ok(new { message = "Пароль успешно изменен" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}