using StroyControl.Core.Entities;

namespace StroyControl.Core.Interfaces;

public interface IProjectRepository : IRepository<Project>
{
    Task<IReadOnlyList<Project>> GetAllWithEstimatesAsync(CancellationToken cancellationToken = default);
}
