using StroyControl.Core.Dtos;

namespace StroyControl.Core.Interfaces;

public interface IDashboardService
{
    /// <summary>
    /// Показатели панели. <paramref name="period"/> ограничивает выборку годом и месяцем
    /// (null — за всё время), чтобы панель не разрасталась при росте числа объектов.
    /// </summary>
    Task<DashboardData> LoadAsync(PeriodFilter? period = null, CancellationToken cancellationToken = default);
}
