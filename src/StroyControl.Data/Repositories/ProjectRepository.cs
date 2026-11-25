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
        // Сортировка по месяцам: объекты последнего месяца начала работ сверху,
        // внутри месяца — сначала действующие, затем по названию. Сметы подгружаются,
        // чтобы реестр мог показать объект и в месяце, когда по нему составлялись
        // документы (долгострой), а не только в месяце начала работ.
        return await db.Projects
            .AsNoTracking()
            .Include(p => p.Estimates)
            .OrderByDescending(p => p.StartDate)
            .ThenByDescending(p => p.Status == StroyControl.Core.Enums.ProjectStatus.InProgress)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }
}
