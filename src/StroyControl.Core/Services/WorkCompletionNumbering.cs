using System.Globalization;
using System.Text.RegularExpressions;

namespace StroyControl.Core.Services;

/// <summary>
/// Сквозная нумерация актов выполненных работ в формате <c>АВР-гг-NNNN</c>.
/// Номер уникален в базе (на колонке стоит уникальный индекс), поэтому следующее
/// значение считается по уже занятым номерам: два акта, оформленных в один день,
/// больше не получают одинаковый номер.
/// </summary>
public static class WorkCompletionNumbering
{
    public const string Prefix = "АВР";

    /// <summary>Номер акта по году и порядковому номеру: АВР-26-0007.</summary>
    public static string Format(DateTime date, int sequence) =>
        $"{Prefix}-{date.ToString("yy", CultureInfo.InvariantCulture)}-{sequence.ToString("0000", CultureInfo.InvariantCulture)}";

    /// <summary>Наибольший занятый порядковый номер за год акта (0 — номеров ещё нет).</summary>
    public static int LastSequence(IEnumerable<string>? used, DateTime date)
    {
        var prefix = $"{Prefix}-{date.ToString("yy", CultureInfo.InvariantCulture)}-";
        var max = 0;

        foreach (var number in used ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(number)) continue;

            var trimmed = number.Trim();
            if (!trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;

            if (int.TryParse(trimmed[prefix.Length..], NumberStyles.None, CultureInfo.InvariantCulture, out var value)
                && value > max)
            {
                max = value;
            }
        }

        return max;
    }

    /// <summary>
    /// Следующий свободный номер за год акта. Учитываются оба случая: обычный
    /// «следующий» номер и разрежённая нумерация в базе (проверка на занятость).
    /// </summary>
    public static string Next(IEnumerable<string>? used, DateTime date)
    {
        var numbers = (used ?? Array.Empty<string>())
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .ToList();

        var taken = new HashSet<string>(numbers, StringComparer.OrdinalIgnoreCase);
        var sequence = LastSequence(numbers, date) + 1;
        var candidate = Format(date, sequence);

        while (taken.Contains(candidate))
            candidate = Format(date, ++sequence);

        return candidate;
    }

    /// <summary>Номер соответствует формату акта выполненных работ.</summary>
    public static bool IsActNumber(string? number) =>
        !string.IsNullOrWhiteSpace(number)
        && Regex.IsMatch(number.Trim(), $@"^{Prefix}-\d{{2}}-\d{{4}}$", RegexOptions.IgnoreCase);
}
