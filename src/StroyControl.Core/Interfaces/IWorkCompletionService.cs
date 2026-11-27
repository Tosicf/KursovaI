using StroyControl.Core.Entities;

namespace StroyControl.Core.Interfaces;

/// <summary>
/// Учёт факта по работам. Акт выполненных работ создаётся только по смете
/// в состоянии «В работе»: строки материалов акта уменьшают остаток склада
/// в той же транзакции, что и сам документ.
/// </summary>
public interface IWorkCompletionService
{
    Task<WorkCompletion> CreateAsync(WorkCompletion completion, CancellationToken cancellationToken = default);

    Task UpdateAsync(WorkCompletion completion, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
