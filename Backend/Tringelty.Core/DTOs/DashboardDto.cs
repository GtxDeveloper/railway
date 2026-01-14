namespace Tringelty.Core.DTOs;

public class DashboardSummaryDto
{
    public decimal TodayEarnings { get; set; }
    public decimal MonthEarnings { get; set; }
    public decimal TotalEarnings { get; set; }
    public int TransactionsCount { get; set; }
}