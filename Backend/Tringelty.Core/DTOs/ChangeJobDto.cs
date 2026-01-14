using System.ComponentModel.DataAnnotations;

namespace Tringelty.Core.DTOs;

public class ChangeJobDto
{
    [Required]
    public string NewJob { get; set; } = string.Empty;
}