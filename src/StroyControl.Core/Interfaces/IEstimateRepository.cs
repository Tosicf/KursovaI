using StroyControl.Core.Entities;

namespace StroyControl.Core.Interfaces;

public interface IEstimateRepository : IRepository<Estimate>
{
    /// <summary>Сметы с позициями и материалами (для экрана смет и анализа).</summary>
    Task<IReadOnlyList<Estimate>> GetAllWithItemsAsync(int? projectId = null, CancellationToken cancellationToken = default);

    Task<Estimate?> GetWithItemsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Обновляет только реквизиты сметы, не затрагивая позиции и объект.</summary>
    Task UpdateEstimateAsync(Estimate estimate, CancellationToken cancellationToken = default);

    Task<EstimateItem> AddItemAsync(EstimateItem item, CancellationToken cancellationToken = default);

    Task UpdateItemAsync(EstimateItem item, CancellationToken cancellationToken = default);

    Task DeleteItemAsync(int itemId, CancellationToken cancellationToken = default);

    /// <summary>Сметы, снятые с учёта (вкладка «Архив»), с позициями.</summary>
    Task<IReadOnlyList<Estimate>> GetArchivedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Подтверждает черновик: состав и суммы фиксируются, дальнейшие правки запрещены.
    /// </summary>
    Task ConfirmAsync(int estimateId, CancellationToken cancellationToken = default);

    /// <summary>Отправляет подтверждённую смету в архив после завершения работ.</summary>
    Task ArchiveAsync(int estimateId, CancellationToken cancellationToken = default);
}
