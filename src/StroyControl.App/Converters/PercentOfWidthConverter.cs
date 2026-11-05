using System.Globalization;
using System.Windows.Data;

namespace StroyControl.App.Converters;

/// <summary>
/// Мультипривязка «процент + фактическая ширина контейнера» → ширина заливки.
/// Позволяет строить адаптивные диаграммы без фиксированных размеров.
/// </summary>
public sealed class PercentOfWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length < 2) return 0d;

        var percent = values[0] switch
        {
            double d => d,
            decimal m => (double)m,
            int i => i,
            _ => 0d
        };

        var width = values[1] switch
        {
            double d => d,
            decimal m => (double)m,
            _ => 0d
        };

        if (double.IsNaN(width) || double.IsInfinity(width) || width <= 0d) return 0d;

        var clamped = Math.Clamp(percent, 0d, 100d);
        return Math.Max(clamped / 100d * width, clamped > 0d ? 3d : 0d);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
