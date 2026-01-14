using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Stripe.Climate;
using Tringelty.Core.Entities;

namespace Tringelty.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    public DbSet<Business> Businesses { get; set; }
    public DbSet<Worker> Workers { get; set; }
    
    public DbSet<Transaction> Transactions { get; set; }
    
    public DbSet<WorkerInvitation> WorkerInvitations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Business ---
        modelBuilder.Entity<Business>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);

            entity.HasMany(b => b.Workers)
                .WithOne(w => w.Business)
                .HasForeignKey(w => w.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- Worker ---
        modelBuilder.Entity<Worker>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        
            // ВАЖНО: Добавляем индекс для LinkedUserId, 
            // так как мы будем часто делать запросы вида: "Найти воркера для текущего UserID"
            entity.HasIndex(w => w.LinkedUserId); 
        });

        // --- WorkerInvitation (НОВОЕ) ---
        modelBuilder.Entity<WorkerInvitation>(entity =>
        {
            entity.HasKey(e => e.Id);
        
            // Токен обязателен и должен быть уникальным (для быстрого поиска)
            entity.Property(e => e.Token).IsRequired();
            entity.HasIndex(e => e.Token).IsUnique(); 

            // Связь: Один Воркер -> Много Инвайтов
            entity.HasOne(i => i.Worker)
                .WithMany() // Если в Worker нет списка инвайтов, оставляем пустым
                .HasForeignKey(i => i.WorkerId)
                .OnDelete(DeleteBehavior.Cascade); // Если удаляем воркера -> удаляем и инвайт
        });
    }
}