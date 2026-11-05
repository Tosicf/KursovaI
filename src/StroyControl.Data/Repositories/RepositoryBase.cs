using Microsoft.EntityFrameworkCore;
using StroyControl.Core.Entities;
using StroyControl.Core.Exceptions;
using StroyControl.Core.Interfaces;

namespace StroyControl.Data.Repositories;

/// <summary>
/// Базовая реализация CRUD для EF Core. Для каждой операции создаётся отдельный
/// контекст (IDbContextFactory), поэтому клиентское приложение может держать
/// репозитории как singleton-сервисы без риска «залипания» кэша.
/// </summary>
public abstract class RepositoryBase<TEntity> : IRepository<TEntity> where TEntity : class, IEntity
{
    protected RepositoryBase(IDbContextFactory<AppDbContext> factory)
    {
        Factory = factory;
    }

    protected IDbContextFactory<AppDbContext> Factory { get; }

    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await Factory.CreateDbContextAsync(cancellationToken);
        return await db.Set<TEntity>().AsNoTracking().ToListAsync(cancellationToken);
    }

    public virtual async Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await Factory.CreateDbContextAsync(cancellationToken);
        return await db.Set<TEntity>().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await using var db = await Factory.CreateDbContextAsync(cancellationToken);
        db.Set<TEntity>().Add(entity);
        await SaveAsync(db, cancellationToken, "Не удалось добавить запись. Проверьте уникальность реквизитов.");
        return entity;
    }

    public virtual async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await using var db = await Factory.CreateDbContextAsync(cancellationToken);
        db.Set<TEntity>().Update(entity);
        await SaveAsync(db, cancellationToken, "Не удалось сохранить изменения. Проверьте уникальность реквизитов.");
    }

    public virtual async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await Factory.CreateDbContextAsync(cancellationToken);
        var entity = await db.Set<TEntity>().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is null) return;

        db.Set<TEntity>().Remove(entity);
        await SaveAsync(db, cancellationToken, "Не удалось удалить запись: на неё ссылаются связанные документы.");
    }

    /// <summary>Единая обработка ошибок сохранения с понятными пользователю сообщениями.</summary>
    protected static async Task SaveAsync(AppDbContext db, CancellationToken cancellationToken, string conflictMessage)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException("Запись уже изменена другим пользователем. Обновите список и повторите попытку.");
        }
        catch (DbUpdateException)
        {
            throw new AppException(conflictMessage);
        }
    }
}
