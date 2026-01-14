using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Tringelty.Core.DTOs;
using Tringelty.Core.Entities;
using Tringelty.Core.Interfaces;

namespace Tringelty.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IBusinessRepository _businessRepository;
    private readonly IEmailService _emailService;
    private readonly IWorkerInvitationRepository _workerInvitationRepository;
    private readonly IConfiguration _configuration;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IBusinessRepository businessRepository,
        IEmailService emailService,
        IWorkerInvitationRepository workerInvitationRepository,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _businessRepository = businessRepository;
        _emailService = emailService;
        _workerInvitationRepository = workerInvitationRepository;
        _configuration = configuration;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        if (!user.EmailConfirmed)
            throw new UnauthorizedAccessException("Email не подтвержден. Введите код.");

        return await GenerateAuthResponse(user);
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto request)
    {
        // 1. Достаем юзера из просроченного токена
        var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        var email = principal.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)
            ?.Value; // Или NameIdentifier для ID

        if (email == null) throw new SecurityTokenException("Invalid token");

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null) throw new SecurityTokenException("User not found");

        // 2. Проверяем Refresh Token (Совпадает? Не истек?)
        if (user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            throw new SecurityTokenException("Invalid refresh token");
        }

        // 3. Генерируем НОВУЮ пару
        var newAccessToken = _tokenService.GenerateAccessToken(user);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        // 4. Обновляем в базе (старый рефреш умирает, это называется Refresh Token Rotation)
        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _userManager.UpdateAsync(user);

        return new AuthResponseDto
        {
            Token = newAccessToken,
            RefreshToken = newRefreshToken,
            UserId = user.Id,
            Email = user.Email!
        };
    }

    public async Task RegisterAsync(RegisterDto request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);

        if (existingUser != null)
        {
            // Сценарий А: Пользователь уже существует и подтвердил почту
            if (existingUser.EmailConfirmed)
            {
                throw new ArgumentException("Пользователь с таким email уже существует.");
            }

            // Сценарий Б: Пользователь существует, но НЕ подтвердил почту (застрял)
            // Мы просто обновляем код и отправляем письмо заново.
            // Не создаем заново Business/Worker, так как они создались в первой попытке.
        
            existingUser.VerificationCode = new Random().Next(100000, 999999).ToString();
            existingUser.VerificationCodeExpiresAt = DateTime.UtcNow.AddMinutes(15);
        
            // Можно обновить пароль, если пользователь решил его сменить при повторной попытке
            // await _userManager.RemovePasswordAsync(existingUser);
            // await _userManager.AddPasswordAsync(existingUser, request.Password);

            await _userManager.UpdateAsync(existingUser);

            // Отправляем письмо снова
            string body = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Overovací kód</title>
                <style>
                    /* Základné štýly */
                    body {{ margin: 0; padding: 0; background-color: #f3f4f6; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; }}
                    .wrapper {{ width: 100%; table-layout: fixed; background-color: #f3f4f6; padding-bottom: 40px; }}
                    .webkit {{ max-width: 600px; background-color: #ffffff; margin: 0 auto; border-radius: 30px; overflow: hidden; box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1); }}
                    
                    /* Štýl pre kód */
                    .code-container {{
                        background-color: #fffdf5; /* Jemný nádych žltej */
                        border: 2px dashed #ffc800; /* Vaša žltá farba */
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
                        font-family: 'Consolas', 'Courier New', monospace; /* Monospace pre lepšiu čitateľnosť čísel */
                    }}
                    
                    /* Texty */
                    .header-title {{ color: #ffffff; font-size: 28px; font-weight: bold; margin: 0; letter-spacing: 1px; }}
                    .content-title {{ color: #27a19b; font-size: 24px; font-weight: bold; margin-top: 0; }}
                    .text-body {{ color: #4b5563; font-size: 16px; line-height: 1.6; margin: 0; }}
                    .text-small {{ color: #9ca3af; font-size: 13px; line-height: 1.5; }}
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
                                        <h2 class='content-title'>Váš overovací kód</h2>
                                        <p class='text-body'>
                                            Použite tento kód na dokončenie prihlásenia alebo registrácie v aplikácii Tringelty.
                                        </p>
                                    </div>

                                    <div class='code-container'>
                                        <span class='code-text'>{existingUser.VerificationCode}</span>
                                    </div>

                                    <div style='text-align: center;'>
                                        <p class='text-body' style='margin-bottom: 10px;'>
                                            <strong>Tento kód nikomu nehovorte.</strong>
                                        </p>
                                        <p class='text-small'>
                                            Naši zamestnanci ho od vás nikdy nebudú pýtať.<br>
                                            Kód je platný 10 minút.
                                        </p>
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
                                        Tento e-mail ste dostali, pretože ste požiadali o kód v aplikácii Tringelty.<br>
                                        Ak ste to neboli vy, tento e-mail môžete ignorovať.
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
            await _emailService.SendEmailAsync(existingUser.Email, "Váš overovací kód Tringelty", body);
        
            return; // ВАЖНО: Выходим, чтобы не создавать дубликаты бизнеса ниже
        }
        // --- ВАЛИДАЦИЯ ---
        // Если нет токена, значит это Владелец -> Brand обязателен
        if (string.IsNullOrEmpty(request.InviteToken) && string.IsNullOrEmpty(request.Brand))
        {
            throw new ArgumentException("Название компании (Brand) обязательно для регистрации владельца.");
        }

        // 1. Создаем Identity User (Общая часть для всех)
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.Name,
            LastName = request.Surname,
            PhoneNumber = request.Phone,
            City = request.City,

            // Генерация кода для подтверждения почты
            VerificationCode = new Random().Next(100000, 999999).ToString(),
            VerificationCodeExpiresAt = DateTime.UtcNow.AddMinutes(15)
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new ArgumentException($"Registration failed: {errors}");
        }

        // 2. РАЗВИЛКА: СОТРУДНИК или ВЛАДЕЛЕЦ

        if (!string.IsNullOrEmpty(request.InviteToken))
        {
            // ==========================
            // ЛОГИКА ДЛЯ СОТРУДНИКА
            // ==========================

            // Ищем инвайт в базе
            var invite = await _workerInvitationRepository.GetByTokenAsync(request.InviteToken);

            if (invite == null) throw new ArgumentException("Неверный токен приглашения");
            if (invite.IsUsed) throw new ArgumentException("Это приглашение уже использовано");
            if (invite.ExpiresAt < DateTime.UtcNow) throw new ArgumentException("Срок действия приглашения истек");

            // Получаем воркера, для которого создали инвайт
            var worker = invite.Worker;
            if (worker == null) throw new Exception("Worker not found for this invite");

            // Привязываем созданного User к существующему Worker
            worker.LinkedUserId = user.Id;
            worker.IsLinked = true;
            worker.Name = $"{request.Name} {request.Surname}"; // Обновляем имя воркера реальными данными юзера

            // Помечаем инвайт как использованный
            invite.IsUsed = true;

            // Сохраняем изменения инвайта и воркера
            await _workerInvitationRepository.SaveChangesAsync();
            // (Если Worker сохраняется через другой репозиторий, вызовите UpdateWorkerAsync)
        }
        else
        {
            // ==========================
            // ЛОГИКА ДЛЯ ВЛАДЕЛЬЦА
            // ==========================

            // Создаем Бизнес
            var business = new Business
            {
                Id = Guid.NewGuid(),
                Name = request.Brand!, // Тут мы уверены, что Brand есть, благодаря проверке в начале
                OwnerId = Guid.Parse(user.Id)
            };

            await _businessRepository.AddAsync(business);

            // Создаем Воркера-Владельца (самого себя)
            var worker = new Worker
            {
                Id = Guid.NewGuid(),
                Name = $"{request.Name} {request.Surname}",
                BusinessId = business.Id,
                Job = "Owner",
                LinkedUserId = user.Id, // Сразу связываем
                IsLinked = true
            };

            await _businessRepository.AddWorkerAsync(worker);
            await _businessRepository.SaveChangesAsync();
        }

        // 3. Отправка Email (Общая часть)
        // Пользователь создан, но почта еще не подтверждена.
        // Ему нужно ввести код, чтобы зайти и получить токен.
        string emailBody = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Overovací kód</title>
                <style>
                    /* Základné štýly */
                    body {{ margin: 0; padding: 0; background-color: #f3f4f6; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; }}
                    .wrapper {{ width: 100%; table-layout: fixed; background-color: #f3f4f6; padding-bottom: 40px; }}
                    .webkit {{ max-width: 600px; background-color: #ffffff; margin: 0 auto; border-radius: 30px; overflow: hidden; box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1); }}
                    
                    /* Štýl pre kód */
                    .code-container {{
                        background-color: #fffdf5; /* Jemný nádych žltej */
                        border: 2px dashed #ffc800; /* Vaša žltá farba */
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
                        font-family: 'Consolas', 'Courier New', monospace; /* Monospace pre lepšiu čitateľnosť čísel */
                    }}
                    
                    /* Texty */
                    .header-title {{ color: #ffffff; font-size: 28px; font-weight: bold; margin: 0; letter-spacing: 1px; }}
                    .content-title {{ color: #27a19b; font-size: 24px; font-weight: bold; margin-top: 0; }}
                    .text-body {{ color: #4b5563; font-size: 16px; line-height: 1.6; margin: 0; }}
                    .text-small {{ color: #9ca3af; font-size: 13px; line-height: 1.5; }}
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
                                        <h2 class='content-title'>Váš overovací kód</h2>
                                        <p class='text-body'>
                                            Použite tento kód na dokončenie prihlásenia alebo registrácie v aplikácii Tringelty.
                                        </p>
                                    </div>

                                    <div class='code-container'>
                                        <span class='code-text'>{user.VerificationCode}</span>
                                    </div>

                                    <div style='text-align: center;'>
                                        <p class='text-body' style='margin-bottom: 10px;'>
                                            <strong>Tento kód nikomu nehovorte.</strong>
                                        </p>
                                        <p class='text-small'>
                                            Naši zamestnanci ho od vás nikdy nebudú pýtať.<br>
                                            Kód je platný 10 minút.
                                        </p>
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
                                        Tento e-mail ste dostali, pretože ste požiadali o kód v aplikácii Tringelty.<br>
                                        Ak ste to neboli vy, tento e-mail môžete ignorovať.
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

        // 2. Odoslanie
        await _emailService.SendEmailAsync(user.Email, "Váš overovací kód Tringelty", emailBody);
    }

    public async Task<AuthResponseDto> VerifyEmailAsync(VerifyEmailDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null) throw new ArgumentException("User not found");

        // Если уже подтвержден - просто логиним
        if (user.EmailConfirmed) return await GenerateAuthResponse(user);

        // Проверка кода
        if (user.VerificationCode != request.Code) throw new ArgumentException("Неверный код");
        if (user.VerificationCodeExpiresAt < DateTime.UtcNow) throw new ArgumentException("Код истек");

        // Активация
        user.EmailConfirmed = true;
        user.VerificationCode = null;

        await _userManager.UpdateAsync(user);

        // Выдаем роль User (или можно разграничить роли тут, если нужно)
        await _userManager.AddToRoleAsync(user, "User");

        // Генерируем JWT (внутри этого метода вызови GetUserContextAsync, чтобы в токене или ответе вернулась правильная роль Owner/Worker)
        return await GenerateAuthResponse(user);
    }


    private async Task<AuthResponseDto> GenerateAuthResponse(ApplicationUser user)
    {
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        await _userManager.UpdateAsync(user);

        return new AuthResponseDto
        {
            Token = accessToken,
            RefreshToken = refreshToken,
            UserId = user.Id,
            Email = user.Email!
        };
    }

    public async Task LogoutAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return; // Если юзера нет, считаем, что он и так вышел

        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = DateTime.MinValue;

        await _userManager.UpdateAsync(user);
    }

    public async Task ForgotPasswordAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        // ВАЖНО: Если юзера нет, мы НЕ должны говорить "Юзер не найден" (security reasons).
        // Мы просто ничего не делаем или шлем "фейковое" письмо.
        if (user == null) return;

        // Генерируем токен (это длинная строка)
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        var frontendUrl = _configuration["AppSettings:FrontendUrl"];
        
        // Формируем ссылку (на твой Фронтенд!)
        // В реальном проекте домен берем из конфига
        // Токен нужно закодировать, т.к. в нем могут быть символы "+", "/"
        var encodedToken = System.Web.HttpUtility.UrlEncode(token);
        var link = $"{frontendUrl}/reset-password?token={encodedToken}&email={email}";

        // 1. Vytvorenie HTML tela e-mailu
        string emailBody = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <title>Obnovenie hesla</title>
            <style>
                /* Základné štýly */
                body {{ margin: 0; padding: 0; background-color: #f3f4f6; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; }}
                table {{ border-spacing: 0; width: 100%; }}
                td {{ padding: 0; }}
                img {{ border: 0; }}
                
                /* Kontajner */
                .wrapper {{ width: 100%; table-layout: fixed; background-color: #f3f4f6; padding-bottom: 40px; }}
                .webkit {{ max-width: 600px; background-color: #ffffff; margin: 0 auto; border-radius: 30px; overflow: hidden; box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -2px rgba(0, 0, 0, 0.05); }}
                
                /* Tlačidlo */
                .btn {{ 
                    background-color: #ffc800; 
                    color: #000000; 
                    padding: 16px 40px; 
                    border-radius: 9999px; /* Plne zaoblené ako na webe */
                    text-decoration: none; 
                    font-weight: bold; 
                    font-size: 18px; 
                    display: inline-block; 
                    text-align: center;
                    mso-padding-alt: 0;
                    text-transform: none; /* Raleway štýl */
                }}
                .btn:hover {{ background-color: #e6b400; }}
                
                /* Texty */
                .header-title {{ color: #ffffff; font-size: 28px; font-weight: bold; margin: 0; letter-spacing: 0.5px; }}
                .content-title {{ color: #27a19b; font-size: 24px; font-weight: bold; margin-top: 0; margin-bottom: 20px; }}
                .text-body {{ color: #4b5563; font-size: 16px; line-height: 1.6; margin-bottom: 30px; }}
                .footer-text {{ color: #9ca3af; font-size: 12px; line-height: 1.5; }}
                .link-fallback {{ color: #27a19b; word-break: break-all; text-decoration: none; font-weight: bold; }}
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
                            <td style='padding: 40px 30px; text-align: center;'>
                                <h2 class='content-title'>Obnovenie zabudnutého hesla</h2>
                                
                                <p class='text-body'>
                                    Dostali sme požiadavku na zmenu hesla pre váš účet. <br>
                                    Ak chcete pokračovať a vytvoriť si nové heslo, kliknite na tlačidlo nižšie:
                                </p>

                                <table role='presentation' cellspacing='0' cellpadding='0' border='0' width='100%'>
                                    <tr>
                                        <td align='center' style='padding-bottom: 30px;'>
                                            <a href='{link}' class='btn'>
                                                <span style='mso-text-raise: 15pt;'>Obnoviť heslo</span>
                                                </a>
                                        </td>
                                    </tr>
                                </table>

                                <p class='text-body' style='font-size: 14px; margin-bottom: 0;'>
                                    Tento odkaz je platný <strong>24 hodín</strong>.
                                </p>
                            </td>
                        </tr>

                        <tr>
                            <td style='padding: 0 30px;'>
                                <div style='height: 1px; background-color: #e5e7eb; width: 100%;'></div>
                            </td>
                        </tr>

                        <tr>
                            <td style='padding: 30px; text-align: center; background-color: #ffffff;'>
                                <p class='footer-text' style='margin-bottom: 20px;'>
                                    Ak tlačidlo vyššie nefunguje, skopírujte a vložte nasledujúci odkaz do svojho prehliadača:<br>
                                    <a href='{link}' class='link-fallback'>{link}</a>
                                </p>
                                
                                <p class='footer-text'>
                                    Ak ste o zmenu hesla nežiadali, tento e-mail môžete ignorovať.<br>
                                    Váš účet je v bezpečí.
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


        await _emailService.SendEmailAsync(email, "Obnovenie hesla - Tringelty", emailBody);
    }

    public async Task ResetPasswordAsync(ResetPasswordDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null) throw new ArgumentException("Invalid request");

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new ArgumentException(errors);
        }
    }
}