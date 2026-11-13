using Microsoft.EntityFrameworkCore;
using StroyControl.Core.Entities;
using StroyControl.Core.Interfaces;

namespace StroyControl.Data.Repositories;

public sealed class DeliveryRepository : RepositoryBase<Delivery>, IDeliveryRepository
{
    public DeliveryRepository(IDbContextFactory<AppDbContext> factory) : base(factory)
    {
    }

    public async Task<IReadOnlyList<Delivery>> GetAllWithItemsAsync(int? projectId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await Factory.CreateDbContextAsync(cancellationToken);
        var query = db.Deliveries
            .AsNoTracking()
            .Include(d => d.Supplier)
            .Include(d => d.Project)
            .Include(d => d.Items)
            .ThenInclude(i => i.Material)
            .AsQueryable();

        if (projectId.HasValue)
            query = query.Where(d => d.ProjectId == projectId.Value);

        return await query.OrderByDescending(d => d.Date).ThenByDescending(d => d.Id).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetNumbersAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        await using var db = await Factory.CreateDbContextAsync(cancellationToken);
        var day = date.Date;

        return await db.Deliveries
            .AsNoTracking()
            .Where(d => d.Date == day)
            .Select(d => d.Number)
            .ToListAsync(cancellationToken);
    }

    public async Task<Delivery?> GetWithItemsAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await Factory.CreateDbContextAsync(cancellationToken);
        return await db.Deliveries
            .AsNoTracking()
            .Include(d => d.Supplier)
            .Include(d => d.Project)
            .Include(d => d.Items)
            .ThenInclude(i => i.Material)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }
}
