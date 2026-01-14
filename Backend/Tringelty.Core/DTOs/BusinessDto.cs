namespace Tringelty.Core.DTOs;

public class BusinessDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LogoUrl { get; set; } // Это наш Business.AvatarUrl
}