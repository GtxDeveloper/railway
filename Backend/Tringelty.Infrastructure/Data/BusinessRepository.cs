using Microsoft.EntityFrameworkCore;
using Tringelty.Core.DTOs;
using Tringelty.Core.Entities;
using Tringelty.Core.Interfaces;

namespace Tringelty.Infrastructure.Data;

public class BusinessRepository : IBusinessRepository
{
    private readonly AppDbContext _context;

    public BusinessRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Business business)
    {
        await _context.Businesses.AddAsync(business);
    }

    public async Task AddWorkerAsync(Worker worker)
    {
        await _context.Workers.AddAsync(worker);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
    
    public async Task<Worker?> GetWorkerByStripeIdAsync(string stripeAccountId)
    {
        return await _context.Workers
            .FirstOrDefaultAsync(w => w.StripeAccountId == stripeAccountId);
    }
    
    public async Task<Worker?> GetWorkerByIdAsync(Guid workerId)
    {
        return await _context.Workers
            .Include(w => w.Business) // <--- ЭТО ОБЯЗАТЕЛЬНО!
            .FirstOrDefaultAsync(w => w.Id == workerId);
    }

    public void UpdateWorker(Worker worker)
    {
        _context.Workers.Update(worker);
    }
    
    public async Task<Business?> GetBusinessByOwnerIdAsync(Guid ownerId)
    {
        return await _context.Businesses
            .Include(b => b.Workers) // Подгружаем воркеров
            .FirstOrDefaultAsync(b => b.OwnerId == ownerId);
    }

    public async Task<Business?> GetBusinessByIdAsync(Guid businessId)
    {
        return await _context.Businesses
            .Include(b => b.Workers) // Подгружаем воркеров
            .FirstOrDefaultAsync(b => b.Id == businessId);
    }

    public async Task AddTransactionAsync(Transaction transaction)
    {
        await _context.Transactions.AddAsync(transaction);
    }

    public async Task<List<Transaction>> GetTransactionsByBusinessIdAsync(Guid businessId)
    {
        // Ищем транзакции всех воркеров, принадлежащих этому бизнесу
        return await _context.Transactions
            .Include(t => t.Worker)
            .Where(t => t.Worker.BusinessId == businessId)
            .OrderByDescending(t => t.CreatedAt) // Сначала новые
            .ToListAsync();
    }
    
    public async Task<AdminStatsDto> GetGlobalStatsAsync()
    {
        // Считаем всё в базе данных, чтобы не тянуть данные в память
        var totalRevenue = await _context.Transactions.SumAsync(t => t.PlatformFee);
        var totalTurnover = await _context.Transactions.SumAsync(t => t.Amount);
        var totalUsers = await _context.Users.CountAsync();
        var totalBusinesses = await _context.Businesses.CountAsync();
        var totalTransactions = await _context.Transactions.CountAsync();

        return new AdminStatsDto
        {
            TotalRevenue = totalRevenue,
            TotalTurnover = totalTurnover,
            TotalUsers = totalUsers,
            TotalBusinesses = totalBusinesses,
            TotalTransactions = totalTransactions
        };
    }

    public async Task<List<Transaction>> GetTransactionsByWorkerIdAsync(Guid workerId)
    {
        return await _context.Transactions
            .Where(t => t.WorkerId == workerId)
            .OrderByDescending(t => t.CreatedAt) // Сортируем: сначала новые
            .ToListAsync();
    }
    
    public async Task<List<AdminTransactionDto>> GetGlobalTransactionsAsync(int take = 20)
    {
        return await _context.Transactions
            .Include(t => t.Worker)
            .ThenInclude(w => w!.Business) // Подгружаем вложенные связи
            .OrderByDescending(t => t.CreatedAt)
            .Take(take)
            .Select(t => new AdminTransactionDto
            {
                Id = t.Id,
                Amount = t.Amount,
                PlatformFee = t.PlatformFee,
                WorkerName = t.Worker!.Name,
                BusinessName = t.Worker.Business!.Name,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();
    }
    
    public async Task<Worker?> GetWorkerByLinkedUserIdAsync(string linkedUserId)
    {
        return await _context.Workers
            .Include(w => w.Business) // <--- ВАЖНО: Грузим бизнес сразу
            .FirstOrDefaultAsync(w => w.LinkedUserId == linkedUserId);
    }
    
    public async Task<List<Business>> GetAllWithDetailsAsync()
    {
        // Используем Query Syntax (синтаксис запросов), так как он удобнее для JOIN
        var query = from b in _context.Businesses
                
                // Подгружаем работников (стандартный Include тут работает)
                .Include(b => b.Workers) 
                
            // ДЕЛАЕМ ХИТРЫЙ LEFT JOIN
            // Соединяем: Guid.ToString() == String
            join u in _context.Users 
                on b.OwnerId.ToString() equals u.Id into owners
            from owner in owners.DefaultIfEmpty() // Это делает Join левым (Left Join)
                
            // Формируем результат
            select new 
            { 
                Business = b, 
                Owner = owner 
            };

        // Выполняем запрос
        var data = await query.AsNoTracking().ToListAsync();

        // Собираем результаты обратно в объекты Business
        // (EF Core при таком запросе вернет анонимные объекты, нам нужно склеить их)
        var result = data.Select(x => 
        {
            var business = x.Business;
            business.Owner = x.Owner; // Вручную кладем владельца в свойство
            return business;
        }).ToList();

        return result;
    }

    public async Task DeleteWorkerAsync(Worker worker)
    {
        // 1. Помечаем сущность как удаленную
        _context.Workers.Remove(worker);
        
        // 2. Асинхронно сохраняем изменения в БД
        await _context.SaveChangesAsync();
    }
}