namespace StroyControl.Core.Interfaces;

public sealed record DatabaseHealth(bool IsAvailable, string Message, string Server, string Database, int UsersCount, int MaterialsCount);

/// <summary>Диагностика сетевого подключения к серверу MySQL (экран «Настройки»).</summary>
public interface IDatabaseHealthService
{
    Task<DatabaseHealth> CheckAsync(CancellationToken cancellationToken = default);
}
