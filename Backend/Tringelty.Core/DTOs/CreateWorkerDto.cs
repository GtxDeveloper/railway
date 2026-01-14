using System.ComponentModel.DataAnnotations;

namespace Tringelty.Core.DTOs;

public class CreateWorkerDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public string Job { get; set; } = string.Empty;
}