namespace Tringelty.Core.DTOs;

public class UserProfileDto
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? City { get; set; }
    public string? AvatarUrl { get; set; } // <-- Мы же только что добавили это!
    
    // Данные бизнеса
    public string? BrandName { get; set; }
}