using StroyControl.Core.Dtos;

namespace StroyControl.Core.Interfaces;

public interface IDashboardService
{
    /// <summary>
    /// Показатели панели. <paramref name="month"/> ограничивает выборку одним месяцем
    /// (null — за всё время), чтобы панель не разрасталась при росте числа объектов.
    /// </summary>
    Task<DashboardData> LoadAsync(DateTime? month = null, CancellationToken cancellationToken = default);
}
