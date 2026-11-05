using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace StroyControl.App.Converters;

/// <summary>
/// bool → кисть. Имена кистей задаются параметром «TrueBrush|FalseBrush»
/// (например, «DangerBrush|SuccessBrush»).
/// </summary>
public sealed class BoolToBrushConverter : IValueConverter
{
    private static readonly Brush Fallback = new SolidColorBrush(Colors.Gray);

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var flag = value is bool b && b;
        var names = (parameter as string ?? string.Empty).Split('|');
        if (names.Length < 2) return Fallback;

        var name = flag ? names[0] : names[1];
        return System.Windows.Application.Current?.TryFindResource(name) as Brush ?? Fallback;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
