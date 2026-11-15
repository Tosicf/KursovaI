namespace StroyControl.Core.Dtos;

/// <summary>
/// Предложение по минимальному (страховому) запасу материала, рассчитанному из смет:
/// сколько материала ещё предстоит израсходовать по плану и сколько его не хватает
/// на складе прямо сейчас.
/// </summary>
public sealed record MinStockSuggestion(
    int MaterialId,
    string Code,
    string Name,
    string Unit,
    decimal StockQuantity,
    decimal PlannedNeed,
    decimal CurrentMinStock,
    decimal SuggestedMinStock)
{
    /// <summary>Изменится ли минимальный запас, если применить предложение.</summary>
    public bool HasChanges => SuggestedMinStock != CurrentMinStock;

    /// <summary>На сколько изменится минимальный запас (положительное — увеличится).</summary>
    public decimal Delta => SuggestedMinStock - CurrentMinStock;
}

/// <summary>
/// Правило расчёта минимального запаса из сметы (без обращений к БД, покрыто тестами).
/// </summary>
public static class MinStockCalculator
{
    /// <summary>Страховой резерв от потребности по смете, %.</summary>
    public const decimal ReservePercent = 10m;

    /// <summary>
    /// Минимальный запас считается из сметы и всегда реагирует на её изменение:
    /// берётся большее из двух значений — непокрытая остатком потребность
    /// (потребность − остаток) и страховой резерв (10 % потребности).
    /// <para>
    /// Примеры: потребность 100, остаток 40 → 60; потребность 100, остаток 500 → 10
    /// (резерв, а не ноль); потребность 200 при том же остатке 500 → 20.
    /// </para>
    /// Материалы, которых нет ни в одной смете, не пересчитываются — для них запас
    /// остаётся экспертным, введённым вручную.
    /// </summary>
    public static decimal SuggestedMinStock(decimal plannedNeed, decimal stockQuantity)
    {
        if (plannedNeed <= 0m) return 0m;

        var uncovered = plannedNeed - stockQuantity;
        var reserve = plannedNeed * ReservePercent / 100m;
        var result = Math.Max(uncovered, reserve);

        return result <= 0m ? 0m : Math.Round(result, 3, MidpointRounding.AwayFromZero);
    }
}
