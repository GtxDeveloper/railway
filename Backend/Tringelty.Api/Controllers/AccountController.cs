using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tringelty.Core.DTOs;
using Tringelty.Core.Interfaces;

namespace Tringelty.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize] // <--- Только для вошедших
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        try
        {
            await _accountService.ChangePasswordAsync(userId, request);
            return Ok(new { message = "Пароль успешно изменен" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("change-email/init")]
    public async Task<IActionResult> InitiateChangeEmail(InitiateChangeEmailDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        try
        {
            await _accountService.InitiateChangeEmailAsync(userId, request.NewEmail);
            return Ok(new { message = $"Код подтверждения отправлен на {request.NewEmail}" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("change-email/confirm")]
    public async Task<IActionResult> ConfirmChangeEmail(ConfirmChangeEmailDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        try
        {
            await _accountService.ConfirmChangeEmailAsync(userId, request);
            return Ok(new { message = "Email успешно изменен. Пожалуйста, войдите с новым Email." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    [HttpGet("auth-status")]
    public async Task<ActionResult<UserAuthStatusDto>> GetAuthStatus()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var status = await _accountService.GetAuthStatusAsync(userId);
        return Ok(status);
    }

    [HttpPost("add-password")]
    public async Task<IActionResult> AddPassword(AddPasswordDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        try
        {
            await _accountService.AddPasswordAsync(userId, request.NewPassword);
            return Ok(new { message = "Пароль успешно создан" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message); // Уже есть пароль
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("unlink-provider")]
    public async Task<IActionResult> UnlinkProvider(UnlinkProviderDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        try
        {
            await _accountService.UnlinkProviderAsync(userId, request.Provider);
            return Ok(new { message = $"{request.Provider} успешно отвязан" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message); // Попытка удалить последний вход
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    [HttpPut("profile")] // PUT для полного обновления
    public async Task<IActionResult> UpdateProfile(UpdateProfileDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        try
        {
            await _accountService.UpdateProfileAsync(userId, request);
            return Ok(new { message = "Профиль обновлен" });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    [HttpPost("avatar")]
    // [FromForm] обязательно, так как мы шлем не JSON, а multipart/form-data
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (file == null || file.Length == 0)
            return BadRequest("Файл не выбран");

        try
        {
            // Открываем поток для чтения файла
            using var stream = file.OpenReadStream();
            
            var avatarUrl = await _accountService.UploadAvatarAsync(userId, stream, file.FileName);
            
            return Ok(new { url = avatarUrl });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    // И метод получить текущий профиль (чтобы заполнить форму на фронте)
    [HttpGet("profile")]
    public async Task<ActionResult<UserProfileDto>> GetProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        try 
        {
            var profile = await _accountService.GetProfileAsync(userId);
            return Ok(profile);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    [Authorize] // Обязательно!
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        // Вытаскиваем ID из токена
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
    
        if (string.IsNullOrEmpty(userIdString)) 
            return Unauthorized();

        try 
        {
            var context = await _accountService.GetUserContextAsync(userIdString);
            return Ok(context);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}