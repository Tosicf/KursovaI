using System.Globalization;
using System.Windows;
using System.Windows.Media;
using StroyControl.App.Services;

namespace StroyControl.App.Controls;

/// <summary>
/// Общая основа самодельных диаграмм: палитра из ресурсов темы, типографика и
/// перерисовка при смене темы. Сторонние библиотеки не используются.
/// </summary>
public abstract class ChartBase : FrameworkElement
{
    protected static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");

    protected ChartBase()
    {
        ClipToBounds = true;
        Loaded += (_, _) => InvalidateVisual();
        Unloaded += (_, _) => ThemeManager.ThemeChanged -= OnThemeChanged;
        ThemeManager.ThemeChanged += OnThemeChanged;
    }

    /// <summary>Цвет сетки и подписей по умолчанию, если ресурс темы не найден.</summary>
    protected Brush GridBrush => Resource("BorderBrush", Color.FromRgb(0xE4, 0xE9, 0xF2));

    protected Brush MutedBrush => Resource("TextMutedBrush", Color.FromRgb(0x94, 0xA3, 0xB8));

    protected Brush PrimaryTextBrush => Resource("TextPrimaryBrush", Color.FromRgb(0x0F, 0x17, 0x2A));

    protected Brush AccentBrush => Resource("AccentBrush", Color.FromRgb(0x7C, 0x3A, 0xED));

    protected Brush AccentSoftBrush => Resource("AccentSoftBrush", Color.FromRgb(0xEE, 0xE8, 0xFD));

    protected Brush SuccessBrush => Resource("SuccessBrush", Color.FromRgb(0x16, 0xA3, 0x4A));

    protected Brush PaletteColor(int index)
    {
        // Фиолетовая палитра: акцент первым, далее бирюзовый, зелёный, янтарный,
        // синий, коралловый и нейтральный серый.
        string[] colors = { "#7C3AED", "#22B8CF", "#2FA36B", "#F0A020", "#3B82F6", "#E4637C", "#6E6880" };
        var color = (Color)ColorConverter.ConvertFromString(colors[Math.Abs(index) % colors.Length]);
        return new SolidColorBrush(color);
    }

    protected Brush Resource(string key, Color fallback)
    {
        if (TryFindResource(key) is Brush brush) return brush;
        var solid = new SolidColorBrush(fallback);
        solid.Freeze();
        return solid;
    }

    protected FormattedText Text(
        string text,
        double size,
        Brush brush,
        FontWeight? weight = null,
        TextAlignment alignment = TextAlignment.Left)
    {
        var typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal,
            weight ?? FontWeights.Normal, FontStretches.Normal);

        return new FormattedText(text, Russian, FlowDirection.LeftToRight, typeface, size, brush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip)
        {
            TextAlignment = alignment,
            Trimming = TextTrimming.CharacterEllipsis
        };
    }

    /// <summary>Рисует текст по левому краю, обрезая по ширине.</summary>
    protected void DrawText(DrawingContext dc, string text, Point origin, double maxWidth, double size,
        Brush brush, FontWeight? weight = null)
    {
        if (string.IsNullOrEmpty(text) || maxWidth <= 2) return;

        var formatted = Text(text, size, brush, weight);
        formatted.MaxTextWidth = Math.Max(1, maxWidth);
        dc.DrawText(formatted, origin);
    }

    /// <summary>Рисует текст, выровненный по центру указанной точки.</summary>
    protected void DrawCenteredText(DrawingContext dc, string text, Point center, double size,
        Brush brush, FontWeight? weight = null)
    {
        var formatted = Text(text, size, brush, weight);
        dc.DrawText(formatted, new Point(center.X - formatted.Width / 2, center.Y));
    }

    protected static string Compact(decimal value) => value switch
    {
        >= 1_000_000m => (value / 1_000_000m).ToString("0.#", Russian) + " млн",
        >= 1_000m => (value / 1_000m).ToString("0.#", Russian) + " тыс.",
        _ => value.ToString("0", Russian)
    };

    protected static double NiceMaximum(double value)
    {
        if (value <= 0d) return 1d;

        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(value)));
        var normalized = value / magnitude;

        var step = normalized switch
        {
            <= 1d => 1d,
            <= 2d => 2d,
            <= 2.5d => 2.5d,
            <= 5d => 5d,
            _ => 10d
        };

        return step * magnitude;
    }

    private void OnThemeChanged(object? sender, EventArgs e) => InvalidateVisual();
}
