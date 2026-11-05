using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace StroyControl.App.Converters;

/// <summary>Строковые данные иконки (path data) → Geometry для элемента Path.</summary>
public sealed class StringToGeometryConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Geometry geometry) return geometry;
        if (value is not string data || string.IsNullOrWhiteSpace(data)) return null;

        try
        {
            return Geometry.Parse(data);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
