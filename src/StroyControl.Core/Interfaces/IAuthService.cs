using StroyControl.Core.Entities;

namespace StroyControl.Core.Interfaces;

public sealed record AuthResult(bool Success, string? Error, User? User, bool MustChangePassword = false);

public interface IAuthService
{
    Task<AuthResult> SignInAsync(string login, string password, CancellationToken cancellationToken = default);
}
