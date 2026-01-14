using System.Security.Claims;
using Tringelty.Core.Entities;

namespace Tringelty.Core.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user); // Переименовали для ясности (было GenerateToken)
    string GenerateRefreshToken();
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}