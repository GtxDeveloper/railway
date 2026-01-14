using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tringelty.Core.DTOs;
using Tringelty.Core.Interfaces;

namespace Tringelty.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WorkersController : ControllerBase
{
    private readonly IWorkerService _workerService;

    public WorkersController(IWorkerService workerService)
    {
        _workerService = workerService;
    }

    [HttpPost]
    public async Task<ActionResult<WorkerDto>> CreateWorker(CreateWorkerDto request)
    {
        var userId = GetCurrentUserId();
        try
        {
            var result = await _workerService.CreateWorkerAsync(request, userId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<WorkerDto>>> GetMyWorkers()
    {
        var userId = GetCurrentUserId();
        var result = await _workerService.GetWorkersByOwnerAsync(userId);
        return Ok(result);
    }

    [HttpPost("{workerId}/job")]
    public async Task<IActionResult> ChangeJob(Guid workerId, ChangeJobDto request)
    {
        var userId = GetCurrentUserId();
        try
        {
            await _workerService.ChangeJobAsync(workerId, request.NewJob);
            return Ok(new { message = "Job updated successfully" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return NotFound(ex.Message);
        }
    }
    
    [HttpPost("{workerId}/onboard")]
    public async Task<IActionResult> OnboardWorker(Guid workerId)
    {
        var userId = GetCurrentUserId();
        try
        {
            var url = await _workerService.OnboardWorkerAsync(workerId, userId);
            return Ok(new { url });
        }
        catch (UnauthorizedAccessException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet("{workerId}/qr")]
    public async Task<IActionResult> GetWorkerQr(Guid workerId)
    {
        var userId = GetCurrentUserId();
        try
        {
            var qrBytes = await _workerService.GenerateWorkerQrAsync(workerId, userId);
            return File(qrBytes, "image/png", $"qr-{workerId}.png");
        }
        catch (UnauthorizedAccessException)
        {
            return NotFound();
        }
    }
    
    [HttpGet("{workerId}/pay-link")]

    public async Task<IActionResult> GetPayLink(Guid workerId)
    {
        var userId = GetCurrentUserId();
        try
        {
            var linkUrl = await _workerService.GeneratePayLinkAsync(workerId, userId);
            return Ok(new { url = linkUrl });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message }); // 404 - Воркер не найден
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message }); // 400 - Stripe аккаунт не подключен
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Ошибка сервера: " + ex.Message });
        }
    }

    [HttpPost("{workerId}/login-link")]

    public async Task<IActionResult> GetLoginLink(Guid workerId)
    {
        try
        {
            var linkUrl = await _workerService.CreateLoginLinkAsync(workerId);
            return Ok(new { url = linkUrl });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message }); // 404 - Воркер не найден
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message }); // 400 - Stripe аккаунт не подключен
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Ошибка сервера: " + ex.Message });
        }
    }
    
    // Вспомогательный метод для получения ID из токена
    private Guid GetCurrentUserId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(idStr)) throw new UnauthorizedAccessException();
        return Guid.Parse(idStr);
    }
    
    [HttpGet("{workerId}")]
    public async Task<ActionResult<WorkerDto>> GetWorker(Guid workerId)
    {
        // Получаем ID того, кто делает запрос
        var currentUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserIdString)) return Unauthorized();

        try
        {
            // Передаем в сервис ID работника и ID текущего юзера
            var result = await _workerService.GetWorkerByIdAsync(workerId, Guid.Parse(currentUserIdString));
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Worker not found" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid(); // 403 Forbidden - Доступ запрещен
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    
    [HttpGet("{workerId}/public")]
    [AllowAnonymous] // <--- ВАЖНО: Разрешает доступ без токена
    public async Task<ActionResult<PublicWorkerDto>> GetPublicWorkerInfo(Guid workerId)
    {
        try
        {
            var result = await _workerService.GetPublicWorkerInfoAsync(workerId);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Worker not found" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}