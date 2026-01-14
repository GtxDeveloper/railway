using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tringelty.Core.DTOs;
using Tringelty.Core.Interfaces;

namespace Tringelty.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    // TODO: Future Feature - Worker Dashboard
// Once we implement worker login via LinkedUserId, add a new endpoint:
// GET /api/worker-dashboard/summary
// Logic: Fetch transactions where Transaction.WorkerId == CurrentUserId (linked worker)
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var summary = await _dashboardService.GetOwnerSummaryAsync(Guid.Parse(userId!));
        return Ok(summary);
    }
    [HttpGet("worker/{workerId}/balance")]
    public async Task<IActionResult> GetWorkerBalance(Guid workerId)
    {
        try
        {
            // Здесь можно добавить проверку: имеет ли текущий юзер право смотреть баланс этого воркера?
            // Например: var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var balance = await _dashboardService.GetWorkerBalanceAsync(workerId);
            return Ok(balance);
        }
        catch (Exception ex)
        {
            // Возвращаем ошибку, если Stripe не подключен или воркер не найден
            return BadRequest(new { message = ex.Message });
        }
    }
    
    [HttpGet("worker/{workerId}/transactions")]
    public async Task<IActionResult> GetWorkerTransactions(Guid workerId)
    {
        // TODO: Здесь хорошо бы добавить проверку: принадлежит ли этот workerId 
        // текущему авторизованному владельцу бизнеса (BusinessOwner).
    
        var transactions = await _dashboardService.GetWorkerTransactionsAsync(workerId);
        return Ok(transactions);
    }
    
    // POST: api/dashboard/business/avatar
    [HttpPost("business/avatar")]
    public async Task<IActionResult> UploadBusinessAvatar(IFormFile file)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        if (file == null || file.Length == 0) return BadRequest("File is empty");

        try
        {
            using var stream = file.OpenReadStream();
            var url = await _dashboardService.UploadBusinessAvatarAsync(userId, stream, file.FileName);
            
            return Ok(new { url });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // POST: api/dashboard/worker/{workerId}/avatar
    [HttpPost("worker/{workerId}/avatar")]
    public async Task<IActionResult> UploadWorkerAvatar(Guid workerId, IFormFile file)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        if (file == null || file.Length == 0) return BadRequest("File is empty");

        try
        {
            using var stream = file.OpenReadStream();
            // Передаем и ID владельца (для проверки прав), и ID работника
            var url = await _dashboardService.UploadWorkerAvatarAsync(userId, workerId, stream, file.FileName);
            
            return Ok(new { url });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    [HttpGet("business")]
    public async Task<IActionResult> GetBusiness()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
    
        var userId = Guid.Parse(userIdString);

        var businessDto = await _dashboardService.GetBusinessProfileAsync(userId);

        if (businessDto == null)
        {
            // Если бизнеса еще нет, можно вернуть 404 или пустой объект,
            // но для владельца он обычно создается при регистрации.
            return NotFound("Business not found"); 
        }

        return Ok(businessDto);
    }
    
    [HttpPut("worker/{workerId}")]
    public async Task<IActionResult> UpdateWorker(Guid workerId, [FromBody] UpdateWorkerDto dto)
    {
        // Получаем ID текущего пользователя (Владельца) из токена
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
    
        var ownerId = Guid.Parse(userIdString);

        try
        {
            await _dashboardService.UpdateWorkerAsync(ownerId, workerId, dto);
            return Ok(new { message = "Worker updated successfully" });
        }
        catch (KeyNotFoundException)
        {
            return NotFound("Worker not found");
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid(); // 403 Forbidden
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    [HttpPost("worker/{workerId}/invite")]
    public async Task<IActionResult> CreateInvite(Guid workerId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        try
        {
            var link = await _dashboardService.GenerateInviteLinkAsync(userId, workerId);
            // Возвращаем полный URL для удобства (или только токен)
            return Ok(new { inviteUrl = link });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    [HttpPost("link-profile")]
    [Authorize] // Юзер уже должен войти в систему
    public async Task<IActionResult> LinkProfile([FromBody] LinkProfileDto dto)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    
        try 
        {
            await _dashboardService.AcceptInviteAsync(currentUserId, dto.Token);
            return Ok(new { message = "Profile linked successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    [HttpGet("{id}/summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetWorkerSummary(Guid id)
    {
        var currentUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserIdString)) return Unauthorized();

        try
        {
            var summary = await _dashboardService.GetWorkerSummaryAsync(id, Guid.Parse(currentUserIdString));
            return Ok(summary);
        }
        catch (KeyNotFoundException)
        {
            return NotFound("Worker not found");
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
    
    // DELETE: api/dashboard/worker/{workerId}
    [HttpDelete("worker/{workerId}")]
    public async Task<IActionResult> DeleteWorker(Guid workerId)
    {
        // 1. Получаем ID текущего пользователя (Владельца)
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

        var ownerId = Guid.Parse(userIdString);

        try
        {
            // 2. Вызываем сервис для удаления
            // Мы передаем ownerId, чтобы в сервисе проверить, имеет ли он право удалять этого работника
            await _dashboardService.DeleteWorkerAsync(ownerId, workerId);
            
            // 3. Возвращаем 204 No Content (успешное удаление без тела ответа)
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound("Worker not found");
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid(); // Пытается удалить чужого работника
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message); // Например, попытка удалить самого себя (Owner)
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}