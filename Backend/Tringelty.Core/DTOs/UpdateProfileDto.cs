using System.ComponentModel.DataAnnotations;

namespace Tringelty.Core.DTOs;

public class UpdateProfileDto
{
    [Required]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    public string LastName { get; set; } = string.Empty;
    
    [Phone]
    public string? PhoneNumber { get; set; }
    
    public string? City { get; set; }
    
    // Если пустое - не меняем бренд
    public string? BrandName { get; set; }
}