using StroyControl.Core.Entities;

namespace StroyControl.Core.Interfaces;

public interface IUsageRepository : IRepository<MaterialUsage>
{
    Task<IReadOnlyList<MaterialUsage>> GetAllWithDetailsAsync(int? projectId = null, CancellationToken cancellationToken = default);
}
