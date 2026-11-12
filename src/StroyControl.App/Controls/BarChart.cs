using System.Collections;
using System.Windows;
using System.Windows.Media;
using StroyControl.Core.Dtos;

namespace StroyControl.App.Controls;

/// <summary>
/// Сгруппированные столбики «план / факт» по объектам: подписи объектов по оси X,
/// сетка и денежная шкала по оси Y. Рисуется напрямую через DrawingContext.
/// </summary>
public sealed class BarChart : ChartBase
{
    public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
        nameof(Items), typeof(IEnumerable), typeof(BarChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public IEnumerable? Items
    {
        get => (IEnumerable?)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    protected override void OnRender(DrawingContext dc)
    {
        var width = ActualWidth;
        var height = ActualHeight;
        if (width < 60 || height < 60) return;

        var rows = (Items?.Cast<ProjectBudgetRow>() ?? Enumerable.Empty<ProjectBudgetRow>())
            .Where(r => r.PlannedCost > 0m || r.ActualCost > 0m)
            .Take(5)
            .ToList();

        const double left = 52;
        const double top = 16;
        const double right = 8;
        const double bottom = 44;

        var plotWidth = width - left - right;
        var plotHeight = height - top - bottom;

        if (rows.Count == 0 || plotWidth < 40 || plotHeight < 30)
        {
            DrawText(dc, "Нет данных по объектам", new Point(left, top + plotHeight / 2), plotWidth, 12, MutedBrush);
            return;
        }

        var maximum = NiceMaximum((double)rows.Max(r => Math.Max(r.PlannedCost, r.ActualCost)));

        // Сетка и подписи шкалы.
        var gridPen = new Pen(GridBrush, 1);
        gridPen.Freeze();

        const int steps = 4;
        for (var step = 0; step <= steps; step++)
        {
            var y = top + plotHeight - plotHeight * step / steps;
            dc.DrawLine(gridPen, new Point(left, y), new Point(left + plotWidth, y));

            var label = Compact((decimal)(maximum * step / steps));
            var text = Text(label, 10, MutedBrush);
            dc.DrawText(text, new Point(left - text.Width - 8, y - text.Height / 2));
        }

        var groupWidth = plotWidth / rows.Count;
        var barWidth = Math.Min(26d, groupWidth * 0.26);

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var center = left + groupWidth * i + groupWidth / 2;

            DrawBar(dc, center - barWidth - 2, top + plotHeight, plotHeight, (double)row.PlannedCost / maximum, AccentBrush, barWidth);
            DrawBar(dc, center + 2, top + plotHeight, plotHeight, (double)row.ActualCost / maximum,
                row.IsOverrun ? Resource("DangerBrush", Color.FromRgb(0xDC, 0x26, 0x26)) : SuccessBrush, barWidth);

            var caption = string.IsNullOrWhiteSpace(row.Code) ? row.Name : row.Code;
            var captionText = Text(caption, 10, MutedBrush, FontWeights.SemiBold);
            captionText.MaxTextWidth = Math.Max(20, groupWidth - 6);
            dc.DrawText(captionText, new Point(center - captionText.Width / 2, top + plotHeight + 8));

            var percentText = Text($"{row.ProgressPercent:0} %", 9.5,
                row.IsOverrun ? Resource("DangerBrush", Color.FromRgb(0xDC, 0x26, 0x26)) : MutedBrush);
            dc.DrawText(percentText, new Point(center - percentText.Width / 2, top + plotHeight + 24));
        }
    }

    private static void DrawBar(DrawingContext dc, double x, double baseline, double plotHeight,
        double ratio, Brush brush, double barWidth)
    {
        var barHeight = Math.Max(2d, Math.Clamp(ratio, 0d, 1d) * plotHeight);
        var rect = new Rect(x, baseline - barHeight, barWidth, barHeight);
        dc.DrawRoundedRectangle(brush, null, rect, 4, 4);
    }
}
