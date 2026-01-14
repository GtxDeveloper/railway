using System.ComponentModel.DataAnnotations;

namespace Tringelty.Core.DTOs;

public class GoogleLoginDto
{
    [Required]
    public string IdToken { get; set; } = string.Empty; // Токен, который придет с фронта
}

public class GoogleRegisterDto
{
    [Required]
    public string IdToken { get; set; } = string.Empty;

    // Недостающие данные, которые юзер введет руками
    [Required]
    public string Brand { get; set; } = string.Empty;
    
    public string? City { get; set; }
    public string? Phone { get; set; }
}