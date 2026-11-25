namespace StroyControl.Core.Dtos;

/// <summary>
/// Пункт выпадающего списка периодов: конкретный месяц либо строка «Все месяцы».
/// Обычный класс, а не запись: для привязки WPF важна уникальность экземпляра,
/// иначе ComboBox теряет выбранный пункт после перестроения списка.
/// </summary>
public sealed class MonthOption
{
    public MonthOption(DateTime? month, string title)
    {
        Month = month;
        Title = title;
    }

    /// <summary>Пункт «Все месяцы» — список не ограничивается датой.</summary>
    public static MonthOption All { get; } = new(null, Services.MonthPeriod.AllTitle);

    /// <summary>Первый день месяца; null — ограничения по дате нет.</summary>
    public DateTime? Month { get; }

    /// <summary>Подпись пункта: «Все месяцы» или «Ноябрь 2026».</summary>
    public string Title { get; }

    /// <summary>Выбрано «Все месяцы».</summary>
    public bool IsAll => Month is null;

    /// <summary>Подпись периода для фраз интерфейса: «за ноябрь 2026» / «за всё время».</summary>
    public string Caption => Month is null ? "за всё время" : "за " + Title.ToLowerInvariant();

    public override string ToString() => Title;
}
