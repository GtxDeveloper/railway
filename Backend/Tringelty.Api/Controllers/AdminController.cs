using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tringelty.Core.DTOs;
using Tringelty.Core.Interfaces;

namespace Tringelty.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")] // <--- ВХОД ТОЛЬКО ДЛЯ ЭЛИТЫ
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<AdminStatsDto>> GetStats()
    {
        var stats = await _adminService.GetStatsAsync();
        return Ok(stats);
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<List<AdminTransactionDto>>> GetTransactions()
    {
        var transactions = await _adminService.GetRecentTransactionsAsync();
        return Ok(transactions);
    }
    
    [HttpGet("businesses")]
    public async Task<ActionResult<List<AdminBusinessDto>>> GetAllBusinesses()
    {
        try
        {
            var result = await _adminService.GetAllBusinessesWithWorkersAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            // Логируем ошибку
            return BadRequest(ex.Message);
        }
    }
    
    [HttpGet("workers/{workerId}/qr")]
    public async Task<IActionResult> GetWorkerQr(Guid workerId)
    {
        try
        {
            var qrBytes = await _adminService.GenerateWorkerQrAnyAsync(workerId);
            return File(qrBytes, "image/png");
        }
        catch (KeyNotFoundException)
        {
            return NotFound("Worker not found");
        }
    }

    [HttpGet("workers/{workerId}/link")]
    public async Task<IActionResult> GetWorkerLink(Guid workerId)
    {
        try
        {
            var link = await _adminService.GeneratePayLinkAnyAsync(workerId);
            return Ok(new { url = link });
        }
        catch (KeyNotFoundException)
        {
            return NotFound("Worker not found");
        }
    }
}