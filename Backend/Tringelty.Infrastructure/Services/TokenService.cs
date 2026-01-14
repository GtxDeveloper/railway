using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Tringelty.Core.Entities;
using Tringelty.Core.Interfaces;

namespace Tringelty.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;
    private readonly UserManager<ApplicationUser> _userManager;

    public TokenService(IConfiguration config, UserManager<ApplicationUser> userManager)
    {
        _config = config;
        _userManager = userManager;
    }

    public string GenerateAccessToken(ApplicationUser user)
    {
        var jwtSettings = _config.GetSection("JwtSettings");
        var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var roles = _userManager.GetRolesAsync(user).Result; 
        
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }
        
        var creds = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(15), // TODO: Move expiration to config
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"],
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
    
    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var jwtSettings = _config.GetSection("JwtSettings");
        var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateLifetime = false // <--- САМОЕ ВАЖНОЕ: Разрешаем просроченные токены
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        
        // Пытаемся расшифровать токен
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

        // Проверяем, что это действительно был HMACSHA256 токен
        if (securityToken is not JwtSecurityToken jwtSecurityToken || 
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenException("Invalid token");
        }

        return principal;
    }
}