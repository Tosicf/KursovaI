using Microsoft.EntityFrameworkCore;
using StroyControl.Core.Entities;
using StroyControl.Core.Interfaces;

namespace StroyControl.Data.Repositories;

public sealed class UserRepository : RepositoryBase<User>, IUserRepository
{
    public UserRepository(IDbContextFactory<AppDbContext> factory) : base(factory)
    {
    }

    public override async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await Factory.CreateDbContextAsync(cancellationToken);
        return await db.Users.AsNoTracking().OrderBy(u => u.DisplayName).ToListAsync(cancellationToken);
    }

    public async Task<User?> FindByLoginAsync(string login, CancellationToken cancellationToken = default)
    {
        await using var db = await Factory.CreateDbContextAsync(cancellationToken);
        return await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Login == login, cancellationToken);
    }

    public async Task<bool> LoginExistsAsync(string login, int? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await Factory.CreateDbContextAsync(cancellationToken);
        var query = db.Users.AsNoTracking().Where(u => u.Login == login);
        if (excludeUserId.HasValue)
            query = query.Where(u => u.Id != excludeUserId.Value);

        return await query.AnyAsync(cancellationToken);
    }
}
