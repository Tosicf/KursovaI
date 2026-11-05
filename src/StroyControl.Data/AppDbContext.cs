using Microsoft.EntityFrameworkCore;
using StroyControl.Core.Entities;

namespace StroyControl.Data;

/// <summary>
/// Контекст доступа к базе MySQL. Одна база обслуживает все рабочие места,
/// подключённые по сети (см. строку подключения в appsettings.json).
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Supplier> Suppliers => Set<Supplier>();

    public DbSet<Material> Materials => Set<Material>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<Estimate> Estimates => Set<Estimate>();

    public DbSet<EstimateItem> EstimateItems => Set<EstimateItem>();

    public DbSet<Delivery> Deliveries => Set<Delivery>();

    public DbSet<DeliveryItem> DeliveryItems => Set<DeliveryItem>();

    public DbSet<MaterialUsage> MaterialUsages => Set<MaterialUsage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override int SaveChanges()
    {
        ApplyAuditInfo();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInfo();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Проставляет даты создания/изменения и увеличивает версию строки.
    /// Версия используется как токен оптимистической блокировки: если два
    /// сетевых клиента изменили одну запись, второй получит понятную ошибку.
    /// </summary>
    private void ApplyAuditInfo()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<EntityBase>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = null;
                    entry.Entity.RowVersion = 1;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.RowVersion += 1;
                    entry.Property(nameof(EntityBase.CreatedAt)).IsModified = false;
                    break;
            }
        }
    }
}
