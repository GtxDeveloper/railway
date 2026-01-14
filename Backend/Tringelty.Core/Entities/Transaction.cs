using System.ComponentModel.DataAnnotations.Schema;

namespace Tringelty.Core.Entities;

public class Transaction
{
    public Guid Id { get; set; }

    // Важно: Храним суммы в основной валюте (например, 10.00 EUR), а не в копейках
    public decimal Amount { get; set; }        // Сколько заплатил гость (10.00)
    public decimal WorkerAmount { get; set; }  // Сколько ушло работнику (9.00)
    public decimal PlatformFee { get; set; }   // Наш заработок (1.00)
    public string Currency { get; set; } = "eur";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ID платежа в Stripe (чтобы избежать дублей и для сверки)
    public string StripePaymentIntentId { get; set; } = string.Empty;

    // Связь с Работником (Кому платили)
    public Guid WorkerId { get; set; }
    public Worker? Worker { get; set; }
}