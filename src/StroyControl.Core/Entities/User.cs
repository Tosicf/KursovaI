using StroyControl.Core.Enums;

namespace StroyControl.Core.Entities;

/// <summary>Учётная запись сотрудника строительной организации.</summary>
public class User : EntityBase
{
    public string Login { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string PasswordSalt { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Engineer;

    public bool IsActive { get; set; } = true;

    public DateTime? LastLoginAt { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string Initials
    {
        get
        {
            var parts = DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "??";
            if (parts.Length == 1) return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpperInvariant();
            return (parts[0][0].ToString() + parts[1][0]).ToUpperInvariant();
        }
    }

    public string RoleDisplay => Role.ToDisplayName();

    public string LastLoginDisplay => LastLoginAt.HasValue
        ? LastLoginAt.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm")
        : "ещё не входил(а)";

    public string StatusDisplay => IsActive ? "Активна" : "Заблокирована";
}
