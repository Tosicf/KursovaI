using StroyControl.Core.Entities;

namespace StroyControl.Core.Interfaces;

public interface IMaterialRepository : IRepository<Material>
{
    /// <summary>Материалы вместе с поставщиками (для таблицы справочника).</summary>
    Task<IReadOnlyList<Material>> GetAllWithSupplierAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken cancellationToken = default);
}
