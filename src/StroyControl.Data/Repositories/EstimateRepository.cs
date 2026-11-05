using Microsoft.EntityFrameworkCore;
using StroyControl.Core.Entities;
using StroyControl.Core.Interfaces;

namespace StroyControl.Data.Repositories;

public sealed class EstimateRepository : RepositoryBase<Estimate>, IEstimateRepository
{
    public EstimateRepository(IDbContextFactory<AppDbContext> factory) : base(factory)
    {
    }

    public async Task<IReadOnlyList<Estimate>> GetAllWithItemsAsync(int? projectId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await Factory.CreateDbContextAsync(cancellationToken);
        var query = db.Estimates
            .AsNoTracking()
            .Include(e => e.Project)
            .Include(e => e.Items)
            .ThenInclude(i => i.Material)
            .AsQueryable();

        if (projectId.HasValue)
            query = query.Where(e => e.ProjectId == projectId.Value);

        return await query.OrderBy(e => e.Number).ToListAsync(cancellationToken);
    }

    public async Task<Estimate?> GetWithItemsAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await Factory.CreateDbContextAsync(cancellationToken);
        return await db.Estimates
            .AsNoTracking()
            .Include(e => e.Project)
            .Include(e => e.Items)
            .ThenInclude(i => i.Material)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task UpdateEstimateAsync(Estimate estimate, CancellationToken cancellationToken = default)
    {
        await using var db = await Factory.CreateDbContextAsync(cancellationToken);
        var entity = await db.Estimates.FirstOrDefaultAsync(e => e.Id == estimate.Id, cancellationToken)
                     ?? throw new StroyControl.Core.Exceptions.AppException("Смета не найдена или удалена другим пользователем.");

        entity.Number = estimate.Number;
        entity.Name = estimate.Name;
        entity.ProjectId = estimate.ProjectId;
        entity.Status = estimate.Status;
        entity.Note = estimate.Note;

        await SaveAsync(db, cancellationToken, "Не удалось сохранить смету. Проверьте уникальность номера.");

        // Синхронизируем версию строки, чтобы следующее изменение не вызвало ложный конфликт.
        estimate.RowVersion = entity.RowVersion;
        estimate.UpdatedAt = entity.UpdatedAt;
    }

    public async Task<EstimateItem> AddItemAsync(EstimateItem item, CancellationToken cancellationToken = default)
    {
        await using var db = await Factory.CreateDbContextAsync(cancellationToken);
        var entity = new EstimateItem
        {
            EstimateId = item.EstimateId,
            MaterialId = item.MaterialId,
            Section = item.Section,
            PlannedQuantity = item.PlannedQuantity,
            PlannedUnitPrice = item.PlannedUnitPrice,
            Note = item.Note
        };

        db.EstimateItems.Add(entity);
        await SaveAsync(db, cancellationToken, "Не удалось добавить позицию сметы.");
        return entity;
    }

    public async Task UpdateItemAsync(EstimateItem item, CancellationToken cancellationToken = default)
    {
        await using var db = await Factory.CreateDbContextAsync(cancellationToken);
        var entity = await db.EstimateItems.FirstOrDefaultAsync(i => i.Id == item.Id, cancellationToken)
                     ?? throw new StroyControl.Core.Exceptions.AppException("Позиция сметы не найдена или удалена.");

        entity.MaterialId = item.MaterialId;
        entity.Section = item.Section;
        entity.PlannedQuantity = item.PlannedQuantity;
        entity.PlannedUnitPrice = item.PlannedUnitPrice;
        entity.Note = item.Note;

        await SaveAsync(db, cancellationToken, "Не удалось сохранить позицию сметы.");
    }

    public async Task DeleteItemAsync(int itemId, CancellationToken cancellationToken = default)
    {
        await using var db = await Factory.CreateDbContextAsync(cancellationToken);
        var entity = await db.EstimateItems.FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken);
        if (entity is null) return;

        db.EstimateItems.Remove(entity);
        await SaveAsync(db, cancellationToken, "Не удалось удалить позицию сметы.");
    }
}
