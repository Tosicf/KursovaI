using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using StroyControl.Core.Entities;

namespace StroyControl.App.Views.Reports;

/// <summary>
/// Печатный «Акт выполненных работ» (FlowDocument, A4): шапка с номером и датой,
/// объект и основание (смета), таблица фактически выполненных работ, таблица
/// израсходованных материалов, итоги по разделам, подписи и отметка о формировании.
/// Документ показывает и то, что учтено в факте, и то, что списано со склада.
/// </summary>
public static class WorkCompletionDocument
{
    private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");

    private static readonly Brush HeaderFill = Create(0xF2, 0xEC, 0xE6);
    private static readonly Brush TotalFill = Create(0xE8, 0xEE, 0xE4);
    private static readonly Brush LineBrush = Create(0x93, 0xA1, 0xB5);
    private static readonly Brush Ink = Create(0x1F, 0x2A, 0x37);
    private static readonly Brush Muted = Create(0x52, 0x60, 0x6D);

    public static FlowDocument Build(WorkCompletion completion, string organization)
    {
        HeaderFill.Freeze();
        TotalFill.Freeze();
        LineBrush.Freeze();
        Ink.Freeze();
        Muted.Freeze();

        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Times New Roman"),
            FontSize = 11,
            Foreground = Brushes.Black,
            Background = Brushes.White,
            PagePadding = new Thickness(36),
            PageWidth = 794,   // A4 при 96 dpi
            PageHeight = 1123,
            ColumnWidth = double.PositiveInfinity
        };

        doc.Blocks.Add(new Paragraph(new Run("АКТ ВЫПОЛНЕННЫХ РАБОТ"))
        {
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 2)
        });

        var number = string.IsNullOrWhiteSpace(completion.DocumentNumber)
            ? "б/н"
            : completion.DocumentNumber.Trim();

        doc.Blocks.Add(new Paragraph(new Run($"№ {number} от {completion.Date:dd.MM.yyyy}"))
        {
            FontSize = 11.5,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10)
        });

        doc.Blocks.Add(BuildParty("Организация",
            string.IsNullOrWhiteSpace(organization) ? "СтройКонтроль" : organization.Trim()));
        doc.Blocks.Add(BuildParty("Объект", BuildProject(completion)));

        if (completion.Estimate is { } estimate)
        {
            doc.Blocks.Add(BuildParty("Основание",
                $"смета № {estimate.Number} от {estimate.Date:dd.MM.yyyy} — {estimate.Name}"));
        }

        doc.Blocks.Add(BuildParty("Ответственный", completion.AuthorName));

        doc.Blocks.Add(BuildSectionTitle("1. Выполненные работы"));
        doc.Blocks.Add(BuildWorksTable(completion));

        doc.Blocks.Add(BuildSectionTitle("2. Израсходованные материалы (списаны со склада)"));
        doc.Blocks.Add(BuildMaterialsTable(completion));

        doc.Blocks.Add(BuildTotals(completion));

        if (!string.IsNullOrWhiteSpace(completion.Note))
        {
            doc.Blocks.Add(new Paragraph(new Run($"Примечание: {completion.Note.Trim()}"))
            {
                FontSize = 10,
                Foreground = Muted,
                Margin = new Thickness(0, 0, 0, 8)
            });
        }

        doc.Blocks.Add(new Paragraph(new Run(
                "Работы приняты по фактическому объёму, материалы отпущены со склада и израсходованы на объекте. " +
                "Остатки склада уменьшены на указанное количество, фактические затраты учтены в смете."))
        {
            FontSize = 9.5,
            Foreground = Muted,
            Margin = new Thickness(0, 0, 0, 10)
        });

        doc.Blocks.Add(BuildSignatures());
        doc.Blocks.Add(new Paragraph(new Run(
                $"Акт сформирован {DateTime.Now:dd.MM.yyyy HH:mm} в системе «СтройКонтроль»."))
        {
            FontSize = 9,
            Foreground = Muted,
            Margin = new Thickness(0, 10, 0, 0)
        });

        return doc;
    }

    // ------------------------------------------------------------------

    private static string BuildProject(WorkCompletion completion)
    {
        var project = completion.Project;
        if (project is null) return completion.ProjectName;

        var parts = new List<string> { $"«{project.Name}»" };
        if (!string.IsNullOrWhiteSpace(project.Code))
            parts.Insert(0, project.Code.Trim());
        if (!string.IsNullOrWhiteSpace(project.Address))
            parts.Add(project.Address.Trim());

        return string.Join(", ", parts);
    }

    private static Paragraph BuildParty(string label, string value)
    {
        var paragraph = new Paragraph { Margin = new Thickness(0, 0, 0, 3), FontSize = 10.5 };
        paragraph.Inlines.Add(new Run($"{label}: ") { FontWeight = FontWeights.Bold, Foreground = Ink });
        paragraph.Inlines.Add(new Run(value) { Foreground = Ink });
        return paragraph;
    }

    private static Paragraph BuildSectionTitle(string text) =>
        new(new Run(text))
        {
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = Ink,
            Margin = new Thickness(0, 10, 0, 2)
        };

    private static Table BuildWorksTable(WorkCompletion completion)
    {
        var table = CreateTable(new[] { 26d, 250d, 52d, 70d, 84d, 100d });

        AddRow(table,
            Cell("№", TextAlignment.Center, header: true),
            Cell("Наименование работы", TextAlignment.Left, header: true),
            Cell("ед.", TextAlignment.Center, header: true),
            Cell("кол-во", TextAlignment.Center, header: true),
            Cell("цена, BYN", TextAlignment.Center, header: true),
            Cell("сумма, BYN", TextAlignment.Center, header: true));

        var index = 1;
        foreach (var item in completion.Items.OrderBy(i => i.SectionDisplay).ThenBy(i => i.WorkName))
        {
            AddRow(table,
                Cell(index.ToString(Russian), TextAlignment.Center),
                Cell(item.WorkName),
                Cell(item.UnitDisplay, TextAlignment.Center),
                Cell(Number(item.Quantity), TextAlignment.Right),
                Cell(Money(item.UnitPrice), TextAlignment.Right),
                Cell(Money(item.Cost), TextAlignment.Right));

            index++;
        }

        if (index == 1)
        {
            AddRow(table,
                Cell("—", TextAlignment.Center),
                Cell("Работы в акте не указаны"),
                Cell(string.Empty),
                Cell(string.Empty),
                Cell(string.Empty),
                Cell(string.Empty));
        }

        AddRow(table,
            Cell("ИТОГО по работам", bold: true, fill: HeaderFill, colSpan: 5),
            Cell(Money(completion.WorksCost), TextAlignment.Right, bold: true, fill: HeaderFill));

        return table;
    }

    private static Table BuildMaterialsTable(WorkCompletion completion)
    {
        var table = CreateTable(new[] { 26d, 250d, 52d, 70d, 84d, 100d });

        AddRow(table,
            Cell("№", TextAlignment.Center, header: true),
            Cell("Наименование материала", TextAlignment.Left, header: true),
            Cell("ед.", TextAlignment.Center, header: true),
            Cell("кол-во", TextAlignment.Center, header: true),
            Cell("цена, BYN", TextAlignment.Center, header: true),
            Cell("сумма, BYN", TextAlignment.Center, header: true));

        var index = 1;
        foreach (var usage in completion.MaterialUsages.OrderBy(u => u.MaterialName))
        {
            AddRow(table,
                Cell(index.ToString(Russian), TextAlignment.Center),
                Cell(usage.MaterialName),
                Cell(usage.UnitDisplay, TextAlignment.Center),
                Cell(Number(usage.Quantity), TextAlignment.Right),
                Cell(Money(usage.UnitPrice), TextAlignment.Right),
                Cell(Money(usage.Cost), TextAlignment.Right));

            index++;
        }

        if (index == 1)
        {
            AddRow(table,
                Cell("—", TextAlignment.Center),
                Cell("Материалы в акте не списывались"),
                Cell(string.Empty),
                Cell(string.Empty),
                Cell(string.Empty),
                Cell(string.Empty));
        }

        AddRow(table,
            Cell("ИТОГО по материалам", bold: true, fill: HeaderFill, colSpan: 5),
            Cell(Money(completion.MaterialsCost), TextAlignment.Right, bold: true, fill: HeaderFill));

        return table;
    }

    private static Table BuildTotals(WorkCompletion completion)
    {
        var table = CreateTable(new[] { 330d, 252d });

        AddRow(table,
            Cell("Работы", bold: true, fill: TotalFill),
            Cell(Money(completion.WorksCost) + " BYN", TextAlignment.Right, fill: TotalFill));
        AddRow(table,
            Cell("Материалы", bold: true, fill: TotalFill),
            Cell(Money(completion.MaterialsCost) + " BYN", TextAlignment.Right, fill: TotalFill));
        AddRow(table,
            Cell("ВСЕГО ПО АКТУ", bold: true, fill: TotalFill),
            Cell(Money(completion.Total) + " BYN", TextAlignment.Right, bold: true, fill: TotalFill));

        return table;
    }

    private static Table BuildSignatures()
    {
        var table = CreateTable(new[] { 300d, 300d }, borders: false);

        AddRow(table,
            Cell("Сдал (прораб) ______________", header: true),
            Cell("Принял (инженер ПТО) ______________", header: true));

        return table;
    }

    // ------------------------------------------------------------------

    private static Table CreateTable(IReadOnlyList<double> widths, bool borders = true)
    {
        var table = new Table
        {
            CellSpacing = 0,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(borders ? 0.6 : 0),
            Margin = new Thickness(0, 6, 0, 8)
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
            FontSize = header ? 9.5 : 10.5,
            FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
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

    private static string Number(decimal value) =>
        value == decimal.Truncate(value) ? value.ToString("N0", Russian) : value.ToString("N3", Russian);

    private static Brush Create(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}
