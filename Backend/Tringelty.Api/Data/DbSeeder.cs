using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tringelty.Core.Entities;
using Tringelty.Infrastructure.Data;

namespace Tringelty.Api.Data;

public static class DbSeeder
{
    public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        
        // Получаем логгер, чтобы видеть ошибки в консоли
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        // 1. Создаем Роли
        string[] roleNames = { "Admin", "User" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
                logger.LogInformation($"Роль {roleName} создана.");
            }
        }

        // 2. Создаем Админа
        var adminEmail = "superadmin@tringelty.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            var newAdmin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "Super",
                LastName = "Admin",
                // Если у тебя есть обязательные поля в БД (например, AvatarUrl или City), 
                // Identity может упасть, если их не заполнить.
                // Заполни их заглушками, если они [Required]
            };

            var result = await userManager.CreateAsync(newAdmin, "AdminSuperSecret12345!");
            
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(newAdmin, "Admin");
                logger.LogInformation("Админ успешно создан!");
            }
            else
            {
                // !!! ВОТ САМОЕ ВАЖНОЕ: ВЫВОД ОШИБОК !!!
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                logger.LogError($"Ошибка создания админа: {errors}");
            }
        }
        else
        {
            logger.LogInformation("Админ уже существует, пропускаем создание.");
        }
    }
    
    public static async Task FixWorkerFlagsAsync(AppDbContext context)
    {
        // Получаем всех воркеров
        var workers = await context.Workers.ToListAsync();
        
        bool hasChanges = false;
        int updatedCount = 0;

        foreach (var worker in workers)
        {
            // --- ЛОГИКА ДЛЯ IsLinked ---
            // Если LinkedUserId не пустой -> True, иначе -> False
            bool actualLinkedState = !string.IsNullOrEmpty(worker.LinkedUserId);
            
            if (worker.IsLinked != actualLinkedState)
            {
                worker.IsLinked = actualLinkedState;
                hasChanges = true;
            }

            // --- ЛОГИКА ДЛЯ IsOnboarded ---
            // Если StripeAccountId не пустой -> True, иначе -> False
            bool actualOnboardedState = !string.IsNullOrEmpty(worker.StripeAccountId);

            if (worker.IsOnboarded != actualOnboardedState)
            {
                worker.IsOnboarded = actualOnboardedState;
                hasChanges = true;
            }

            if (hasChanges) updatedCount++;
        }

        if (hasChanges)
        {
            await context.SaveChangesAsync();
            Console.WriteLine($"[DbSeeder] Updated flags for {updatedCount} workers.");
        }
        else
        {
            Console.WriteLine("[DbSeeder] All worker flags are already correct.");
        }
    }
}