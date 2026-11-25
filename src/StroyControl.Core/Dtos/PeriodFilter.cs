using StroyControl.Core.Services;

namespace StroyControl.Core.Dtos;

/// <summary>
/// Период выборки: сначала год, затем месяц внутри года. Пустое значение означает
/// «все годы» или «все месяцы»; будущие периоды интерфейс не предлагает.
/// </summary>
public sealed record PeriodFilter(int? Year, int? Month)
{
    /// <summary>Период не ограничен.</summary>
    public static PeriodFilter All { get; } = new(null, null);

    public bool IsAll => Year is null && Month is null;

    /// <summary>Первое число выбранного месяца (когда выбраны и год, и месяц).</summary>
    public DateTime? MonthStart => Year is null || Month is null ? null : new DateTime(Year.Value, Month.Value, 1);

    /// <summary>
    /// Последний день периода — для накопленных показателей (план и факт «на конец
    /// периода»). null — период не ограничен.
    /// </summary>
    public DateTime? End => Year switch
    {
        null => null,
        _ when Month is null => new DateTime(Year.Value, 12, 31),
        _ => new DateTime(Year.Value, Month.Value, 1).AddMonths(1).AddDays(-1)
    };

    /// <summary>Дата попадает в период.</summary>
    public bool Contains(DateTime date) => MonthPeriod.Matches(date, Year, Month);

    /// <summary>Подпись периода: «за ноябрь 2026», «за 2026 год», «за всё время».</summary>
    public string Caption => Year switch
    {
        null => "за всё время",
        _ when Month is null => $"за {Year} год",
        _ => $"за {MonthPeriod.MonthName(Month.Value).ToLowerInvariant()} {Year}"
    };

    /// <summary>Подпись накопленных показателей: «по состоянию на ноябрь 2026».</summary>
    public string BalanceCaption => Year switch
    {
        null => "за всё время",
        _ when Month is null => $"по состоянию на конец {Year} года",
        _ => $"по состоянию на {MonthPeriod.MonthName(Month.Value).ToLowerInvariant()} {Year}"
    };
}
