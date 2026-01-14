using System.ComponentModel.DataAnnotations;

namespace Tringelty.Core.DTOs;

public class VerifyEmailDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;
}