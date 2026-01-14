namespace Tringelty.Core.Entities;

public class Worker
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Job { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    
    // Stripe и бизнес
    public string? StripeAccountId { get; set; }
    public bool IsOnboarded { get; set; } = false;
    
    public bool IsLinked  { get; set; } = false;
    public Guid BusinessId { get; set; }
    public Business? Business { get; set; }
    
    // ССЫЛКА НА РЕАЛЬНОГО ЮЗЕРА
    // Храним ID из таблицы AspNetUsers
    public string? LinkedUserId { get; set; } 
}