using StroyControl.Core.Entities;

namespace StroyControl.Core.Interfaces;

/// <summary>Акты выполненных работ: документы факта по работам и материалам сметы.</summary>
public interface IWorkCompletionRepository : IRepository<WorkCompletion>
{
    /// <summary>Акты со строками работ, материалами, сметой и объектом.</summary>
    Task<IReadOnlyList<WorkCompletion>> GetAllWithDetailsAsync(
        int? projectId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Один акт со строками: используется перед редактированием и печатью.</summary>
    Task<WorkCompletion?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default);
}
