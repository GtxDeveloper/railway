using System.ComponentModel.DataAnnotations;

namespace Tringelty.Core.DTOs;

public class ChangePasswordDto
{
    [Required]
    public string OldPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
}