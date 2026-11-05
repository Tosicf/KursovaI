using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using StroyControl.Core.Enums;

namespace StroyControl.App.Converters;

/// <summary>Статус строительного объекта → цвет бейджа.</summary>
public sealed class ProjectStatusToBrushConverter : IValueConverter
{
    private static readonly Brush PlanningForeground = new SolidColorBrush(Color.FromRgb(0x03, 0x69, 0xA1));
    private static readonly Brush PlanningBackground = new SolidColorBrush(Color.FromRgb(0xE6, 0xF6, 0xFE));
    private static readonly Brush ActiveForeground = new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A));
    private static readonly Brush ActiveBackground = new SolidColorBrush(Color.FromRgb(0xE7, 0xF8, 0xEF));
    private static readonly Brush SuspendedForeground = new SolidColorBrush(Color.FromRgb(0xD9, 0x77, 0x06));
    private static readonly Brush SuspendedBackground = new SolidColorBrush(Color.FromRgb(0xFE, 0xF3, 0xE2));
    private static readonly Brush CompletedForeground = new SolidColorBrush(Color.FromRgb(0x5B, 0x6B, 0x84));
    private static readonly Brush CompletedBackground = new SolidColorBrush(Color.FromRgb(0xF1, 0xF5, 0xFA));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var background = string.Equals(parameter as string, "background", StringComparison.OrdinalIgnoreCase);
        return value switch
        {
            ProjectStatus.Planning => background ? PlanningBackground : PlanningForeground,
            ProjectStatus.InProgress => background ? ActiveBackground : ActiveForeground,
            ProjectStatus.Suspended => background ? SuspendedBackground : SuspendedForeground,
            _ => background ? CompletedBackground : CompletedForeground
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
