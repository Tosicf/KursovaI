using StroyControl.Core.Entities;
using StroyControl.Core.Enums;
using StroyControl.Core.Exceptions;
using StroyControl.Core.Interfaces;
using StroyControl.Core.Security;

namespace StroyControl.Core.Services;

/// <summary>Управление учётными записями: создание, изменение, блокировка, сброс пароля.</summary>
public sealed class UserService : IUserService
{
    private readonly IUserRepository _users;

    public UserService(IUserRepository users) => _users = users;

    public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _users.GetAllAsync(cancellationToken);

    public async Task<User> CreateAsync(
        string login,
        string displayName,
        UserRole role,
        string password,
        string? email = null,
        string? phone = null,
        CancellationToken cancellationToken = default)
    {
        login = (login ?? string.Empty).Trim();
        if (login.Length < 3)
            throw new AppException("Логин должен содержать не менее 3 символов.");
        if (string.IsNullOrWhiteSpace(displayName))
            throw new AppException("Укажите ФИО сотрудника.");
        var passwordText = password ?? string.Empty;
        if (passwordText.Length < 6)
            throw new AppException("Пароль должен содержать не менее 6 символов.");
        if (await _users.LoginExistsAsync(login, null, cancellationToken))
            throw new AppException($"Логин «{login}» уже используется.");

        var (hash, salt) = PasswordHasher.CreateHash(passwordText);
        var user = new User
        {
            Login = login,
            DisplayName = displayName.Trim(),
            Role = role,
            Email = email,
            Phone = phone,
            PasswordHash = hash,
            PasswordSalt = salt,
            IsActive = true
        };

        return await _users.AddAsync(user, cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(user.DisplayName))
            throw new AppException("Укажите ФИО сотрудника.");
        if (await _users.LoginExistsAsync(user.Login, user.Id, cancellationToken))
            throw new AppException($"Логин «{user.Login}» уже используется.");

        await _users.UpdateAsync(user, cancellationToken);
    }

    public async Task ResetPasswordAsync(int userId, string newPassword, CancellationToken cancellationToken = default)
    {
        var passwordText = newPassword ?? string.Empty;
        if (passwordText.Length < 6)
            throw new AppException("Пароль должен содержать не менее 6 символов.");

        var user = await _users.GetByIdAsync(userId, cancellationToken)
                   ?? throw new AppException("Пользователь не найден.");

        var (hash, salt) = PasswordHasher.CreateHash(passwordText);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        await _users.UpdateAsync(user, cancellationToken);
    }

    public async Task SetActiveAsync(int userId, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken)
                   ?? throw new AppException("Пользователь не найден.");
        user.IsActive = isActive;
        await _users.UpdateAsync(user, cancellationToken);
    }

    public async Task DeleteAsync(int userId, CancellationToken cancellationToken = default)
    {
        var all = await _users.GetAllAsync(cancellationToken);
        var admins = all.Count(u => u.Role == UserRole.Admin && u.IsActive);
        var target = all.FirstOrDefault(u => u.Id == userId);

        if (target is null)
            throw new AppException("Пользователь не найден.");
        if (target.Role == UserRole.Admin && admins <= 1)
            throw new AppException("Нельзя удалить последнего администратора системы.");

        await _users.DeleteAsync(userId, cancellationToken);
    }
}
