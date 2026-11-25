namespace StroyControl.Core.Dtos;

/// <summary>
/// Пункт выпадающего списка периодов: «Все годы», конкретный год, «Все месяцы» или
/// месяц внутри года. Обычный класс, а не запись: для привязки WPF важна
/// уникальность экземпляра, иначе ComboBox теряет выбранный пункт после
/// перестроения списка.
/// </summary>
public sealed class PeriodOption
{
    public PeriodOption(int? year, int? month, string title)
    {
        Year = year;
        Month = month;
        Title = title;
    }

    /// <summary>Пункт «Все годы» — ограничения по году нет.</summary>
    public static PeriodOption AllYears { get; } = new(null, null, Services.MonthPeriod.AllYearsTitle);

    /// <summary>Пункт «Все месяцы» для указанного года (null — год не выбран).</summary>
    public static PeriodOption AllMonthsOf(int? year) =>
        new(year, null, Services.MonthPeriod.AllMonthsTitle);

    /// <summary>Выбранный год; null — все годы.</summary>
    public int? Year { get; }

    /// <summary>Выбранный месяц (1–12); null — все месяцы.</summary>
    public int? Month { get; }

    /// <summary>Подпись пункта: «Все годы», «2026», «Все месяцы», «Ноябрь».</summary>
    public string Title { get; }

    /// <summary>Пункт «Все годы».</summary>
    public bool IsAllYears => Year is null;

    public override string ToString() => Title;
}
