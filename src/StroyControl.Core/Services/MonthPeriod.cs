using StroyControl.Core.Dtos;

namespace StroyControl.Core.Services;

/// <summary>
/// Работа с периодами документов: сначала год, затем месяц. Списки выбора
/// ограничены наступившими датами — будущие годы и месяцы выбрать нельзя.
/// </summary>
public static class MonthPeriod
{
    /// <summary>Подпись пункта «все годы».</summary>
    public const string AllYearsTitle = "Все годы";

    /// <summary>Подпись пункта «все месяцы».</summary>
    public const string AllMonthsTitle = "Все месяцы";

    // Нарицательные названия месяцев заданы явно: формат «MMMM» в ru-RU зависит
    // от версии ICU и может вернуть родительный падеж («ноября»), а группировка
    // списков должна одинаково выглядеть на любой машине.
    private static readonly string[] MonthNames =
    [
        "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь",
        "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь"
    ];

    private static readonly string[] ShortMonthNames =
    [
        "Янв", "Фев", "Мар", "Апр", "Май", "Июн",
        "Июл", "Авг", "Сен", "Окт", "Ноя", "Дек"
    ];

    /// <summary>Первое число месяца, к которому относится дата.</summary>
    public static DateTime Start(DateTime date) => new(date.Year, date.Month, 1);

    /// <summary>Первое число текущего месяца.</summary>
    public static DateTime CurrentMonth => Start(DateTime.Today);

    /// <summary>Месяц ещё не наступил — такую дату нельзя ни выбрать, ни отфильтровать.</summary>
    public static bool IsFuture(DateTime date) => Start(date) > CurrentMonth;

    /// <summary>Дата относится к выбранному году и месяцу (null — без ограничения).</summary>
    public static bool Matches(DateTime date, int? year, int? month) =>
        (year is null || date.Year == year) && (month is null || date.Month == month);

    /// <summary>Название месяца по номеру: «Ноябрь».</summary>
    public static string MonthName(int month) => month is >= 1 and <= 12 ? MonthNames[month - 1] : string.Empty;

    /// <summary>Короткое название месяца по номеру: «Янв», «Ноя» — для сетки календаря.</summary>
    public static string ShortName(int month) => month is >= 1 and <= 12 ? ShortMonthNames[month - 1] : string.Empty;

    /// <summary>Название месяца с годом: «Ноябрь 2026».</summary>
    public static string Title(DateTime month) => $"{MonthName(Start(month).Month)} {month.Year}";

    /// <summary>Короткое название месяца с годом: «Ноя 2026».</summary>
    public static string ShortTitle(DateTime month) => $"{ShortMonthNames[Start(month).Month - 1]} {month.Year}";

    /// <summary>
    /// Годы, за которые есть документы, от нового к старому. Будущие годы в список
    /// не попадают, текущий год присутствует всегда.
    /// </summary>
    public static IReadOnlyList<int> Years(IEnumerable<DateTime?> dates)
    {
        var current = DateTime.Today.Year;

        var years = dates
            .Where(d => d.HasValue)
            .Select(d => d.Value.Year)
            .Where(year => year <= current)
            .Distinct()
            .ToList();

        if (!years.Contains(current))
            years.Add(current);

        return [.. years.OrderByDescending(year => year)];
    }

    /// <summary>
    /// Месяцы выбранного года от нового к старому: только наступившие и только те,
    /// за которые есть документы (для «всех лет» — все месяцы, встречающиеся в данных).
    /// Текущий месяц доступен всегда.
    /// </summary>
    public static IReadOnlyList<int> Months(int? year, IEnumerable<DateTime?> dates)
    {
        var today = DateTime.Today;
        var limit = year is null || year >= today.Year ? today.Month : 12;

        var months = dates
            .Where(d => d.HasValue)
            .Select(d => d.Value)
            .Where(date => year is null || date.Year == year)
            .Select(date => date.Month)
            .Where(month => month <= limit)
            .Distinct()
            .ToList();

        if ((year is null || year == today.Year) && !months.Contains(today.Month))
            months.Add(today.Month);

        return [.. months.OrderByDescending(month => month)];
    }
}
