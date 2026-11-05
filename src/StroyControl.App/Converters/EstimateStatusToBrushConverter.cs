using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using StroyControl.Core.Enums;

namespace StroyControl.App.Converters;

/// <summary>Статус сметы → цвет бейджа.</summary>
public sealed class EstimateStatusToBrushConverter : IValueConverter
{
    private static readonly Brush DraftForeground = new SolidColorBrush(Color.FromRgb(0x5B, 0x6B, 0x84));
    private static readonly Brush DraftBackground = new SolidColorBrush(Color.FromRgb(0xF1, 0xF5, 0xFA));
    private static readonly Brush ReviewForeground = new SolidColorBrush(Color.FromRgb(0xD9, 0x77, 0x06));
    private static readonly Brush ReviewBackground = new SolidColorBrush(Color.FromRgb(0xFE, 0xF3, 0xE2));
    private static readonly Brush ApprovedForeground = new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A));
    private static readonly Brush ApprovedBackground = new SolidColorBrush(Color.FromRgb(0xE7, 0xF8, 0xEF));
    private static readonly Brush ArchivedForeground = new SolidColorBrush(Color.FromRgb(0x7C, 0x3A, 0xED));
    private static readonly Brush ArchivedBackground = new SolidColorBrush(Color.FromRgb(0xF1, 0xEB, 0xFE));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var background = string.Equals(parameter as string, "background", StringComparison.OrdinalIgnoreCase);
        return value switch
        {
            EstimateStatus.Draft => background ? DraftBackground : DraftForeground,
            EstimateStatus.OnReview => background ? ReviewBackground : ReviewForeground,
            EstimateStatus.Approved => background ? ApprovedBackground : ApprovedForeground,
            _ => background ? ArchivedBackground : ArchivedForeground
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
