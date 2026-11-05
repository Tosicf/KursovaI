using Microsoft.EntityFrameworkCore;
using StroyControl.Core.Entities;
using StroyControl.Core.Interfaces;

namespace StroyControl.Data.Repositories;

public sealed class UsageRepository : RepositoryBase<MaterialUsage>, IUsageRepository
{
    public UsageRepository(IDbContextFactory<AppDbContext> factory) : base(factory)
    {
    }

    public async Task<IReadOnlyList<MaterialUsage>> GetAllWithDetailsAsync(int? projectId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await Factory.CreateDbContextAsync(cancellationToken);
        var query = db.MaterialUsages
            .AsNoTracking()
            .Include(u => u.Material)
            .Include(u => u.Project)
            .Include(u => u.Estimate)
            .Include(u => u.CreatedBy)
            .AsQueryable();

        if (projectId.HasValue)
            query = query.Where(u => u.ProjectId == projectId.Value);

        return await query.OrderByDescending(u => u.Date).ThenByDescending(u => u.Id).ToListAsync(cancellationToken);
    }
}
