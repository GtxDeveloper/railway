using Tringelty.Core.DTOs;

namespace Tringelty.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginDto request);
    Task RegisterAsync(RegisterDto request);
    Task<AuthResponseDto> VerifyEmailAsync(VerifyEmailDto request);
    Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto request);
    Task LogoutAsync(string userId);
    Task ForgotPasswordAsync(string email);
    Task ResetPasswordAsync(ResetPasswordDto request);
}