using StroyControl.Core.Entities;

namespace StroyControl.Core.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> FindByLoginAsync(string login, CancellationToken cancellationToken = default);

    Task<bool> LoginExistsAsync(string login, int? excludeUserId = null, CancellationToken cancellationToken = default);
}
