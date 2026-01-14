namespace Tringelty.Core.DTOs;

public class UserContextDto
{
    public Guid UserId { get; set; }
    public string Role { get; set; } // "Owner" | "Worker" | "New"
    
    // Если Owner - тут ID его бизнеса
    public Guid? BusinessId { get; set; } 
    
    // Если Worker - тут ID его профиля воркера
    public Guid? WorkerId { get; set; } 
}