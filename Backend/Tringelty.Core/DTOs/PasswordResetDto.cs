using System.ComponentModel.DataAnnotations;

namespace Tringelty.Core.DTOs;

public class ForgotPasswordDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty; // Токен из письма

    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
}