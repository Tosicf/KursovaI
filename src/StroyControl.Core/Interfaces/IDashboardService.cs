using StroyControl.Core.Dtos;

namespace StroyControl.Core.Interfaces;

public interface IDashboardService
{
    Task<DashboardData> LoadAsync(CancellationToken cancellationToken = default);
}
