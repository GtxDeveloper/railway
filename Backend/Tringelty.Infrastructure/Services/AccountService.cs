using Microsoft.AspNetCore.Identity;
using Tringelty.Core.DTOs;
using Tringelty.Core.Entities;
using Tringelty.Core.Interfaces;

namespace Tringelty.Infrastructure.Services;

public class AccountService : IAccountService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IBusinessRepository _businessRepository;
    private readonly IEmailService _emailService;
    private readonly IImageService _imageService;

    public AccountService(
        UserManager<ApplicationUser> userManager,
        IBusinessRepository businessRepository,
        IEmailService emailService,
        IImageService imageService)
    {
        _userManager = userManager;
        _businessRepository = businessRepository;
        _emailService = emailService;
        _imageService = imageService;
    }

    public async Task<UserProfileDto> GetProfileAsync(string userId)
    {
        // 1. Ищем Юзера
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) throw new KeyNotFoundException("User not found");

        // 2. Ищем Бизнес (чтобы узнать название бренда)
        var business = await _businessRepository.GetBusinessByOwnerIdAsync(Guid.Parse(userId));

        // 3. Собираем DTO
        return new UserProfileDto
        {
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            City = user.City,
            AvatarUrl = user.AvatarUrl,
            BrandName = business?.Name // Если бизнеса нет (странно, но вдруг), будет null
        };
    }

    public async Task UpdateProfileAsync(string userId, UpdateProfileDto request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) throw new KeyNotFoundException("User not found");

        // 1. Обновляем данные Юзера
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PhoneNumber = request.PhoneNumber;
        user.City = request.City;

        await _userManager.UpdateAsync(user);

        // 2. Обновляем Бренд (если передан)
        if (!string.IsNullOrWhiteSpace(request.BrandName))
        {
            var business = await _businessRepository.GetBusinessByOwnerIdAsync(Guid.Parse(userId));
            if (business != null)
            {
                business.Name = request.BrandName;

                // 3. (Опционально) Если это одиночка, обновляем и имя Воркера
                // Находим воркера, который "принадлежит" этому юзеру (пока по имени или флагу)
                // Но для MVP можно просто обновить бизнес.

                await _businessRepository.SaveChangesAsync();
            }
        }
    }

    public async Task<string> UploadAvatarAsync(string userId, Stream fileStream, string fileName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) throw new KeyNotFoundException("User not found");

        // 1. Загружаем в облако
        var url = await _imageService.UploadImageAsync(fileStream, fileName);

        // 2. Обновляем юзера
        user.AvatarUrl = url;
        await _userManager.UpdateAsync(user);

        return url; // Возвращаем ссылку фронтенду, чтобы он сразу обновил аватарку
    }

    public async Task ChangePasswordAsync(string userId, ChangePasswordDto request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) throw new KeyNotFoundException("User not found");

        var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new ArgumentException($"Ошибка смены пароля: {errors}");
        }
    }

    public async Task<UserAuthStatusDto> GetAuthStatusAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) throw new KeyNotFoundException("User not found");

        // 1. Проверяем, есть ли пароль (если хеш не пустой - пароль есть)
        var hasPassword = !string.IsNullOrEmpty(user.PasswordHash);

        // 2. Получаем список внешних провайдеров (Google, Facebook...)
        var logins = await _userManager.GetLoginsAsync(user);
        var providers = logins.Select(l => l.LoginProvider).ToList();

        return new UserAuthStatusDto
        {
            HasPassword = hasPassword,
            LinkedProviders = providers
        };
    }

    public async Task AddPasswordAsync(string userId, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) throw new KeyNotFoundException("User not found");

        if (!string.IsNullOrEmpty(user.PasswordHash))
        {
            throw new InvalidOperationException("У пользователя уже есть пароль. Используйте смену пароля.");
        }

        var result = await _userManager.AddPasswordAsync(user, newPassword);
        if (!result.Succeeded)
        {
            throw new ArgumentException("Ошибка создания пароля: " +
                                        string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    public async Task UnlinkProviderAsync(string userId, string providerName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) throw new KeyNotFoundException("User not found");

        // ЗАЩИТА: Нельзя отвязать последнее средство входа!
        // Если нет пароля И всего один провайдер - запрещаем.
        var logins = await _userManager.GetLoginsAsync(user);
        var hasPassword = !string.IsNullOrEmpty(user.PasswordHash);

        if (!hasPassword && logins.Count <= 1)
        {
            throw new InvalidOperationException(
                "Нельзя отвязать единственный способ входа. Сначала добавьте пароль или другой соц. аккаунт.");
        }

        // Ищем providerKey (это уникальный ID в гугле), который привязан к этому провайдеру
        var loginInfo = logins.FirstOrDefault(l =>
            l.LoginProvider.Equals(providerName, StringComparison.InvariantCultureIgnoreCase));

        if (loginInfo == null)
        {
            throw new ArgumentException($"Провайдер {providerName} не привязан к аккаунту.");
        }

        var result = await _userManager.RemoveLoginAsync(user, loginInfo.LoginProvider, loginInfo.ProviderKey);
        if (!result.Succeeded)
        {
            throw new Exception("Не удалось отвязать провайдер.");
        }
    }

    public async Task InitiateChangeEmailAsync(string userId, string newEmail)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) throw new KeyNotFoundException("User not found");

        // Проверяем, не занят ли email другим юзером
        var existingUser = await _userManager.FindByEmailAsync(newEmail);
        if (existingUser != null) throw new ArgumentException("Этот Email уже занят.");

        // Генерируем код (то же самое поле, что и при регистрации)
        user.VerificationCode = new Random().Next(100000, 999999).ToString();
        user.VerificationCodeExpiresAt = DateTime.UtcNow.AddMinutes(15);

        await _userManager.UpdateAsync(user);

        // Шлем письмо НА НОВЫЙ ЯЩИК
        string emailBody = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Zmena e-mailu</title>
                <style>
                    /* Základné štýly */
                    body {{ margin: 0; padding: 0; background-color: #f3f4f6; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; }}
                    .wrapper {{ width: 100%; table-layout: fixed; background-color: #f3f4f6; padding-bottom: 40px; }}
                    .webkit {{ max-width: 600px; background-color: #ffffff; margin: 0 auto; border-radius: 30px; overflow: hidden; box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1); }}
                    
                    /* Štýl pre kód */
                    .code-container {{
                        background-color: #fffdf5;
                        border: 2px dashed #ffc800; /* Žlté orámovanie */
                        border-radius: 20px;
                        padding: 24px;
                        margin: 30px 0;
                        text-align: center;
                    }}
                    .code-text {{
                        font-size: 36px;
                        font-weight: bold;
                        letter-spacing: 8px;
                        color: #333333;
                        font-family: 'Consolas', 'Courier New', monospace;
                    }}
                    
                    /* Texty */
                    .header-title {{ color: #ffffff; font-size: 28px; font-weight: bold; margin: 0; letter-spacing: 1px; }}
                    .content-title {{ color: #27a19b; font-size: 24px; font-weight: bold; margin-top: 0; }}
                    .text-body {{ color: #4b5563; font-size: 16px; line-height: 1.6; margin: 0; }}
                    .text-warning {{ color: #b45309; font-size: 14px; background-color: #fffbeb; padding: 10px; border-radius: 8px; margin-top: 20px; }}
                    .footer-text {{ color: #9ca3af; font-size: 12px; }}
                </style>
            </head>
            <body>
                <center class='wrapper'>
                    <div class='webkit'>
                        <table role='presentation' cellspacing='0' cellpadding='0' border='0' width='100%'>
                            
                            <tr>
                                <td style='background-color: #27a19b; padding: 40px 20px; text-align: center;'>
                                    <h1 class='header-title'>Tringelty</h1>
                                </td>
                            </tr>

                            <tr>
                                <td style='padding: 40px 30px;'>
                                    <div style='text-align: center;'>
                                        <h2 class='content-title'>Potvrdenie zmeny e-mailu</h2>
                                        <p class='text-body'>
                                            Prijali sme žiadosť o nastavenie tejto e-mailovej adresy ako novej adresy pre váš účet Tringelty.
                                        </p>
                                        <p class='text-body' style='margin-top: 10px;'>
                                            Na potvrdenie zmeny použite tento kód:
                                        </p>
                                    </div>

                                    <div class='code-container'>
                                        <span class='code-text'>{user.VerificationCode}</span>
                                    </div>

                                    <div style='text-align: center;'>
                                        <p class='text-body'>
                                            Kód je platný <strong>10 minút</strong>.
                                        </p>
                                        
                                        <div class='text-warning'>
                                            Ak ste o zmenu e-mailu nežiadali, tento kód nikomu neposielajte a správu ignorujte.
                                        </div>
                                    </div>
                                </td>
                            </tr>

                            <tr>
                                <td style='padding: 0 30px;'>
                                    <div style='height: 1px; background-color: #e5e7eb; width: 100%;'></div>
                                </td>
                            </tr>

                            <tr>
                                <td style='padding: 30px; text-align: center; background-color: #ffffff;'>
                                    <p class='footer-text'>
                                        Táto správa bola odoslaná automaticky.<br>
                                        Váš starý e-mail zostane aktívny, kým nepotvrdíte túto zmenu.
                                    </p>
                                    <p class='footer-text' style='margin-top: 20px; font-weight: bold;'>
                                        &copy; {DateTime.Now.Year} Tringelty
                                    </p>
                                </td>
                            </tr>
                        </table>
                    </div>
                </center>
            </body>
            </html>";

            // Odoslanie na NOVÝ email
            await _emailService.SendEmailAsync(newEmail, "Kód pre zmenu e-mailu - Tringelty", emailBody);
    }

    public async Task ConfirmChangeEmailAsync(string userId, ConfirmChangeEmailDto request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) throw new KeyNotFoundException("User not found");

        // Проверка кода
        if (user.VerificationCode != request.Code) throw new ArgumentException("Неверный код");
        if (user.VerificationCodeExpiresAt < DateTime.UtcNow) throw new ArgumentException("Код истек");

        // Меняем Email
        // Важно: В Identity Email и UserName часто совпадают, меняем оба
        user.Email = request.NewEmail;
        user.UserName = request.NewEmail;
        user.EmailConfirmed = true; // Считаем подтвержденным, так как код пришел туда
        user.VerificationCode = null; // Чистим код

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new Exception("Не удалось обновить пользователя в БД");
        }
    }
    
    public async Task<UserContextDto> GetUserContextAsync(string userId)
    {
        // 1. Проверяем, владелец ли это (есть ли у него бизнес)
        var business = await _businessRepository.GetBusinessByOwnerIdAsync(Guid.Parse(userId));
        if (business != null)
        {
            var workerOwner = await _businessRepository.GetWorkerByLinkedUserIdAsync(userId);
            return new UserContextDto 
            { 
                UserId = Guid.Parse(userId), 
                Role = "Owner", 
                BusinessId = business.Id,
                WorkerId = workerOwner.Id,
            };
        }

        // 2. Проверяем, прилинкованный ли это работник
        var worker = await _businessRepository.GetWorkerByLinkedUserIdAsync(userId);
        if (worker != null)
        {
            return new UserContextDto 
            { 
                UserId = Guid.Parse(userId), 
                Role = "Worker", 
                WorkerId = worker.Id,
                BusinessId = worker.BusinessId // Ему нужно знать ID бизнеса, чтобы грузить стили
            };
        }

        // 3. Просто новый юзер без ролей
        return new UserContextDto { Role = "New" };
    }
}