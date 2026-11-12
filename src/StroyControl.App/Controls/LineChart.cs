using System.Collections;
using System.Windows;
using System.Windows.Media;
using StroyControl.Core.Dtos;

namespace StroyControl.App.Controls;

/// <summary>
/// Линейный график «план / факт» по месяцам: две линии, точки и подписи месяцев.
/// </summary>
public sealed class LineChart : ChartBase
{
    public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
        nameof(Items), typeof(IEnumerable), typeof(LineChart),
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
        if (width < 80 || height < 70) return;

        var rows = (Items?.Cast<MonthlyCostRow>() ?? Enumerable.Empty<MonthlyCostRow>()).ToList();

        const double left = 52;
        const double top = 14;
        const double right = 10;
        const double bottom = 30;

        var plotWidth = width - left - right;
        var plotHeight = height - top - bottom;

        if (rows.Count < 2 || plotWidth < 40 || plotHeight < 30)
        {
            DrawText(dc, "Недостаточно данных для графика", new Point(left, top + plotHeight / 2), plotWidth, 12, MutedBrush);
            return;
        }

        var maximum = NiceMaximum((double)rows.Max(r => Math.Max(r.PlannedCost, r.ActualCost)));

        var gridPen = new Pen(GridBrush, 1);
        gridPen.Freeze();

        const int steps = 3;
        for (var step = 0; step <= steps; step++)
        {
            var y = top + plotHeight - plotHeight * step / steps;
            dc.DrawLine(gridPen, new Point(left, y), new Point(left + plotWidth, y));

            var label = Text(Compact((decimal)(maximum * step / steps)), 10, MutedBrush);
            dc.DrawText(label, new Point(left - label.Width - 8, y - label.Height / 2));
        }

        var plannedPoints = new List<Point>();
        var actualPoints = new List<Point>();

        for (var i = 0; i < rows.Count; i++)
        {
            var x = left + plotWidth * i / (rows.Count - 1);
            plannedPoints.Add(new Point(x, top + plotHeight - plotHeight * Math.Clamp((double)rows[i].PlannedCost / maximum, 0d, 1d)));
            actualPoints.Add(new Point(x, top + plotHeight - plotHeight * Math.Clamp((double)rows[i].ActualCost / maximum, 0d, 1d)));

            var month = Text(rows[i].MonthDisplay, 10, MutedBrush);
            dc.DrawText(month, new Point(x - month.Width / 2, top + plotHeight + 8));
        }

        var plannedPen = new Pen(SuccessBrush, 2) { DashStyle = new DashStyle(new double[] { 4, 3 }, 0) };
        plannedPen.Freeze();

        var actualPen = new Pen(AccentBrush, 2.2);
        actualPen.Freeze();

        DrawPolyline(dc, plannedPoints, plannedPen);
        DrawPolyline(dc, actualPoints, actualPen);

        foreach (var point in plannedPoints)
            dc.DrawEllipse(SuccessBrush, null, point, 2.8, 2.8);

        foreach (var point in actualPoints)
            dc.DrawEllipse(AccentBrush, null, point, 3.4, 3.4);

        // Подпись последнего фактического значения.
        var last = rows[^1];
        var caption = Text(Compact(last.ActualCost), 10, AccentBrush, FontWeights.SemiBold);
        var lastPoint = actualPoints[^1];
        dc.DrawText(caption, new Point(
            Math.Min(lastPoint.X - caption.Width / 2, left + plotWidth - caption.Width),
            Math.Max(top, lastPoint.Y - caption.Height - 6)));
    }

    private static void DrawPolyline(DrawingContext dc, IReadOnlyList<Point> points, Pen pen)
    {
        if (points.Count < 2) return;

        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(points[0], false, false);
            context.PolyLineTo(points.Skip(1).ToList(), true, false);
        }

        geometry.Freeze();
        dc.DrawGeometry(null, pen, geometry);
    }
}
