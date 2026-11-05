using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace StroyControl.Data.Seed;

/// <summary>Создание схемы БД (если её нет) и первичное наполнение демо-данными.</summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var factory = services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        await db.Database.EnsureCreatedAsync(cancellationToken);
        await DemoDataSeeder.SeedAsync(db, cancellationToken);
    }

    /// <summary>Полная переинициализация демо-базы (используется в демонстрационном режиме).</summary>
    public static async Task RecreateDemoAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var factory = services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        await db.Database.EnsureDeletedAsync(cancellationToken);
        await db.Database.EnsureCreatedAsync(cancellationToken);
        await DemoDataSeeder.SeedAsync(db, cancellationToken);
    }
}
