using System.Globalization;
using System.Windows.Data;

namespace StroyControl.App.Converters;

/// <summary>Сумма в BYN с разделителями разрядов (например, 1 250 400,00 BYN).</summary>
public sealed class MoneyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var amount = value switch
        {
            decimal d => d,
            double db => (decimal)db,
            int i => i,
            _ => 0m
        };

        return amount.ToString("N2", CultureInfo.GetCultureInfo("ru-RU")) + " BYN";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
