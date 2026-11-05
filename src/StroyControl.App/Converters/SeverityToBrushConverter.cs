using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using StroyControl.Core.Enums;

namespace StroyControl.App.Converters;

/// <summary>Степень отклонения → цвет фона/текста бейджа.</summary>
public sealed class SeverityToBrushConverter : IValueConverter
{
    private static readonly Brush NormalForeground = new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A));
    private static readonly Brush NormalBackground = new SolidColorBrush(Color.FromRgb(0xE7, 0xF8, 0xEF));
    private static readonly Brush WarningForeground = new SolidColorBrush(Color.FromRgb(0xD9, 0x77, 0x06));
    private static readonly Brush WarningBackground = new SolidColorBrush(Color.FromRgb(0xFE, 0xF3, 0xE2));
    private static readonly Brush DangerForeground = new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26));
    private static readonly Brush DangerBackground = new SolidColorBrush(Color.FromRgb(0xFD, 0xEC, 0xEC));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var background = string.Equals(parameter as string, "background", StringComparison.OrdinalIgnoreCase);
        return value switch
        {
            DeviationSeverity.Critical => background ? DangerBackground : DangerForeground,
            DeviationSeverity.Warning => background ? WarningBackground : WarningForeground,
            _ => background ? NormalBackground : NormalForeground
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
