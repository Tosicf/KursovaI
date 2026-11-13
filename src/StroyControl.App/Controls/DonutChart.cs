using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using StroyControl.Core.Dtos;

namespace StroyControl.App.Controls;

/// <summary>
/// Кольцевая диаграмма структуры затрат по категориям материалов: сегменты,
/// подпись итога в центре и легенда справа.
/// </summary>
public sealed class DonutChart : ChartBase
{
    public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
        nameof(Items), typeof(IEnumerable), typeof(DonutChart),
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
        if (width < 80 || height < 80) return;

        var rows = (Items?.Cast<CategoryCostRow>() ?? Enumerable.Empty<CategoryCostRow>())
            .Where(r => r.Cost > 0m)
            .ToList();

        var size = Math.Min(width * 0.52, height);
        var radius = size / 2 - 8;
        var center = new Point(size / 2, height / 2);

        if (rows.Count == 0 || radius < 20)
        {
            DrawText(dc, "Нет данных по категориям", new Point(12, height / 2 - 8), width - 24, 12, MutedBrush);
            return;
        }

        var total = rows.Sum(r => r.Cost);
        var thickness = Math.Max(14, radius * 0.36);
        var innerRadius = radius - thickness;

        var startAngle = -90d;
        for (var i = 0; i < rows.Count; i++)
        {
            var share = (double)(rows[i].Cost / total);
            var sweep = share * 360d;
            var brush = rows[i].Category.Contains("Проч", StringComparison.OrdinalIgnoreCase) && rows.Count > 1
                ? PaletteColor(rows.Count)
                : PaletteColor(i);

            DrawSegment(dc, center, radius, innerRadius, startAngle, sweep, brush);
            startAngle += sweep;
        }

        // Подпись итога строго в центре кольца: блок «сумма + BYN» центрируется и по
        // горизонтали, и по вертикали относительно center; если сумма шире внутреннего
        // отверстия, кегль уменьшается, чтобы текст не выходил за кольцо.
        var holeWidth = Math.Max(24d, (innerRadius - 6) * 2);
        var fontSize = 15d;
        var totalText = Text(Compact(total), fontSize, PrimaryTextBrush, FontWeights.Bold);
        while (totalText.Width > holeWidth && fontSize > 9d)
        {
            fontSize -= 1d;
            totalText = Text(Compact(total), fontSize, PrimaryTextBrush, FontWeights.Bold);
        }

        var unitText = Text("BYN", 9.5, MutedBrush);
        var blockHeight = totalText.Height + unitText.Height - 2;
        var blockTop = center.Y - blockHeight / 2;

        dc.DrawText(totalText, new Point(center.X - totalText.Width / 2, blockTop));
        dc.DrawText(unitText, new Point(center.X - unitText.Width / 2, blockTop + totalText.Height - 2));

        // Легенда справа.
        var legendLeft = size + 8;
        var legendTop = Math.Max(6, height / 2 - rows.Count * 11);
        var legendWidth = width - legendLeft - 4;
        if (legendWidth < 40) return;

        for (var i = 0; i < rows.Count; i++)
        {
            var y = legendTop + i * 22;
            if (y > height - 14) break;

            var brush = rows[i].Category.Contains("Проч", StringComparison.OrdinalIgnoreCase) && rows.Count > 1
                ? PaletteColor(rows.Count)
                : PaletteColor(i);

            dc.DrawRoundedRectangle(brush, null, new Rect(legendLeft, y + 2, 9, 9), 2.5, 2.5);

            DrawText(dc, rows[i].Category, new Point(legendLeft + 15, y), legendWidth - 46, 10.5, PrimaryTextBrush);

            var shareText = Text(rows[i].Share.ToString("0.#", CultureInfo.GetCultureInfo("ru-RU")) + " %", 10, MutedBrush);
            dc.DrawText(shareText, new Point(width - shareText.Width - 2, y));
        }
    }

    private static void DrawSegment(DrawingContext dc, Point center, double radius, double innerRadius,
        double startAngle, double sweepAngle, Brush brush)
    {
        if (sweepAngle <= 0.01) return;

        // Единственный сегмент — целое кольцо.
        if (sweepAngle >= 359.99)
        {
            var ring = new GeometryGroup { FillRule = FillRule.EvenOdd };
            ring.Children.Add(new EllipseGeometry(center, radius, radius));
            ring.Children.Add(new EllipseGeometry(center, innerRadius, innerRadius));
            dc.DrawGeometry(brush, null, ring);
            return;
        }

        var endAngle = startAngle + sweepAngle;
        var isLarge = sweepAngle > 180d;

        var outerStart = PointOnCircle(center, radius, startAngle);
        var outerEnd = PointOnCircle(center, radius, endAngle);
        var innerEnd = PointOnCircle(center, innerRadius, endAngle);
        var innerStart = PointOnCircle(center, innerRadius, startAngle);

        var figure = new PathFigure { StartPoint = outerStart, IsClosed = true, IsFilled = true };
        figure.Segments.Add(new ArcSegment(outerEnd, new Size(radius, radius), 0, isLarge, SweepDirection.Clockwise, true));
        figure.Segments.Add(new LineSegment(innerEnd, true));
        figure.Segments.Add(new ArcSegment(innerStart, new Size(innerRadius, innerRadius), 0, isLarge, SweepDirection.Counterclockwise, true));

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        dc.DrawGeometry(brush, null, geometry);
    }

    private static Point PointOnCircle(Point center, double radius, double angleDegrees)
    {
        var radians = angleDegrees * Math.PI / 180d;
        return new Point(center.X + radius * Math.Cos(radians), center.Y + radius * Math.Sin(radians));
    }
}
