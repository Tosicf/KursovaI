using StroyControl.Core.Interfaces;
using StroyControl.Core.Security;

namespace StroyControl.Core.Services;

/// <summary>Проверка учётных данных при входе в систему.</summary>
public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _users;

    public AuthService(IUserRepository users) => _users = users;

    public async Task<AuthResult> SignInAsync(string login, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            return new AuthResult(false, "Введите логин и пароль.", null);

        var user = await _users.FindByLoginAsync(login.Trim(), cancellationToken);
        if (user is null || !PasswordHasher.Verify(password, user.PasswordSalt, user.PasswordHash))
            return new AuthResult(false, "Неверный логин или пароль.", null);

        if (!user.IsActive)
            return new AuthResult(false, "Учётная запись заблокирована администратором.", null);

        user.LastLoginAt = DateTime.UtcNow;
        await _users.UpdateAsync(user, cancellationToken);

        return new AuthResult(true, null, user);
    }
}
