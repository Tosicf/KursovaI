using StroyControl.Core.Entities;

namespace StroyControl.Core.Interfaces;

public interface IDeliveryRepository : IRepository<Delivery>
{
    Task<IReadOnlyList<Delivery>> GetAllWithItemsAsync(int? projectId = null, CancellationToken cancellationToken = default);

    Task<Delivery?> GetWithItemsAsync(int id, CancellationToken cancellationToken = default);
}
