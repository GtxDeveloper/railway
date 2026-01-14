using System.ComponentModel.DataAnnotations;

namespace Tringelty.Core.DTOs;

public class CreatePaymentDto
{
    [Required]
    public Guid WorkerId { get; set; } // Кому платим

    [Required]
    [Range(1, 10000)] // Ограничение от 1 до 10000 условных единиц
    public decimal Amount { get; set; } // Сумма (например, 5.00)

    public string Currency { get; set; } = "eur"; // Валюта (eur для Словакии)
}