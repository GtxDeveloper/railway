using Tringelty.Core.DTOs;

namespace Tringelty.Core.Interfaces;

public interface IAccountService
{
    // Профиль и Аватар
    Task<UserProfileDto> GetProfileAsync(string userId);
    Task UpdateProfileAsync(string userId, UpdateProfileDto request);
    Task<string> UploadAvatarAsync(string userId, Stream fileStream, string fileName);

    // Безопасность и настройки
    Task ChangePasswordAsync(string userId, ChangePasswordDto request);
    Task<UserAuthStatusDto> GetAuthStatusAsync(string userId);
    
    // Управление провайдерами
    Task AddPasswordAsync(string userId, string newPassword);
    Task UnlinkProviderAsync(string userId, string providerName);

    // Смена почты
    Task InitiateChangeEmailAsync(string userId, string newEmail);
    Task ConfirmChangeEmailAsync(string userId, ConfirmChangeEmailDto request);

    Task<UserContextDto> GetUserContextAsync(string userId);
}