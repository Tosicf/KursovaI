using Microsoft.EntityFrameworkCore;
using StroyControl.Core.Entities;
using StroyControl.Core.Interfaces;

namespace StroyControl.Data.Repositories;

public sealed class WorkCompletionRepository : RepositoryBase<WorkCompletion>, IWorkCompletionRepository
{
    public WorkCompletionRepository(IDbContextFactory<AppDbContext> factory) : base(factory)
    {
    }

    public async Task<IReadOnlyList<WorkCompletion>> GetAllWithDetailsAsync(
        int? projectId = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await Factory.CreateDbContextAsync(cancellationToken);
        var query = db.WorkCompletions
            .AsNoTracking()
            .Include(c => c.Estimate)
            .Include(c => c.Project)
            .Include(c => c.CreatedBy)
            .Include(c => c.Items).ThenInclude(i => i.EstimateItem)
            .Include(c => c.MaterialUsages).ThenInclude(u => u.Material)
            .AsQueryable();

        if (projectId.HasValue)
            query = query.Where(c => c.ProjectId == projectId.Value);

        return await query
            .OrderByDescending(c => c.Date)
            .ThenByDescending(c => c.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkCompletion?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await Factory.CreateDbContextAsync(cancellationToken);
        return await db.WorkCompletions
            .AsNoTracking()
            .Include(c => c.Estimate)
            .Include(c => c.Project)
            .Include(c => c.CreatedBy)
            .Include(c => c.Items).ThenInclude(i => i.EstimateItem)
            .Include(c => c.MaterialUsages).ThenInclude(u => u.Material)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }
}
