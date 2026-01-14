using System.ComponentModel.DataAnnotations;

namespace Tringelty.Core.DTOs;

public class InitiateChangeEmailDto
{
    [Required]
    [EmailAddress]
    public string NewEmail { get; set; } = string.Empty;
}

public class ConfirmChangeEmailDto
{
    [Required]
    [EmailAddress]
    public string NewEmail { get; set; } = string.Empty;

    [Required]
    public string Code { get; set; } = string.Empty;
}