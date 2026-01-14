using Microsoft.AspNetCore.Identity;

namespace Tringelty.Core.Entities;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? City { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public string? VerificationCode { get; set; }
    public DateTime? VerificationCodeExpiresAt { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpiryTime { get; set; }
    
    public string? AvatarUrl { get; set; }
}