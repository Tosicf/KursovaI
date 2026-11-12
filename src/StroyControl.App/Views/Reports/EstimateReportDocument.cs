using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using StroyControl.Core.Dtos;

namespace StroyControl.App.Views.Reports;

/// <summary>
/// Собирает печатный документ отчёта по смете (FlowDocument): шапка с итогами,
/// таблица с разделами, работами и материалами, подытоги и итог по смете.
/// Один и тот же документ показывается на экране и печатается — без расхождений.
/// </summary>
public static class EstimateReportDocument
{
    private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");

    private static readonly Brush HeaderFill = new SolidColorBrush(Color.FromRgb(0xEE, 0xF2, 0xF8));
    private static readonly Brush SectionFill = new SolidColorBrush(Color.FromRgb(0xCF, 0xE0, 0xF5));
    private static readonly Brush GroupFill = new SolidColorBrush(Color.FromRgb(0xE8, 0xEE, 0xF7));
    private static readonly Brush LineBrush = new SolidColorBrush(Color.FromRgb(0x93, 0xA1, 0xB5));
    private static readonly Brush Ink = new SolidColorBrush(Color.FromRgb(0x1F, 0x2A, 0x37));

    public static FlowDocument Build(EstimateReport report)
    {
        HeaderFill.Freeze();
        SectionFill.Freeze();
        GroupFill.Freeze();
        LineBrush.Freeze();
        Ink.Freeze();

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

        doc.Blocks.Add(new Paragraph(new Run("РАСЧЁТ КОЛИЧЕСТВА И СТОИМОСТИ РАБОТ И МАТЕРИАЛОВ"))
        {
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 2)
        });

        doc.Blocks.Add(new Paragraph(new Run($"Смета {report.Number} · {report.Name}"))
        {
            FontSize = 11.5,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 1)
        });

        doc.Blocks.Add(new Paragraph(new Run($"{report.ProjectName}{(string.IsNullOrWhiteSpace(report.ProjectAddress) ? string.Empty : ", " + report.ProjectAddress)}"))
        {
            FontSize = 10.5,
            TextAlignment = TextAlignment.Center,
            Foreground = Brushes.DimGray,
            Margin = new Thickness(0, 0, 0, 1)
        });

        doc.Blocks.Add(new Paragraph(new Run($"Сформировано {report.GeneratedAt:dd.MM.yyyy HH:mm} · {report.StatusDisplay}"))
        {
            FontSize = 10,
            TextAlignment = TextAlignment.Center,
            Foreground = Brushes.DimGray,
            Margin = new Thickness(0, 0, 0, 10)
        });

        doc.Blocks.Add(BuildSummary(report));
        doc.Blocks.Add(BuildTable(report));

        doc.Blocks.Add(new Paragraph(new Run(
                $"Расчёт: наценка {report.MarkupPercent:N2} %, прибыль {report.ProfitPercent:N2} %, " +
                $"доля зарплаты рабочих {report.LaborSharePercent:N2} %. " +
                (report.Area > 0m
                    ? $"Площадь строения {report.Area:N2} м², стоимость 1 м² — {report.CostPerSquareMeter:N2} BYN, прибыль от 1 м² — {report.ProfitPerSquareMeter:N2} BYN."
                    : "Площадь строения не задана — удельные показатели не рассчитываются.")))
        {
            FontSize = 9.5,
            Foreground = Brushes.DimGray,
            Margin = new Thickness(0, 10, 0, 0)
        });

        return doc;
    }

    private static Table BuildSummary(EstimateReport report)
    {
        var table = CreateTable(new[] { 150d, 110d, 150d, 110d });

        AddRow(table,
            Cell("Всего затрат, BYN", bold: true), Cell(Money(report.TotalCost), TextAlignment.Right, bold: true),
            Cell("S строения общая, м²", bold: true), Cell(Number(report.Area), TextAlignment.Right, bold: true));

        AddRow(table,
            Cell("Стоимость материалов, BYN"), Cell(Money(report.MaterialsCost), TextAlignment.Right),
            Cell("Стоимость 1 м², BYN"), Cell(Money(report.CostPerSquareMeter), TextAlignment.Right));

        AddRow(table,
            Cell("Стоимость работ, BYN"), Cell(Money(report.WorksCost), TextAlignment.Right),
            Cell("Прибыль от 1 м², BYN"), Cell(Money(report.ProfitPerSquareMeter), TextAlignment.Right));

        AddRow(table,
            Cell("Зарплата рабочих, BYN"), Cell(Money(report.LaborCost), TextAlignment.Right),
            Cell("Наценка, %"), Cell(Number(report.MarkupPercent), TextAlignment.Right));

        AddRow(table,
            Cell("Прибыль, BYN"), Cell(Money(report.Profit), TextAlignment.Right),
            Cell("Прибыль, %"), Cell(Number(report.ProfitPercent), TextAlignment.Right));

        AddRow(table,
            Cell($"Позиций в смете: {report.PositionsCount}"),
            Cell(string.Empty),
            Cell("Доля ЗП рабочих, %"), Cell(Number(report.LaborSharePercent), TextAlignment.Right));

        return table;
    }

    private static Table BuildTable(EstimateReport report)
    {
        var table = CreateTable(new[] { 26d, 214d, 44d, 48d, 70d, 80d, 80d, 70d, 70d });

        AddRow(table,
            Cell("№", TextAlignment.Center, bold: true, header: true),
            Cell("Наименование работ и затрат", TextAlignment.Left, bold: true, header: true),
            Cell("ед. изм.", TextAlignment.Center, bold: true, header: true),
            Cell("кол-во", TextAlignment.Center, bold: true, header: true),
            Cell("Цена ед. изм.", TextAlignment.Center, bold: true, header: true),
            Cell("Цена с наценкой", TextAlignment.Center, bold: true, header: true),
            Cell("Итого", TextAlignment.Center, bold: true, header: true),
            Cell("Прибыль", TextAlignment.Center, bold: true, header: true),
            Cell("ЗП раб.", TextAlignment.Center, bold: true, header: true));

        var sectionNumber = 1;

        foreach (var group in report.Groups)
        {
            AddRow(table,
                Cell($"{sectionNumber++}. {group.Section}", bold: true, fill: SectionFill, colSpan: 6),
                Cell(Money(group.Total), TextAlignment.Right, bold: true, fill: SectionFill),
                Cell(Money(group.Profit), TextAlignment.Right, bold: true, fill: SectionFill),
                Cell(Money(group.LaborCost), TextAlignment.Right, bold: true, fill: SectionFill));

            if (group.HasWorks)
            {
                AddRow(table, Cell("Работы", italic: true, bold: true, fill: GroupFill, colSpan: 9));

                foreach (var row in group.Works)
                    AppendItemRow(table, row);

                AddRow(table,
                    Cell("Итого работы", bold: true, fill: HeaderFill, colSpan: 6),
                    Cell(Money(group.WorksTotal), TextAlignment.Right, bold: true, fill: HeaderFill),
                    Cell(Money(group.Works.Sum(r => r.Profit)), TextAlignment.Right, bold: true, fill: HeaderFill),
                    Cell(Money(group.LaborCost), TextAlignment.Right, bold: true, fill: HeaderFill));
            }

            if (group.HasMaterials)
            {
                AddRow(table, Cell("Материалы", italic: true, bold: true, fill: GroupFill, colSpan: 9));

                foreach (var row in group.Materials)
                    AppendItemRow(table, row);

                AddRow(table,
                    Cell("Итого материалы", bold: true, fill: HeaderFill, colSpan: 6),
                    Cell(Money(group.MaterialsTotal), TextAlignment.Right, bold: true, fill: HeaderFill),
                    Cell(Money(group.Materials.Sum(r => r.Profit)), TextAlignment.Right, bold: true, fill: HeaderFill),
                    Cell(Money(0m), TextAlignment.Right, bold: true, fill: HeaderFill));
            }

            AddRow(table,
                Cell($"Итого по разделу «{group.Section}»", bold: true, colSpan: 6),
                Cell(Money(group.Total), TextAlignment.Right, bold: true),
                Cell(Money(group.Profit), TextAlignment.Right, bold: true),
                Cell(Money(group.LaborCost), TextAlignment.Right, bold: true));
        }

        AddRow(table,
            Cell("ИТОГО ПО СМЕТЕ", bold: true, fill: SectionFill, colSpan: 6),
            Cell(Money(report.DirectCost), TextAlignment.Right, bold: true, fill: SectionFill),
            Cell(Money(report.Profit), TextAlignment.Right, bold: true, fill: SectionFill),
            Cell(Money(report.LaborCost), TextAlignment.Right, bold: true, fill: SectionFill));

        return table;
    }

    private static void AppendItemRow(Table table, EstimateReportRow row)
    {
        AddRow(table,
            Cell(row.Number.ToString(Russian), TextAlignment.Center),
            Cell(row.Name),
            Cell(row.Unit, TextAlignment.Center),
            Cell(Number(row.Quantity), TextAlignment.Right),
            Cell(Money(row.UnitPrice), TextAlignment.Right),
            Cell(Money(row.PriceWithMarkup), TextAlignment.Right),
            Cell(Money(row.Total), TextAlignment.Right),
            Cell(Money(row.Profit), TextAlignment.Right),
            Cell(Money(row.LaborCost), TextAlignment.Right));
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
        bool italic = false,
        bool header = false,
        Brush? fill = null,
        int colSpan = 1)
    {
        var paragraph = new Paragraph(new Run(text))
        {
            TextAlignment = alignment,
            Margin = new Thickness(1),
            FontSize = header ? 9.5 : 10.5,
            FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
            FontStyle = italic ? FontStyles.Italic : FontStyles.Normal,
            Foreground = Ink,
            LineHeight = 12
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

    private static string Number(decimal value)
    {
        var text = value.ToString("N2", Russian);
        return text;
    }
}
