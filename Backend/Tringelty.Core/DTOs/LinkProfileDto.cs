using System.ComponentModel.DataAnnotations;

namespace Tringelty.Core.DTOs;

public class LinkProfileDto
{
    // Токен, который пришел в ссылке (например, из URL /invite/{token})
    [Required]
    public string Token { get; set; } = string.Empty;
}