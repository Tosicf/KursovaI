using Microsoft.EntityFrameworkCore;
using StroyControl.Core.Interfaces;

namespace StroyControl.Data.Services;

/// <summary>Проверка доступности сервера SQL Server с клиентского рабочего места.</summary>
public sealed class DatabaseHealthService : IDatabaseHealthService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public DatabaseHealthService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<DatabaseHealth> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var db = await _factory.CreateDbContextAsync(cancellationToken);
            var canConnect = await db.Database.CanConnectAsync(cancellationToken);
            var connection = db.Database.GetDbConnection();

            if (!canConnect)
            {
                return new DatabaseHealth(
                    false,
                    "Сервер не отвечает. Проверьте, запущена ли служба SQL Server (MSSQL$SQLEXPRESS) и доступен ли порт 1433.",
                    connection.DataSource,
                    connection.Database,
                    0,
                    0);
            }

            var users = await db.Users.AsNoTracking().CountAsync(cancellationToken);
            var materials = await db.Materials.AsNoTracking().CountAsync(cancellationToken);

            return new DatabaseHealth(
                true,
                "Подключение установлено. Данные синхронизированы.",
                connection.DataSource,
                connection.Database,
                users,
                materials);
        }
        catch (Exception ex)
        {
            return new DatabaseHealth(false, ex.Message, "—", "—", 0, 0);
        }
    }
}
