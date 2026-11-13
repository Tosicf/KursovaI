using System.Globalization;

namespace StroyControl.Core.Services;

/// <summary>
/// Номера приходных накладных: «ПН-26-1113-001», где 26 — год, 1113 — месяц и день,
/// 001 — порядковый номер документа за этот день. В отличие от прежней схемы
/// «ПН-26-111318» (час вместо последовательности) номер не повторяется у нескольких
/// накладных, созданных в один час, поэтому уникальный индекс в БД не нарушается.
/// </summary>
public static class DeliveryNumbers
{
    public const string Prefix = "ПН";

    /// <summary>Следующий свободный номер на дату по уже существующим номерам.</summary>
    public static string Next(IEnumerable<string> existingNumbers, DateTime date)
    {
        ArgumentNullException.ThrowIfNull(existingNumbers);

        var prefix = PrefixFor(date);
        var max = 0;

        foreach (var number in existingNumbers)
        {
            var sequence = ParseSequence(number, prefix);
            if (sequence > max) max = sequence;
        }

        return prefix + (max + 1).ToString("000", CultureInfo.InvariantCulture);
    }

    /// <summary>Префикс номера на дату: «ПН-26-1113-».</summary>
    public static string PrefixFor(DateTime date) =>
        $"{Prefix}-{date.ToString("yy", CultureInfo.InvariantCulture)}-" +
        $"{date.ToString("MMdd", CultureInfo.InvariantCulture)}-";

    /// <summary>
    /// Порядковый номер из строки документа; 0, если строка к этому дню не относится
    /// или номер записан в другом формате (старые документы «ПН-26-111318»).
    /// </summary>
    private static int ParseSequence(string? number, string prefix)
    {
        if (string.IsNullOrWhiteSpace(number)) return 0;

        var trimmed = number.Trim();
        if (!trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return 0;

        var tail = trimmed[prefix.Length..];
        var digits = new string(tail.TakeWhile(char.IsDigit).ToArray());

        return int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var sequence)
            ? sequence
            : 0;
    }
}
