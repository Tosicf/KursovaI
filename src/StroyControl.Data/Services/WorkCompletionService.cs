using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using StroyControl.Core.Entities;
using StroyControl.Core.Enums;
using StroyControl.Core.Exceptions;
using StroyControl.Core.Interfaces;

namespace StroyControl.Data.Services;

/// <summary>
/// Учёт фактических работ и израсходованных на них материалов.
/// Документ, строки работ, списания материалов и остатки склада изменяются в одной
/// транзакции. Главное бизнес-правило: факт учитывается только по смете в состоянии
/// «В работе» — проверка выполняется по базе, поэтому обойти её из интерфейса нельзя.
/// </summary>
public sealed class WorkCompletionService : IWorkCompletionService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public WorkCompletionService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public Task<WorkCompletion> CreateAsync(WorkCompletion completion, CancellationToken cancellationToken = default) =>
        ExecuteInTransactionAsync(
            (db, transaction, ct) => CreateAsyncCore(completion, db, transaction, ct),
            cancellationToken);

    public Task UpdateAsync(WorkCompletion completion, CancellationToken cancellationToken = default) =>
        RunInTransactionAsync(
            (db, transaction, ct) => UpdateAsyncCore(completion, db, transaction, ct),
            cancellationToken);

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        RunInTransactionAsync(
            (db, transaction, ct) => DeleteAsyncCore(id, db, transaction, ct),
            cancellationToken);

    // ------------------------------------------------------------------

    private static async Task<WorkCompletion> CreateAsyncCore(
        WorkCompletion completion,
        AppDbContext db,
        IDbContextTransaction? transaction,
        CancellationToken cancellationToken)
    {
        Validate(completion);

        var estimate = await EnsureEstimateInWorkAsync(db, completion.EstimateId, cancellationToken);

        var entity = new WorkCompletion
        {
            EstimateId = estimate.Id,
            ProjectId = estimate.ProjectId,
            Date = completion.Date.Date,
            DocumentNumber = completion.DocumentNumber.Trim(),
            Note = completion.Note,
            CreatedByUserId = completion.CreatedByUserId
        };

        foreach (var item in NormalizeItems(completion))
        {
            entity.Items.Add(new WorkCompletionItem
            {
                EstimateItemId = item.EstimateItemId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            });
        }

        var materials = await LoadMaterialsAsync(db, completion.MaterialUsages.Select(u => u.MaterialId), cancellationToken);

        foreach (var usage in NormalizeMaterials(completion))
        {
            var material = materials[usage.MaterialId];
            material.StockQuantity -= usage.Quantity;

            entity.MaterialUsages.Add(new MaterialUsage
            {
                ProjectId = estimate.ProjectId,
                MaterialId = usage.MaterialId,
                EstimateId = estimate.Id,
                EstimateItemId = usage.EstimateItemId,
                Quantity = usage.Quantity,
                UnitPrice = usage.UnitPrice > 0m ? usage.UnitPrice : material.Price,
                Date = entity.Date,
                DocumentNumber = entity.DocumentNumber,
                Note = $"Израсходовано по акту выполненных работ {entity.DocumentNumber}",
                CreatedByUserId = completion.CreatedByUserId
            });
        }

        db.WorkCompletions.Add(entity);
        await SaveAsync(db, cancellationToken, "Не удалось сохранить акт выполненных работ. Проверьте номер документа.");
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return entity;
    }

    private static async Task UpdateAsyncCore(
        WorkCompletion completion,
        AppDbContext db,
        IDbContextTransaction? transaction,
        CancellationToken cancellationToken)
    {
        Validate(completion);

        var entity = await db.WorkCompletions
                         .Include(c => c.Items)
                         .Include(c => c.MaterialUsages)
                         .FirstOrDefaultAsync(c => c.Id == completion.Id, cancellationToken)
                     ?? throw new AppException("Акт не найден или удалён другим пользователем.");

        var estimate = await EnsureEstimateInWorkAsync(db, completion.EstimateId, cancellationToken);

        // Сначала возвращаем на склад материалы прошлой редакции акта, затем списываем новые:
        // остаток всегда соответствует действующей версии документа.
        var materials = await LoadMaterialsAsync(
            db,
            entity.MaterialUsages.Select(u => u.MaterialId).Concat(completion.MaterialUsages.Select(u => u.MaterialId)),
            cancellationToken);

        foreach (var usage in entity.MaterialUsages)
        {
            if (materials.TryGetValue(usage.MaterialId, out var oldMaterial))
                oldMaterial.StockQuantity += usage.Quantity;
        }

        db.MaterialUsages.RemoveRange(entity.MaterialUsages);
        db.WorkCompletionItems.RemoveRange(entity.Items);

        entity.EstimateId = estimate.Id;
        entity.ProjectId = estimate.ProjectId;
        entity.Date = completion.Date.Date;
        entity.DocumentNumber = completion.DocumentNumber.Trim();
        entity.Note = completion.Note;

        foreach (var item in NormalizeItems(completion))
        {
            entity.Items.Add(new WorkCompletionItem
            {
                WorkCompletionId = entity.Id,
                EstimateItemId = item.EstimateItemId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            });
        }

        foreach (var usage in NormalizeMaterials(completion))
        {
            var material = materials[usage.MaterialId];
            material.StockQuantity -= usage.Quantity;

            entity.MaterialUsages.Add(new MaterialUsage
            {
                WorkCompletionId = entity.Id,
                ProjectId = estimate.ProjectId,
                MaterialId = usage.MaterialId,
                EstimateId = estimate.Id,
                EstimateItemId = usage.EstimateItemId,
                Quantity = usage.Quantity,
                UnitPrice = usage.UnitPrice > 0m ? usage.UnitPrice : material.Price,
                Date = entity.Date,
                DocumentNumber = entity.DocumentNumber,
                Note = $"Израсходовано по акту выполненных работ {entity.DocumentNumber}",
                CreatedByUserId = completion.CreatedByUserId
            });
        }

        await SaveAsync(db, cancellationToken, "Не удалось сохранить акт выполненных работ. Проверьте номер документа.");
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
    }

    private static async Task DeleteAsyncCore(
        int id,
        AppDbContext db,
        IDbContextTransaction? transaction,
        CancellationToken cancellationToken)
    {
        var entity = await db.WorkCompletions
                         .Include(c => c.Items)
                         .Include(c => c.MaterialUsages)
                         .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
                     ?? throw new AppException("Акт не найден или удалён другим пользователем.");

        // Удаление акта возвращает материалы на склад: документ и остатки снова согласованы.
        var materials = await LoadMaterialsAsync(
            db,
            entity.MaterialUsages.Select(u => u.MaterialId).Distinct(),
            cancellationToken);

        foreach (var usage in entity.MaterialUsages)
        {
            if (materials.TryGetValue(usage.MaterialId, out var material))
                material.StockQuantity += usage.Quantity;
        }

        db.MaterialUsages.RemoveRange(entity.MaterialUsages);
        db.WorkCompletionItems.RemoveRange(entity.Items);
        db.WorkCompletions.Remove(entity);

        await SaveAsync(db, cancellationToken, "Не удалось удалить акт выполненных работ.");
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
    }

    // ------------------------------------------------------------------

    /// <summary>
    /// Факт учитывается только по смете в состоянии «В работе». Черновик, смета на
    /// проверке, просто утверждённая и архивная смета факт не собирают.
    /// </summary>
    private static async Task<Estimate> EnsureEstimateInWorkAsync(
        AppDbContext db,
        int estimateId,
        CancellationToken cancellationToken)
    {
        var estimate = await db.Estimates
                           .AsNoTracking()
                           .FirstOrDefaultAsync(e => e.Id == estimateId, cancellationToken)
                       ?? throw new AppException("Смета не найдена: выберите смету заново.");

        if (estimate.Status != EstimateStatus.InWork)
        {
            throw new AppException(estimate.Status switch
            {
                EstimateStatus.Archived => "Смета в архиве: фактические затраты по ней больше не учитываются.",
                EstimateStatus.Draft => "Смета ещё черновик: сначала утвердите её и переведите в работу.",
                EstimateStatus.OnReview => "Смета на проверке: дождитесь утверждения и перевода в работу.",
                _ => "Смета не в работе: нажмите «В работу», чтобы учитывать фактические затраты."
            });
        }

        return estimate;
    }

    private static void Validate(WorkCompletion completion)
    {
        if (completion.EstimateId <= 0)
            throw new AppException("Выберите смету, по которой учитываются выполненные работы.");

        if (string.IsNullOrWhiteSpace(completion.DocumentNumber))
            throw new AppException("Укажите номер акта выполненных работ.");

        if (completion.Date.Date > DateTime.Today)
            throw new AppException("Дата акта не может быть в будущем.");

        var hasWorks = NormalizeItems(completion).Count > 0;
        var hasMaterials = NormalizeMaterials(completion).Count > 0;

        if (!hasWorks && !hasMaterials)
            throw new AppException("В акте нет ни выполненных работ, ни израсходованных материалов.");
    }

    private static List<WorkCompletionItem> NormalizeItems(WorkCompletion completion) =>
        (completion.Items ?? Array.Empty<WorkCompletionItem>())
            .Where(i => i.Quantity > 0m)
            .Select(i => new WorkCompletionItem
            {
                EstimateItemId = i.EstimateItemId,
                Quantity = Math.Round(i.Quantity, 3),
                UnitPrice = Math.Round(i.UnitPrice, 2)
            })
            .GroupBy(i => i.EstimateItemId)
            .Select(g => new WorkCompletionItem
            {
                EstimateItemId = g.Key,
                Quantity = g.Sum(i => i.Quantity),
                UnitPrice = g.First().UnitPrice
            })
            .ToList();

    private static List<MaterialUsage> NormalizeMaterials(WorkCompletion completion) =>
        (completion.MaterialUsages ?? Array.Empty<MaterialUsage>())
            .Where(u => u.Quantity > 0m)
            .Select(u => new MaterialUsage
            {
                MaterialId = u.MaterialId,
                EstimateItemId = u.EstimateItemId,
                Quantity = Math.Round(u.Quantity, 3),
                UnitPrice = Math.Round(u.UnitPrice, 2)
            })
            .GroupBy(u => (u.MaterialId, u.EstimateItemId))
            .Select(g => new MaterialUsage
            {
                MaterialId = g.Key.MaterialId,
                EstimateItemId = g.Key.EstimateItemId,
                Quantity = g.Sum(u => u.Quantity),
                UnitPrice = g.First().UnitPrice
            })
            .ToList();

    private static async Task<Dictionary<int, Material>> LoadMaterialsAsync(
        AppDbContext db,
        IEnumerable<int> materialIds,
        CancellationToken cancellationToken)
    {
        var ids = materialIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, Material>();

        var materials = await db.Materials
            .Where(m => ids.Contains(m.Id))
            .ToListAsync(cancellationToken);

        var missing = ids.Where(id => materials.All(m => m.Id != id)).ToList();
        if (missing.Count > 0)
            throw new AppException("Материал из строки акта не найден в справочнике.");

        return materials.ToDictionary(m => m.Id);
    }

    /// <summary>
    /// Транзакция для операции без результата. Отдельный метод нужен потому, что
    /// лямбда, возвращающая <c>Task</c>, не даёт вывести <c>T</c> у версии с результатом
    /// (CS0411), а имя с тем же названием делало бы вызовы Update и Delete неоднозначными.
    /// </summary>
    private async Task RunInTransactionAsync(
        Func<AppDbContext, IDbContextTransaction?, CancellationToken, Task> body,
        CancellationToken cancellationToken) =>
        await ExecuteInTransactionAsync<bool>(async (db, transaction, ct) =>
        {
            await body(db, transaction, ct);
            return true;
        }, cancellationToken);

    /// <summary>Транзакция для операции, возвращающей результат.</summary>
    private async Task<T> ExecuteInTransactionAsync<T>(
        Func<AppDbContext, IDbContextTransaction?, CancellationToken, Task<T>> body,
        CancellationToken cancellationToken)
    {
        await using var probe = await _factory.CreateDbContextAsync(cancellationToken);
        var strategy = probe.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var db = await _factory.CreateDbContextAsync(cancellationToken);
            var transaction = db.Database.IsRelational()
                ? await db.Database.BeginTransactionAsync(cancellationToken)
                : null;

            try
            {
                return await body(db, transaction, cancellationToken);
            }
            finally
            {
                if (transaction is not null) await transaction.DisposeAsync();
            }
        });
    }

    private static async Task SaveAsync(AppDbContext db, CancellationToken cancellationToken, string message)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException("Акт уже изменён другим пользователем. Обновите список и повторите попытку.");
        }
        catch (DbUpdateException ex) when (IsDuplicateKey(ex))
        {
            throw new AppException("Номер акта уже используется. Измените номер и повторите сохранение.", ex);
        }
        catch (DbUpdateException ex)
        {
            throw new AppException(message, ex);
        }
    }

    private static bool IsDuplicateKey(DbUpdateException ex)
    {
        for (Exception? current = ex; current is not null; current = current.InnerException)
        {
            if (current is SqlException sql && sql.Number is 2601 or 2627)
                return true;
        }

        return false;
    }
}
