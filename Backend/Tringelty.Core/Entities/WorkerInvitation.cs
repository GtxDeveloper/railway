namespace Tringelty.Core.Entities;

public class WorkerInvitation
{
    public Guid Id { get; set; }
    
    public Guid WorkerId { get; set; }
    public Worker? Worker { get; set; }
    
    public string Token { get; set; } = string.Empty; // Уникальный код в ссылке
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}