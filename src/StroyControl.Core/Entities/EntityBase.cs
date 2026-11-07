namespace StroyControl.Core.Entities;

/// <summary>
/// Базовая сущность: первичный ключ, метка времени и версия строки.
/// RowVersion используется как токен оптимистической блокировки при сетевой работе
/// нескольких клиентов с одной базой SQL Server.
/// </summary>
public abstract class EntityBase : IEntity
{
    public int Id { get; set; }

    public int RowVersion { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public string CreatedAtDisplay => CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
}
