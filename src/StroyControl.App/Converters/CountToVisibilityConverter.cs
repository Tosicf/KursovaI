using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace StroyControl.App.Converters;

/// <summary>0 → Visible (пустое состояние). Параметр «invert» → Visible при наличии элементов.</summary>
public sealed class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var count = value switch
        {
            int i => i,
            System.Collections.ICollection collection => collection.Count,
            _ => 0
        };

        var isEmpty = count == 0;
        if (string.Equals(parameter as string, "invert", StringComparison.OrdinalIgnoreCase))
            isEmpty = !isEmpty;

        return isEmpty ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
