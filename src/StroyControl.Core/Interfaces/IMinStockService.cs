using StroyControl.Core.Dtos;

namespace StroyControl.Core.Interfaces;

/// <summary>
/// Пересчёт минимального (страхового) запаса материалов по потребности смет:
/// запас показывает, сколько материала ещё нужно докупить под оставшийся объём работ.
/// </summary>
public interface IMinStockService
{
    /// <summary>Предложения по минимальному запасу для материалов, встречающихся в сметах.</summary>
    Task<IReadOnlyList<MinStockSuggestion>> BuildSuggestionsAsync(int? projectId = null, CancellationToken cancellationToken = default);

    /// <summary>Сохраняет новые значения минимального запаса, возвращает число обновлённых материалов.</summary>
    Task<int> ApplyAsync(IReadOnlyList<MinStockSuggestion> suggestions, CancellationToken cancellationToken = default);
}
