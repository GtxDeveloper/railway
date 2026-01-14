using System.ComponentModel.DataAnnotations;

namespace Tringelty.Core.DTOs;

// Ответ фронтенду: что у меня подключено?
public class UserAuthStatusDto
{
    public bool HasPassword { get; set; }
    public List<string> LinkedProviders { get; set; } = new();
}

// Запрос на добавление пароля
public class AddPasswordDto
{
    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
}

// Запрос на отвязку (например, provider = "Google")
public class UnlinkProviderDto
{
    [Required]
    public string Provider { get; set; } = string.Empty;
}