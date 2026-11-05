using StroyControl.Core.Entities;

namespace StroyControl.Core.Interfaces;

public interface ISupplierRepository : IRepository<Supplier>
{
    Task<IReadOnlyList<Supplier>> GetAllWithMaterialsAsync(CancellationToken cancellationToken = default);
}
