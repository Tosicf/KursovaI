using StroyControl.Core.Entities;

namespace StroyControl.Core.Interfaces;

/// <summary>
/// Документы движения материалов. Каждая операция в одной транзакции меняет
/// и документ, и остаток материала на складе. Списание можно привязать к смете
/// только в состоянии «В работе» — факт учитывается лишь по действующей смете.
/// </summary>
public interface IInventoryService
{
    Task<Delivery> CreateDeliveryAsync(Delivery delivery, CancellationToken cancellationToken = default);

    Task UpdateDeliveryAsync(Delivery delivery, CancellationToken cancellationToken = default);

    Task DeleteDeliveryAsync(int deliveryId, CancellationToken cancellationToken = default);

    Task<MaterialUsage> CreateUsageAsync(MaterialUsage usage, CancellationToken cancellationToken = default);

    Task UpdateUsageAsync(MaterialUsage usage, CancellationToken cancellationToken = default);

    Task DeleteUsageAsync(int usageId, CancellationToken cancellationToken = default);
}
