using StroyControl.Core.Enums;
using StroyControl.Core.Diagnostics;
using StroyControl.Core.Interfaces;
using StroyControl.Core.Security;

namespace StroyControl.Core.Services;

/// <summary>Проверка учётных данных при входе в систему.</summary>
public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IServerLog? _log;

    public AuthService(IUserRepository users, IServerLog? log = null)
    {
        _users = users;
        _log = log;
    }

    public async Task<AuthResult> SignInAsync(string login, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            return new AuthResult(false, "Введите логин и пароль.", null);

        var user = await _users.FindByLoginAsync(login.Trim(), cancellationToken);
        if (user is null || !PasswordHasher.Verify(password, user.PasswordSalt, user.PasswordHash))
        {
            if (_log is not null) _log.Warning("Доступ", $"Отказано во входе: логин «{login.Trim()}»");
            return new AuthResult(false, "Неверный логин или пароль.", null);
        }

        if (!user.IsActive)
        {
            if (_log is not null) _log.Warning("Доступ", $"Отказано во входе: учётная запись «{user.Login}» заблокирована");
            return new AuthResult(false, "Учётная запись заблокирована администратором.", null);
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _users.UpdateAsync(user, cancellationToken);

        if (_log is not null) _log.Info("Доступ", $"Вход выполнен: {user.DisplayName} ({user.RoleDisplay})");

        return new AuthResult(true, null, user);
    }
}
