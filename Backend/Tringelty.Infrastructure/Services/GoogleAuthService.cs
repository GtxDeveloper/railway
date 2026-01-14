using Google.Apis.Auth; // NuGet: Google.Apis.Auth
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Tringelty.Core.DTOs;
using Tringelty.Core.Entities;
using Tringelty.Core.Interfaces;

namespace Tringelty.Infrastructure.Services;

public class GoogleAuthService : IGoogleAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ITokenService _tokenService;
    private readonly IBusinessRepository _businessRepository;

    public GoogleAuthService(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ITokenService tokenService,
        IBusinessRepository businessRepository)
    {
        _userManager = userManager;
        _configuration = configuration;
        _tokenService = tokenService;
        _businessRepository = businessRepository;
    }

    public async Task<AuthResponseDto> LoginAsync(GoogleLoginDto request)
    {
        var payload = await ValidateGoogleToken(request.IdToken);

        var user = await _userManager.FindByEmailAsync(payload.Email);
        if (user == null)
        {
            // Сигнал контроллеру: "Юзера нет, нужна форма регистрации"
            throw new KeyNotFoundException("User not registered");
        }
        
        // Проверяем: привязан ли уже Google к этому аккаунту?
        var logins = await _userManager.GetLoginsAsync(user);
        var googleLogin = logins.FirstOrDefault(l => l.LoginProvider == "Google");

        if (googleLogin == null)
        {
            // Если Email совпал, но привязки нет -> Доверяем Google и создаем привязку сейчас
            var loginInfo = new UserLoginInfo("Google", payload.Subject, "Google");
            await _userManager.AddLoginAsync(user, loginInfo);
        }

        return await GenerateTokensAndSaveAsync(user);
    }

    public async Task<AuthResponseDto> RegisterAsync(GoogleRegisterDto request)
    {
        var payload = await ValidateGoogleToken(request.IdToken);

        // Защита от дублей
        var existingUser = await _userManager.FindByEmailAsync(payload.Email);
        if (existingUser != null) throw new ArgumentException("User already exists");

        // 1. Создаем Юзера
        var user = new ApplicationUser
        {
            UserName = payload.Email,
            Email = payload.Email,
            EmailConfirmed = true, // Google уже подтвердил
            FirstName = payload.GivenName ?? "User",
            LastName = payload.FamilyName ?? "",
            PhoneNumber = request.Phone,
            City = request.City
        };

        var result = await _userManager.CreateAsync(user); // Без пароля!
        if (!result.Succeeded)
            throw new Exception("Failed to create user: " +
                                string.Join(", ", result.Errors.Select(e => e.Description)));


        // 2. Явно привязываем Google к этому юзеру
        var loginInfo = new UserLoginInfo("Google", payload.Subject, "Google");
        var addLoginResult = await _userManager.AddLoginAsync(user, loginInfo);

        if (!addLoginResult.Succeeded)
        {
            // Логируем ошибку, но не прерываем процесс (не критично, но лучше знать)
            throw new Exception("Failed to link Google account");
        }

        // 3. Создаем Бизнес
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = request.Brand,
            OwnerId = Guid.Parse(user.Id)
        };
        await _businessRepository.AddAsync(business);

        // 4. Создаем Воркера
        var worker = new Worker
        {
            Id = Guid.NewGuid(),
            Name = $"{user.FirstName} {user.LastName}",
            BusinessId = business.Id,
            IsOnboarded = false
        };
        await _businessRepository.AddWorkerAsync(worker);
        await _businessRepository.SaveChangesAsync();

        return await GenerateTokensAndSaveAsync(user);
    }

    // --- Private Helpers ---

    private async Task<GoogleJsonWebSignature.Payload> ValidateGoogleToken(string idToken)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings()
            {
                Audience = new List<string> { _configuration["GoogleAuthSettings:ClientId"]! }
            };
            return await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
        }
        catch (InvalidJwtException)
        {
            throw new ArgumentException("Invalid Google Token");
        }
    }

    // Тот самый DRY метод для токенов
    private async Task<AuthResponseDto> GenerateTokensAndSaveAsync(ApplicationUser user)
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
}