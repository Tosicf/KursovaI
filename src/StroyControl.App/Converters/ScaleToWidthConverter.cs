using System.Globalization;
using System.Windows.Data;

namespace StroyControl.App.Converters;

/// <summary>Процент (0–100) → ширина в пикселях для горизонтальных диаграмм.</summary>
public sealed class ScaleToWidthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var percent = value switch
        {
            double d => d,
            decimal m => (double)m,
            int i => i,
            _ => 0d
        };

        var max = 320d;
        if (parameter is string text && double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            max = parsed;

        var clamped = Math.Clamp(percent, 0d, 100d);
        return Math.Max(clamped / 100d * max, clamped > 0d ? 3d : 0d);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
