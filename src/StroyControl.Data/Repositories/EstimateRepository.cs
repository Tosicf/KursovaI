using Microsoft.EntityFrameworkCore;
using StroyControl.Core.Diagnostics;
using StroyControl.Core.Entities;
using StroyControl.Core.Enums;
using StroyControl.Core.Exceptions;
using StroyControl.Core.Interfaces;

namespace StroyControl.Data.Repositories;

/// <summary>
/// Сметы и их позиции. После подтверждения смета защищена от изменений:
/// репозиторий — последняя линия обороны, он проверяет состояние документа
/// независимо от того, что показывает интерфейс.
/// </summary>
public sealed class EstimateRepository : RepositoryBase<Estimate>, IEstimateRepository
{
    private const string LockedMessage =
        "Смета подтверждена и защищена от изменений: состав и суммы зафиксированы. " +
        "Изменять можно только черновик.";

    private const string ArchivedMessage =
        "Смета находится в архиве: изменение запрещено, доступны только просмотр и отчёты.";

    private readonly IServerLog _log;

    public EstimateRepository(IDbContextFactory<AppDbContext> factory, IServerLog log) : base(factory)
    {
        _log = log;
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

    public async Task<IReadOnlyList<Estimate>> GetArchivedAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await Factory.CreateDbContextAsync(cancellationToken);
        var archived = await db.Estimates
            .AsNoTracking()
            .Include(e => e.Project)
            .Include(e => e.Items)
            .ThenInclude(i => i.Material)
            .Where(e => e.Status == EstimateStatus.Archived)
            .OrderByDescending(e => e.UpdatedAt)
            .ThenBy(e => e.Number)
            .ToListAsync(cancellationToken);

        _log.Info("Сметы", $"Архив: загружено смет — {archived.Count}");
        return archived;
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
                     ?? throw new AppException("Смета не найдена или удалена другим пользователем.");

        EnsureDraft(entity.Status, entity.Number);

        entity.Number = estimate.Number;
        entity.Name = estimate.Name;
        entity.ProjectId = estimate.ProjectId;
        entity.Note = estimate.Note;
        entity.MarkupPercent = estimate.MarkupPercent;
        entity.ProfitPercent = estimate.ProfitPercent;
        entity.LaborSharePercent = estimate.LaborSharePercent;
        // Статус меняется только командами «Подтвердить» и «В архив»:
        // иначе правку реквизитов можно было бы использовать для обхода блокировки.

        await SaveAsync(db, cancellationToken, "Не удалось сохранить смету. Проверьте уникальность номера.");

        // Синхронизируем версию строки, чтобы следующее изменение не вызвало ложный конфликт.
        estimate.RowVersion = entity.RowVersion;
        estimate.UpdatedAt = entity.UpdatedAt;
        _log.Info("Сметы", $"Изменены реквизиты сметы {entity.Number} (черновик)");
    }

    public async Task<EstimateItem> AddItemAsync(EstimateItem item, CancellationToken cancellationToken = default)
    {
        await using var db = await Factory.CreateDbContextAsync(cancellationToken);
        var estimate = await db.Estimates.AsNoTracking()
                           .FirstOrDefaultAsync(e => e.Id == item.EstimateId, cancellationToken)
                       ?? throw new AppException("Смета не найдена или удалена другим пользователем.");

        EnsureDraft(estimate.Status, estimate.Number);

        var entity = new EstimateItem
        {
            EstimateId = item.EstimateId,
            MaterialId = item.MaterialId,
            IsWork = item.IsWork,
            WorkName = item.WorkName,
            WorkUnit = item.WorkUnit,
            Section = item.Section,
            PlannedQuantity = item.PlannedQuantity,
            PlannedUnitPrice = item.PlannedUnitPrice,
            Note = item.Note
        };

        db.EstimateItems.Add(entity);
        await SaveAsync(db, cancellationToken, "Не удалось добавить позицию сметы.");
        _log.Info("Сметы", $"Смета {estimate.Number}: добавлена позиция «{entity.MaterialId}»");
        return entity;
    }

    public async Task UpdateItemAsync(EstimateItem item, CancellationToken cancellationToken = default)
    {
        await using var db = await Factory.CreateDbContextAsync(cancellationToken);
        var entity = await db.EstimateItems.FirstOrDefaultAsync(i => i.Id == item.Id, cancellationToken)
                     ?? throw new AppException("Позиция сметы не найдена или удалена.");

        var estimate = await db.Estimates.AsNoTracking()
                           .FirstOrDefaultAsync(e => e.Id == entity.EstimateId, cancellationToken)
                       ?? throw new AppException("Смета не найдена или удалена другим пользователем.");

        EnsureDraft(estimate.Status, estimate.Number);

        entity.MaterialId = item.MaterialId;
        entity.IsWork = item.IsWork;
        entity.WorkName = item.WorkName;
        entity.WorkUnit = item.WorkUnit;
        entity.Section = item.Section;
        entity.PlannedQuantity = item.PlannedQuantity;
        entity.PlannedUnitPrice = item.PlannedUnitPrice;
        entity.Note = item.Note;

        await SaveAsync(db, cancellationToken, "Не удалось сохранить позицию сметы.");
        _log.Info("Сметы", $"Смета {estimate.Number}: изменена позиция #{entity.Id}");
    }

    public async Task DeleteItemAsync(int itemId, CancellationToken cancellationToken = default)
    {
        await using var db = await Factory.CreateDbContextAsync(cancellationToken);
        var entity = await db.EstimateItems.FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken);
        if (entity is null) return;

        var estimate = await db.Estimates.AsNoTracking()
                           .FirstOrDefaultAsync(e => e.Id == entity.EstimateId, cancellationToken)
                       ?? throw new AppException("Смета не найдена или удалена другим пользователем.");

        EnsureDraft(estimate.Status, estimate.Number);

        db.EstimateItems.Remove(entity);
        await SaveAsync(db, cancellationToken, "Не удалось удалить позицию сметы.");
        _log.Info("Сметы", $"Смета {estimate.Number}: удалена позиция #{itemId}");
    }

    /// <summary>
    /// Черновик можно удалить, подтверждённую смету — нет: иначе исчезла бы
    /// история, на которой построен анализ отклонений.
    /// </summary>
    public override async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await Factory.CreateDbContextAsync(cancellationToken);
        var entity = await db.Estimates.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is null) return;

        EnsureDraft(entity.Status, entity.Number);

        db.Estimates.Remove(entity);
        await SaveAsync(db, cancellationToken, "Не удалось удалить запись: на неё ссылаются связанные документы.");
        _log.Info("Сметы", $"Удалён черновик сметы {entity.Number}");
    }

    public async Task ConfirmAsync(int estimateId, CancellationToken cancellationToken = default)
    {
        await using var db = await Factory.CreateDbContextAsync(cancellationToken);
        var entity = await db.Estimates.FirstOrDefaultAsync(e => e.Id == estimateId, cancellationToken)
                     ?? throw new AppException("Смета не найдена или удалена другим пользователем.");

        if (entity.Status != EstimateStatus.Draft)
        {
            _log.Warning("Сметы", $"Отказ в подтверждении {entity.Number}: текущее состояние «{entity.StatusDisplay}»");
            throw new AppException(entity.Status == EstimateStatus.Archived
                ? ArchivedMessage
                : "Смета уже подтверждена — повторное подтверждение не требуется.");
        }

        var itemsCount = await db.EstimateItems.CountAsync(i => i.EstimateId == estimateId, cancellationToken);
        if (itemsCount == 0)
            throw new AppException("В смете нет ни одной позиции: подтверждать пустую смету нельзя.");

        entity.Status = EstimateStatus.Approved;
        entity.UpdatedAt = DateTime.Now;

        await SaveAsync(db, cancellationToken, "Не удалось подтвердить смету.");
        _log.Info("Сметы", $"Смета {entity.Number} подтверждена: позиций {itemsCount}, правки заблокированы");
    }

    public async Task ArchiveAsync(int estimateId, CancellationToken cancellationToken = default)
    {
        await using var db = await Factory.CreateDbContextAsync(cancellationToken);
        var entity = await db.Estimates.FirstOrDefaultAsync(e => e.Id == estimateId, cancellationToken)
                     ?? throw new AppException("Смета не найдена или удалена другим пользователем.");

        if (entity.Status != EstimateStatus.Approved)
        {
            _log.Warning("Сметы", $"Отказ в архивировании {entity.Number}: текущее состояние «{entity.StatusDisplay}»");
            throw new AppException(entity.Status == EstimateStatus.Archived
                ? "Смета уже находится в архиве."
                : "В архив отправляется только подтверждённая смета. Сначала нажмите «Подтвердить».");
        }

        entity.Status = EstimateStatus.Archived;
        entity.UpdatedAt = DateTime.Now;

        await SaveAsync(db, cancellationToken, "Не удалось отправить смету в архив.");
        _log.Info("Сметы", $"Смета {entity.Number} отправлена в архив");
    }

    /// <summary>Проверка состояния документа до любой попытки изменения.</summary>
    private void EnsureDraft(EstimateStatus status, string number)
    {
        if (status == EstimateStatus.Draft) return;

        _log.Warning("Сметы", $"Заблокирована попытка изменить смету {number} в состоянии «{status.ToDisplayName()}»");

        throw new AppException(status == EstimateStatus.Archived ? ArchivedMessage : LockedMessage);
    }
}
