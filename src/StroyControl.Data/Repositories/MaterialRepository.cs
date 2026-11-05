using Microsoft.EntityFrameworkCore;
using StroyControl.Core.Entities;
using StroyControl.Core.Interfaces;

namespace StroyControl.Data.Repositories;

public sealed class MaterialRepository : RepositoryBase<Material>, IMaterialRepository
{
    public MaterialRepository(IDbContextFactory<AppDbContext> factory) : base(factory)
    {
    }

    public async Task<IReadOnlyList<Material>> GetAllWithSupplierAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await Factory.CreateDbContextAsync(cancellationToken);
        return await db.Materials
            .AsNoTracking()
            .Include(m => m.Supplier)
            .OrderBy(m => m.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await Factory.CreateDbContextAsync(cancellationToken);
        return await db.Materials
            .AsNoTracking()
            .Where(m => m.Category != string.Empty)
            .Select(m => m.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);
    }
}
