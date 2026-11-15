using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using StroyControl.Core.Dtos;
using StroyControl.Core.Enums;

namespace StroyControl.App.Views.Reports;

/// <summary>
/// Печатный отчёт по анализу отклонений (A4, FlowDocument): шапка со сводкой,
/// позиции, сгруппированные по разделам сметы, подытоги по разделам, итог и
/// легенда порогов критичности. Один и тот же документ показывается в окне,
/// печатается на принтере и выгружается в PDF.
/// </summary>
public static class DeviationReportDocument
{
    private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");

    private static readonly Brush HeaderFill = Create(0xEE, 0xE8, 0xFD);
    private static readonly Brush SectionFill = Create(0xD9, 0xCC, 0xFA);
    private static readonly Brush OverrunFill = Create(0xFD, 0xEC, 0xEC);
    private static readonly Brush EconomyFill = Create(0xE7, 0xF7, 0xEF);
    private static readonly Brush LineBrush = Create(0x93, 0xA1, 0xB5);
    private static readonly Brush Ink = Create(0x1F, 0x2A, 0x37);
    private static readonly Brush Muted = Create(0x52, 0x60, 0x6D);

    static DeviationReportDocument()
    {
        foreach (var brush in new[] { HeaderFill, SectionFill, OverrunFill, EconomyFill, LineBrush, Ink, Muted })
            brush.Freeze();
    }

    public static FlowDocument Build(DeviationReport report)
    {
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Times New Roman"),
            FontSize = 11,
            Foreground = Brushes.Black,
            Background = Brushes.White,
            PagePadding = new Thickness(30),
            PageWidth = 794,   // A4 при 96 dpi
            PageHeight = 1123,
            ColumnWidth = double.PositiveInfinity
        };

        doc.Blocks.Add(new Paragraph(new Run("АНАЛИЗ ОТКЛОНЕНИЙ ФАКТИЧЕСКОГО РАСХОДА ОТ СМЕТЫ"))
        {
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 2)
        });

        doc.Blocks.Add(new Paragraph(new Run(report.ScopeName))
        {
            FontSize = 11.5,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 1)
        });

        doc.Blocks.Add(new Paragraph(new Run(
                $"Сформировано {DateTime.Now:dd.MM.yyyy HH:mm} · позиций в отчёте: {report.Rows.Count}"))
        {
            FontSize = 10,
            TextAlignment = TextAlignment.Center,
            Foreground = Muted,
            Margin = new Thickness(0, 0, 0, 10)
        });

        doc.Blocks.Add(BuildSummary(report));
        doc.Blocks.Add(BuildTable(report));

        doc.Blocks.Add(new Paragraph(new Run(
                "Порядок расчёта: отклонение = факт − план; процент = отклонение ÷ план × 100 %; " +
                "фактическая цена = стоимость списаний ÷ израсходованное количество. " +
                "Порог внимания — 5 %, критично — 10 % (по модулю процента)."))
        {
            FontSize = 9.5,
            Foreground = Muted,
            Margin = new Thickness(0, 10, 0, 0)
        });

        return doc;
    }

    // ------------------------------------------------------------------

    private static Table BuildSummary(DeviationReport report)
    {
        var table = CreateTable(new[] { 176d, 130d, 176d, 130d });

        AddRow(table,
            Cell("Сметная стоимость, BYN", bold: true), Cell(Money(report.PlannedTotal), TextAlignment.Right, bold: true),
            Cell("Фактические затраты, BYN", bold: true), Cell(Money(report.ActualTotal), TextAlignment.Right, bold: true));

        AddRow(table,
            Cell("Отклонение, BYN"), Cell(Signed(report.CostDeviation), TextAlignment.Right),
            Cell("Отклонение, %"), Cell(SignedPercent(report.DeviationPercent), TextAlignment.Right));

        AddRow(table,
            Cell("Сводная оценка"), Cell(report.Severity.ToDisplayName()),
            Cell("Позиций с перерасходом"), Cell(report.OverrunCount.ToString(Russian), TextAlignment.Right));

        AddRow(table,
            Cell("Внимание (5–10 %)"), Cell(report.WarningCount.ToString(Russian), TextAlignment.Right),
            Cell("Критично (≥ 10 %)"), Cell(report.CriticalCount.ToString(Russian), TextAlignment.Right));

        AddRow(table,
            Cell("Экономия"), Cell(report.EconomyCount.ToString(Russian), TextAlignment.Right),
            Cell("Позиций всего"), Cell(report.Rows.Count.ToString(Russian), TextAlignment.Right));

        return table;
    }

    private static Table BuildTable(DeviationReport report)
    {
        var table = CreateTable(new[] { 22d, 150d, 34d, 52d, 52d, 48d, 48d, 62d, 62d, 58d, 40d, 46d });

        AddRow(table,
            Cell("№", TextAlignment.Center, header: true),
            Cell("Материал", TextAlignment.Left, header: true),
            Cell("ед.", TextAlignment.Center, header: true),
            Cell("кол-во план", TextAlignment.Center, header: true),
            Cell("кол-во факт", TextAlignment.Center, header: true),
            Cell("цена план", TextAlignment.Center, header: true),
            Cell("цена факт", TextAlignment.Center, header: true),
            Cell("стоимость план", TextAlignment.Center, header: true),
            Cell("стоимость факт", TextAlignment.Center, header: true),
            Cell("отклонение", TextAlignment.Center, header: true),
            Cell("откл., %", TextAlignment.Center, header: true),
            Cell("оценка", TextAlignment.Center, header: true));

        var number = 1;
        var sectionNumber = 1;

        foreach (var section in GroupBySection(report))
        {
            AddRow(table, Cell($"{sectionNumber++}. {section.Name}", bold: true, fill: SectionFill, colSpan: 12));

            foreach (var row in section.Rows)
            {
                AddRow(table,
                    Cell(number++.ToString(Russian), TextAlignment.Center),
                    Cell(row.MaterialName),
                    Cell(row.Unit, TextAlignment.Center),
                    Cell(Number(row.PlannedQuantity), TextAlignment.Right),
                    Cell(Number(row.ActualQuantity), TextAlignment.Right),
                    Cell(Amount(row.PlannedPrice), TextAlignment.Right),
                    Cell(Amount(row.ActualPrice), TextAlignment.Right),
                    Cell(Money(row.PlannedCost), TextAlignment.Right),
                    Cell(Money(row.ActualCost), TextAlignment.Right),
                    Cell(Signed(row.CostDeviation), TextAlignment.Right,
                        fill: row.CostDeviation == 0m ? null : row.IsOverrun ? OverrunFill : EconomyFill),
                    Cell(SignedPercent(row.DeviationPercent), TextAlignment.Right),
                    Cell(row.SeverityDisplay, TextAlignment.Center));
            }

            AddRow(table,
                Cell($"Итого по разделу «{section.Name}»", bold: true, fill: HeaderFill, colSpan: 7),
                Cell(Money(section.Planned), TextAlignment.Right, bold: true, fill: HeaderFill),
                Cell(Money(section.Actual), TextAlignment.Right, bold: true, fill: HeaderFill),
                Cell(Signed(section.Deviation), TextAlignment.Right, bold: true, fill: HeaderFill),
                Cell(SignedPercent(section.Percent), TextAlignment.Right, bold: true, fill: HeaderFill),
                Cell(string.Empty, fill: HeaderFill));
        }

        AddRow(table,
            Cell("ИТОГО ПО ОТЧЁТУ", bold: true, fill: SectionFill, colSpan: 7),
            Cell(Money(report.PlannedTotal), TextAlignment.Right, bold: true, fill: SectionFill),
            Cell(Money(report.ActualTotal), TextAlignment.Right, bold: true, fill: SectionFill),
            Cell(Signed(report.CostDeviation), TextAlignment.Right, bold: true, fill: SectionFill),
            Cell(SignedPercent(report.DeviationPercent), TextAlignment.Right, bold: true, fill: SectionFill),
            Cell(report.Severity.ToDisplayName(), TextAlignment.Center, bold: true, fill: SectionFill));

        return table;
    }

    private sealed record SectionBlock(
        string Name,
        IReadOnlyList<DeviationRow> Rows,
        decimal Planned,
        decimal Actual)
    {
        public decimal Deviation => Actual - Planned;

        public decimal Percent => Planned == 0m ? 0m : Math.Round(Deviation / Planned * 100m, 1);
    }

    private static IReadOnlyList<SectionBlock> GroupBySection(DeviationReport report)
    {
        return report.Rows
            .GroupBy(r => string.IsNullOrWhiteSpace(r.Section) ? "Без раздела" : r.Section)
            .OrderBy(g => g.Key, StringComparer.CurrentCulture)
            .Select(g => new SectionBlock(
                g.Key,
                g.OrderBy(r => r.MaterialName, StringComparer.CurrentCulture).ToList(),
                g.Sum(r => r.PlannedCost),
                g.Sum(r => r.ActualCost)))
            .ToList();
    }

    // ------------------------------------------------------------------

    private static Table CreateTable(IReadOnlyList<double> widths)
    {
        var table = new Table
        {
            CellSpacing = 0,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(0.6),
            Margin = new Thickness(0, 0, 0, 8)
        };

        foreach (var width in widths)
            table.Columns.Add(new TableColumn { Width = new GridLength(width) });

        table.RowGroups.Add(new TableRowGroup());
        return table;
    }

    private static void AddRow(Table table, params TableCell[] cells)
    {
        var row = new TableRow();
        foreach (var cell in cells)
            row.Cells.Add(cell);

        table.RowGroups[0].Rows.Add(row);
    }

    private static TableCell Cell(
        string text,
        TextAlignment alignment = TextAlignment.Left,
        bool bold = false,
        bool header = false,
        Brush? fill = null,
        int colSpan = 1)
    {
        var paragraph = new Paragraph(new Run(text))
        {
            TextAlignment = alignment,
            Margin = new Thickness(1),
            FontSize = header ? 8.5 : 9.5,
            FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
            Foreground = Ink,
            LineHeight = 11.5
        };

        return new TableCell(paragraph)
        {
            Background = fill ?? Brushes.Transparent,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(0.5),
            Padding = new Thickness(3, 2, 3, 2),
            ColumnSpan = colSpan
        };
    }

    private static string Money(decimal value) => value.ToString("N2", Russian);

    private static string Amount(decimal value) => value.ToString("N2", Russian);

    private static string Number(decimal value) => value.ToString("N3", Russian);

    private static string Signed(decimal value) =>
        (value > 0m ? "+" : value < 0m ? "−" : string.Empty) +
        Math.Abs(value).ToString("N2", Russian);

    private static string SignedPercent(decimal value) =>
        (value > 0m ? "+" : value < 0m ? "−" : string.Empty) +
        Math.Abs(value).ToString("0.0", Russian) + " %";

    private static Brush Create(byte r, byte g, byte b) => new SolidColorBrush(Color.FromRgb(r, g, b));
}
