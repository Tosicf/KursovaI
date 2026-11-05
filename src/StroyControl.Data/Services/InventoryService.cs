using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using StroyControl.Core.Entities;
using StroyControl.Core.Exceptions;
using StroyControl.Core.Interfaces;

namespace StroyControl.Data.Services;

/// <summary>
/// Движение материалов: поступления увеличивают остаток, списания уменьшают.
/// Документ и остаток изменяются в одной транзакции, поэтому при сбое сети
/// данные остаются согласованными.
/// </summary>
public sealed class InventoryService : IInventoryService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public InventoryService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<Delivery> CreateDeliveryAsync(Delivery delivery, CancellationToken cancellationToken = default)
    {
        var items = NormalizeItems(delivery);
        if (items.Count == 0)
            throw new AppException("В поступлении должна быть хотя бы одна строка с материалом и количеством.");

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var transaction = await BeginTransactionAsync(db, cancellationToken);
        try
        {
            var materials = await LoadMaterialsAsync(db, items.Select(i => i.MaterialId), cancellationToken);
            var entity = new Delivery
            {
                Number = delivery.Number.Trim(),
                Date = delivery.Date.Date,
                SupplierId = delivery.SupplierId,
                ProjectId = delivery.ProjectId,
                InvoiceNumber = delivery.InvoiceNumber,
                Note = delivery.Note
            };

            foreach (var item in items)
            {
                materials[item.MaterialId].StockQuantity += item.Quantity;
                entity.Items.Add(new DeliveryItem
                {
                    MaterialId = item.MaterialId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                });
            }

            db.Deliveries.Add(entity);
            await SaveAsync(db, cancellationToken, "Не удалось сохранить поступление. Проверьте номер документа.");
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return entity;
        }
        catch (DbUpdateException ex)
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            throw new AppException("Не удалось сохранить поступление: возможно, номер документа уже используется.", ex);
        }
        finally
        {
            if (transaction is not null) await transaction.DisposeAsync();
        }
    }

    public async Task UpdateDeliveryAsync(Delivery delivery, CancellationToken cancellationToken = default)
    {
        var items = NormalizeItems(delivery);
        if (items.Count == 0)
            throw new AppException("В поступлении должна быть хотя бы одна строка с материалом и количеством.");

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var transaction = await BeginTransactionAsync(db, cancellationToken);
        try
        {
            var existing = await db.Deliveries
                               .Include(d => d.Items)
                               .FirstOrDefaultAsync(d => d.Id == delivery.Id, cancellationToken)
                           ?? throw new AppException("Поступление не найдено: возможно, оно удалено другим пользователем.");

            var materialIds = existing.Items.Select(i => i.MaterialId)
                .Concat(items.Select(i => i.MaterialId))
                .Distinct();
            var materials = await LoadMaterialsAsync(db, materialIds, cancellationToken);

            foreach (var oldItem in existing.Items)
                materials[oldItem.MaterialId].StockQuantity -= oldItem.Quantity;

            existing.Items.Clear();
            existing.Number = delivery.Number.Trim();
            existing.Date = delivery.Date.Date;
            existing.SupplierId = delivery.SupplierId;
            existing.ProjectId = delivery.ProjectId;
            existing.InvoiceNumber = delivery.InvoiceNumber;
            existing.Note = delivery.Note;

            foreach (var item in items)
            {
                materials[item.MaterialId].StockQuantity += item.Quantity;
                existing.Items.Add(new DeliveryItem
                {
                    MaterialId = item.MaterialId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                });
            }

            await SaveAsync(db, cancellationToken, "Не удалось изменить поступление. Проверьте номер документа.");
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            throw new AppException("Не удалось изменить поступление: возможно, номер документа уже используется.", ex);
        }
        finally
        {
            if (transaction is not null) await transaction.DisposeAsync();
        }
    }

    public async Task DeleteDeliveryAsync(int deliveryId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var transaction = await BeginTransactionAsync(db, cancellationToken);
        try
        {
            var delivery = await db.Deliveries
                               .Include(d => d.Items)
                               .FirstOrDefaultAsync(d => d.Id == deliveryId, cancellationToken)
                           ?? throw new AppException("Поступление не найдено.");

            var materials = await LoadMaterialsAsync(db, delivery.Items.Select(i => i.MaterialId), cancellationToken);
            foreach (var item in delivery.Items)
                materials[item.MaterialId].StockQuantity -= item.Quantity;

            db.Deliveries.Remove(delivery);
            await SaveAsync(db, cancellationToken, "Не удалось удалить поступление.");
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            throw new AppException("Не удалось удалить поступление.", ex);
        }
        finally
        {
            if (transaction is not null) await transaction.DisposeAsync();
        }
    }

    public async Task<MaterialUsage> CreateUsageAsync(MaterialUsage usage, CancellationToken cancellationToken = default)
    {
        ValidateUsage(usage);

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var transaction = await BeginTransactionAsync(db, cancellationToken);
        try
        {
            var material = await db.Materials.FirstOrDefaultAsync(m => m.Id == usage.MaterialId, cancellationToken)
                           ?? throw new AppException("Материал не найден.");

            var entity = new MaterialUsage
            {
                ProjectId = usage.ProjectId,
                MaterialId = usage.MaterialId,
                EstimateId = usage.EstimateId,
                Quantity = usage.Quantity,
                UnitPrice = usage.UnitPrice > 0m ? usage.UnitPrice : material.Price,
                Date = usage.Date.Date,
                DocumentNumber = usage.DocumentNumber.Trim(),
                Note = usage.Note,
                CreatedByUserId = usage.CreatedByUserId
            };

            material.StockQuantity -= entity.Quantity;
            db.MaterialUsages.Add(entity);
            await SaveAsync(db, cancellationToken, "Не удалось сохранить списание.");
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return entity;
        }
        catch (DbUpdateException ex)
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            throw new AppException("Не удалось сохранить списание материала.", ex);
        }
        finally
        {
            if (transaction is not null) await transaction.DisposeAsync();
        }
    }

    public async Task UpdateUsageAsync(MaterialUsage usage, CancellationToken cancellationToken = default)
    {
        ValidateUsage(usage);

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var transaction = await BeginTransactionAsync(db, cancellationToken);
        try
        {
            var existing = await db.MaterialUsages.FirstOrDefaultAsync(u => u.Id == usage.Id, cancellationToken)
                           ?? throw new AppException("Списание не найдено: возможно, оно удалено другим пользователем.");

            var oldMaterial = await db.Materials.FirstOrDefaultAsync(m => m.Id == existing.MaterialId, cancellationToken)
                              ?? throw new AppException("Материал не найден.");
            oldMaterial.StockQuantity += existing.Quantity;

            var newMaterial = await db.Materials.FirstOrDefaultAsync(m => m.Id == usage.MaterialId, cancellationToken)
                              ?? throw new AppException("Материал не найден.");
            newMaterial.StockQuantity -= usage.Quantity;

            existing.ProjectId = usage.ProjectId;
            existing.MaterialId = usage.MaterialId;
            existing.EstimateId = usage.EstimateId;
            existing.Quantity = usage.Quantity;
            existing.UnitPrice = usage.UnitPrice;
            existing.Date = usage.Date.Date;
            existing.DocumentNumber = usage.DocumentNumber.Trim();
            existing.Note = usage.Note;

            await SaveAsync(db, cancellationToken, "Не удалось изменить списание.");
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            throw new AppException("Не удалось изменить списание материала.", ex);
        }
        finally
        {
            if (transaction is not null) await transaction.DisposeAsync();
        }
    }

    public async Task DeleteUsageAsync(int usageId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var transaction = await BeginTransactionAsync(db, cancellationToken);
        try
        {
            var usage = await db.MaterialUsages.FirstOrDefaultAsync(u => u.Id == usageId, cancellationToken)
                        ?? throw new AppException("Списание не найдено.");

            var material = await db.Materials.FirstOrDefaultAsync(m => m.Id == usage.MaterialId, cancellationToken);
            if (material is not null) material.StockQuantity += usage.Quantity;

            db.MaterialUsages.Remove(usage);
            await SaveAsync(db, cancellationToken, "Не удалось удалить списание.");
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            throw new AppException("Не удалось удалить списание материала.", ex);
        }
        finally
        {
            if (transaction is not null) await transaction.DisposeAsync();
        }
    }

    private static void ValidateUsage(MaterialUsage usage)
    {
        if (usage.Quantity <= 0m)
            throw new AppException("Количество должно быть больше нуля.");
        if (string.IsNullOrWhiteSpace(usage.DocumentNumber))
            throw new AppException("Укажите номер акта списания.");
        if (usage.ProjectId <= 0)
            throw new AppException("Выберите строительный объект.");
    }

    private static List<DeliveryItem> NormalizeItems(Delivery delivery)
    {
        return delivery.Items
            .Where(i => i.MaterialId > 0 && i.Quantity > 0m)
            .Select(i => new DeliveryItem
            {
                MaterialId = i.MaterialId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            })
            .ToList();
    }

    private static async Task<Dictionary<int, Material>> LoadMaterialsAsync(
        AppDbContext db,
        IEnumerable<int> materialIds,
        CancellationToken cancellationToken)
    {
        var ids = materialIds.Distinct().ToList();
        var materials = await db.Materials.Where(m => ids.Contains(m.Id)).ToListAsync(cancellationToken);
        return materials.ToDictionary(m => m.Id);
    }

    private static Task<IDbContextTransaction?> BeginTransactionAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        // Для реляционной MySQL открываем настоящую транзакцию; для демо-режима
        // (InMemory) транзакции не поддерживаются, операции выполняются сразу.
        return db.Database.IsRelational()
            ? BeginAsync(db, cancellationToken)
            : Task.FromResult<IDbContextTransaction?>(null);
    }

    private static async Task<IDbContextTransaction?> BeginAsync(AppDbContext db, CancellationToken cancellationToken)
        => await db.Database.BeginTransactionAsync(cancellationToken);

    private static async Task SaveAsync(AppDbContext db, CancellationToken cancellationToken, string message)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException("Документ уже изменён другим пользователем. Обновите список и повторите попытку.");
        }
        catch (DbUpdateException ex)
        {
            throw new AppException(message, ex);
        }
    }
}
