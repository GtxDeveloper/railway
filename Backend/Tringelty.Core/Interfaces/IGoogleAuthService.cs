using Tringelty.Core.DTOs;

namespace Tringelty.Core.Interfaces;

public interface IGoogleAuthService
{
    // Попытка входа. Если юзера нет - выбросит ошибку KeyNotFoundException
    Task<AuthResponseDto> LoginAsync(GoogleLoginDto request);

    // Полная регистрация (с брендом и городом)
    Task<AuthResponseDto> RegisterAsync(GoogleRegisterDto request);
}