namespace Tringelty.Core.DTOs;

public class WorkerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    public string Job { get; set; } = string.Empty;
    public bool IsOnboarded { get; set; }
    
    public bool IsLinked  { get; set; }
    public string? StripeAccountId { get; set; }
    
    public string? AvatarUrl { get; set; }
}