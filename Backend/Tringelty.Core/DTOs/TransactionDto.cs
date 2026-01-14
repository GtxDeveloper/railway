namespace Tringelty.Core.DTOs;

public class TransactionDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }       // Общая сумма
    public decimal WorkerAmount { get; set; } // Чистые чаевые
    public string Currency { get; set; } = "eur";
    public DateTime CreatedAt { get; set; }
}