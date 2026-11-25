using StroyControl.Core.Dtos;

namespace StroyControl.Core.Services;

/// <summary>
/// Единое правило работы с месяцами для всех списков приложения: от нового месяца
/// к старому, «Все месяцы» — отсутствие ограничения по дате. Месяц документа
/// определяется его датой: дата начала работ у объекта, дата сметы, дата
/// поступления или списания.
/// </summary>
public static class MonthPeriod
{
    /// <summary>Подпись пункта без ограничения по дате.</summary>
    public const string AllTitle = "Все месяцы";

    // Нарицательные названия месяцев заданы явно: формат «MMMM» в ru-RU зависит
    // от версии ICU и может вернуть родительный падеж («ноября»), а группировка
    // списков должна одинаково выглядеть на любой машине.
    private static readonly string[] MonthNames =
    {
        "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь",
        "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь"
    };

    private static readonly string[] ShortMonthNames =
    {
        "Янв", "Фев", "Мар", "Апр", "Май", "Июн",
        "Июл", "Авг", "Сен", "Окт", "Ноя", "Дек"
    };

    /// <summary>Первое число месяца, к которому относится дата.</summary>
    public static DateTime Start(DateTime date) => new(date.Year, date.Month, 1);

    /// <summary>Первое число текущего месяца.</summary>
    public static DateTime CurrentMonth => Start(DateTime.Today);

    /// <summary>Дата относится к выбранному месяцу (null — ограничения нет).</summary>
    public static bool Matches(DateTime date, DateTime? month) =>
        month is null || (date.Year == month.Value.Year && date.Month == month.Value.Month);

    /// <summary>Название месяца для подписей: «Ноябрь 2026».</summary>
    public static string Title(DateTime month) =>
        $"{MonthNames[Start(month).Month - 1]} {month.Year}";

    /// <summary>Короткое название месяца: «Ноя 2026» (для компактных подписей в сетках).</summary>
    public static string ShortTitle(DateTime month) =>
        $"{ShortMonthNames[Start(month).Month - 1]} {month.Year}";

    /// <summary>Месяцы, встречающиеся в датах, от нового к старому (без пункта «Все месяцы»).</summary>
    public static IReadOnlyList<MonthOption> Months(IEnumerable<DateTime?> dates)
    {
        return dates
            .Where(d => d.HasValue)
            .Select(d => Start(d.Value))
            .Distinct()
            .OrderByDescending(d => d)
            .Select(d => new MonthOption(d, Title(d)))
            .ToList();
    }

    /// <summary>
    /// Полный список месяцев для выпадающего списка: «Все месяцы», затем месяцы данных
    /// от нового к старому. Текущий месяц присутствует всегда, даже если данных в нём нет.
    /// </summary>
    public static IReadOnlyList<MonthOption> Options(IEnumerable<DateTime?> dates)
    {
        var months = Months(dates).ToList();
        var current = CurrentMonth;
        if (months.All(m => m.Month != current))
            months.Add(new MonthOption(current, Title(current)));

        var result = new List<MonthOption> { MonthOption.All };
        result.AddRange(months.OrderByDescending(m => m.Month));
        return result;
    }
}
