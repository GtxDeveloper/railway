using System.ComponentModel.DataAnnotations;

public class RegisterDto
{
    [Required] public string Name { get; set; } = string.Empty;
    [Required] public string Surname { get; set; } = string.Empty;
    [Required] [EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
    
    // Сделали nullable, так как работнику бренд вводить не нужно
    public string? Brand { get; set; } 
    
    // Новое поле для токена
    public string? InviteToken { get; set; }

    public string? Phone { get; set; }
    public string? City { get; set; }
}