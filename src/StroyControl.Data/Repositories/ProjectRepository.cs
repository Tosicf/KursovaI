using Microsoft.EntityFrameworkCore;
using StroyControl.Core.Entities;
using StroyControl.Core.Interfaces;

namespace StroyControl.Data.Repositories;

public sealed class ProjectRepository : RepositoryBase<Project>, IProjectRepository
{
    public ProjectRepository(IDbContextFactory<AppDbContext> factory) : base(factory)
    {
    }

    public async Task<IReadOnlyList<Project>> GetAllWithEstimatesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await Factory.CreateDbContextAsync(cancellationToken);
        return await db.Projects
            .AsNoTracking()
            .Include(p => p.Estimates)
            .OrderByDescending(p => p.Status == StroyControl.Core.Enums.ProjectStatus.InProgress)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }
}
