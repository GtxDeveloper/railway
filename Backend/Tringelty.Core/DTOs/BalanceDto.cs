public class BalanceDto
{
    public decimal Available { get; set; } // Можно вывести прямо сейчас
    public decimal Pending { get; set; }   // В обработке (будет доступно через пару дней)
    public string Currency { get; set; }   // Валюта (eur, usd)
}