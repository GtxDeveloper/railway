namespace Tringelty.Core.DTOs;

public class AdminStatsDto
{
    public decimal TotalRevenue { get; set; } // Твой чистый заработок (комиссия)
    public decimal TotalTurnover { get; set; } // Оборот всех денег через платформу
    public int TotalUsers { get; set; }
    public int TotalBusinesses { get; set; }
    public int TotalTransactions { get; set; }
}

public class AdminTransactionDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public decimal PlatformFee { get; set; }
    public string WorkerName { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
public class AdminBusinessDto
{
    public Guid Id { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    
    public string? AvatarUrl { get; set; } = string.Empty;
    
    // Инфо о владельце (из таблицы Users)
    public Guid OwnerId { get; set; }
    public string OwnerEmail { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;

    // Список работников
    public List<AdminWorkerDetailDto> Workers { get; set; } = new();
}

public class AdminWorkerDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Job { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    
    // Технические поля
    public string? StripeAccountId { get; set; }
    public bool IsOnboarded { get; set; }
    public bool IsLinked { get; set; }
    public string? LinkedUserId { get; set; }
}