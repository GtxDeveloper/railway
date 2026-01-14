namespace Tringelty.Core.DTOs;

public class PublicWorkerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Job { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}