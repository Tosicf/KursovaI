using Microsoft.EntityFrameworkCore;
using StroyControl.Core.Entities;
using StroyControl.Core.Interfaces;

namespace StroyControl.Data.Repositories;

public sealed class SupplierRepository : RepositoryBase<Supplier>, ISupplierRepository
{
    public SupplierRepository(IDbContextFactory<AppDbContext> factory) : base(factory)
    {
    }

    public async Task<IReadOnlyList<Supplier>> GetAllWithMaterialsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await Factory.CreateDbContextAsync(cancellationToken);
        return await db.Suppliers
            .AsNoTracking()
            .Include(s => s.Materials)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }
}
