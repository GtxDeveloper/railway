using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tringelty.Core.Entities;
using Tringelty.Infrastructure.Data;

namespace Tringelty.Api.Data;

public static class DbSeeder
{
    public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
    {
        // Получаем зависимости
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        // 1. Читаем настройки из переменных окружения (Railway) или appsettings.json
        var adminEmail = configuration["AdminSettings:Email"] ?? "superadmin@tringelty.com";
        var adminPassword = configuration["AdminSettings:Password"];

        // Если пароль не задан — мы не можем гарантировать безопасность, поэтому выходим
        if (string.IsNullOrEmpty(adminPassword))
        {
            logger.LogWarning("⚠️ AdminSettings:Password is missing in configuration! Skipping admin seeding.");
            return;
        }

        // 2. Создаем Роли (если их нет)
        string[] roleNames = { "Admin", "User", "Worker" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
                logger.LogInformation($"✅ Role '{roleName}' created.");
            }
        }

        // 3. Работаем с Админом
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            // --- СЦЕНАРИЙ 1: Админа нет, создаем с нуля ---
            logger.LogInformation("Creating new Admin user...");

            var newAdmin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true, // Важно, чтобы сразу пускало
                FirstName = "Super",
                LastName = "Admin",
            };

            var result = await userManager.CreateAsync(newAdmin, adminPassword);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(newAdmin, "Admin");
                logger.LogInformation($"✅ Admin '{adminEmail}' created successfully.");
            }
            else
            {
                LogErrors(logger, result.Errors, "creating admin");
            }
        }
        else
        {
            // --- СЦЕНАРИЙ 2: Админ есть, проверяем актуальность пароля ---

            // Проверяем, совпадает ли текущий пароль в БД с тем, что в конфиге
            var isPasswordCorrect = await userManager.CheckPasswordAsync(adminUser, adminPassword);

            if (!isPasswordCorrect)
            {
                logger.LogWarning($"🔄 Admin password mismatch in config. Updating password for '{adminEmail}'...");

                // Генерируем токен сброса (так как старый пароль нам знать не обязательно)
                var token = await userManager.GeneratePasswordResetTokenAsync(adminUser);

                // Принудительно ставим пароль из конфига
                var result = await userManager.ResetPasswordAsync(adminUser, token, adminPassword);

                if (result.Succeeded)
                {
                    logger.LogInformation("✅ Admin password updated successfully.");
                }
                else
                {
                    LogErrors(logger, result.Errors, "updating admin password");
                }
            }
            else
            {
                logger.LogInformation("ℹ️ Admin exists and password is up to date.");
            }

            // На всякий случай проверяем, есть ли у него роль (вдруг случайно удалили)
            if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                logger.LogInformation("✅ Restored 'Admin' role to user.");
            }
        }
    }

    private static void LogErrors(ILogger logger, IEnumerable<IdentityError> errors, string action)
    {
        var errorList = string.Join(", ", errors.Select(e => e.Description));
        logger.LogError($"❌ Error while {action}: {errorList}");
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