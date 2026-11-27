using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using StroyControl.Core.Entities;

namespace StroyControl.App.Views.Reports;

/// <summary>
/// Собирает печатный «Акт списания материалов» (FlowDocument, A5): шапка с номером
/// и датой акта, объект и основание (смета), таблица списанных материалов, итог,
/// подписи материально ответственных лиц и отметка о формировании в системе.
/// Тот же документ показывается в предпросмотре и уходит на печать.
/// </summary>
public static class UsageActDocument
{
    private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");

    private static readonly Brush HeaderFill = Create(0xF2, 0xEC, 0xE6);
    private static readonly Brush LineBrush = Create(0x93, 0xA1, 0xB5);
    private static readonly Brush Ink = Create(0x1F, 0x2A, 0x37);
    private static readonly Brush Muted = Create(0x52, 0x60, 0x6D);

    public static FlowDocument Build(MaterialUsage usage, string organization)
    {
        HeaderFill.Freeze();
        LineBrush.Freeze();
        Ink.Freeze();
        Muted.Freeze();

        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Times New Roman"),
            FontSize = 11,
            Foreground = Brushes.Black,
            Background = Brushes.White,
            PagePadding = new Thickness(28),
            PageWidth = 559,   // A5 при 96 dpi
            PageHeight = 794,
            ColumnWidth = double.PositiveInfinity
        };

        doc.Blocks.Add(new Paragraph(new Run("АКТ СПИСАНИЯ МАТЕРИАЛОВ"))
        {
            FontSize = 15,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 2)
        });

        var number = string.IsNullOrWhiteSpace(usage.DocumentNumber)
            ? "б/н"
            : usage.DocumentNumber.Trim();

        doc.Blocks.Add(new Paragraph(new Run($"№ {number} от {usage.Date:dd.MM.yyyy}"))
        {
            FontSize = 11.5,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10)
        });

        doc.Blocks.Add(BuildParty("Организация",
            string.IsNullOrWhiteSpace(organization) ? "СтройКонтроль" : organization.Trim()));
        doc.Blocks.Add(BuildParty("Объект", BuildProject(usage)));

        if (usage.Estimate is { } estimate)
        {
            doc.Blocks.Add(BuildParty("Основание",
                $"смета № {estimate.Number} от {estimate.Date:dd.MM.yyyy} — {estimate.Name}"));
        }
        else
        {
            doc.Blocks.Add(BuildParty("Основание", "производственная необходимость (без привязки к смете)"));
        }

        doc.Blocks.Add(BuildParty("Материально ответственное лицо", usage.AuthorName));

        doc.Blocks.Add(BuildTable(usage));

        doc.Blocks.Add(new Paragraph(new Run(
                $"Списано на сумму {Money(usage.Cost)} BYN. Стоимость определена по учётной цене на дату списания."))
        {
            FontSize = 10.5,
            Margin = new Thickness(0, 0, 0, 10)
        });

        if (!string.IsNullOrWhiteSpace(usage.Note))
        {
            doc.Blocks.Add(new Paragraph(new Run($"Примечание: {usage.Note.Trim()}"))
            {
                FontSize = 10,
                Foreground = Muted,
                Margin = new Thickness(0, 0, 0, 8)
            });
        }

        doc.Blocks.Add(new Paragraph(new Run(
                "Материал отпущен со склада и израсходован на объекте. Остатки склада уменьшены на указанное количество."))
        {
            FontSize = 9.5,
            Foreground = Muted,
            Margin = new Thickness(0, 0, 0, 10)
        });

        doc.Blocks.Add(BuildSignatures());
        doc.Blocks.Add(new Paragraph(new Run(
                $"Акт сформирован {usage.Date:dd.MM.yyyy} в системе «СтройКонтроль»."))
        {
            FontSize = 9,
            Foreground = Muted,
            Margin = new Thickness(0, 10, 0, 0)
        });

        return doc;
    }

    // ------------------------------------------------------------------

    private static string BuildProject(MaterialUsage usage)
    {
        var project = usage.Project;
        if (project is null) return usage.ProjectName;

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

    private static Table BuildTable(MaterialUsage usage)
    {
        var table = CreateTable(new[] { 24d, 190d, 48d, 62d, 78d, 84d });

        AddRow(table,
            Cell("№", TextAlignment.Center, header: true),
            Cell("Наименование материала", TextAlignment.Left, header: true),
            Cell("ед.", TextAlignment.Center, header: true),
            Cell("кол-во", TextAlignment.Center, header: true),
            Cell("цена, BYN", TextAlignment.Center, header: true),
            Cell("сумма, BYN", TextAlignment.Center, header: true));

        AddRow(table,
            Cell("1", TextAlignment.Center),
            Cell(usage.MaterialName),
            Cell(usage.UnitDisplay, TextAlignment.Center),
            Cell(Number(usage.Quantity), TextAlignment.Right),
            Cell(Money(usage.UnitPrice), TextAlignment.Right),
            Cell(Money(usage.Cost), TextAlignment.Right));

        AddRow(table,
            Cell("ИТОГО", bold: true, fill: HeaderFill, colSpan: 5),
            Cell(Money(usage.Cost), TextAlignment.Right, bold: true, fill: HeaderFill));

        return table;
    }

    private static Table BuildSignatures()
    {
        var table = CreateTable(new[] { 234d, 235d }, borders: false);

        AddRow(table,
            Cell("Списал (прораб) ______________", header: true),
            Cell("Проверил (инженер ПТО) ______________", header: true));

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
