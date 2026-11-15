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
    /// <summary>
    /// Минимальный запас = потребность по сметам минус текущий остаток, но не меньше нуля.
    /// Смысл: порог, при котором на складе ещё хватает материала на оставшийся объём работ.
    /// Материалы, которых нет ни в одной смете, не пересчитываются — для них запас остаётся
    /// экспертным, введённым вручную.
    /// </summary>
    public static decimal SuggestedMinStock(decimal plannedNeed, decimal stockQuantity)
    {
        if (plannedNeed <= 0m) return 0m;

        var remaining = plannedNeed - stockQuantity;
        return remaining <= 0m ? 0m : Math.Round(remaining, 3, MidpointRounding.AwayFromZero);
    }
}
