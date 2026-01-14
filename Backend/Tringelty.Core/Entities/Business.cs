using System.ComponentModel.DataAnnotations.Schema;

namespace Tringelty.Core.Entities;

public class Business
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? OwnerId { get; set; }
    
    [NotMapped] 
    public ApplicationUser? Owner { get; set; }
    // Новое поле
    public string? AvatarUrl { get; set; } 

    public List<Worker> Workers { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

