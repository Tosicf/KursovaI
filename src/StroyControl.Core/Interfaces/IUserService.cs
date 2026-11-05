using StroyControl.Core.Entities;
using StroyControl.Core.Enums;

namespace StroyControl.Core.Interfaces;

public interface IUserService
{
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<User> CreateAsync(string login, string displayName, UserRole role, string password, string? email = null, string? phone = null, CancellationToken cancellationToken = default);

    Task UpdateAsync(User user, CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(int userId, string newPassword, CancellationToken cancellationToken = default);

    Task SetActiveAsync(int userId, bool isActive, CancellationToken cancellationToken = default);

    Task DeleteAsync(int userId, CancellationToken cancellationToken = default);
}
